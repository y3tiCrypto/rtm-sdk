package org.raptoreum;

import com.google.gson.Gson;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Base64;

public class RaptoreumClient {
    public static class RaptoreumRPCException extends RuntimeException {
        private final int code;
        public RaptoreumRPCException(int code, String message) {
            super("RPC Error [" + code + "]: " + message);
            this.code = code;
        }
        public int getCode() {
            return code;
        }
    }

    private final String url;
    private final String authHeader;
    private final HttpClient httpClient;
    private final Gson gson;

    public RaptoreumClient(String host, int port, String user, String password, boolean useSsl) {
        String scheme = useSsl ? "https" : "http";
        this.url = scheme + "://" + host + ":" + port + "/";
        this.gson = new Gson();
        this.httpClient = HttpClient.newBuilder()
                .connectTimeout(Duration.ofSeconds(10))
                .build();

        if (user != null && !user.isEmpty() && password != null && !password.isEmpty()) {
            String credentials = user + ":" + password;
            this.authHeader = "Basic " + Base64.getEncoder().encodeToString(credentials.getBytes(StandardCharsets.UTF_8));
        } else {
            this.authHeader = null;
        }
    }

    public JsonElement request(String method, Object... params) throws Exception {
        JsonObject payload = new JsonObject();
        payload.addProperty("jsonrpc", "1.0");
        payload.addProperty("id", "rtm-sdk-java");
        payload.addProperty("method", method);
        payload.add("params", gson.toJsonTree(params));

        String body = gson.toJson(payload);

        HttpRequest.Builder reqBuilder = HttpRequest.newBuilder()
                .uri(URI.create(this.url))
                .timeout(Duration.ofSeconds(30))
                .header("Content-Type", "application/json")
                .POST(HttpRequest.BodyPublishers.ofString(body));

        if (this.authHeader != null) {
            reqBuilder.header("Authorization", this.authHeader);
        }

        HttpResponse<String> response = this.httpClient.send(reqBuilder.build(), HttpResponse.BodyHandlers.ofString());

        if (response.statusCode() != 200 && response.statusCode() != 500) {
            throw new RuntimeException("HTTP Error: Received status code " + response.statusCode());
        }

        JsonObject responseJson = gson.fromJson(response.body(), JsonObject.class);
        if (responseJson.has("error") && !responseJson.get("error").isJsonNull()) {
            JsonObject err = responseJson.getAsJsonObject("error");
            throw new RaptoreumRPCException(err.get("code").getAsInt(), err.get("message").getAsString());
        }

        return responseJson.get("result");
    }

    // Blockchain API
    public JsonElement getBlockchainInfo() throws Exception {
        return request("getblockchaininfo");
    }

    public int getBlockCount() throws Exception {
        return request("getblockcount").getAsInt();
    }

    public String getBlockHash(int height) throws Exception {
        return request("getblockhash", height).getAsString();
    }

    // Wallet API
    public double getBalance() throws Exception {
        return request("getbalance").getAsDouble();
    }

    public JsonElement validateAddress(String address) throws Exception {
        return request("validateaddress", address);
    }

    public String sendMany(java.util.Map<String, Double> amounts, int minconf, String comment) throws Exception {
        return request("sendmany", "", amounts, minconf, comment).getAsString();
    }

    public JsonElement listAssets(boolean mine) throws Exception {
        return request("listassets", mine);
    }

    public JsonElement createAsset(String name, double amount, java.util.Map<String, Object> options) throws Exception {
        return request("createasset", name, amount, options != null ? options : new java.util.HashMap<>());
    }

    public String sendAsset(String assetId, double amount, String address) throws Exception {
        return request("sendasset", assetId, amount, address).getAsString();
    }
}
