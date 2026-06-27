# Raptoreum SDK Security & Cryptographic Audit

This document details the security model, cryptographic algorithms, and dependency audits for the Raptoreum (RTM) SDK.

---

## 🔒 1. Cryptographic Derivation Audit

The SDK implements local key generation, address derivation, and offline transaction signing. This isolates private key materials entirely from network nodes.

### Elliptic Curve Cryptography (ECC)
*   **Curve**: `secp256k1` (standard Koblitz curve).
*   **Key Validation**: Asserts private key sizes are precisely 32 bytes and public keys are compressed (33 bytes, starting with prefix `0x02` or `0x03`).
*   **Signing**: Uses ECDSA signatures over double-SHA256 message digests.

### Address Derivation Hash Chain
Raptoreum addresses use the standard Base58Check-encoded public key hash (P2PKH):
1.  `ECDSA Public Key` (compressed, 33 bytes)
2.  `SHA-256` hashing of the public key.
3.  `RIPEMD-160` hashing of the SHA-256 output (yielding the 20-byte `hash160` public key hash).
4.  Prefix byte `0x3c` (yielding the Raptoreum Mainnet address starting prefix `R`).
5.  `Base58Check` encoding (payload + 4-byte double-SHA256 checksum) ensuring typos trigger validation errors.

---

## 📦 2. Dependency Footprint Audit

To minimize supply-chain risk and package bloat, the SDK was engineered with a strict **zero-dependency** goal for core operations:

| SDK Language | Core Dependencies | Streaming Dependencies | Audit Verdict |
| :--- | :--- | :--- | :--- |
| **Python** | `None` (Standard library only) | `pyzmq` (Optional for ZMQ) | **Highly Secure**: Negligible supply-chain risk. |
| **JavaScript** | `None` (Standard built-ins) | `ws`, `zeromq` (Optional for ZMQ) | **Secure**: Common utility libraries isolated. |
| **TypeScript** | `None` (Standard built-ins) | `ws`, `zeromq` (Optional for ZMQ) | **Secure**: Fully typed interface declarations. |
| **Go** | `None` (Standard library only) | `None` (Custom ZMTP protocol engine) | **Max Security**: Zero external dependencies monorepo-wide. |
| **C# / F#** | `System.Text.Json` | `None` (Custom ZMTP TCP client engine) | **Max Security**: No third-party assemblies required. |

---

## 🌐 3. Connection & Transport Security

*   **SSL/TLS Support**: The HTTP client wrappers natively support standard secure connections (`https://`) when instantiating the client options `use_ssl=True`.
*   **Basic Authentication**: HTTP Basic Authentication headers are encoded in Base64 and handled inside local in-memory byte blocks.
*   **Connection Reuse**: HTTP Keep-Alive persistent connection pooling is enabled across all language client libraries, reducing socket exhaustion vulnerabilities.

---

## 📡 4. Custom ZMTP Protocol Safety (Go & C#)

To eliminate heavy native bindings for ZeroMQ subscription clients in Go and C#, a lightweight custom ZMTP v3.0 framing engine was implemented over raw TCP:
*   **Input Sanitization**: Implements packet length validation limits to prevent buffer overflow attacks.
*   **Heartbeat Isolation**: Periodically discards empty ZMTP ping/pong frames without allocation, preventing memory leaks during long-running subscription cycles.
