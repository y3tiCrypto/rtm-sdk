# Raptoreum (RTM) SDK - Julia

A standard Julia client wrapper for the Raptoreum Core JSON-RPC interface, built on `HTTP` and `JSON3`.

---

## Installation

Within Julia package manager:
```julia
using Pkg
Pkg.add(["HTTP", "JSON3"])
```

---

## Quick Start

```julia
using Raptoreum

client = RaptoreumClient("127.0.0.1", 8766, "user", "pass")
balance = getbalance(client)
println("Balance: $balance")
```
