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

        if result and result:find('"error":') and not result:find('"error":%s*null') then
            local code = -1
            local message = "Unknown RPC Error"
            local code_str = result:match('"code":%s*(-?%d+)')
            if code_str then
                code = tonumber(code_str)
            end
            local message_str = result:match('"message":%s*"([^"]*)"')
            if message_str then
                message = message_str
            end
            error("RPC Error [" .. code .. "]: " .. message)
        end
        
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

    function self:validateaddress(address)
        return self:request("validateaddress", string.format('["%s"]', escape_str(address)))
    end

    function self:sendmany(amounts_table, minconf, comment)
        minconf = minconf or 1
        comment = comment or ""
        local kv = {}
        for k, v in pairs(amounts_table) do
            table.insert(kv, string.format('"%s":%s', escape_str(k), tostring(v)))
        end
        local amounts_json = "{" .. table.concat(kv, ",") .. "}"
        local params = string.format('["",%s,%d,"%s"]', amounts_json, minconf, escape_str(comment))
        return self:request("sendmany", params)
    end

    return self
end

return raptoreum
