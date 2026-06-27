# Raptoreum (RTM) SDK - Python

A lightweight, zero-dependency Python client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

You can install this SDK from PyPI:
```bash
pip install rtm-sdk
```

Or install it locally from source:
```bash
cd python
pip install -e .
```

---

## Quick Start

```python
from raptoreum import RaptoreumClient

# Initialize the client
client = RaptoreumClient(
    host="127.0.0.1",
    port=8766,
    user="your_rpc_user",
    password="your_rpc_password"
)

# Fetch blockchain info
info = client.getblockchaininfo()
print(f"Current Block Height: {info['blocks']}")

# Get wallet balance
balance = client.getbalance()
print(f"Hot Wallet Balance: {balance} RTM")
```
