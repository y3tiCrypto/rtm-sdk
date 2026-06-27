Import-Module (Join-Path $PSScriptRoot "RaptoreumClient.psm1") -Force

$hostName = if ($env:RTM_RPC_HOST) { $env:RTM_RPC_HOST } else { "127.0.0.1" }
$port = if ($env:RTM_RPC_PORT) { [int]$env:RTM_RPC_PORT } else { 8766 }
$user = if ($env:RTM_RPC_USER) { $env:RTM_RPC_USER } else { "rtm_rpc_user" }
$pass = if ($env:RTM_RPC_PASS) { $env:RTM_RPC_PASS } else { "rtm_rpc_secure_password_98231" }

Write-Host "Connecting to Raptoreum Node at http://$hostName:$port (PowerShell)..."
$client = New-RaptoreumClient -HostName $hostName -Port $port -User $user -Password $pass

try {
    $info = $client.GetBlockchainInfo()
    Write-Host "`nConnection Successful!"
    $info | Format-List
} catch {
    Write-Host "`nCould not connect to node: $_" -ForegroundColor Red
}
