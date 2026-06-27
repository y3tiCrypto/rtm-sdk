using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RaptoreumSdk
{
    public class RaptoreumRPCException : Exception
    {
        public int Code { get; }
        public RaptoreumRPCException(int code, string message) : base($"RPC Error [{code}]: {message}")
        {
            Code = code;
        }
    }

    public class RaptoreumClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RaptoreumClient(string host = "127.0.0.1", int port = 8766, string user = "", string password = "", bool useSsl = false)
        {
            var scheme = useSsl ? "https" : "http";
            _baseUrl = $"{scheme}://{host}:{port}/";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password))
            {
                var authBytes = Encoding.ASCII.GetBytes($"{user}:{password}");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }
        }

        public async Task<JsonElement> RequestAsync(string method, params object[] parameters)
        {
            var payload = new
            {
                jsonrpc = "1.0",
                id = "rtm-sdk-csharp",
                method = method,
                @params = parameters ?? new object[] { }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, content);
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.InternalServerError)
            {
                throw new HttpRequestException($"HTTP error status code: {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement.Clone();

            if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null)
            {
                var code = errorProp.GetProperty("code").GetInt32();
                var message = errorProp.GetProperty("message").GetString() ?? "Unknown RPC Error";
                throw new RaptoreumRPCException(code, message);
            }

            return root.GetProperty("result").Clone();
        }

        // Blockchain API
        public async Task<JsonElement> GetBlockchainInfoAsync() => await RequestAsync("getblockchaininfo");
        public async Task<JsonElement> GetBlockCountAsync() => await RequestAsync("getblockcount");
        public async Task<JsonElement> GetBlockHashAsync(int height) => await RequestAsync("getblockhash", height);
        public async Task<JsonElement> GetBlockAsync(string hash, int verbosity = 1) => await RequestAsync("getblock", hash, verbosity);

        // Wallet API
        public async Task<JsonElement> GetBalanceAsync() => await RequestAsync("getbalance");
        public async Task<JsonElement> GetNewAddressAsync(string label = "", string addressType = "legacy") => await RequestAsync("getnewaddress", label, addressType);
        public async Task<JsonElement> ValidateAddressAsync(string address) => await RequestAsync("validateaddress", address);
        public async Task<JsonElement> SendManyAsync(System.Collections.Generic.Dictionary<string, double> amounts, int minconf = 1, string comment = "") => await RequestAsync("sendmany", "", amounts, minconf, comment);
    }
}
