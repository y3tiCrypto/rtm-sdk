# Raptoreum (RTM) SDK - Rust

An idiomatic, blocking Rust client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

Add this to your `Cargo.toml`:
```toml
[dependencies]
rtm-sdk = "1.0.0"
```

---

## Quick Start

```rust
use rtm_sdk::RaptoreumClient;

fn main() {
    let client = RaptoreumClient::new(
        "127.0.0.1", 
        8766, 
        Some("your_user".to_string()), 
        Some("your_pass".to_string()), 
        false
    );

    match client.getblockcount() {
        Ok(blocks) => println!("Blocks: {}", blocks),
        Err(e) => eprintln!("Error: {}", e),
    }
}
```
