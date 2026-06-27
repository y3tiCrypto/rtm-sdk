import { RaptoreumClient } from './index';

async function main() {
  const host = process.env.RTM_RPC_HOST || '127.0.0.1';
  const port = parseInt(process.env.RTM_RPC_PORT || '8766', 10);
  const user = process.env.RTM_RPC_USER || 'rtm_rpc_user';
  const password = process.env.RTM_RPC_PASS || 'rtm_rpc_secure_password_98231';

  console.log(`Connecting to Raptoreum Node at http://${host}:${port} (TypeScript)...`);
  const client = new RaptoreumClient({ host, port, user, password });

  try {
    const info = await client.getBlockchainInfo();
    console.log('\nConnection Successful!');
    console.log(`Chain: ${info.chain}`);
    console.log(`Blocks: ${info.blocks}`);
  } catch (error: any) {
    console.error('\nCould not connect to node:', error.message);
  }
}

main();
