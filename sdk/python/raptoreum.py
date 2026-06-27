import json
import urllib.request
import urllib.error
import base64
import asyncio

__version__ = "1.0.1"

class RaptoreumRPCException(Exception):
    def __init__(self, code, message):
        self.code = code
        self.message = message
        super().__init__(f"RPC Error [{code}]: {message}")

class InvalidAddressException(RaptoreumRPCException):
    pass

class InsufficientFundsException(RaptoreumRPCException):
    pass

class WalletLockedException(RaptoreumRPCException):
    pass

class NodeWarmingUpException(RaptoreumRPCException):
    pass

def get_rpc_error(code, message):
    if code == -5:
        return InvalidAddressException(code, message)
    elif code == -6:
        return InsufficientFundsException(code, message)
    elif code == -13:
        return WalletLockedException(code, message)
    elif code == -28:
        return NodeWarmingUpException(code, message)
    else:
        return RaptoreumRPCException(code, message)

def raise_rpc_error(code, message):
    raise get_rpc_error(code, message)

class RaptoreumClient:
    def __init__(self, host="127.0.0.1", port=8766, user="", password="", use_ssl=False):
        self.host = host
        self.port = port
        self.user = user
        self.password = password
        self.use_ssl = use_ssl
        self.max_retries = 3
        self.retry_delay = 1.0
        self._conn = None

    def _get_connection(self):
        import http.client
        if self._conn is None:
            if self.use_ssl:
                self._conn = http.client.HTTPSConnection(self.host, self.port, timeout=30)
            else:
                self._conn = http.client.HTTPConnection(self.host, self.port, timeout=30)
        return self._conn

    def _post(self, payload):
        import time
        import random
        
        headers = {
            "Content-Type": "application/json",
            "Connection": "keep-alive"
        }
        if self.user or self.password:
            auth_str = f"{self.user}:{self.password}".encode("utf-8")
            auth_header = "Basic " + base64.b64encode(auth_str).decode("utf-8")
            headers["Authorization"] = auth_header
            
        data = json.dumps(payload).encode("utf-8")
        
        for attempt in range(self.max_retries + 1):
            conn = self._get_connection()
            try:
                conn.request("POST", "/", body=data, headers=headers)
                response = conn.getresponse()
                resp_bytes = response.read()
                
                if response.status == 429:
                    raise Exception("HTTP Error 429: Too Many Requests")
                    
                resp_data = json.loads(resp_bytes.decode("utf-8"))
                return resp_data
            except Exception as e:
                if self._conn:
                    self._conn.close()
                    self._conn = None
                
                if attempt == self.max_retries:
                    raise e
                    
                sleep_time = self.retry_delay * (2 ** attempt) + random.uniform(0, 0.5)
                time.sleep(sleep_time)

    def request(self, method, params=None):
        if params is None:
            params = []
        
        payload = {
            "jsonrpc": "1.0",
            "id": "rtm-sdk-python",
            "method": method,
            "params": params
        }
        
        resp_data = self._post(payload)
        if resp_data.get("error"):
            err = resp_data["error"]
            raise_rpc_error(err.get("code"), err.get("message"))
        return resp_data.get("result")

    def create_batch(self):
        return RaptoreumBatch(self)


class RaptoreumBatch:
    def __init__(self, client):
        self.client = client
        self.requests = []

    def add(self, method, params=None):
        if params is None:
            params = []
        self.requests.append({
            "jsonrpc": "1.0",
            "id": f"rtm-batch-{len(self.requests)}",
            "method": method,
            "params": params
        })

    def execute(self):
        if not self.requests:
            return []
            
        resp_data = self.client._post(self.requests)
        if not isinstance(resp_data, list):
            if isinstance(resp_data, dict) and resp_data.get("error"):
                err = resp_data["error"]
                raise_rpc_error(err.get("code"), err.get("message"))
            raise Exception("Invalid batch response from server")
            
        results = [None] * len(self.requests)
        for resp in resp_data:
            resp_id = resp.get("id")
            if resp_id and resp_id.startswith("rtm-batch-"):
                try:
                    idx = int(resp_id.split("-")[-1])
                    if 0 <= idx < len(results):
                        if resp.get("error"):
                            err = resp["error"]
                            results[idx] = get_rpc_error(err.get("code"), err.get("message"))
                        else:
                            results[idx] = resp.get("result")
                except ValueError:
                    pass
        return results

    # Blockchain API
    def getblockchaininfo(self):
        return self.request("getblockchaininfo")

    def getblockcount(self):
        return self.request("getblockcount")

    def getblockhash(self, height):
        return self.request("getblockhash", [height])

    def getblock(self, blockhash, verbosity=1):
        return self.request("getblock", [blockhash, verbosity])

    def getbestblockhash(self):
        return self.request("getbestblockhash")

    # Wallet API
    def getbalance(self):
        return self.request("getbalance")

    def getnewaddress(self, label="", address_type="legacy"):
        return self.request("getnewaddress", [label, address_type])

    def sendtoaddress(self, address, amount, comment="", comment_to="", subtract_fee=False):
        return self.request("sendtoaddress", [address, amount, comment, comment_to, subtract_fee])

    def validateaddress(self, address):
        return self.request("validateaddress", [address])

    def sendmany(self, amounts, minconf=1, comment="", subtract_fee_from=None):
        if subtract_fee_from is None:
            subtract_fee_from = []
        return self.request("sendmany", ["", amounts, minconf, comment, subtract_fee_from])

    # Asset API
    def listassets(self, mine=False):
        return self.request("listassets", [mine])

    def createasset(self, name, amount, options=None):
        if options is None:
            options = {}
        return self.request("createasset", [name, amount, options])

    def sendasset(self, asset_id, amount, address):
        return self.request("sendasset", [asset_id, amount, address])

    # Smartnode API
    def smartnodelist(self):
        return self.request("smartnodelist")

    def smartnode_status(self):
        return self.request("smartnode", ["status"])


class RaptoreumWallet:
    P = 2**256 - 2**32 - 977
    N = 115792089237316195423570985008687907852837564279074904382605163141518161494337
    A = 0
    B = 7
    Gx = 55066263022277343669578718895168534318117789391449992603003453700757405786800
    Gy = 32670510020758816978083085130507043184471273380659244275771129596662263420883
    G = (Gx, Gy)

    @classmethod
    def _inv(cls, a, n):
        if a == 0: return 0
        lm, hm = 1, 0
        low, high = a % n, n
        while low > 1:
            r = high // low
            nm, new = hm - lm * r, high - low * r
            lm, low, hm, high = nm, new, lm, low
        return lm % n

    @classmethod
    def _ec_add(cls, p, q):
        if p is None: return q
        if q is None: return p
        (px, py), (qx, qy) = p, q
        if px == qx and py == qy:
            m = (3 * px * px + cls.A) * cls._inv(2 * py, cls.P)
        else:
            m = (qy - py) * cls._inv(qx - px, cls.P)
        rx = (m * m - px - qx) % cls.P
        ry = (m * (px - rx) - py) % cls.P
        return (rx, ry)

    @classmethod
    def _ec_mul(cls, p, k):
        r = None
        b = p
        while k > 0:
            if k & 1: r = cls._ec_add(r, b)
            b = cls._ec_add(b, b)
            k >>= 1
        return r

    @classmethod
    def generate_private_key(cls):
        while True:
            k_bytes = os.urandom(32)
            k = int.from_bytes(k_bytes, 'big')
            if 0 < k < cls.N:
                return k_bytes

    @classmethod
    def private_key_to_address(cls, private_key_bytes):
        k = int.from_bytes(private_key_bytes, 'big')
        pub_point = cls._ec_mul(cls.G, k)
        prefix = b'\x02' if pub_point[1] % 2 == 0 else b'\x03'
        pub_bytes = prefix + pub_point[0].to_bytes(32, 'big')
        
        # Hash160
        sha = hashlib.sha256(pub_bytes).digest()
        h = hashlib.new('ripemd160')
        h.update(sha)
        h160 = h.digest()
        
        # Raptoreum version byte is 0x3c (60)
        payload = b'\x3c' + h160
        checksum = hashlib.sha256(hashlib.sha256(payload).digest()).digest()[:4]
        
        # Base58Check
        B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
        n = int.from_bytes(payload + checksum, 'big')
        res = []
        while n > 0:
            n, r = divmod(n, 58)
            res.append(B58[r])
        res = ''.join(reversed(res))
        pad = 0
        for x in (payload + checksum):
            if x == 0: pad += 1
            else: break
        return '1' * pad + res

    @classmethod
    def sign_message(cls, private_key_bytes, message_bytes):
        z = int.from_bytes(hashlib.sha256(hashlib.sha256(message_bytes).digest()).digest(), 'big')
        k_priv = int.from_bytes(private_key_bytes, 'big')
        
        import random
        while True:
            k = random.randint(1, cls.N - 1)
            r_point = cls._ec_mul(cls.G, k)
            r = r_point[0] % cls.N
            if r == 0: continue
            s = (cls._inv(k, cls.N) * (z + r * k_priv)) % cls.N
            if s == 0: continue
            if s > cls.N // 2:
                s = cls.N - s
            
            # Format signature as DER-encoded
            r_bytes = r.to_bytes(32, 'big').lstrip(b'\x00')
            if len(r_bytes) == 0: r_bytes = b'\x00'
            elif r_bytes[0] >= 0x80: r_bytes = b'\x00' + r_bytes
            
            s_bytes = s.to_bytes(32, 'big').lstrip(b'\x00')
            if len(s_bytes) == 0: s_bytes = b'\x00'
            elif s_bytes[0] >= 0x80: s_bytes = b'\x00' + s_bytes
            
            der = bytearray()
            der.append(0x30)
            der.append(len(r_bytes) + len(s_bytes) + 4)
            der.append(0x02)
            der.append(len(r_bytes))
            der.extend(r_bytes)
            der.append(0x02)
            der.append(len(s_bytes))
            der.extend(s_bytes)
            return bytes(der)

    @classmethod
    def private_key_to_public_key(cls, private_key_bytes):
        k = int.from_bytes(private_key_bytes, 'big')
        pub_point = cls._ec_mul(cls.G, k)
        prefix = b'\x02' if pub_point[1] % 2 == 0 else b'\x03'
        return prefix + pub_point[0].to_bytes(32, 'big')


class RaptoreumTransactionBuilder:
    def __init__(self):
        self.inputs = []
        self.outputs = []
        self.locktime = 0
        self.version = 1

    def add_input(self, txid, vout, script_pub_key, amount_rtm):
        self.inputs.append({
            'txid': txid,
            'vout': int(vout),
            'script_pub_key': bytes.fromhex(script_pub_key) if isinstance(script_pub_key, str) else script_pub_key,
            'amount': int(float(amount_rtm) * 100000000),
            'script_sig': b''
        })

    def add_output(self, address, amount_rtm):
        hash160 = self._address_to_hash160(address)
        # P2PKH scriptPubKey: OP_DUP OP_HASH160 <hash160> OP_EQUALVERIFY OP_CHECKSIG
        script = b'\x76\xa9\x14' + hash160 + b'\x88\xac'
        self.outputs.append({
            'value': int(float(amount_rtm) * 100000000),
            'script': script
        })

    def _address_to_hash160(self, address):
        B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
        n = 0
        for char in address:
            n = n * 58 + B58.index(char)
        full = n.to_bytes(25, 'big')
        payload = full[:21]
        checksum = full[21:]
        h1 = hashlib.sha256(payload).digest()
        h2 = hashlib.sha256(h1).digest()
        if h2[:4] != checksum:
            raise ValueError("Invalid checksum")
        return payload[1:]

    def _encode_varint(self, n):
        if n < 0xfd:
            return bytes([n])
        elif n <= 0xffff:
            return b'\xfd' + struct.pack('<H', n)
        elif n <= 0xffffffff:
            return b'\xfe' + struct.pack('<I', n)
        else:
            return b'\xff' + struct.pack('<Q', n)

    def serialize(self):
        res = struct.pack('<I', self.version)
        res += self._encode_varint(len(self.inputs))
        for tx_in in self.inputs:
            txid_bytes = bytes.fromhex(tx_in['txid'])[::-1]
            res += txid_bytes
            res += struct.pack('<I', tx_in['vout'])
            res += self._encode_varint(len(tx_in['script_sig']))
            res += tx_in['script_sig']
            res += struct.pack('<I', 0xffffffff)  # sequence
        res += self._encode_varint(len(self.outputs))
        for tx_out in self.outputs:
            res += struct.pack('<Q', tx_out['value'])
            res += self._encode_varint(len(tx_out['script']))
            res += tx_out['script']
        res += struct.pack('<I', self.locktime)
        return res

    def sign(self, private_key_bytes):
        pubkey = RaptoreumWallet.private_key_to_public_key(private_key_bytes)
        
        # Sign each input
        for i in range(len(self.inputs)):
            # Save original inputs script_sig
            original_script_sigs = [tx_in['script_sig'] for tx_in in self.inputs]
            
            # Temporary state for signing input i
            for j in range(len(self.inputs)):
                if j == i:
                    self.inputs[j]['script_sig'] = self.inputs[j]['script_pub_key']
                else:
                    self.inputs[j]['script_sig'] = b''
            
            # Serialize pre-image and append SIGHASH_ALL (0x00000001 in little-endian / big-endian depending on spec, standard is 4 bytes little endian 0x01000000)
            preimage = self.serialize() + b'\x01\x00\x00\x00'
            
            # Sign double SHA256 of the preimage
            sig = RaptoreumWallet.sign_message(private_key_bytes, preimage)
            
            # Append SIGHASH_ALL byte (0x01)
            sig_with_hash = sig + b'\x01'
            
            # Build scriptSig: OP_DATA_SIG <sig> OP_DATA_PUBKEY <pubkey>
            script_sig = bytes([len(sig_with_hash)]) + sig_with_hash + bytes([len(pubkey)]) + pubkey
            
            # Restore script_sigs and save signature for i
            for j in range(len(self.inputs)):
                self.inputs[j]['script_sig'] = original_script_sigs[j]
            self.inputs[i]['script_sig'] = script_sig

    @staticmethod
    def select_inputs(utxos, target_amount_rtm, fee_rate_sat_byte=1):
        target_sat = int(float(target_amount_rtm) * 100000000)
        selected = []
        accumulated = 0
        num_outputs = 2 # standard receiver + change
        
        for utxo in utxos:
            selected.append(utxo)
            accumulated += int(float(utxo['amount']) * 100000000)
            
            # Size estimate: 148 * inputs + 34 * outputs + 10
            size = 148 * len(selected) + 34 * num_outputs + 10
            fee = size * fee_rate_sat_byte
            
            if accumulated >= (target_sat + fee):
                return selected, fee
                
        raise ValueError("Insufficient funds")


class RaptoreumWebSocketClient:
    def __init__(self, url):
        import urllib.parse
        self.url = url
        self.parsed = urllib.parse.urlparse(url)
        self.reader = None
        self.writer = None
        self.connected = False
        self.callbacks = {}

    async def connect(self):
        import ssl
        host = self.parsed.hostname
        port = self.parsed.port or (443 if self.parsed.scheme == 'wss' else 80)
        
        if self.parsed.scheme == 'wss':
            ssl_context = ssl.create_default_context()
            self.reader, self.writer = await asyncio.open_connection(host, port, ssl=ssl_context)
        else:
            self.reader, self.writer = await asyncio.open_connection(host, port)
            
        path = self.parsed.path or '/'
        if self.parsed.query:
            path += '?' + self.parsed.query
            
        handshake = (
            f"GET {path} HTTP/1.1\r\n"
            f"Host: {host}:{port}\r\n"
            f"Upgrade: websocket\r\n"
            f"Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"
            f"Sec-WebSocket-Version: 13\r\n\r\n"
        )
        self.writer.write(handshake.encode())
        await self.writer.drain()
        
        resp = b''
        while b'\r\n\r\n' not in resp:
            chunk = await self.reader.read(1024)
            if not chunk:
                break
            resp += chunk
            
        self.connected = True
        import asyncio as a
        a.create_task(self._listen_loop())

    def on(self, event, callback):
        self.callbacks[event] = callback

    async def _listen_loop(self):
        try:
            while self.connected:
                header = await self.reader.readexactly(2)
                opcode = header[0] & 0x0f
                masked = header[1] & 0x80
                payload_len = header[1] & 0x7f
                
                if payload_len == 126:
                    len_bytes = await self.reader.readexactly(2)
                    payload_len = int.from_bytes(len_bytes, 'big')
                elif payload_len == 127:
                    len_bytes = await self.reader.readexactly(8)
                    payload_len = int.from_bytes(len_bytes, 'big')
                    
                mask = b''
                if masked:
                    mask = await self.reader.readexactly(4)
                    
                payload = await self.reader.readexactly(payload_len)
                
                if masked:
                    payload = bytes(b ^ mask[i % 4] for i, b in enumerate(payload))
                    
                if opcode == 8:
                    break
                elif opcode == 1:
                    text = payload.decode('utf-8')
                    if 'message' in self.callbacks:
                        self.callbacks['message'](text)
        except Exception as e:
            if 'error' in self.callbacks:
                self.callbacks['error'](e)
        finally:
            self.connected = False

    async def close(self):
        self.connected = False
        if self.writer:
            self.writer.close()
            await self.writer.wait_closed()


class RaptoreumZmqListener:
    def __init__(self, host="127.0.0.1", port=28332):
        self.host = host
        self.port = port
        self.running = False

    def start(self, callback):
        try:
            import zmq
        except ImportError:
            raise ImportError("Please install pyzmq to use ZeroMQ listeners: pip install pyzmq")
            
        self.running = True
        context = zmq.Context()
        socket = context.socket(zmq.SUB)
        socket.connect(f"tcp://{self.host}:{self.port}")
        socket.setsockopt_string(zmq.SUBSCRIBE, "rawtx")
        socket.setsockopt_string(zmq.SUBSCRIBE, "rawblock")
        socket.setsockopt_string(zmq.SUBSCRIBE, "hashblock")
        socket.setsockopt_string(zmq.SUBSCRIBE, "hashtx")
        
        while self.running:
            try:
                topic, body, seq = socket.recv_multipart()
                callback(topic.decode('utf-8'), body)
            except Exception:
                break

    def stop(self):
        self.running = False
