# Raptoreum (RTM) SDK Documentation Hub

Welcome to the official developer documentation for the Raptoreum (RTM) Multi-Language SDK. This SDK provides developers with robust, clean, and language-idiomatic libraries to communicate with Raptoreum Full Nodes.

---

## 📖 Document Index

Explore our guides to get started with Raptoreum integration:

1.  **[Integration Setup Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/integration_guide.md)**: Learn how to set up `raptoreumd`, configure your node's JSON-RPC server, secure your connection, and test network access.
2.  **[API Reference](file:///e:/RTM-Scripts/rtm-sdk/docs/api_reference.md)**: Explore the detailed mapping of standard RPC methods, wallet methods, asset layers, and smartnode controllers.
3.  **[Publishing Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/publishing_guide.md)**: Maintenance documentation on packaging and publishing libraries to public registries.

---

## ⚡ Quick Start Matrix

Below is a quick reference table showing the main packages and how to install them:

| Language | Install Command | Module / Import path |
| :--- | :--- | :--- |
| **Python** | `pip install rtm-sdk` | `import raptoreum` |
| **JavaScript** | `npm install rtm-sdk` | `const { RaptoreumClient } = require('rtm-sdk');` |
| **TypeScript** | `npm install @rtm-sdk/typescript` | `import { RaptoreumClient } from '@rtm-sdk/typescript';` |
| **Go** | `go get github.com/Raptor3um/rtm-sdk/sdk/go` | `"github.com/Raptor3um/rtm-sdk/sdk/go"` |
| **Rust** | `cargo add rtm-sdk` | `use rtm_sdk::RaptoreumClient;` |
| **C#** | `dotnet add package RaptoreumSdk` | `using RaptoreumSdk;` |
| **PHP** | `composer require raptoreum/rtm-sdk` | `use Raptoreum\RaptoreumClient;` |
| **Java** | Add maven dependency | `import org.raptoreum.RaptoreumClient;` |
| **Ruby** | `gem install rtm-sdk` | `require 'raptoreum'` |
| **Swift** | Add SPM Dependency | `import RaptoreumClient` |
| **Kotlin** | `implementation("org.raptoreum:rtm-sdk")` | `import org.raptoreum.RaptoreumClient` |
| **Dart** | `dart pub add rtm_sdk` | `import 'package:rtm_sdk/raptoreum.dart';` |

For all other 13 languages, navigate to their individual subdirectories in this repository to find custom build scripts and usage instructions.

---

## ⚠️ Security Reminder

All clients connect via JSON-RPC. In production, always host your node behind a firewall or configure access limits using SSH tunnels or reverse proxies with HTTPS. See [SECURITY.md](file:///e:/RTM-Scripts/rtm-sdk/SECURITY.md) for details.
