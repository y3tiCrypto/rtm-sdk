module Raptoreum

using HTTP
using JSON3
using Base64

export RaptoreumClient, RaptoreumRPCException, request, getblockchaininfo, getblockcount, getbalance, validateaddress, sendmany, listassets, createasset, sendasset

struct RaptoreumRPCException <: Exception
    code::Int
    message::String
end

Base.showerror(io::IO, e::RaptoreumRPCException) = print(io, "RPC Error [", e.code, "]: ", e.message)

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
            throw(RaptoreumRPCException(parsed.error.code, parsed.error.message))
        end
        
        return parsed.result
    catch e
        rethrow(e)
    end
end

getblockchaininfo(client::RaptoreumClient) = request(client, "getblockchaininfo")
getblockcount(client::RaptoreumClient) = request(client, "getblockcount")
getbalance(client::RaptoreumClient) = request(client, "getbalance")
validateaddress(client::RaptoreumClient, address::String) = request(client, "validateaddress", [address])
sendmany(client::RaptoreumClient, amounts::Dict{String, Float64}, minconf::Int=1, comment::String="") = request(client, "sendmany", ["", amounts, minconf, comment])
listassets(client::RaptoreumClient, mine::Bool=false) = request(client, "listassets", [mine])
createasset(client::RaptoreumClient, name::String, amount::Float64, options::Dict=Dict()) = request(client, "createasset", [name, amount, options])
sendasset(client::RaptoreumClient, asset_id::String, amount::Float64, address::String) = request(client, "sendasset", [asset_id, amount, address])

end # module
