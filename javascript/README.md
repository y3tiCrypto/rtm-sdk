# Raptoreum (RTM) SDK - JavaScript

A lightweight, zero-dependency JavaScript client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

You can install this SDK from NPM:
```bash
npm install rtm-sdk
```

---

## Quick Start

```javascript
const { RaptoreumClient } = require('rtm-sdk');

// Initialize the client
const client = new RaptoreumClient({
  host: '127.0.0.1',
  port: 8766,
  user: 'your_rpc_user',
  password: 'your_rpc_password'
});

// Fetch blockchain info
client.getBlockchainInfo()
  .then(info => {
    console.log(`Current Block Height: ${info.blocks}`);
  })
  .catch(error => {
    console.error('Error fetching blockchain info:', error);
  });
```
