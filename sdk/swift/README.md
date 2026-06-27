# Raptoreum (RTM) SDK - Swift

An asynchronous Swift Package client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

Add this package to your `Package.swift`:
```swift
dependencies: [
    .package(url: "https://github.com/Raptor3um/rtm-sdk.git", from: "1.0.0")
]
```

---

## Quick Start

```swift
import RaptoreumClient

let client = RaptoreumClient(host: "127.0.0.1", port: 8766, user: "user", pass: "pass")

client.request(method: "getblockchaininfo") { (result: Result<[String: AnyCodable], Error>) in
    switch result {
    case .success(let info):
        print("Blocks: \(info["blocks"]?.value ?? 0)")
    case .failure(let error):
        print("Error: \(error)")
    }
}
```
