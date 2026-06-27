pub mod wallet;

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
    error: Option<RaptoreumRPCError>,
}

#[derive(Deserialize, Debug)]
pub struct RaptoreumRPCError {
    pub code: i32,
    pub message: String,
}

impl std::fmt::Display for RaptoreumRPCError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "RPC Error [{}]: {}", self.code, self.message)
    }
}

impl std::error::Error for RaptoreumRPCError {}

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

    pub fn validateaddress(&self, address: &str) -> Result<Value, Box<dyn std::error::Error>> {
        self.request("validateaddress", vec![Value::from(address)])
    }

    pub fn sendmany(&self, amounts: std::collections::HashMap<String, f64>, minconf: i32, comment: &str) -> Result<String, Box<dyn std::error::Error>> {
        let amounts_val = serde_json::to_value(amounts)?;
        let val = self.request("sendmany", vec![
            Value::from(""),
            amounts_val,
            Value::from(minconf),
            Value::from(comment)
        ])?;
        Ok(val.as_str().ok_or("Invalid response type")?.to_string())
    }

    pub fn listassets(&self, mine: bool) -> Result<Value, Box<dyn std::error::Error>> {
        self.request("listassets", vec![Value::from(mine)])
    }

    pub fn createasset(&self, name: &str, amount: f64, options: Value) -> Result<Value, Box<dyn std::error::Error>> {
        let opts = if options.is_null() { Value::Object(serde_json::Map::new()) } else { options };
        self.request("createasset", vec![Value::from(name), Value::from(amount), opts])
    }

    pub fn sendasset(&self, asset_id: &str, amount: f64, address: &str) -> Result<String, Box<dyn std::error::Error>> {
        let val = self.request("sendasset", vec![Value::from(asset_id), Value::from(amount), Value::from(address)])?;
        Ok(val.as_str().ok_or("Invalid response type")?.to_string())
    }
}
