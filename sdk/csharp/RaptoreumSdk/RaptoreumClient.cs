#nullable enable
using System;
using System.Collections.Generic;
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

        public static RaptoreumRPCException Create(int code, string message)
        {
            switch (code)
            {
                case -5:
                    return new InvalidAddressException(code, message);
                case -6:
                    return new InsufficientFundsException(code, message);
                case -13:
                    return new WalletLockedException(code, message);
                case -28:
                    return new NodeWarmingUpException(code, message);
                default:
                    return new RaptoreumRPCException(code, message);
            }
        }
    }

    public class InvalidAddressException : RaptoreumRPCException
    {
        public InvalidAddressException(int code, string message) : base(code, message) { }
    }

    public class InsufficientFundsException : RaptoreumRPCException
    {
        public InsufficientFundsException(int code, string message) : base(code, message) { }
    }

    public class WalletLockedException : RaptoreumRPCException
    {
        public WalletLockedException(int code, string message) : base(code, message) { }
    }

    public class NodeWarmingUpException : RaptoreumRPCException
    {
        public NodeWarmingUpException(int code, string message) : base(code, message) { }
    }

    public class RaptoreumClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;

        public RaptoreumClient(string host = "127.0.0.1", int port = 8766, string user = "", string password = "", bool useSsl = false)
        {
            var scheme = useSsl ? "https" : "http";
            _baseUrl = $"{scheme}://{host}:{port}/";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");

            if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password))
            {
                var authBytes = Encoding.ASCII.GetBytes($"{user}:{password}");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            }
        }

        public async Task<JsonElement> PostRawAsync(object payload)
        {
            var jsonPayload = JsonSerializer.Serialize(payload);
            Exception lastException = new Exception("Request failed");

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_baseUrl, content);

                    if ((int)response.StatusCode == 429)
                    {
                        throw new HttpRequestException("HTTP Error 429: Too Many Requests");
                    }

                    if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.InternalServerError)
                    {
                        throw new HttpRequestException($"HTTP error status code: {response.StatusCode}");
                    }

                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    return doc.RootElement.Clone();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt == MaxRetries) break;

                    // Exponential backoff with jitter
                    var delay = RetryDelayMilliseconds * Math.Pow(2, attempt) + new Random().Next(0, 500);
                    await Task.Delay((int)delay);
                }
            }
            throw lastException;
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

            var root = await PostRawAsync(payload);

            if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null)
            {
                var code = errorProp.GetProperty("code").GetInt32();
                var message = errorProp.GetProperty("message").GetString() ?? "Unknown RPC Error";
                throw RaptoreumRPCException.Create(code, message);
            }

            return root.GetProperty("result").Clone();
        }

        public RaptoreumBatch CreateBatch()
        {
            return new RaptoreumBatch(this);
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
        public async Task<JsonElement> ListAssetsAsync(bool mine = false) => await RequestAsync("listassets", mine);
        public async Task<JsonElement> CreateAssetAsync(string name, double amount, object? options = null) => await RequestAsync("createasset", name, amount, options ?? new { });
        public async Task<JsonElement> SendAssetAsync(string assetId, double amount, string address) => await RequestAsync("sendasset", assetId, amount, address);
    }

    public class RaptoreumBatch
    {
        private readonly RaptoreumClient _client;
        private readonly List<object> _requests = new List<object>();

        public RaptoreumBatch(RaptoreumClient client)
        {
            _client = client;
        }

        public void Add(string method, params object[] parameters)
        {
            _requests.Add(new
            {
                jsonrpc = "1.0",
                id = $"rtm-batch-{_requests.Count}",
                method = method,
                @params = parameters ?? new object[] { }
            });
        }

        public async Task<List<object?>> ExecuteAsync()
        {
            if (_requests.Count == 0) return new List<object?>();

            var root = await _client.PostRawAsync(_requests);
            if (root.ValueKind != JsonValueKind.Array)
            {
                if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null)
                {
                    var code = errorProp.GetProperty("code").GetInt32();
                    var message = errorProp.GetProperty("message").GetString() ?? "Unknown RPC Error";
                    throw RaptoreumRPCException.Create(code, message);
                }
                throw new InvalidOperationException("Invalid batch response");
            }

            var results = new object?[_requests.Count];
            foreach (var resp in root.EnumerateArray())
            {
                if (resp.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                {
                    var idStr = idProp.GetString();
                    if (idStr != null && idStr.StartsWith("rtm-batch-"))
                    {
                        if (int.TryParse(idStr.Substring("rtm-batch-".Length), out int idx))
                        {
                            if (idx >= 0 && idx < results.Length)
                            {
                                if (resp.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null)
                                {
                                    var code = errorProp.GetProperty("code").GetInt32();
                                    var message = errorProp.GetProperty("message").GetString() ?? "Unknown RPC Error";
                                    results[idx] = RaptoreumRPCException.Create(code, message);
                                }
                                else if (resp.TryGetProperty("result", out var resultProp))
                                {
                                    results[idx] = resultProp.Clone();
                                }
                            }
                        }
                    }
                }
            }
            return new List<object?>(results);
        }
    }
}
