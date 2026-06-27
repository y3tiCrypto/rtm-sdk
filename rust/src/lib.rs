use serde::{Deserialize, Serialize};
use serde_json::Value;

pub struct RaptoreumClient {
    url: String,
    user: Option<String>,
    password: Option<String>,
    client: reqwest::blocking::Client,
}

#[derive(Serialize)]
struct RPCRequest<'a> {
    jsonrpc: &'static str,
    id: &'static str,
    method: &'a str,
    params: Vec<Value>,
}

#[derive(Deserialize)]
struct RPCResponse {
    result: Option<Value>,
    error: Option<RPCError>,
}

#[derive(Deserialize, Debug)]
pub struct RPCError {
    pub code: i32,
    pub message: String,
}

impl std::fmt::Display for RPCError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "RPC Error [{}]: {}", self.code, self.message)
    }
}

impl std::error::Error for RPCError {}

impl RaptoreumClient {
    pub fn new(host: &str, port: u16, user: Option<String>, password: Option<String>, use_ssl: bool) -> Self {
        let scheme = if use_ssl { "https" } else { "http" };
        let url = format!("{}://{}:{}/", scheme, host, port);
        
        Self {
            url,
            user,
            password,
            client: reqwest::blocking::Client::new(),
        }
    }

    pub fn request(&self, method: &str, params: Vec<Value>) -> Result<Value, Box<dyn std::error::Error>> {
        let payload = RPCRequest {
            jsonrpc: "1.0",
            id: "rtm-sdk-rust",
            method,
            params,
        };

        let mut req = self.client.post(&self.url)
            .header("Content-Type", "application/json")
            .json(&payload);

        if let (Some(u), Some(p)) = (&self.user, &self.password) {
            req = req.basic_auth(u, Some(p));
        }

        let resp = req.send()?;
        let status = resp.status();
        
        if !status.is_success() && status != reqwest::StatusCode::INTERNAL_SERVER_ERROR {
            return Err(format!("HTTP Error {}", status).into());
        }

        let rpc_resp: RPCResponse = resp.json()?;
        if let Some(err) = rpc_resp.error {
            return Err(Box::new(err));
        }

        Ok(rpc_resp.result.unwrap_or(Value::Null))
    }

    // Blockchain methods
    pub fn getblockchaininfo(&self) -> Result<Value, Box<dyn std::error::Error>> {
        self.request("getblockchaininfo", vec![])
    }

    pub fn getblockcount(&self) -> Result<i64, Box<dyn std::error::Error>> {
        let val = self.request("getblockcount", vec![])?;
        Ok(val.as_i64().ok_or("Invalid response type")?)
    }

    pub fn getblockhash(&self, height: i64) -> Result<String, Box<dyn std::error::Error>> {
        let val = self.request("getblockhash", vec![Value::from(height)])?;
        Ok(val.as_str().ok_or("Invalid response type")?.to_string())
    }

    // Wallet methods
    pub fn getbalance(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let val = self.request("getbalance", vec![])?;
        Ok(val.as_f64().ok_or("Invalid response type")?)
    }
}
