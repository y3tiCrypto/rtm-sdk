package main

import (
	"fmt"
	"os"
	"strconv"

	"github.com/Raptor3um/rtm-sdk/go"
)

func main() {
	host := os.Getenv("RTM_RPC_HOST")
	if host == "" {
		host = "127.0.0.1"
	}
	portStr := os.Getenv("RTM_RPC_PORT")
	port := 8766
	if portStr != "" {
		if p, err := strconv.Atoi(portStr); err == nil {
			port = p
		}
	}
	user := os.Getenv("RTM_RPC_USER")
	if user == "" {
		user = "rtm_rpc_user"
	}
	pass := os.Getenv("RTM_RPC_PASS")
	if pass == "" {
		pass = "rtm_rpc_secure_password_98231"
	}

	fmt.Printf("Connecting to Raptoreum Node at http://%s:%d (Go)...\n", host, port)
	client := rtm.NewClient(host, port, user, pass, false)

	info, err := client.GetBlockchainInfo()
	if err != nil {
		fmt.Printf("\nCould not connect to node: %v\n", err)
		return
	}

	fmt.Println("\nConnection Successful!")
	fmt.Printf("Response: %s\n", string(info))
}
