# Raptoreum (RTM) SDK - TypeScript

A type-safe, lightweight, zero-dependency client wrapper for the Raptoreum Core JSON-RPC interface.

---

## Installation

```bash
npm install @rtm-sdk/typescript
```

---

## Quick Start

```typescript
import { RaptoreumClient } from '@rtm-sdk/typescript';

const client = new RaptoreumClient({
  host: '127.0.0.1',
  port: 8766,
  user: 'your_rpc_user',
  password: 'your_rpc_password'
});

async function run() {
  const blockCount = await client.getBlockCount();
  console.log(`Blocks: ${blockCount}`);
}

run();
```
