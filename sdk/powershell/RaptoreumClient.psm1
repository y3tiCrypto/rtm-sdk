function New-RaptoreumClient {
    param (
        [string]$HostName = "127.0.0.1",
        [int]$Port = 8766,
        [string]$User = "",
        [string]$Password = "",
        [bool]$UseSsl = $false
    )

    $scheme = if ($UseSsl) { "https" } else { "http" }
    $url = "$scheme://$HostName:$Port/"

    $headers = @{
        "Content-Type" = "application/json"
    }

    if ($User -ne "" -or $Password -ne "") {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes("$User:$Password")
        $base64 = [Convert]::ToBase64String($bytes)
        $headers.Add("Authorization", "Basic $base64")
    }

    $clientObj = [PSCustomObject]@{
        Url     = $url
        Headers = $headers
    }

    # Attach request method
    $clientObj | Add-Member -MemberType ScriptMethod -Name "Request" -Value {
        param (
            [string]$Method,
            [array]$Params = @()
        )

        $payload = @{
            jsonrpc = "1.0"
            id      = "rtm-sdk-powershell"
            method  = $Method
            params  = $Params
        } | ConvertTo-Json -Depth 5

        try {
            $response = Invoke-RestMethod -Uri $this.Url -Method Post -Headers $this.Headers -Body $payload
            return $response.result
        } catch {
            $stream = $_.Exception.Response.GetResponseStream()
            if ($null -ne $stream) {
                $reader = New-Object System.IO.StreamReader($stream)
                $errBody = $reader.ReadToEnd() | ConvertFrom-Json
                if ($null -ne $errBody.error) {
                    $rpcException = New-Object System.Management.Automation.RuntimeException("RPC Error [$($errBody.error.code)]: $($errBody.error.message)")
                    $rpcException | Add-Member -NotePropertyName "Code" -NotePropertyValue $errBody.error.code
                    $rpcException | Add-Member -NotePropertyName "RPCMessage" -NotePropertyValue $errBody.error.message
                    throw $rpcException
                }
            }
            throw $_
        }
    }

    # Attach wrapper methods
    $clientObj | Add-Member -MemberType ScriptMethod -Name "GetBlockchainInfo" -Value {
        return $this.Request("getblockchaininfo")
    }

    $clientObj | Add-Member -MemberType ScriptMethod -Name "GetBlockCount" -Value {
        return $this.Request("getblockcount")
    }

    $clientObj | Add-Member -MemberType ScriptMethod -Name "GetBalance" -Value {
        return $this.Request("getbalance")
    }

    $clientObj | Add-Member -MemberType ScriptMethod -Name "ValidateAddress" -Value {
        param([string]$Address)
        return $this.Request("validateaddress", @($Address))
    }

    $clientObj | Add-Member -MemberType ScriptMethod -Name "SendMany" -Value {
        param(
            [hashtable]$Amounts,
            [int]$MinConf = 1,
            [string]$Comment = ""
        )
        return $this.Request("sendmany", @("", $Amounts, $MinConf, $Comment))
    }

    return $clientObj
}

Export-ModuleMember -Function New-RaptoreumClient
