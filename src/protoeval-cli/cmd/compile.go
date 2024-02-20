package cmd

import (
	"context"

	"github.com/spf13/cobra"
)

var compileCmd = &cobra.Command{
	Use:   "compile --desc=desc --mname=mname --fname=fname",
	Short: "Compile proto-buf descriptor",
	Long:  `Compile proto-buf descriptor`,
	Run: func(cmd *cobra.Command, args []string) {
		handleCompileDesc(cmd.Context())
	},
}

func init() {
	compileCmd.Flags().StringVar(&descriptor, "desc", "", "proto-buf descriptor")
	compileCmd.Flags().StringVar(&masterMsgName, "mname", "", "master message name")
	compileCmd.Flags().StringVar(&fileName, "fname", "", "file name")
	compileCmd.MarkFlagRequired("desc")
	compileCmd.MarkFlagRequired("mname")
	compileCmd.MarkFlagRequired("fname")
	rootCmd.AddCommand(compileCmd)
}

func handleCompileDesc(context context.Context) {
	_, err := compileDescriptor(descriptor, masterMsgName, fileName)
	if err != nil {
		terminate(err)
	}
}
