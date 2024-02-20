/*
Copyright © 2023 NAME HERE <EMAIL ADDRESS>
*/
package cmd

import (
	"encoding/base64"
	"errors"
	"os"
	"strings"

	"github.com/spf13/cobra"
	"google.golang.org/protobuf/encoding/protojson"
	"google.golang.org/protobuf/proto"
	"google.golang.org/protobuf/reflect/protodesc"
	"google.golang.org/protobuf/reflect/protoreflect"
	"google.golang.org/protobuf/types/descriptorpb"
	"google.golang.org/protobuf/types/dynamicpb"
)

const serializationCmdArgs = "--payload=payload --desc=desc --mname=mname --fname=fname"

var (
	// base64 encoded payload
	payload string
	// path to proto-buf descriptor
	descriptor string
	// masterMsgName of the message
	masterMsgName string
	// struct name
	fileName string
)

// rootCmd represents the base command when called without any subcommands
var rootCmd = &cobra.Command{
	Use:   "protobuf",
	Short: "A command line application to work with protobuf schema and payload",
}

// Execute adds all child commands to the root command and sets flags appropriately.
// This is called by main.main(). It only needs to happen once to the rootCmd.
func Execute() {
	err := rootCmd.Execute()
	if err != nil {
		os.Exit(1)
	}
}

func init() {
	rootCmd.Flags().BoolP("toggle", "t", false, "Help message for toggle")
}

func protoevalError(err error) error {
	if err == nil {
		return nil
	}
	message := strings.Replace(err.Error(), "nats", "memphis", -1)
	return errors.New(message)
}

func terminate(err error) {
	if err != nil {
		os.Stderr.WriteString(err.Error())
		os.Exit(1)
	}
}

func compileMsgDescriptor(desc []byte, MasterMsgName, fileName string) (protoreflect.MessageDescriptor, error) {
	descriptorSet := descriptorpb.FileDescriptorSet{}
	err := proto.Unmarshal(desc, &descriptorSet)
	if err != nil {
		return nil, err
	}

	localRegistry, err := protodesc.NewFiles(&descriptorSet)
	if err != nil {
		return nil, err
	}

	fileDesc, err := localRegistry.FindFileByPath(fileName)
	if err != nil {
		return nil, err
	}

	msgsDesc := fileDesc.Messages()
	return msgsDesc.ByName(protoreflect.Name(MasterMsgName)), nil
}

func compileDescriptor(desc64 string, masterMsgName string, fileName string) (protoreflect.MessageDescriptor, error) {
	dbytes, err := base64.StdEncoding.DecodeString(desc64)
	if err != nil {
		return nil, err
	}
	return compileMsgDescriptor(dbytes, masterMsgName, fileName)
}

// deprecated
func compileDescriptorOld(desc64 string, masterMsgName string, fileName string) (protoreflect.MessageDescriptor, error) {
	descriptorSet := descriptorpb.FileDescriptorSet{}
	dbytes, err := base64.StdEncoding.DecodeString(desc64)
	if err != nil {
		return nil, err
	}
	err = proto.Unmarshal(dbytes, &descriptorSet)
	if err != nil {
		return nil, err
	}

	localRegistry, err := protodesc.NewFiles(&descriptorSet)
	if err != nil {
		return nil, err
	}

	// filePath := fmt.Sprintf("%v.proto", fileName)
	filePath := fileName
	fileDesc, err := localRegistry.FindFileByPath(filePath)
	if err != nil {
		return nil, err
	}

	msgsDesc := fileDesc.Messages()
	msgDesc := msgsDesc.ByName(protoreflect.Name(masterMsgName))

	return msgDesc, nil
}

func protoToJson(m []byte, desc protoreflect.MessageDescriptor) ([]byte, error) {
	newMsg := dynamicpb.NewMessage(desc)
	err := proto.Unmarshal(m, newMsg)
	if err != nil {
		return nil, err
	}

	jsonBytes, err := protojson.Marshal(newMsg)
	if err != nil {
		return nil, err
	}

	return jsonBytes, nil
}

func jsonToProto(msgBytes []byte, desc protoreflect.MessageDescriptor) ([]byte, error) {
	newMsg := dynamicpb.NewMessage(desc)
	err := protojson.Unmarshal(msgBytes, newMsg)
	if err != nil {
		return nil, err
	}

	protoBytes, err := proto.Marshal(newMsg)
	if err != nil {
		return nil, err
	}

	return protoBytes, nil
}
