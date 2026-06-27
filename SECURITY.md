# Security Policy

This document outlines the security policies, best practices, and reporting procedures for the Raptoreum (RTM) Multi-Language SDK.

## Security Best Practices for Integration

Because this SDK connects to your Raptoreum Core Wallet Node (`raptoreumd` or `raptoreum-qt`) using JSON-RPC, protecting your node's credentials and networking is critical to securing your wallet and data.

### 1. Protect Your RPC Credentials
*   **Do not hardcode credentials:** Never write your `rpcuser` or `rpcpassword` directly into code files that are committed to git repositories.
*   **Use environment variables:** Store credentials in environment variables or a configuration manager (e.g., `.env` files, HashiCorp Vault, AWS Secrets Manager) and inject them at runtime.
*   **Restrict file permissions:** Ensure that configuration files (like `.env` or `raptoreum.conf`) are read-only to the user running the process.

### 2. Network Isolation
*   **Never expose RPC ports to the internet:** By default, Raptoreum's RPC port is `8766` (or custom). This port should **never** be open to the public web.
*   **Bind to localhost:** If your application runs on the same server as the daemon, configure `rpcallowip=127.0.0.1` or `rpcbind=127.0.0.1` in your `raptoreum.conf`.
*   **Use a VPN or SSH Tunnel:** If your application must connect to a remote node:
    *   Set up a secure SSH tunnel forwarding the RPC port.
    *   Run the node inside a private VPC/VPN and restrict access via firewall rules (e.g., `iptables` or cloud security groups) so only your application servers can connect.
*   **Reverse Proxy with SSL:** Consider running an Nginx or Apache reverse proxy in front of the daemon to terminate SSL/TLS. The native daemon RPC port does not use SSL by default.

### 3. Least Privilege
*   **Limit Wallet Access:** If your application only needs to read blockchain data (e.g., block explorer or transaction tracking), run the Raptoreum daemon with wallet disabled (`disablewallet=1`) or use a node without loaded wallets.
*   **Split wallets:** Keep cold storage funds separate from hot wallets used by the JSON-RPC interface.

---

## Reporting a Vulnerability

If you discover a security vulnerability in this SDK, please do **not** open a public issue. Instead, report it privately via one of the following methods:

1.  **Email**: Send a detailed report to `security@raptoreum.com` (or the repository maintainer's contact).
2.  **Encrypted Message**: If necessary, encrypt your report using a public PGP key (include key details if available).

Please include:
*   A description of the vulnerability.
*   The language client and version affected.
*   Step-by-step instructions to reproduce the issue (including proof of concept code if possible).
*   Any suggestions for remediation.

We aim to acknowledge receipt of all vulnerability reports within **48 hours** and provide a timeline for a patch or mitigation within **7 days**.
