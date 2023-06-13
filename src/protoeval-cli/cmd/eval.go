package cmd

import (
	"context"
	"encoding/base64"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"strings"

	"github.com/spf13/cobra"
	"google.golang.org/protobuf/encoding/protojson"
	"google.golang.org/protobuf/proto"
	"google.golang.org/protobuf/reflect/protodesc"
	"google.golang.org/protobuf/reflect/protoreflect"
	"google.golang.org/protobuf/types/descriptorpb"
	"google.golang.org/protobuf/types/dynamicpb"
)

var (
	payloadBase64      string
	activeSchemaBase64 string
	schemaName         string
)

func init() {
	cmd := cobra.Command{
		Use:     "eval --payload=payload --schema=schema --schema-name=schema-name",
		Aliases: []string{"e"},
		Short:   "Evaluates proto-buf payload against proto-buf schema",
		Args:    cobra.MaximumNArgs(0),
		Run: func(cmd *cobra.Command, _ []string) {
			handleProtoBufEval(cmd.Context())
		},
	}
	cmd.Flags().StringVar(&payloadBase64, "payload", "", "base64 proto-buf payload")
	cmd.Flags().StringVar(&activeSchemaBase64, "schema", "", "base64 proto-buf active schema")
	cmd.Flags().StringVar(&schemaName, "schema-name", "", "active schema name")
	cmd.MarkFlagRequired("payload")
	cmd.MarkFlagRequired("schema")
	cmd.MarkFlagRequired("schema-name")
	rootCmd.AddCommand(&cmd)
}

type any interface{}

type SchemaVersion struct {
	VersionNumber     int    `json:"version_number"`
	Descriptor        string `json:"descriptor"`
	Content           string `json:"schema_content"`
	MessageStructName string `json:"message_struct_name"`
}

func handleProtoBufEval(context context.Context) {
	payloadBytes, err := base64.StdEncoding.DecodeString(payloadBase64)
	if err != nil {
		return
	}
	schemaVersion, err := unmarshalSchemaVersion(activeSchemaBase64)
	if err != nil {
		return
	}
	descriptor, err := compileDescriptor(schemaVersion, schemaName)
	if err != nil {
		return
	}
	_, err = validateProtoBufMessage(payloadBytes, descriptor)
	if err != nil {
		log.SetFlags(0)
		log.Fatal(err)
	}
}

func unmarshalSchemaVersion(data string) (SchemaVersion, error) {
	var schemaVersion SchemaVersion
	err := json.Unmarshal([]byte(data), &schemaVersion)
	if err != nil {
		return schemaVersion, memphisError(err)
	}
	return schemaVersion, nil
}

func compileDescriptor(activeVersion SchemaVersion, schemaName string) (protoreflect.MessageDescriptor, error) {
	descriptorSet := descriptorpb.FileDescriptorSet{}
	fmt.Println(activeVersion.Descriptor)
	fmt.Println([]byte(activeVersion.Descriptor))
	err := proto.Unmarshal([]byte(activeVersion.Descriptor), &descriptorSet)
	if err != nil {
		return nil, memphisError(err)
	}

	localRegistry, err := protodesc.NewFiles(&descriptorSet)
	if err != nil {
		return nil, memphisError(err)
	}

	filePath := fmt.Sprintf("%v_%v.proto", schemaName, activeVersion.VersionNumber)
	fileDesc, err := localRegistry.FindFileByPath(filePath)
	if err != nil {
		return nil, memphisError(err)
	}

	msgsDesc := fileDesc.Messages()
	msgDesc := msgsDesc.ByName(protoreflect.Name(activeVersion.MessageStructName))

	return msgDesc, nil
}

func validateProtoBufMessage(msg any, msgDescriptor protoreflect.MessageDescriptor) ([]byte, error) {
	var (
		msgBytes []byte
		err      error
	)
	switch msg.(type) {
	case protoreflect.ProtoMessage:
		msgBytes, err = proto.Marshal(msg.(protoreflect.ProtoMessage))
		if err != nil {
			return nil, memphisError(err)
		}
	case []byte:
		msgBytes = msg.([]byte)
	case map[string]interface{}:
		bytes, err := json.Marshal(msg)
		if err != nil {
			return nil, err
		}
		pMsg := dynamicpb.NewMessage(msgDescriptor)
		err = protojson.Unmarshal(bytes, pMsg)
		if err != nil {
			return nil, memphisError(err)
		}
		msgBytes, err = proto.Marshal(pMsg)
		if err != nil {
			return nil, memphisError(err)
		}
	default:
		return nil, memphisError(errors.New("unsupported message type"))
	}

	protoMsg := dynamicpb.NewMessage(msgDescriptor)
	err = proto.Unmarshal(msgBytes, protoMsg)
	if err != nil {
		if strings.Contains(err.Error(), "cannot parse invalid wire-format data") {
			err = errors.New("invalid message format, expecting protobuf")
		}
		return msgBytes, memphisError(err)
	}

	return msgBytes, nil
}
