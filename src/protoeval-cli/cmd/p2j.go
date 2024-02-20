package cmd

import (
	"context"
	"encoding/base64"
	"fmt"
	"os"

	"github.com/spf13/cobra"
)

var p2jCmd = &cobra.Command{
	Use:   fmt.Sprintf("p2j %s", serializationCmdArgs),
	Short: "ProtoBuf to JSON",
	Long:  `Converts ProtoBuf to JSON`,
	Run: func(cmd *cobra.Command, args []string) {
		handleP2J(cmd.Context())
	},
}

func init() {
	p2jCmd.Flags().StringVar(&payload, "payload", "", "proto-buf payload")
	p2jCmd.Flags().StringVar(&descriptor, "desc", "", "proto-buf descriptor int base64")
	p2jCmd.Flags().StringVar(&masterMsgName, "mname", "", "master message name")
	p2jCmd.Flags().StringVar(&fileName, "fname", "", "file name")
	p2jCmd.MarkFlagRequired("payload")
	p2jCmd.MarkFlagRequired("desc")
	p2jCmd.MarkFlagRequired("mname")
	p2jCmd.MarkFlagRequired("fname")
	rootCmd.AddCommand(p2jCmd)
}

func handleP2J(context context.Context) {
	pb, err := base64.StdEncoding.DecodeString(payload)
	if err != nil {
		terminate(err)
	}
	desc, err := compileDescriptor(descriptor, masterMsgName, fileName)
	if err != nil {
		terminate(err)
	}
	jsonBytes, err := protoToJson(pb, desc)
	if err != nil {
		terminate(err)
	}
	j64 := base64.StdEncoding.EncodeToString(jsonBytes)
	os.Stdout.WriteString(j64)
}
