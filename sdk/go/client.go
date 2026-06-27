package raptoreum

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"math"
	"math/rand"
	"net/http"
	"strings"
	"time"
)

type Client struct {
	host       string
	port       int
	user       string
	pass       string
	useSsl     bool
	httpClient *http.Client
	MaxRetries int
	RetryDelay time.Duration
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
	// Standard transport automatically pools TCP connections (Keep-Alive)
	transport := &http.Transport{
		MaxIdleConns:        32,
		MaxIdleConnsPerHost: 32,
		IdleConnTimeout:     90 * time.Second,
	}

	return &Client{
		host:       host,
		port:       port,
		user:       user,
		pass:       pass,
		useSsl:     useSsl,
		MaxRetries: 3,
		RetryDelay: 1 * time.Second,
		httpClient: &http.Client{
			Transport: transport,
			Timeout:   30 * time.Second,
		},
	}
}

func (c *Client) postRaw(payload interface{}) ([]byte, error) {
	protocol := "http"
	if c.useSsl {
		protocol = "https"
	}
	url := fmt.Sprintf("%s://%s:%d/", protocol, c.host, c.port)

	bodyBytes, err := json.Marshal(payload)
	if err != nil {
		return nil, fmt.Errorf("marshal request error: %w", err)
	}

	var lastErr error
	for attempt := 0; attempt <= c.MaxRetries; attempt++ {
		req, err := http.NewRequest("POST", url, bytes.NewBuffer(bodyBytes))
		if err != nil {
			return nil, fmt.Errorf("create request error: %w", err)
		}

		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("Connection", "keep-alive")
		if c.user != "" || c.pass != "" {
			req.SetBasicAuth(c.user, c.pass)
		}

		resp, err := c.httpClient.Do(req)
		if err != nil {
			lastErr = err
		} else {
			if resp.StatusCode == 429 {
				resp.Body.Close()
				lastErr = fmt.Errorf("HTTP Error 429: Too Many Requests")
			} else if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusInternalServerError {
				resp.Body.Close()
				lastErr = fmt.Errorf("HTTP error: received status %d", resp.StatusCode)
			} else {
				respBytes, err := io.ReadAll(resp.Body)
				resp.Body.Close()
				if err != nil {
					lastErr = err
				} else {
					return respBytes, nil
				}
			}
		}

		if attempt < c.MaxRetries {
			// Exponential backoff + jitter
			backoff := float64(c.RetryDelay) * math.Pow(2, float64(attempt))
			jitter := rand.Float64() * 500.0 * float64(time.Millisecond)
			time.Sleep(time.Duration(backoff) + time.Duration(jitter))
		}
	}

	return nil, fmt.Errorf("post execution failed after retries: %w", lastErr)
}

func (c *Client) Call(method string, params ...interface{}) (json.RawMessage, error) {
	reqPayload := RPCRequest{
		JSONRPC: "1.0",
		ID:      "rtm-sdk-go",
		Method:  method,
		Params:  params,
	}

	respBytes, err := c.postRaw(reqPayload)
	if err != nil {
		return nil, err
	}

	var rpcResp RPCResponse
	if err := json.Unmarshal(respBytes, &rpcResp); err != nil {
		return nil, fmt.Errorf("unmarshal response error: %w", err)
	}

	if rpcResp.Error != nil {
		return nil, rpcResp.Error
	}

	return rpcResp.Result, nil
}

type RaptoreumBatch struct {
	client   *Client
	requests []RPCRequest
}

func (c *Client) CreateBatch() *RaptoreumBatch {
	return &RaptoreumBatch{
		client:   c,
		requests: make([]RPCRequest, 0),
	}
}

func (b *RaptoreumBatch) Add(method string, params ...interface{}) {
	idStr := fmt.Sprintf("rtm-batch-%d", len(b.requests))
	b.requests = append(b.requests, RPCRequest{
		JSONRPC: "1.0",
		ID:      idStr,
		Method:  method,
		Params:  params,
	})
}

func (b *RaptoreumBatch) Execute() ([]interface{}, error) {
	if len(b.requests) == 0 {
		return nil, nil
	}

	respBytes, err := b.client.postRaw(b.requests)
	if err != nil {
		return nil, err
	}

	var rpcResps []RPCResponse
	if err := json.Unmarshal(respBytes, &rpcResps); err != nil {
		// Single error object returned instead of array
		var rpcResp RPCResponse
		if err2 := json.Unmarshal(respBytes, &rpcResp); err2 == nil && rpcResp.Error != nil {
			return nil, rpcResp.Error
		}
		return nil, fmt.Errorf("unmarshal batch response error: %w", err)
	}

	results := make([]interface{}, len(b.requests))
	for _, resp := range rpcResps {
		if strings.HasPrefix(resp.ID, "rtm-batch-") {
			var idx int
			_, err := fmt.Sscanf(resp.ID, "rtm-batch-%d", &idx)
			if err == nil && idx >= 0 && idx < len(results) {
				if resp.Error != nil {
					results[idx] = resp.Error
				} else {
					results[idx] = resp.Result
				}
			}
		}
	}
	return results, nil
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
