# Standalone Docker Node Deployment Guide

This guide details how to build, run, and connect to a local standalone Raptoreum Core node inside Docker for development, SDK testing, and transaction verification.

---

## 🚀 Getting Started

### 1. Build and Run via Docker Compose

In the root repository directory (`E:\RTM-Scripts\rtm-sdk`), spin up the Docker stack:
```bash
docker-compose up -d --build
```

This command will:
1. Build the image fetching the verified Raptoreum Core `v1.2.1.2` release binary.
2. Initialize default configurations (`raptoreum.conf`) with JSON-RPC enabled.
3. Expose port `8766` (Mainnet RPC) and `18766` (Testnet RPC).
4. Create a persistent named volume `raptoreum-data` to preserve blockchain blocks.

---

## 🛠️ CLI Interactions

You can send CLI commands directly into the running container daemon using `docker exec`:

### Check Node Sync Status
```bash
docker exec -it raptoreum-node raptoreum-cli getblockchaininfo
```

### Check Wallet Balance
```bash
docker exec -it raptoreum-node raptoreum-cli getbalance
```

### View Live Logs
```bash
docker logs -f raptoreum-node
```

---

## 🔌 Connecting your SDK Client

When connecting your SDK wrappers (e.g. JavaScript, Python, Go, C#) to the Docker container, use the following credentials:

| Parameter | Configuration Value |
| :--- | :--- |
| **Endpoint** | `http://127.0.0.1:8766` |
| **RPC Username** | `rtmuser` |
| **RPC Password** | `rtmpassword` |
| **SSL / HTTPS** | Disabled (plain HTTP) |

---

## 🛑 Stopping the Stack

To stop the node and remove containers (preserving data volumes):
```bash
docker-compose down
```

To wipe the containers and clear block history (e.g., to reset state):
```bash
docker-compose down -v
```
