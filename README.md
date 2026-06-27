# Raptoreum (RTM) Multi-Language SDK

Welcome to the **Raptoreum (RTM) Multi-Language SDK** repository. This project provides clean, native, and lightweight JSON-RPC clients for Raptoreum Core across the top 25 programming languages.

Integrating Raptoreum blockchain functionalities—like custom tokens (Assets), Smartnode queries, raw transaction builders, and wallet integrations—has never been easier, regardless of your stack.

---

## 🚀 Supported Languages & Directories

Select your language of choice to view setup details, package configurations, and examples:

| Language | Directory | Package Manager | Primary Registry |
| :--- | :--- | :--- | :--- |
| **Python** | [`sdk/python/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/python/) | `pip` | PyPI |
| **JavaScript** | [`sdk/javascript/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/javascript/) | `npm` | NPM |
| **TypeScript** | [`sdk/typescript/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/typescript/) | `npm` | NPM |
| **Go** | [`sdk/go/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/go/) | `go get` | pkg.go.dev |
| **Rust** | [`sdk/rust/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/rust/) | `cargo` | crates.io |
| **C++** | [`sdk/cpp/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/cpp/) | `vcpkg` | GitHub |
| **C#** | [`sdk/csharp/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/csharp/) | `nuget` | NuGet |
| **Java** | [`sdk/java/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/java/) | `maven` | Maven Central |
| **PHP** | [`sdk/php/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/php/) | `composer` | Packagist |
| **Ruby** | [`sdk/ruby/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/ruby/) | `gem` | RubyGems |
| **Swift** | [`sdk/swift/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/swift/) | `swift package` | Swift Registry |
| **Kotlin** | [`sdk/kotlin/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/kotlin/) | `gradle` | Maven Central |
| **Dart** | [`sdk/dart/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/dart/) | `pub` | pub.dev |
| **Scala** | [`sdk/scala/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/scala/) | `sbt` | Maven Central |
| **Perl** | [`sdk/perl/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/perl/) | `cpan` | CPAN |
| **Lua** | [`sdk/lua/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/lua/) | `luarocks` | LuaRocks |
| **R** | [`sdk/r_lang/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/r_lang/) | `install.packages` | CRAN |
| **Haskell** | [`sdk/haskell/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/haskell/) | `cabal` | Hackage |
| **Julia** | [`sdk/julia/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/julia/) | `Pkg` | Julia Registries |
| **Elixir** | [`sdk/elixir/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/elixir/) | `mix` | Hex.pm |
| **Clojure** | [`sdk/clojure/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/clojure/) | `deps` | Clojars |
| **Erlang** | [`sdk/erlang/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/erlang/) | `rebar3` | Hex.pm |
| **Bash** | [`sdk/bash/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/bash/) | `curl` | GitHub Releases |
| **PowerShell**| [`sdk/powershell/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/powershell/) | `Install-Module` | PowerShell Gallery|
| **F#** | [`sdk/fsharp/`](file:///e:/RTM-Scripts/rtm-sdk/sdk/fsharp/) | `nuget` | NuGet |

---

## 🎯 Milestone Roadmap

### 📍 Phase 1: Core JSON-RPC Wrappers (Milestone 1) - **Completed**
- [x] Standardized wrapper class structure for all 25 programming languages.
- [x] Package managers and publishing configuration files implemented.
- [x] Dynamic, zero-dependency HTTP connections where possible.
- [x] Read-only blockchain queries (`getblockchaininfo`, `getblockcount`, `getblock`, `getbestblockhash`).
- [x] Connection testing scripts and usage guides for every folder.

### 📍 Phase 2: Wallet & Transaction Writing (Milestone 2) - **Completed**
- [x] Direct credential-signed transaction execution (`sendtoaddress`, `sendmany`).
- [x] Custom RPC exceptions (`RaptoreumRPCException` / `RaptoreumRPCError`) for safe error parsing.
- [x] Support for address creation and verification commands (`getnewaddress`, `validateaddress`).

### 📍 Phase 3: Asset Layer Integration (Milestone 3) - **Completed**
- [x] Raptoreum Asset-specific CLI endpoints mapped to SDK methods.
- [x] Helper methods to format and serialize asset minting and transfer parameters (`createasset`, `sendasset`, `listassets`).

### 📍 Phase 4: Offline Key Management & Signatures (Milestone 4) - **Completed**
- [x] Add offline local wallet architecture (generating private keys and deriving addresses locally).
- [x] Implement local ECDSA secp256k1 key management and signing wrappers in higher-tier languages (Python, JS, TS, Go, Rust, C#, C++).

### 📍 Phase 5: Global Registry Publishing (Milestone 5) - **Completed**
- [x] Publish core packages to public registries (NPM, PyPI, Packagist, NuGet, Crates.io).
- [x] Establish automated GitHub Action pipelines for automated publishing.

### 📍 Phase 6: Offline Transaction Serialization & Local UTXO Selection (Milestone 6) - **Completed**
- [x] Implement local coin selection (FIFO, Knapsack) algorithms to manage inputs/fees offline.
- [x] Build offline raw transaction serialization wrappers across the client packages.

### 📍 Phase 7: Live Event Streaming (Milestone 7) - **Completed**
- [x] Add ZeroMQ subscription modules to capture block and transaction alerts from the local node.
- [x] Integrate lightweight WebSocket clients for streaming public explorer updates.

### 📍 Phase 8: Connection Pooling, Request Batching & Rate Limiting (Milestone 8) - **Completed**
- [x] Implement client-side connection pooling (HTTP persistent connections) to minimize handshake latency.
- [x] Introduce JSON-RPC array request batching and adaptive exponential backoff rate limiting.

### 📍 Phase 9: Unified Exception Taxonomy (Milestone 9) - **Completed**
- [x] Replace generic exception classes with specific typed structures (e.g. `InsufficientFundsException`, `WalletLockedException`).

---

## 🔒 Security Policy

Please read [SECURITY.md](file:///e:/RTM-Scripts/rtm-sdk/SECURITY.md) before deploying this SDK in a production environment. Never expose your node's RPC port to the public internet without proper authentication and encryption (SSL/TLS).

## 📚 General Documentation

Detailed integration guides and API maps are located in the [`docs/`](file:///e:/RTM-Scripts/rtm-sdk/docs/) folder:
- [Integration Setup Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/integration_guide.md)
- [Unified API Reference](file:///e:/RTM-Scripts/rtm-sdk/docs/api_reference.md)
- [SDK Architectural & Cryptographic Whitepaper](file:///e:/RTM-Scripts/rtm-sdk/docs/whitepaper.md)
- [Docker Node Deployment Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/docker_setup.md)
- [Security & Cryptographic Audit](file:///e:/RTM-Scripts/rtm-sdk/docs/audit.md)
- [Publishing Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/publishing_guide.md)