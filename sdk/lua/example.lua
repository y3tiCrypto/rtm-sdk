local raptoreum = require("raptoreum")

local host = os.getenv("RTM_RPC_HOST") or "127.0.0.1"
local port_str = os.getenv("RTM_RPC_PORT") or "8766"
local port = tonumber(port_str) or 8766
local user = os.getenv("RTM_RPC_USER") or "rtm_rpc_user"
local pass = os.getenv("RTM_RPC_PASS") or "rtm_rpc_secure_password_98231"

print("Connecting to Raptoreum Node at http://" .. host .. ":" .. port .. " (Lua)...")
local client = raptoreum.new(host, port, user, pass, false)

local res, err = client:getblockchaininfo()
if not res then
    print("\nCould not connect: " .. tostring(err))
else
    print("\nConnection Successful!")
    print("Response:\n" .. res)
end
