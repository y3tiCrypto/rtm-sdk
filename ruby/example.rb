require_relative 'lib/raptoreum'

host = ENV['RTM_RPC_HOST'] || '127.0.0.1'
port = (ENV['RTM_RPC_PORT'] || '8766').to_i
user = ENV['RTM_RPC_USER'] || 'rtm_rpc_user'
pass = ENV['RTM_RPC_PASS'] || 'rtm_rpc_secure_password_98231'

puts "Connecting to Raptoreum Node at http://#{host}:#{port} (Ruby)..."
client = Raptoreum::Client.new(host: host, port: port, user: user, password: pass)

begin
  info = client.getblockchaininfo
  puts "\nConnection Successful!"
  puts "Chain: #{info['chain']}"
  puts "Blocks: #{info['blocks']}"
rescue => e
  puts "\nCould not connect to node: #{e.message}"
end
