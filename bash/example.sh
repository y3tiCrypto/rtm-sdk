#!/usr/bin/env bash

# Source the client functions
source "$(dirname "$0")/raptoreum.sh"

export RTM_RPC_HOST=${RTM_RPC_HOST:-"127.0.0.1"}
export RTM_RPC_PORT=${RTM_RPC_PORT:-8766}
export RTM_RPC_USER=${RTM_RPC_USER:-"rtm_rpc_user"}
export RTM_RPC_PASS=${RTM_RPC_PASS:-"rtm_rpc_secure_password_98231"}

echo "Connecting to Raptoreum Node at http://${RTM_RPC_HOST}:${RTM_RPC_PORT} (Bash)..."
response=$(rtm_getblockchaininfo)

if [ $? -eq 0 ] && [ -n "$response" ]; then
  echo -e "\nConnection Successful!"
  echo "$response"
else
  echo -e "\nCould not connect to node."
fi
