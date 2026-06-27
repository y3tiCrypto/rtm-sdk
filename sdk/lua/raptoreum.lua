local raptoreum = {}

local function escape_str(s)
    return s:gsub('"', '\\"')
end

function raptoreum.new(host, port, user, password, use_ssl)
    local self = {}
    self.host = host or "127.0.0.1"
    self.port = port or 8766
    self.user = user or ""
    self.password = password or ""
    self.use_ssl = use_ssl or false
    
    local scheme = self.use_ssl and "https" or "http"
    self.url = scheme .. "://" .. self.host .. ":" .. self.port .. "/"

    function self:request(method, params_json)
        params_json = params_json or "[]"
        local payload = string.format('{"jsonrpc":"1.0","id":"rtm-sdk-lua","method":"%s","params":%s}', method, params_json)
        
        -- Build curl command
        local cmd = 'curl -s -X POST -H "Content-Type: application/json"'
        if self.user ~= "" or self.password ~= "" then
            cmd = cmd .. string.format(' --user "%s:%s"', escape_str(self.user), escape_str(self.password))
        end
        cmd = cmd .. string.format(' -d \'%s\' \'%s\'', payload, self.url)
        
        local handle = io.popen(cmd)
        if not handle then
            return nil, "Failed to execute curl"
        end
        local result = handle:read("*a")
        handle:close()
        
        return result
    end

    function self:getblockchaininfo()
        return self:request("getblockchaininfo")
    end

    function self:getblockcount()
        return self:request("getblockcount")
    end

    function self:getbalance()
        return self:request("getbalance")
    end

    return self
end

return raptoreum
