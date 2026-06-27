# Raptoreum (RTM) SDK - Lua

A lightweight Lua client wrapper for the Raptoreum Core JSON-RPC interface, invoking system curl.

---

## Installation

```bash
luarocks install rtm-sdk
```

---

## Quick Start

```lua
local raptoreum = require("raptoreum")

local client = raptoreum.new("127.0.0.1", 8766, "user", "pass")
local response = client:getbalance()
print("Balance JSON: " .. response)
```
