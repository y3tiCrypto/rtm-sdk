class RaptoreumRPCError extends Error {
  constructor(code, message) {
    super(`RPC Error [${code}]: ${message}`);
    this.code = code;
    this.name = 'RaptoreumRPCError';
  }
}

class RaptoreumClient {
  constructor({ host = '127.0.0.1', port = 8766, user = '', password = '', useSsl = false } = {}) {
    this.host = host;
    this.port = port;
    this.user = user;
    this.password = password;
    this.useSsl = useSsl;
    const scheme = useSsl ? 'https' : 'http';
    this.url = `${scheme}://${host}:${port}/`;
  }

  async request(method, params = []) {
    const headers = {
      'Content-Type': 'application/json'
    };

    if (this.user || this.password) {
      const auth = Buffer.from(`${this.user}:${this.password}`).toString('base64');
      headers['Authorization'] = `Basic ${auth}`;
    }

    const payload = {
      jsonrpc: '1.0',
      id: 'rtm-sdk-js',
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

    return json.result;
  }

  // Blockchain API
  getBlockchainInfo() { return this.request('getblockchaininfo'); }
  getBlockCount() { return this.request('getblockcount'); }
  getBlockHash(height) { return this.request('getblockhash', [height]); }
  getBlock(hash, verbosity = 1) { return this.request('getblock', [hash, verbosity]); }
  getBestBlockHash() { return this.request('getbestblockhash'); }

  // Wallet API
  getBalance() { return this.request('getbalance'); }
  getNewAddress(label = '', addressType = 'legacy') { return this.request('getnewaddress', [label, addressType]); }
  sendToAddress(address, amount, comment = '', commentTo = '', subtractFee = false) {
    return this.request('sendtoaddress', [address, amount, comment, commentTo, subtractFee]);
  }

  validateAddress(address) {
    return this.request('validateaddress', [address]);
  }

  sendMany(amounts, minConf = 1, comment = '', subtractFeeFrom = []) {
    return this.request('sendmany', ['', amounts, minConf, comment, subtractFeeFrom]);
  }

  // Asset API
  listAssets(mine = false) { return this.request('listassets', [mine]); }
  createAsset(name, amount, options = {}) { return this.request('createasset', [name, amount, options]); }
  sendAsset(assetId, amount, address) { return this.request('sendasset', [assetId, amount, address]); }

  // Smartnode API
  smartnodeList() { return this.request('smartnodelist'); }
  smartnodeStatus() { return this.request('smartnode', ['status']); }
}

module.exports = { RaptoreumClient, RaptoreumRPCError };
