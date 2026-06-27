package rtm

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"time"
)

type Client struct {
	host       string
	port       int
	user       string
	pass       string
	useSsl     bool
	httpClient *http.Client
}

type RPCRequest struct {
	JSONRPC string        `json:"jsonrpc"`
	ID      string        `json:"id"`
	Method  string        `json:"method"`
	Params  []interface{} `json:"params"`
}

type RPCResponse struct {
	Result json.RawMessage    `json:"result"`
	Error  *RaptoreumRPCError `json:"error"`
	ID     string             `json:"id"`
}

type RaptoreumRPCError struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
}

func (e *RaptoreumRPCError) Error() string {
	return fmt.Sprintf("RPC Error [%d]: %s", e.Code, e.Message)
}

func NewClient(host string, port int, user string, pass string, useSsl bool) *Client {
	return &Client{
		host:   host,
		port:   port,
		user:   user,
		pass:   pass,
		useSsl: useSsl,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

func (c *Client) Call(method string, params ...interface{}) (json.RawMessage, error) {
	protocol := "http"
	if c.useSsl {
		protocol = "https"
	}
	url := fmt.Sprintf("%s://%s:%d/", protocol, c.host, c.port)

	reqPayload := RPCRequest{
		JSONRPC: "1.0",
		ID:      "rtm-sdk-go",
		Method:  method,
		Params:  params,
	}

	bodyBytes, err := json.Marshal(reqPayload)
	if err != nil {
		return nil, fmt.Errorf("marshal request error: %w", err)
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(bodyBytes))
	if err != nil {
		return nil, fmt.Errorf("create request error: %w", err)
	}

	req.Header.Set("Content-Type", "application/json")
	if c.user != "" || c.pass != "" {
		req.SetBasicAuth(c.user, c.pass)
	}

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("http execute error: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusInternalServerError {
		return nil, fmt.Errorf("http error: received status %d", resp.StatusCode)
	}

	respBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("read response body error: %w", err)
	}

	var rpcResp RPCResponse
	if err := json.Unmarshal(respBytes, &rpcResp); err != nil {
		return nil, fmt.Errorf("unmarshal response error: %w", err)
	}

	if rpcResp.Error != nil {
		return rpcResp.Error
	}

	return rpcResp.Result, nil
}

// Blockchain Helpers
func (c *Client) GetBlockchainInfo() (json.RawMessage, error) {
	return c.Call("getblockchaininfo")
}

func (c *Client) GetBlockCount() (int, error) {
	resp, err := c.Call("getblockcount")
	if err != nil {
		return 0, err
	}
	var count int
	err = json.Unmarshal(resp, &count)
	return count, err
}

func (c *Client) GetBlockHash(height int) (string, error) {
	resp, err := c.Call("getblockhash", height)
	if err != nil {
		return "", err
	}
	var hash string
	err = json.Unmarshal(resp, &hash)
	return hash, err
}

// Wallet Helpers
func (c *Client) GetBalance() (float64, error) {
	resp, err := c.Call("getbalance")
	if err != nil {
		return 0, err
	}
	var balance float64
	err = json.Unmarshal(resp, &balance)
	return balance, err
}

func (c *Client) GetNewAddress(label string) (string, error) {
	resp, err := c.Call("getnewaddress", label)
	if err != nil {
		return "", err
	}
	var address string
	err = json.Unmarshal(resp, &address)
	return address, err
}

func (c *Client) ValidateAddress(address string) (json.RawMessage, error) {
	return c.Call("validateaddress", address)
}

func (c *Client) SendMany(amounts map[string]float64, minconf int, comment string) (string, error) {
	resp, err := c.Call("sendmany", "", amounts, minconf, comment)
	if err != nil {
		return "", err
	}
	var txid string
	err = json.Unmarshal(resp, &txid)
	return txid, err
}

func (c *Client) ListAssets(mine bool) (json.RawMessage, error) {
	return c.Call("listassets", mine)
}

func (c *Client) CreateAsset(name string, amount float64, options map[string]interface{}) (json.RawMessage, error) {
	if options == nil {
		options = make(map[string]interface{})
	}
	return c.Call("createasset", name, amount, options)
}

func (c *Client) SendAsset(assetId string, amount float64, address string) (string, error) {
	resp, err := c.Call("sendasset", assetId, amount, address)
	if err != nil {
		return "", err
	}
	var txid string
	err = json.Unmarshal(resp, &txid)
	return txid, err
}
