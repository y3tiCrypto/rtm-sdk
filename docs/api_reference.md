# Unified API Reference Guide

This API Reference maps the core JSON-RPC methods exposed by the Raptoreum Core Node that are wrapped inside the RTM Multi-Language SDK.

---

## 📨 Request & Response Format

All API calls are executed as HTTP POST requests with a JSON body:

### Request Structure
*   `jsonrpc` (string): Always `"1.0"`.
*   `id` (string): Request identifier (e.g. `"rtm-sdk-client"`).
*   `method` (string): The RPC command.
*   `params` (array): Input arguments.

### Response Structure
*   `result` (object/array/value): Output of the command if successful.
*   `error` (object/null): If an error occurred, contains `code` (integer) and `message` (string).
*   `id` (string): Request identifier matching the input request.

---

## 🔗 Category 1: Blockchain Data

### 1. `getblockchaininfo`
Returns state information about the blockchain sync progress, active forks, block count, and headers.
*   **Parameters**: None
*   **Result**: Object with sync status, difficulty, active chain.

### 2. `getblockcount`
Returns the height of the most-work chain.
*   **Parameters**: None
*   **Result**: Number (height)

### 3. `getblockhash`
Returns the hash of the block at the specified block height.
*   **Parameters**:
    *   `height` (integer, required): Block height index.
*   **Result**: String (blockhash hex)

### 4. `getblock`
Returns block data.
*   **Parameters**:
    *   `hash` (string, required): Block hash hex.
    *   `verbosity` (integer, optional, default: `1`): `0` for raw hex, `1` for parsed object, `2` for parsed object with transaction details.
*   **Result**: String or Object.

---

## 💼 Category 2: Wallet Management

> [!NOTE]
> Requires the wallet system to be enabled on your node (`disablewallet=0` in config).

### 1. `getbalance`
Returns the total available balance of the wallet.
*   **Parameters**: None
*   **Result**: Number (RTM balance)

### 2. `getnewaddress`
Generates a new RTM address for receiving payments.
*   **Parameters**:
    *   `label` (string, optional): A local label for the address.
    *   `address_type` (string, optional): Address format (e.g. `"legacy"`).
*   **Result**: String (RTM address)

### 3. `sendtoaddress`
Sends RTM from the wallet to a target address.
*   **Parameters**:
    *   `address` (string, required): Destination RTM address.
    *   `amount` (number, required): Amount of RTM.
    *   `comment` (string, optional): A local description comment.
    *   `comment_to` (string, optional): A comment describing the destination.
    *   `subtractfeefromamount` (boolean, optional, default: `false`)
*   **Result**: String (Transaction Hash hex)

### 4. `validateaddress`
Returns details about the given Raptoreum address.
*   **Parameters**:
    *   `address` (string, required): The Raptoreum address to validate.
*   **Result**: Object containing address validation properties (e.g. `isvalid` boolean, `address` string, `scriptPubKey` hex, etc.).

### 5. `sendmany`
Sends RTM to multiple addresses in a single transaction.
*   **Parameters**:
    *   `from_account` (string, required): Account name (must be empty string `""` for default account).
    *   `amounts` (object, required): A JSON object mapping RTM addresses to payment amounts (e.g. `{"address1": 10.5, "address2": 2.0}`).
    *   `minconf` (number, optional, default: `1`): Minimum confirmations of UTXOs to use.
    *   `comment` (string, optional): A comment to describe the transaction.
    *   `subtractfeefrom` (array of strings, optional): Array of addresses to subtract transaction fees from.
*   **Result**: String (Transaction Hash hex)

---

## 💎 Category 3: Custom Asset Layer (RIPs)

Raptoreum supports user-minted custom tokens and NFTs.

### 1. `listassets`
Lists all assets matching search parameters.
*   **Parameters**:
    *   `mine` (boolean, optional, default: `false`): If `true`, returns only assets created by your node.
*   **Result**: Array of Asset details objects.

### 2. `createasset`
Creates (mints) a new custom asset.
*   **Parameters**:
    *   `name` (string, required): Name of the asset.
    *   `amount` (number, required): Initial supply.
    *   `options` (object, optional): Configuration options (e.g., decimals, transferability).
*   **Result**: Object (Asset registration metadata)

### 3. `sendasset`
Transfers custom assets to a target address.
*   **Parameters**:
    *   `asset_id` (string, required): ID of the asset.
    *   `qty` (number, required): Quantity to transfer.
    *   `to_address` (string, required): RTM receiver address.
*   **Result**: String (Transaction Hash hex)

---

## 🖥️ Category 4: Smartnodes

Smartnodes provide services to the Raptoreum network.

### 1. `smartnode status`
Queries the status of the smartnode running on the node.
*   **Parameters**: None (mapped as request `"smartnode"` with parameter `["status"]`).
*   **Result**: Object containing status, collateral, and network properties.

### 2. `smartnodelist`
Returns the status list of all smartnodes registered on the network.
*   **Parameters**: None
*   **Result**: Object mapping smartnode outputs to status.

---

## ⚠️ Category 5: RPC Error Handling

Every SDK client parses error responses from the Raptoreum Core JSON-RPC node. If an error payload is returned, the client throws a custom exception `RaptoreumRPCException` (or language-equivalent casing, e.g., `RaptoreumRPCError`, `RaptoreumRpcException`).

### Standard Exception Structure:
*   `code` (int): The negative integer error code returned by the daemon.
    *   `-1`: Generic model/JSON error.
    *   `-4`: Wallet error (e.g., wallet locked, invalid address).
    *   `-6`: Insufficient funds.
    *   `-28`: Node warming up.
*   `message` (string): The detailed error message explaining the cause.

---

## 🔒 Category 6: Offline Local Wallet (`RaptoreumWallet`)

Exposed in higher-tier SDK languages (Python, JS, TS, Go, Rust, C#, C++) for managing keys locally without node RPC interactions.

### 1. `generatePrivateKey` / `generate_private_key`
Generates a cryptographically secure 32-byte secp256k1 private key.
*   **Parameters**: None
*   **Result**: 32 bytes (raw bytes or buffer depending on language)

### 2. `privateKeyToAddress` / `private_key_to_address`
Derives the compressed public key and Base58Check address starting with `R`.
*   **Parameters**:
    *   `privateKeyBytes` (32 bytes): The private key.
*   **Result**: String (Raptoreum address)

### 3. `signMessage` / `sign_message`
Signs arbitrary messages using standard secp256k1 ECDSA low-S DER signature format over double-SHA256 hashes.
*   **Parameters**:
    *   `privateKeyBytes` (32 bytes): The private key.
    *   `messageBytes` (bytes): The message to sign.
*   **Result**: DER-encoded byte sequence (signature bytes)

---

## 🔨 Category 7: Offline Transaction Serialization & Builder (`RaptoreumTransactionBuilder`)

Exposed in higher-tier SDK languages (Python, JS, TS, Go, Rust, C#, C++) for building, signing, and serializing transactions offline.

### 1. `addInput` / `add_input`
Adds a transaction input (UTXO) to spend.
*   **Parameters**:
    *   `txid` (string): The transaction ID of the UTXO.
    *   `vout` (number): The output index.
    *   `scriptPubKey` (string/bytes): The public key script of the output.
    *   `amountRtm` (number/string): The amount of RTM in the output.

### 2. `addOutput` / `add_output`
Adds a target payment output.
*   **Parameters**:
    *   `address` (string): The receiver's Raptoreum address (verified and decoded locally).
    *   `amountRtm` (number/string): The amount of RTM to transfer.

### 3. `serialize`
Serializes the transaction into its raw byte array representation.
*   **Parameters**: None
*   **Result**: Serialized byte sequence (Buffer/bytes) representing raw transaction hex.

### 4. `sign`
Signs all added inputs using standard double-SHA256 signature pre-images and inserts signature scripts.
*   **Parameters**:
    *   `privateKeyBytes` (32 bytes): The private key of the inputs.

### 5. `selectInputs` / `select_inputs` (Static)
Performs local FIFO UTXO coin selection and calculates transaction fees.
*   **Parameters**:
    *   `utxos` (array): List of available UTXOs.
    *   `targetAmountRtm` (number/string): Target amount to spend.
    *   `feeRateSatByte` (number, default: 1): Fee rate in Satoshis per byte.
*   **Result**: Object/Pair containing array of selected UTXOs and the calculated fee.

---

## 📡 Category 8: Real-Time Event Streaming

Exposed in higher-tier SDK languages (Python, JS, TS, Go, C#) for subscribing to live block and transaction events.

### 1. `RaptoreumWebSocketClient`
A WebSocket connection manager to stream events from explorer API nodes.
*   **Methods**:
    *   `connect()` / `ConnectAsync()`: Establishes connection to the websocket endpoint.
    *   `on(event, callback)`: Binds a listener (e.g. `message`, `error`) to incoming websocket messages.
    *   `close()` / `CloseAsync()`: Closes connection.

### 2. `RaptoreumZmqListener`
A native TCP ZMTP socket subscription client to capture publishers directly from local node ports.
*   **Methods**:
    *   `start(callback)` / `StartAsync(callback)`: Connects, performs ZMTP handshakes, subscribes to raw block and transaction topics, and streams message byte sequences to the callback.
    *   `stop()`: Closes the subscriber socket connection.
