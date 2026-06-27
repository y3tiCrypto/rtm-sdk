# Raptoreum (RTM) SDK - F#

An asynchronous, standard F#/.NET SDK for the Raptoreum Core JSON-RPC interface.

---

## Installation

Install via NuGet:
```bash
dotnet add package RaptoreumSdk.FSharp
```

---

## Quick Start

```fsharp
open System
open RaptoreumSdk

let client = RaptoreumClient("127.0.0.1", 8766, "user", "pass", false)
let balance = client.GetBalanceAsync().Result
printfn "Balance: %O RTM" balance
```
