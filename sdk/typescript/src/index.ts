export interface ClientOptions {
  host?: string;
  port?: number;
  user?: string;
  password?: string;
  useSsl?: boolean;
}

export class RaptoreumRPCError extends Error {
  public code: number;
  constructor(code: number, message: string) {
    super(`RPC Error [${code}]: ${message}`);
    this.code = code;
    this.name = 'RaptoreumRPCError';
  }
}

export class RaptoreumClient {
  private url: string;
  private user?: string;
  private password?: string;

  constructor(options: ClientOptions = {}) {
    const host = options.host || '127.0.0.1';
    const port = options.port || 8766;
    this.user = options.user;
    this.password = options.password;
    const scheme = options.useSsl ? 'https' : 'http';
    this.url = `${scheme}://${host}:${port}/`;
  }

  async request<T>(method: string, params: any[] = []): Promise<T> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json'
    };

    if (this.user || this.password) {
      const auth = Buffer.from(`${this.user || ''}:${this.password || ''}`).toString('base64');
      headers['Authorization'] = `Basic ${auth}`;
    }

    const payload = {
      jsonrpc: '1.0',
      id: 'rtm-sdk-ts',
      method,
      params
    };

    const response = await fetch(this.url, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload)
    });

    if (!response.ok && response.status !== 500) {
      throw new Error(`HTTP Error ${response.status}: ${response.statusText}`);
    }

    const json = await response.json();
    if (json.error) {
      throw new RaptoreumRPCError(json.error.code, json.error.message);
    }

    return json.result as T;
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
}
