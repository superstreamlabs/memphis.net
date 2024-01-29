package cmd

import (
	"context"
	"encoding/base64"
	"encoding/json"
	"log"
	"os"
	"strings"

	"github.com/spf13/cobra"
)

func init() {
	cmd := cobra.Command{
		Use:     "j2p --payload=payload --schema=schema --schema-name=schema-name",
		Aliases: []string{"c"},
		Short:   "Json to proto-buf",
		Args:    cobra.MaximumNArgs(0),
		Run: func(cmd *cobra.Command, _ []string) {
			handleConvertToProtoBuf(cmd.Context())
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

func handleConvertToProtoBuf(context context.Context) {
	jsonBytes, err := base64.StdEncoding.DecodeString(payloadBase64)
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

	jsonStr := string(jsonBytes)
	jsonObj := make(map[string]interface{})
	err = json.Unmarshal([]byte(jsonStr), &jsonObj)
	if err != nil {
		log.SetFlags(0)
		log.Fatal(err)
	}
	protoBytes, err := validateProtoBufMessage(jsonObj, descriptor)
	if err != nil {
		log.SetFlags(0)
		log.Fatal(err)
	}

	protoBase64 := base64.StdEncoding.EncodeToString(protoBytes)
	protoBase64 = strings.TrimRight(protoBase64, "\n")
	os.Stdout.WriteString(protoBase64)
}
