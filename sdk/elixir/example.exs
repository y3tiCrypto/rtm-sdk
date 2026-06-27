# Ensure mix is running if executing standalone
Mix.install([{:jason, "~> 1.3"}])

Code.require_file("lib/raptoreum.ex")

host = System.get_env("RTM_RPC_HOST") || "127.0.0.1"
port_str = System.get_env("RTM_RPC_PORT") || "8766"
port = String.to_integer(port_str)
user = System.get_env("RTM_RPC_USER") || "rtm_rpc_user"
pass = System.get_env("RTM_RPC_PASS") || "rtm_rpc_secure_password_98231"

IO.puts("Connecting to Raptoreum Node at http://#{host}:#{port} (Elixir)...")
client = Raptoreum.Client.new(host, port, user, pass, false)

case Raptoreum.Client.get_blockchain_info(client) do
  {:ok, info} ->
    IO.puts("\nConnection Successful!")
    IO.inspect(info)
  {:error, reason} ->
    IO.puts("\nCould not connect to node: #{reason}")
end
