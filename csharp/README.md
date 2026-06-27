# Raptoreum (RTM) SDK - C#

An asynchronous, standard C#/.NET SDK for the Raptoreum Core JSON-RPC interface.

---

## Installation

Install via NuGet:
```bash
dotnet add package RaptoreumSdk
```

---

## Quick Start

```csharp
using System;
using System.Threading.Tasks;
using RaptoreumSdk;

class Program
{
    static async Task Main()
    {
        var client = new RaptoreumClient("127.0.0.1", 8766, "user", "pass");
        var balance = await client.GetBalanceAsync();
        Console.WriteLine($"Balance: {balance} RTM");
    }
}
```
