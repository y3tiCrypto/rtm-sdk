# Raptoreum (RTM) SDK - PowerShell

A standard PowerShell module wrapper for the Raptoreum Core JSON-RPC interface using `Invoke-RestMethod`.

---

## Installation

```powershell
Install-Module -Name RaptoreumSdk
```

Or import the module locally:
```powershell
Import-Module ./powershell/RaptoreumClient.psm1
```

---

## Quick Start

```powershell
$client = New-RaptoreumClient -HostName "127.0.0.1" -Port 8766 -User "user" -Password "pass"
$balance = $client.GetBalance()
Write-Host "Balance: $balance"
```
