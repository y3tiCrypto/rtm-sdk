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

const crypto = require('crypto');

class RaptoreumWallet {
  static generatePrivateKey() {
    const { privateKey } = crypto.generateKeyPairSync('ec', {
      namedCurve: 'secp256k1'
    });
    return privateKey.export({ type: 'sec1', format: 'der' }).slice(7, 39);
  }

  static privateKeyToAddress(privateKeyBytes) {
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

  static signMessage(privateKeyBytes, messageBytes) {
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

module.exports = { RaptoreumClient, RaptoreumRPCError, RaptoreumWallet };
