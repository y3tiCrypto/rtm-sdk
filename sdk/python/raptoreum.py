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
