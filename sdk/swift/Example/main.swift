import Foundation
import RaptoreumClient

let host = ProcessInfo.processInfo.environment["RTM_RPC_HOST"] ?? "127.0.0.1"
let portStr = ProcessInfo.processInfo.environment["RTM_RPC_PORT"] ?? "8766"
let port = Int(portStr) ?? 8766
let user = ProcessInfo.processInfo.environment["RTM_RPC_USER"] ?? "rtm_rpc_user"
let pass = ProcessInfo.processInfo.environment["RTM_RPC_PASS"] ?? "rtm_rpc_secure_password_98231"

print("Connecting to Raptoreum Node at http://\(host):\(port) (Swift)...")
let client = RaptoreumClient(host: host, port: port, user: user, pass: pass, useSsl: false)

let semaphore = DispatchSemaphore(value: 0)

client.request(method: "getblockchaininfo", params: []) { (result: Result<[String: AnyCodable], Error>) in
    switch result {
    case .success(let info):
        print("\nConnection Successful!")
        if let chain = info["chain"]?.value as? String,
           let blocks = info["blocks"]?.value as? Int {
            print("Chain: \(chain)")
            print("Blocks: \(blocks)")
        } else {
            print("Response fields not parsed as expected.")
        }
    case .failure(let error):
        print("\nCould not connect to node: \(error)")
    }
    semaphore.signal()
}

_ = semaphore.wait(timeout: .now() + 30.0)
