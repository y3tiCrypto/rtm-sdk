import json
import urllib.request
import urllib.error
import base64

class RaptoreumRPCException(Exception):
    def __init__(self, code, message):
        self.code = code
        self.message = message
        super().__init__(f"RPC Error [{code}]: {message}")

class RaptoreumClient:
    def __init__(self, host="127.0.0.1", port=8766, user="", password="", use_ssl=False):
        self.host = host
        self.port = port
        self.user = user
        self.password = password
        self.use_ssl = use_ssl
        scheme = "https" if use_ssl else "http"
        self.url = f"{scheme}://{host}:{port}/"

    def request(self, method, params=None):
        if params is None:
            params = []
        
        payload = {
            "jsonrpc": "1.0",
            "id": "rtm-sdk-python",
            "method": method,
            "params": params
        }
        
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(self.url, data=data, headers={"Content-Type": "application/json"})
        
        if self.user or self.password:
            auth_str = f"{self.user}:{self.password}".encode("utf-8")
            auth_header = b"Basic " + base64.b64encode(auth_str)
            req.add_header("Authorization", auth_header.decode("utf-8"))
            
        try:
            with urllib.request.urlopen(req, timeout=30) as response:
                resp_data = json.loads(response.read().decode("utf-8"))
                if resp_data.get("error"):
                    err = resp_data["error"]
                    raise RaptoreumRPCException(err.get("code"), err.get("message"))
                return resp_data.get("result")
        except urllib.error.HTTPError as e:
            # RPC node often returns 500 on execution error with details in JSON
            try:
                resp_data = json.loads(e.read().decode("utf-8"))
                if resp_data.get("error"):
                    err = resp_data["error"]
                    raise RaptoreumRPCException(err.get("code"), err.get("message"))
            except Exception:
                pass
            raise Exception(f"HTTP Error {e.code}: {e.reason}")
        except urllib.error.URLError as e:
            raise Exception(f"Network Error: {e.reason}")

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

