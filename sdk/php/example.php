<?php
require_once __DIR__ . '/src/RaptoreumClient.php';

use Raptoreum\RaptoreumClient;

$host = getenv('RTM_RPC_HOST') ?: '127.0.0.1';
$port = getenv('RTM_RPC_PORT') ?: 8766;
$user = getenv('RTM_RPC_USER') ?: 'rtm_rpc_user';
$pass = getenv('RTM_RPC_PASS') ?: 'rtm_rpc_secure_password_98231';

echo "Connecting to Raptoreum Node at http://$host:$port (PHP)...\n";
$client = new RaptoreumClient($host, $port, $user, $pass);

try {
    $info = $client->getBlockchainInfo();
    echo "\nConnection Successful!\n";
    echo "Chain: " . $info['chain'] . "\n";
    echo "Blocks: " . $info['blocks'] . "\n";
} catch (\Exception $e) {
    echo "\nCould not connect to node: " . $e->getMessage() . "\n";
}
