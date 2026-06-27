package org.raptoreum

import com.google.gson.Gson
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.nio.charset.StandardCharsets
import java.time.Duration
import java.util.Base64

class RaptoreumRPCException(val code: Int, message: String) : RuntimeException("RPC Error [$code]: $message")

class RaptoreumClient(
    host: String = "127.0.0.1",
    port: Int = 8766,
    user: String = "",
    password: String = "",
    useSsl: Boolean = false
) {
    private val url = "${if (useSsl) "https" else "http"}://$host:$port/"
    private val authHeader: String?
    private val httpClient: HttpClient = HttpClient.newBuilder()
        .connectTimeout(Duration.ofSeconds(10))
        .build()
    private val gson = Gson()

    init {
        authHeader = if (user.isNotEmpty() && password.isNotEmpty()) {
            val credentials = "$user:$password"
            "Basic " + Base64.getEncoder().encodeToString(credentials.toByteArray(StandardCharsets.UTF_8))
        } else {
            null
        }
    }

    fun request(method: String, vararg params: Any): JsonElement {
        val payload = JsonObject().apply {
            addProperty("jsonrpc", "1.0")
            addProperty("id", "rtm-sdk-kotlin")
            addProperty("method", method)
            add("params", gson.toJsonTree(params))
        }

        val body = gson.toJson(payload)

        val reqBuilder = HttpRequest.newBuilder()
            .uri(URI.create(url))
            .timeout(Duration.ofSeconds(30))
            .header("Content-Type", "application/json")
            .POST(HttpRequest.BodyPublishers.ofString(body))

        authHeader?.let {
            reqBuilder.header("Authorization", it)
        }

        val response = httpClient.send(reqBuilder.build(), HttpResponse.BodyHandlers.ofString())

        if (response.statusCode() != 200 && response.statusCode() != 500) {
            throw RuntimeException("HTTP Error: Received status code ${response.statusCode()}")
        }

        val responseJson = gson.fromJson(response.body(), JsonObject::class.java)
        if (responseJson.has("error") && !responseJson.get("error").isJsonNull) {
            val err = responseJson.getAsJsonObject("error")
            throw RaptoreumRPCException(err.get("code").asInt, err.get("message").asString)
        }

        return responseJson.get("result")
    }

    // Blockchain Helpers
    fun getBlockchainInfo(): JsonElement = request("getblockchaininfo")
    fun getBlockCount(): Int = request("getblockcount").asInt
    fun getBlockHash(height: Int): String = request("getblockhash", height).asString

    // Wallet Helpers
    fun getBalance(): Double = request("getbalance").asDouble
    fun validateAddress(address: String): JsonElement = request("validateaddress", address)
    fun sendMany(amounts: Map<String, Double>, minconf: Int = 1, comment: String = ""): String =
        request("sendmany", "", amounts, minconf, comment).asString

    fun listAssets(mine: Boolean = false): JsonElement = request("listassets", mine)
    fun createAsset(name: String, amount: Double, options: Map<String, Any> = mapOf()): JsonElement = request("createasset", name, amount, options)
    fun sendAsset(assetId: String, amount: Double, address: String): String = request("sendasset", assetId, amount, address).asString
}
