package cmd

import (
	"context"
	"encoding/base64"
	"fmt"
	"os"

	"github.com/spf13/cobra"
)

var j2pCmd = &cobra.Command{
	Use:   fmt.Sprintf("j2p %s", serializationCmdArgs),
	Short: "JSON to ProtoBuf",
	Long:  `Converts JSON to ProtoBuf`,
	Run: func(cmd *cobra.Command, args []string) {
		handleJ2P(cmd.Context())
	},
}

func init() {
	j2pCmd.Flags().StringVar(&payload, "payload", "", "json payload")
	j2pCmd.Flags().StringVar(&descriptor, "desc", "", "proto-buf descriptor")
	j2pCmd.Flags().StringVar(&masterMsgName, "mname", "", "master message name")
	j2pCmd.Flags().StringVar(&fileName, "fname", "", "file name")
	j2pCmd.MarkFlagRequired("payload")
	j2pCmd.MarkFlagRequired("desc")
	j2pCmd.MarkFlagRequired("mname")
	j2pCmd.MarkFlagRequired("fname")
	rootCmd.AddCommand(j2pCmd)
}

func handleJ2P(context context.Context) {
	jbytes, err := base64.StdEncoding.DecodeString(payload)
	if err != nil {
		terminate(err)
	}
	desc, err := compileDescriptor(descriptor, masterMsgName, fileName)
	if err != nil {
		terminate(err)
	}
	pb, err := jsonToProto(jbytes, desc)
	if err != nil {
		terminate(err)
	}
	p64 := base64.StdEncoding.EncodeToString(pb)
	os.Stdout.WriteString(p64)
}
