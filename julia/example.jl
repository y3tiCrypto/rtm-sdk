push!(LOAD_PATH, joinpath(@__DIR__, "src"))
using Raptoreum

host = get(ENV, "RTM_RPC_HOST", "127.0.0.1")
port_str = get(ENV, "RTM_RPC_PORT", "8766")
port = parse(Int, port_str)
user = get(ENV, "RTM_RPC_USER", "rtm_rpc_user")
pass = get(ENV, "RTM_RPC_PASS", "rtm_rpc_secure_password_98231")

println("Connecting to Raptoreum Node at http://$host:$port (Julia)...")
client = RaptoreumClient(host, port, user, pass, false)

try
    info = getblockchaininfo(client)
    println("\nConnection Successful!")
    println("Response: $info")
catch e
    println("\nCould not connect to node: $e")
end
