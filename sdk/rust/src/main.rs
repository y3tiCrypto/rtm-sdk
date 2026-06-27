use std::env;
use rtm_sdk::RaptoreumClient;

fn main() {
    let host = env::var("RTM_RPC_HOST").unwrap_or_else(|_| "127.0.0.1".to_string());
    let port_str = env::var("RTM_RPC_PORT").unwrap_or_else(|_| "8766".to_string());
    let port = port_str.parse::<u16>().unwrap_or(8766);
    let user = Some(env::var("RTM_RPC_USER").unwrap_or_else(|_| "rtm_rpc_user".to_string()));
    let password = Some(env::var("RTM_RPC_PASS").unwrap_or_else(|_| "rtm_rpc_secure_password_98231".to_string()));

    println!("Connecting to Raptoreum Node at http://{}:{} (Rust)...", host, port);
    let client = RaptoreumClient::new(&host, port, user, password, false);

    match client.getblockchaininfo() {
        Ok(info) => {
            println!("\nConnection Successful!");
            println!("Response: {}", info);
        }
        Err(e) => {
            println!("\nCould not connect to node: {}", e);
        }
    }
}
