# Raptoreum (RTM) SDK - Bash

A lightweight Bash script wrapping `curl` for the Raptoreum Core JSON-RPC interface.

---

## Installation

Download and source the wrapper in your scripts:
```bash
source bash/raptoreum.sh
```

---

## Quick Start

```bash
export RTM_RPC_USER="your_rpc_user"
export RTM_RPC_PASS="your_rpc_password"

# Query the blockchain info
rtm_getblockchaininfo
```
