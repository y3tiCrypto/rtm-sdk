<?php
namespace Raptoreum;

class RaptoreumClient {
    private $host;
    private $port;
    private $user;
    private $password;
    private $useSsl;
    private $baseUrl;

    public function __construct($host = '127.0.0.1', $port = 8766, $user = '', $password = '', $useSsl = false) {
        $this->host = $host;
        $this->port = $port;
        $this->user = $user;
        $this->password = $password;
        $this->useSsl = $useSsl;
        $this->baseUrl = ($useSsl ? 'https' : 'http') . '://' . $host . ':' . $port . '/';
    }

    public function request($method, $params = []) {
        $payload = json_encode([
            'jsonrpc' => '1.0',
            'id' => 'rtm-sdk-php',
            'method' => $method,
            'params' => $params
        ]);

        $ch = curl_init($this->baseUrl);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_POST, true);
        curl_setopt($ch, CURLOPT_POSTFIELDS, $payload);
        curl_setopt($ch, CURLOPT_HTTPHEADER, [
            'Content-Type: application/json',
            'Content-Length: ' . strlen($payload)
        ]);

        if ($this->user || $this->password) {
            curl_setopt($ch, CURLOPT_USERPWD, $this->user . ":" . $this->password);
        }

        if ($this->useSsl) {
            curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, true);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 2);
        } else {
            curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
            curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
        }

        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $error = curl_error($ch);
        curl_close($ch);

        if ($response === false) {
            throw new \Exception("cURL Error: " . $error);
        }

        if ($httpCode !== 200 && $httpCode !== 500) {
            throw new \Exception("HTTP Error: Received status code " . $httpCode);
        }

        $json = json_decode($response, true);
        if ($json === null) {
            throw new \Exception("JSON Decode Error: Invalid response format");
        }

        if (isset($json['error']) && $json['error'] !== null) {
            throw new \Exception("RPC Error [" . $json['error']['code'] . "]: " . $json['error']['message']);
        }

        return $json['result'];
    }

    // Blockchain API
    public function getBlockchainInfo() { return $this->request('getblockchaininfo'); }
    public function getBlockCount() { return $this->request('getblockcount'); }
    public function getBlockHash($height) { return $this->request('getblockhash', [$height]); }
    public function getBlock($hash, $verbosity = 1) { return $this->request('getblock', [$hash, $verbosity]); }
    public function getBestBlockHash() { return $this->request('getbestblockhash'); }

    // Wallet API
    public function getBalance() { return $this->request('getbalance'); }
    public function getNewAddress($label = '', $addressType = 'legacy') { return $this->request('getnewaddress', [$label, $addressType]); }
    public function sendToAddress($address, $amount, $comment = '', $commentTo = '', $subtractFee = false) {
        return $this->request('sendtoaddress', [$address, $amount, $comment, $commentTo, $subtractFee]);
    }
}
