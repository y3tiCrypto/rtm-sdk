# Raptoreum Node Integration Setup Guide

This guide describes how to configure, secure, and verify a Raptoreum full node JSON-RPC interface to enable communication with the RTM Multi-Language SDK.

---

## 🛠️ Step 1: Install Raptoreum Core

To communicate with the Raptoreum network, you must run a full node (`raptoreumd` or `raptoreum-qt`).

1.  Download the latest release binaries from the [Official Raptoreum GitHub Releases page](https://github.com/Raptor3um/raptoreum/releases).
2.  Extract the archive and move the binaries (`raptoreumd` and `raptoreum-cli`) to your system path (e.g., `/usr/local/bin` on Linux or standard folder on Windows).
3.  Launch the node to create default folders and configurations.

---

## ⚙️ Step 2: Configure `raptoreum.conf`

Locate your Raptoreum data directory:
*   **Linux**: `~/.raptoreumcore/`
*   **Windows**: `%APPDATA%\RaptoreumCore\`
*   **macOS**: `~/Library/Application Support/RaptoreumCore/`

Create or edit the `raptoreum.conf` file and add the following settings:

```ini
# Enable JSON-RPC server
server=1

# RPC Authentication Credentials (change these!)
rpcuser=rtm_rpc_user
rpcpassword=rtm_rpc_secure_password_98231

# TCP Port to bind to
# Default mainnet: 8766
# Default testnet: 18766
rpcport=8766

# Restrict IP address access (highly recommended)
# Bound to localhost (127.0.0.1) for local apps
rpcallowip=127.0.0.1

# Optional: Run in background (daemon mode - Linux only)
daemon=1

# Optional: Run on testnet instead of mainnet
# testnet=1
```

Save the file and restart your daemon:
```bash
# To stop:
raptoreum-cli stop

# To start:
raptoreumd
```

---

## 🔒 Step 3: Secure the RPC Interface

In production settings, you must secure the traffic.

### Options for Remote Node Security:

#### 1. SSH Tunneling (Recommended)
Do not expose the RPC port directly. Create an SSH tunnel from your application server to your node server:
```bash
ssh -N -L 8766:127.0.0.1:8766 user@node-ip
```
Your application can now communicate with `http://127.0.0.1:8766` as if the node were running locally.

#### 2. Reverse Proxy with SSL (Nginx)
Configure Nginx as a reverse proxy to handle HTTPS termination and pass traffic to the node:
```nginx
server {
    listen 443 ssl;
    server_name rpc.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/rpc.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/rpc.yourdomain.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:8766;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🧪 Step 4: Verify Your Connection

You can verify that your JSON-RPC server is running and authenticating requests correctly using `curl`:

```bash
curl --user rtm_rpc_user:rtm_rpc_secure_password_98231 \
     --data-binary '{"jsonrpc": "1.0", "id":"curltest", "method": "getblockchaininfo", "params": [] }' \
     -H 'content-type: text/plain;' \
     http://127.0.0.1:8766/
```

### Expected Output:
```json
{
  "result": {
    "chain": "main",
    "blocks": 1543029,
    "headers": 1543029,
    "bestblockhash": "00000000000...",
    "difficulty": 12345.67,
    "mediantime": 1782637211,
    "verificationprogress": 1.0,
    "initialblockdownload": false,
    "chainwork": "0000000000000...",
    "size_on_disk": 123456789,
    "pruned": false,
    "softforks": { ... },
    "warnings": ""
  },
  "error": null,
  "id": "curltest"
}
```
If you get this output, your node is fully configured and ready to be used with the SDK!
