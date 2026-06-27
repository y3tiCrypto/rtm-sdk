export const SDK_VERSION = '1.0.1';

export interface ClientOptions {
  host?: string;
  port?: number;
  user?: string;
  password?: string;
  useSsl?: boolean;
  maxRetries?: number;
  retryDelay?: number;
}

export class RaptoreumRPCError extends Error {
  public code: number;
  constructor(code: number, message: string) {
    super(`RPC Error [${code}]: ${message}`);
    this.code = code;
    this.name = 'RaptoreumRPCError';
  }
}

export class InvalidAddressError extends RaptoreumRPCError {
  constructor(code: number, message: string) {
    super(code, message);
    this.name = 'InvalidAddressError';
  }
}

export class InsufficientFundsError extends RaptoreumRPCError {
  constructor(code: number, message: string) {
    super(code, message);
    this.name = 'InsufficientFundsError';
  }
}

export class WalletLockedError extends RaptoreumRPCError {
  constructor(code: number, message: string) {
    super(code, message);
    this.name = 'WalletLockedError';
  }
}

export class NodeWarmingUpError extends RaptoreumRPCError {
  constructor(code: number, message: string) {
    super(code, message);
    this.name = 'NodeWarmingUpError';
  }
}

export function getRPCError(code: number, message: string): RaptoreumRPCError {
  if (code === -5) {
    return new InvalidAddressError(code, message);
  } else if (code === -6) {
    return new InsufficientFundsError(code, message);
  } else if (code === -13) {
    return new WalletLockedError(code, message);
  } else if (code === -28) {
    return new NodeWarmingUpError(code, message);
  } else {
    return new RaptoreumRPCError(code, message);
  }
}

export class RaptoreumClient {
  private url: string;
  private user?: string;
  private password?: string;
  public maxRetries: number;
  public retryDelay: number;

  constructor(options: ClientOptions = {}) {
    const host = options.host || '127.0.0.1';
    const port = options.port || 8766;
    this.user = options.user;
    this.password = options.password;
    const scheme = options.useSsl ? 'https' : 'http';
    this.url = `${scheme}://${host}:${port}/`;
    this.maxRetries = options.maxRetries ?? 3;
    this.retryDelay = options.retryDelay ?? 1000;
  }

  async _post(payload: any): Promise<any> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      'Connection': 'keep-alive'
    };

    if (this.user || this.password) {
      const auth = Buffer.from(`${this.user || ''}:${this.password || ''}`).toString('base64');
      headers['Authorization'] = `Basic ${auth}`;
    }

    for (let attempt = 0; attempt <= this.maxRetries; attempt++) {
      try {
        const response = await fetch(this.url, {
          method: 'POST',
          headers,
          body: JSON.stringify(payload)
        });

        if (response.status === 429) {
          throw new Error("HTTP Error 429: Too Many Requests");
        }

        if (!response.ok && response.status !== 500) {
          throw new Error(`HTTP Error ${response.status}: ${response.statusText}`);
        }

        const json = await response.json();
        return json;
      } catch (err) {
        if (attempt === this.maxRetries) {
          throw err;
        }
        const delay = this.retryDelay * Math.pow(2, attempt) + Math.random() * 500;
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }

  async request<T>(method: string, params: any[] = []): Promise<T> {
    const payload = {
      jsonrpc: '1.0',
      id: 'rtm-sdk-ts',
      method,
      params
    };

    const json = await this._post(payload);
    if (json.error) {
      throw getRPCError(json.error.code, json.error.message);
    }

    return json.result as T;
  }

  createBatch(): RaptoreumBatch {
    return new RaptoreumBatch(this);
  }

  // Blockchain API
  getBlockchainInfo(): Promise<any> { return this.request('getblockchaininfo'); }
  getBlockCount(): Promise<number> { return this.request<number>('getblockcount'); }
  getBlockHash(height: number): Promise<string> { return this.request<string>('getblockhash', [height]); }
  getBlock(hash: string, verbosity: number = 1): Promise<any> { return this.request('getblock', [hash, verbosity]); }
  getBestBlockHash(): Promise<string> { return this.request<string>('getbestblockhash'); }

  // Wallet API
  getBalance(): Promise<number> { return this.request<number>('getbalance'); }
  getNewAddress(label: string = '', addressType: string = 'legacy'): Promise<string> {
    return this.request<string>('getnewaddress', [label, addressType]);
  }
  sendToAddress(address: string, amount: number, comment: string = '', commentTo: string = '', subtractFee: boolean = false): Promise<string> {
    return this.request<string>('sendtoaddress', [address, amount, comment, commentTo, subtractFee]);
  }

  validateAddress(address: string): Promise<any> {
    return this.request<any>('validateaddress', [address]);
  }

  sendMany(amounts: Record<string, number>, minConf: number = 1, comment: string = '', subtractFeeFrom: string[] = []): Promise<string> {
    return this.request<string>('sendmany', ['', amounts, minConf, comment, subtractFeeFrom]);
  }

  // Asset API
  listAssets(mine: boolean = false): Promise<any[]> { return this.request<any[]>('listassets', [mine]); }
  createAsset(name: string, amount: number, options: any = {}): Promise<any> {
    return this.request('createasset', [name, amount, options]);
  }
  sendAsset(assetId: string, amount: number, address: string): Promise<string> {
    return this.request<string>('sendasset', [assetId, amount, address]);
  }

  // Smartnode API
  smartnodeList(): Promise<any> { return this.request('smartnodelist'); }
  smartnodeStatus(): Promise<any> { return this.request('smartnode', ['status']); }
}

import * as crypto from 'crypto';

export class RaptoreumWallet {
  static generatePrivateKey(): Buffer {
    const { privateKey } = crypto.generateKeyPairSync('ec', {
      namedCurve: 'secp256k1'
    });
    return privateKey.export({ type: 'sec1', format: 'der' }).slice(7, 39);
  }

  static privateKeyToAddress(privateKeyBytes: Buffer): string {
    const key = crypto.createECDH('secp256k1');
    key.setPrivateKey(privateKeyBytes);
    const pubKey = key.getPublicKey(null, 'compressed');

    const sha = crypto.createHash('sha256').update(pubKey).digest();
    const h160 = crypto.createHash('ripemd160').update(sha).digest();

    const payload = Buffer.concat([Buffer.from([0x3c]), h160]);
    const hash1 = crypto.createHash('sha256').update(payload).digest();
    const checksum = crypto.createHash('sha256').update(hash1).digest().slice(0, 4);

    const fullPayload = Buffer.concat([payload, checksum]);

    const B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    let n = BigInt('0x' + fullPayload.toString('hex'));
    let res = "";
    while (n > 0n) {
      const r = n % 58n;
      n = n / 58n;
      res = B58[Number(r)] + res;
    }
    let pad = 0;
    for (let i = 0; i < fullPayload.length; i++) {
      if (fullPayload[i] === 0) pad++;
      else break;
    }
    return "1".repeat(pad) + res;
  }

  static signMessage(privateKeyBytes: Buffer, messageBytes: Buffer): Buffer {
    const hash = crypto.createHash('sha256').update(crypto.createHash('sha256').update(messageBytes).digest()).digest();
    
    const keyObject = crypto.createPrivateKey({
      key: Buffer.concat([
        Buffer.from([0x30, 0x2e, 0x02, 0x01, 0x01, 0x04, 0x20]),
        privateKeyBytes,
        Buffer.from([0xa0, 0x07, 0x06, 0x05, 0x2b, 0x81, 0x04, 0x00, 0x0a])
      ]),
      format: 'der',
      type: 'sec1'
    });

    return crypto.sign(null, hash, keyObject);
  }

  static privateKeyToPublicKey(privateKeyBytes: Buffer): Buffer {
    const key = crypto.createECDH('secp256k1');
    key.setPrivateKey(privateKeyBytes);
    return key.getPublicKey(null, 'compressed');
  }
}

function decodeBase58(str: string): Buffer {
  const B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
  let n = 0n;
  for (let i = 0; i < str.length; i++) {
    const c = str[i];
    const index = B58.indexOf(c);
    if (index === -1) throw new Error("Invalid Base58 character");
    n = n * 58n + BigInt(index);
  }
  let hex = n.toString(16);
  if (hex.length % 2 !== 0) hex = '0' + hex;
  let bytes = Buffer.from(hex, 'hex');
  if (bytes.length < 25) {
    bytes = Buffer.concat([Buffer.alloc(25 - bytes.length), bytes]);
  }
  return bytes;
}

function encodeVarInt(n: number): Buffer {
  if (n < 0xfd) {
    return Buffer.from([n]);
  } else if (n <= 0xffff) {
    const buf = Buffer.alloc(3);
    buf[0] = 0xfd;
    buf.writeUInt16LE(n, 1);
    return buf;
  } else if (n <= 0xffffffff) {
    const buf = Buffer.alloc(5);
    buf[0] = 0xfe;
    buf.writeUInt32LE(n, 1);
    return buf;
  } else {
    const buf = Buffer.alloc(9);
    buf[0] = 0xff;
    buf.writeBigUInt64LE(BigInt(n), 1);
    return buf;
  }
}

export interface UTXO {
  txid: string;
  vout: number;
  amount: number | string;
  scriptPubKey: string;
}

export class RaptoreumTransactionBuilder {
  inputs: {
    txid: string;
    vout: number;
    scriptPubKey: Buffer;
    amount: bigint;
    scriptSig: Buffer;
  }[];
  outputs: {
    value: bigint;
    script: Buffer;
  }[];
  locktime: number;
  version: number;

  constructor() {
    this.inputs = [];
    this.outputs = [];
    this.locktime = 0;
    this.version = 1;
  }

  addInput(txid: string, vout: number, scriptPubKey: string | Buffer, amountRtm: number | string): void {
    this.inputs.push({
      txid: txid,
      vout: Number(vout),
      scriptPubKey: typeof scriptPubKey === 'string' ? Buffer.from(scriptPubKey, 'hex') : scriptPubKey,
      amount: BigInt(Math.round(Number(amountRtm) * 100000000)),
      scriptSig: Buffer.alloc(0)
    });
  }

  addOutput(address: string, amountRtm: number | string): void {
    const full = decodeBase58(address);
    const payload = full.slice(0, 21);
    const checksum = full.slice(21);
    const h1 = crypto.createHash('sha256').update(payload).digest();
    const h2 = crypto.createHash('sha256').update(h1).digest();
    if (!h2.slice(0, 4).equals(checksum)) {
      throw new Error("Invalid address checksum");
    }
    const hash160 = payload.slice(1);
    
    const script = Buffer.concat([
      Buffer.from([0x76, 0xa9, 0x14]),
      hash160,
      Buffer.from([0x88, 0xac])
    ]);
    
    this.outputs.push({
      value: BigInt(Math.round(Number(amountRtm) * 100000000)),
      script: script
    });
  }

  serialize(): Buffer {
    const parts: Buffer[] = [];
    const verBuf = Buffer.alloc(4);
    verBuf.writeUInt32LE(this.version, 0);
    parts.push(verBuf);
    
    parts.push(encodeVarInt(this.inputs.length));
    for (const txIn of this.inputs) {
      const txidBuf = Buffer.from(txIn.txid, 'hex').reverse();
      parts.push(txidBuf);
      
      const voutBuf = Buffer.alloc(4);
      voutBuf.writeUInt32LE(txIn.vout, 0);
      parts.push(voutBuf);
      
      parts.push(encodeVarInt(txIn.scriptSig.length));
      parts.push(txIn.scriptSig);
      
      const seqBuf = Buffer.alloc(4);
      seqBuf.writeUInt32LE(0xffffffff, 0);
      parts.push(seqBuf);
    }
    
    parts.push(encodeVarInt(this.outputs.length));
    for (const txOut of this.outputs) {
      const valBuf = Buffer.alloc(8);
      valBuf.writeBigUInt64LE(txOut.value, 0);
      parts.push(valBuf);
      
      parts.push(encodeVarInt(txOut.script.length));
      parts.push(txOut.script);
    }
    
    const ltBuf = Buffer.alloc(4);
    ltBuf.writeUInt32LE(this.locktime, 0);
    parts.push(ltBuf);
    
    return Buffer.concat(parts);
  }

  sign(privateKeyBytes: Buffer): void {
    const pubkey = RaptoreumWallet.privateKeyToPublicKey(privateKeyBytes);
    
    for (let i = 0; i < this.inputs.length; i++) {
      const originalScriptSigs = this.inputs.map(x => x.scriptSig);
      
      for (let j = 0; j < this.inputs.length; j++) {
        if (j === i) {
          this.inputs[j].scriptSig = this.inputs[j].scriptPubKey;
        } else {
          this.inputs[j].scriptSig = Buffer.alloc(0);
        }
      }
      
      const preimage = Buffer.concat([
        this.serialize(),
        Buffer.from([0x01, 0x00, 0x00, 0x00])
      ]);
      
      const sig = RaptoreumWallet.signMessage(privateKeyBytes, preimage);
      const sigWithHash = Buffer.concat([sig, Buffer.from([0x01])]);
      
      const scriptSig = Buffer.concat([
        Buffer.from([sigWithHash.length]),
        sigWithHash,
        Buffer.from([pubkey.length]),
        pubkey
      ]);
      
      for (let j = 0; j < this.inputs.length; j++) {
        this.inputs[j].scriptSig = originalScriptSigs[j];
      }
      this.inputs[i].scriptSig = scriptSig;
    }
  }

  static selectInputs(utxos: UTXO[], targetAmountRtm: number | string, feeRateSatByte: number = 1): { selected: UTXO[], fee: number } {
    const targetSat = BigInt(Math.round(Number(targetAmountRtm) * 100000000));
    let accumulated = 0n;
    const selected: UTXO[] = [];
    const numOutputs = 2;
    
    for (const utxo of utxos) {
      selected.push(utxo);
      accumulated += BigInt(Math.round(Number(utxo.amount) * 100000000));
      
      const size = 148 * selected.length + 34 * numOutputs + 10;
      const fee = BigInt(size * feeRateSatByte);
      
      if (accumulated >= targetSat + fee) {
        return { selected, fee: Number(fee) };
      }
    }
    throw new Error("Insufficient funds");
  }
}

export class RaptoreumWebSocketClient {
  url: string;
  ws: any;
  callbacks: { [key: string]: Function };

  constructor(url: string) {
    this.url = url;
    this.ws = null;
    this.callbacks = {};
  }

  connect(): void {
    let WebSocket: any;
    try {
      WebSocket = require('ws');
    } catch (err) {
      throw new Error("Please install 'ws' dependency to use WebSocket client: npm install ws");
    }

    this.ws = new WebSocket(this.url);

    this.ws.on('open', () => {
      if (this.callbacks['open']) this.callbacks['open']();
    });

    this.ws.on('message', (data: any) => {
      if (this.callbacks['message']) this.callbacks['message'](data.toString());
    });

    this.ws.on('error', (err: any) => {
      if (this.callbacks['error']) this.callbacks['error'](err);
    });

    this.ws.on('close', () => {
      if (this.callbacks['close']) this.callbacks['close']();
    });
  }

  on(event: string, callback: Function): void {
    this.callbacks[event] = callback;
  }

  close(): void {
    if (this.ws) {
      this.ws.close();
    }
  }
}

export class RaptoreumZmqListener {
  host: string;
  port: number;
  sock: any;

  constructor(host: string = "127.0.0.1", port: number = 28332) {
    this.host = host;
    this.port = port;
    this.sock = null;
  }

  async start(callback: (topic: string, message: Buffer) => void): Promise<void> {
    let zmq: any;
    try {
      zmq = require('zeromq');
    } catch (err) {
      throw new Error("Please install 'zeromq' dependency to use ZMQ listener: npm install zeromq@6.0.0-beta.6");
    }

    this.sock = new zmq.Subscriber();
    this.sock.connect(`tcp://${this.host}:${this.port}`);
    this.sock.subscribe('rawtx');
    this.sock.subscribe('rawblock');
    this.sock.subscribe('hashblock');
    this.sock.subscribe('hashtx');

    for await (const [topic, msg] of this.sock) {
      callback(topic.toString(), msg as Buffer);
    }
  }

  stop(): void {
    if (this.sock) {
      this.sock.close();
    }
  }
}

export class RaptoreumBatch {
  private client: RaptoreumClient;
  private requests: any[];

  constructor(client: RaptoreumClient) {
    this.client = client;
    this.requests = [];
  }

  add(method: string, params: any[] = []): void {
    this.requests.push({
      jsonrpc: '1.0',
      id: `rtm-batch-${this.requests.length}`,
      method,
      params
    });
  }

  async execute(): Promise<any[]> {
    if (this.requests.length === 0) return [];

    const json = await this.client._post(this.requests);
    if (!Array.isArray(json)) {
      if (json && json.error) {
        throw getRPCError(json.error.code, json.error.message);
      }
      throw new Error("Invalid batch response from server");
    }

    const results = new Array(this.requests.length).fill(null);
    for (const resp of json) {
      const id = resp.id;
      if (id && id.startsWith('rtm-batch-')) {
        const idx = parseInt(id.split('-').pop() || '0', 10);
        if (idx >= 0 && idx < results.length) {
          if (resp.error) {
            results[idx] = getRPCError(resp.error.code, resp.error.message);
          } else {
            results[idx] = resp.result;
          }
        }
      }
    }
    return results;
  }
}
