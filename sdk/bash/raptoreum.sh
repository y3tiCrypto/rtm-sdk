#!/usr/bin/env bash

# Raptoreum CLI RPC Shell Wrapper
# Usage: source raptoreum.sh && rtm_request <method> <params_json_array>

RTM_HOST=${RTM_RPC_HOST:-"127.0.0.1"}
RTM_PORT=${RTM_RPC_PORT:-8766}
RTM_USER=${RTM_RPC_USER:-""}
RTM_PASS=${RTM_RPC_PASS:-""}
RTM_USE_SSL=${RTM_RPC_SSL:-false}

rtm_request() {
  local method="$1"
  local params="${2:-[]}"
  local scheme="http"
  if [ "$RTM_USE_SSL" = true ]; then
    scheme="https"
  fi
  local url="${scheme}://${RTM_HOST}:${RTM_PORT}/"
  
  local payload
  payload=$(cat <<EOF
{
  "jsonrpc": "1.0",
  "id": "rtm-sdk-bash",
  "method": "${method}",
  "params": ${params}
}
EOF
)

  local auth=""
  if [ -n "$RTM_USER" ] || [ -n "$RTM_PASS" ]; then
    auth="-u ${RTM_USER}:${RTM_PASS}"
  fi

  local response
  response=$(curl -s -X POST \
       -H "Content-Type: application/json" \
       $auth \
       -d "$payload" \
       "$url")

  if echo "$response" | grep -q '"error":' && ! echo "$response" | grep -qE '"error":\s*null'; then
    echo "$response" >&2
  fi
  echo "$response"
}

rtm_getblockchaininfo() {
  rtm_request "getblockchaininfo" "[]"
}

rtm_getblockcount() {
  rtm_request "getblockcount" "[]"
}

rtm_getbalance() {
  rtm_request "getbalance" "[]"
}

rtm_validateaddress() {
  rtm_request "validateaddress" "[\"$1\"]"
}

rtm_sendmany() {
  local amounts="$1"
  local minconf="${2:-1}"
  local comment="${3:-""}"
  rtm_request "sendmany" "[\"\", ${amounts}, ${minconf}, \"${comment}\"]"
}

rtm_listassets() {
  local mine="${1:-false}"
  rtm_request "listassets" "[${mine}]"
}

rtm_createasset() {
  local name="$1"
  local amount="$2"
  local options="${3:-{}}"
  rtm_request "createasset" "[\"${name}\", ${amount}, ${options}]"
}

rtm_sendasset() {
  local asset_id="$1"
  local amount="$2"
  local address="$3"
  rtm_request "sendasset" "[\"${asset_id}\", ${amount}, \"${address}\"]"
}
