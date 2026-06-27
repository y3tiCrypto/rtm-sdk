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

  static privateKeyToPublicKey(privateKeyBytes) {
    const key = crypto.createECDH('secp256k1');
    key.setPrivateKey(privateKeyBytes);
    return key.getPublicKey(null, 'compressed');
  }
}

function decodeBase58(str) {
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

function encodeVarInt(n) {
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

class RaptoreumTransactionBuilder {
  constructor() {
    this.inputs = [];
    this.outputs = [];
    this.locktime = 0;
    this.version = 1;
  }

  addInput(txid, vout, scriptPubKey, amountRtm) {
    this.inputs.push({
      txid: txid,
      vout: parseInt(vout),
      scriptPubKey: typeof scriptPubKey === 'string' ? Buffer.from(scriptPubKey, 'hex') : scriptPubKey,
      amount: BigInt(Math.round(parseFloat(amountRtm) * 100000000)),
      scriptSig: Buffer.alloc(0)
    });
  }

  addOutput(address, amountRtm) {
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
      value: BigInt(Math.round(parseFloat(amountRtm) * 100000000)),
      script: script
    });
  }

  serialize() {
    const parts = [];
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

  sign(privateKeyBytes) {
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

  static selectInputs(utxos, targetAmountRtm, feeRateSatByte = 1) {
    const targetSat = BigInt(Math.round(parseFloat(targetAmountRtm) * 100000000));
    let accumulated = 0n;
    const selected = [];
    const numOutputs = 2;
    
    for (const utxo of utxos) {
      selected.push(utxo);
      accumulated += BigInt(Math.round(parseFloat(utxo.amount) * 100000000));
      
      const size = 148 * selected.length + 34 * numOutputs + 10;
      const fee = BigInt(size * feeRateSatByte);
      
      if (accumulated >= targetSat + fee) {
        return { selected, fee: Number(fee) };
      }
    }
    throw new Error("Insufficient funds");
  }
}

class RaptoreumWebSocketClient {
  constructor(url) {
    this.url = url;
    this.ws = null;
    this.callbacks = {};
  }

  connect() {
    let WebSocket;
    try {
      WebSocket = require('ws');
    } catch (err) {
      throw new Error("Please install 'ws' dependency to use WebSocket client: npm install ws");
    }

    this.ws = new WebSocket(this.url);

    this.ws.on('open', () => {
      if (this.callbacks['open']) this.callbacks['open']();
    });

    this.ws.on('message', (data) => {
      if (this.callbacks['message']) this.callbacks['message'](data.toString());
    });

    this.ws.on('error', (err) => {
      if (this.callbacks['error']) this.callbacks['error'](err);
    });

    this.ws.on('close', () => {
      if (this.callbacks['close']) this.callbacks['close']();
    });
  }

  on(event, callback) {
    this.callbacks[event] = callback;
  }

  close() {
    if (this.ws) {
      this.ws.close();
    }
  }
}

class RaptoreumZmqListener {
  constructor(host = "127.0.0.1", port = 28332) {
    this.host = host;
    this.port = port;
    this.sock = null;
  }

  async start(callback) {
    let zmq;
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
      callback(topic.toString(), msg);
    }
  }

  stop() {
    if (this.sock) {
      this.sock.close();
    }
  }
}

module.exports = { 
  RaptoreumClient, 
  RaptoreumRPCError, 
  RaptoreumWallet, 
  RaptoreumTransactionBuilder,
  RaptoreumWebSocketClient,
  RaptoreumZmqListener
};
