using System;
using System.Threading.Tasks;
using RaptoreumSdk;

namespace RaptoreumSdk.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Environment.GetEnvironmentVariable("RTM_RPC_HOST") ?? "127.0.0.1";
            var portStr = Environment.GetEnvironmentVariable("RTM_RPC_PORT") ?? "8766";
            int.TryParse(portStr, out var port);
            var user = Environment.GetEnvironmentVariable("RTM_RPC_USER") ?? "rtm_rpc_user";
            var pass = Environment.GetEnvironmentVariable("RTM_RPC_PASS") ?? "rtm_rpc_secure_password_98231";

            Console.WriteLine($"Connecting to Raptoreum Node at http://{host}:{port} (C#)...");
            var client = new RaptoreumClient(host, port, user, pass, false);

            try
            {
                var info = await client.GetBlockchainInfoAsync();
                Console.WriteLine("\nConnection Successful!");
                Console.WriteLine($"Response: {info}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nCould not connect to node: {ex.Message}");
            }
        }
    }
}
