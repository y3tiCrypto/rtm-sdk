module Raptoreum

using HTTP
using JSON3
using Base64

export RaptoreumClient, request, getblockchaininfo, getblockcount, getbalance

struct RaptoreumClient
    url::String
    headers::Vector{Pair{String, String}}
end

function RaptoreumClient(host::String="127.0.0.1", port::Int=8766, user::String="", password::String="", use_ssl::Bool=false)
    scheme = use_ssl ? "https" : "http"
    url = "$scheme://$host:$port/"
    
    headers = ["Content-Type" => "application/json"]
    if !isempty(user) || !isempty(password)
        auth = base64encode("$user:$password")
        push!(headers, "Authorization" => "Basic $auth")
    end
    
    return RaptoreumClient(url, headers)
end

function request(client::RaptoreumClient, method::String, params::Vector=Any[])
    payload = Dict(
        "jsonrpc" => "1.0",
        "id" => "rtm-sdk-julia",
        "method" => method,
        "params" => params
    )
    
    body = JSON3.write(payload)
    
    try
        response = HTTP.post(client.url, client.headers, body, status_exception=false)
        if response.status != 200 && response.status != 500
            error("HTTP Error: Received status code $(response.status)")
        end
        
        parsed = JSON3.read(String(response.body))
        if haskey(parsed, :error) && parsed.error !== nothing
            error("RPC Error [$(parsed.error.code)]: $(parsed.error.message)")
        end
        
        return parsed.result
    catch e
        rethrow(e)
    end
end

getblockchaininfo(client::RaptoreumClient) = request(client, "getblockchaininfo")
getblockcount(client::RaptoreumClient) = request(client, "getblockcount")
getbalance(client::RaptoreumClient) = request(client, "getbalance")

end # module
