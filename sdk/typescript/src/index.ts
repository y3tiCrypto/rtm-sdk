export interface ClientOptions {
  host?: string;
  port?: number;
  user?: string;
  password?: string;
  useSsl?: boolean;
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
      throw new Error(`RPC Error [${json.error.code}]: ${json.error.message}`);
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
