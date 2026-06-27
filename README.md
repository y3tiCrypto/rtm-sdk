# Raptoreum (RTM) Multi-Language SDK

Welcome to the **Raptoreum (RTM) Multi-Language SDK** repository. This project provides clean, native, and lightweight JSON-RPC clients for Raptoreum Core across the top 25 programming languages.

Integrating Raptoreum blockchain functionalities—like custom tokens (Assets), Smartnode queries, raw transaction builders, and wallet integrations—has never been easier, regardless of your stack.

---

## 🚀 Supported Languages & Directories

Select your language of choice to view setup details, package configurations, and examples:

| Language | Directory | Package Manager | Primary Registry |
| :--- | :--- | :--- | :--- |
| **Python** | [`python/`](file:///e:/RTM-Scripts/rtm-sdk/python/) | `pip` | PyPI |
| **JavaScript** | [`javascript/`](file:///e:/RTM-Scripts/rtm-sdk/javascript/) | `npm` | NPM |
| **TypeScript** | [`typescript/`](file:///e:/RTM-Scripts/rtm-sdk/typescript/) | `npm` | NPM |
| **Go** | [`go/`](file:///e:/RTM-Scripts/rtm-sdk/go/) | `go get` | pkg.go.dev |
| **Rust** | [`rust/`](file:///e:/RTM-Scripts/rtm-sdk/rust/) | `cargo` | crates.io |
| **C++** | [`cpp/`](file:///e:/RTM-Scripts/rtm-sdk/cpp/) | `vcpkg` | GitHub |
| **C#** | [`csharp/`](file:///e:/RTM-Scripts/rtm-sdk/csharp/) | `nuget` | NuGet |
| **Java** | [`java/`](file:///e:/RTM-Scripts/rtm-sdk/java/) | `maven` | Maven Central |
| **PHP** | [`php/`](file:///e:/RTM-Scripts/rtm-sdk/php/) | `composer` | Packagist |
| **Ruby** | [`ruby/`](file:///e:/RTM-Scripts/rtm-sdk/ruby/) | `gem` | RubyGems |
| **Swift** | [`swift/`](file:///e:/RTM-Scripts/rtm-sdk/swift/) | `swift package` | Swift Registry |
| **Kotlin** | [`kotlin/`](file:///e:/RTM-Scripts/rtm-sdk/kotlin/) | `gradle` | Maven Central |
| **Dart** | [`dart/`](file:///e:/RTM-Scripts/rtm-sdk/dart/) | `pub` | pub.dev |
| **Scala** | [`scala/`](file:///e:/RTM-Scripts/rtm-sdk/scala/) | `sbt` | Maven Central |
| **Perl** | [`perl/`](file:///e:/RTM-Scripts/rtm-sdk/perl/) | `cpan` | CPAN |
| **Lua** | [`lua/`](file:///e:/RTM-Scripts/rtm-sdk/lua/) | `luarocks` | LuaRocks |
| **R** | [`r_lang/`](file:///e:/RTM-Scripts/rtm-sdk/r_lang/) | `install.packages` | CRAN |
| **Haskell** | [`haskell/`](file:///e:/RTM-Scripts/rtm-sdk/haskell/) | `cabal` | Hackage |
| **Julia** | [`julia/`](file:///e:/RTM-Scripts/rtm-sdk/julia/) | `Pkg` | Julia Registries |
| **Elixir** | [`elixir/`](file:///e:/RTM-Scripts/rtm-sdk/elixir/) | `mix` | Hex.pm |
| **Clojure** | [`clojure/`](file:///e:/RTM-Scripts/rtm-sdk/clojure/) | `deps` | Clojars |
| **Erlang** | [`erlang/`](file:///e:/RTM-Scripts/rtm-sdk/erlang/) | `rebar3` | Hex.pm |
| **Bash** | [`bash/`](file:///e:/RTM-Scripts/rtm-sdk/bash/) | `curl` | GitHub Releases |
| **PowerShell**| [`powershell/`](file:///e:/RTM-Scripts/rtm-sdk/powershell/) | `Install-Module` | PowerShell Gallery|
| **F#** | [`fsharp/`](file:///e:/RTM-Scripts/rtm-sdk/fsharp/) | `nuget` | NuGet |

---

## 🎯 Milestone Roadmap

### 📍 Phase 1: Core JSON-RPC Wrappers (Milestone 1) - **Completed**
- [x] Standardized wrapper class structure for all 25 programming languages.
- [x] Package managers and publishing configuration files implemented.
- [x] Dynamic, zero-dependency HTTP connections where possible.
- [x] Read-only blockchain queries (`getblockchaininfo`, `getblockcount`, `getblock`, `getbestblockhash`).
- [x] Connection testing scripts and usage guides for every folder.

### 📍 Phase 2: Wallet & Transaction Writing (Milestone 2)
- [ ] Direct credential-signed transaction execution (`sendtoaddress`, `sendmany`).
- [ ] Safe JSON parsing wrappers for outputs.
- [ ] Support for address creation and verification commands (`getnewaddress`, `validateaddress`).

### 📍 Phase 3: Asset Layer Integration (Milestone 3)
- [ ] Raptoreum Asset-specific CLI endpoints mapped to SDK methods.
- [ ] Helper methods to format and serialize asset minting and transfer parameters (`createasset`, `sendasset`, `listassets`).

### 📍 Phase 4: Offline Key Management & Signatures (Milestone 4)
- [ ] Add offline transaction construction (avoiding sending private keys to node RPCs).
- [ ] Implement ECDSA keys and GhostRider hashing locally within higher-tier languages (Python, JS, Go, Rust, C#, C++).

### 📍 Phase 5: Global Registry Publishing (Milestone 5)
- [ ] Publish core packages to public registries (NPM, PyPI, Packagist, NuGet, Crates.io).
- [ ] Establish automated GitHub Action pipelines for automated publishing.

---

## 🔒 Security Policy

Please read [SECURITY.md](file:///e:/RTM-Scripts/rtm-sdk/SECURITY.md) before deploying this SDK in a production environment. Never expose your node's RPC port to the public internet without proper authentication and encryption (SSL/TLS).

## 📚 General Documentation

Detailed integration guides and API maps are located in the [`docs/`](file:///e:/RTM-Scripts/rtm-sdk/docs/) folder:
- [Integration Setup Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/integration_guide.md)
- [Unified API Reference](file:///e:/RTM-Scripts/rtm-sdk/docs/api_reference.md)
- [Publishing Guide](file:///e:/RTM-Scripts/rtm-sdk/docs/publishing_guide.md)