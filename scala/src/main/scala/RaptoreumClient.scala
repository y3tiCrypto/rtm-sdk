package org.raptoreum

import java.net.URI
import java.net.http.{HttpClient, HttpRequest, HttpResponse}
import java.nio.charset.StandardCharsets
import java.time.Duration
import java.util.Base64

class RaptoreumClient(
    host: String = "127.0.0.1",
    port: Int = 8766,
    user: String = "",
    password: String = "",
    useSsl: Boolean = false
) {
    private val url = s"${if (useSsl) "https" else "http"}://$host:$port/"
    private val authHeader: String = if (user.nonEmpty && password.nonEmpty) {
        val credentials = s"$user:$password"
        "Basic " + Base64.getEncoder.encodeToString(credentials.getBytes(StandardCharsets.UTF_8))
    } else null

    private val httpClient = HttpClient.newBuilder()
        .connectTimeout(Duration.ofSeconds(10))
        .build()

    def request(method: String, params: Any*): String = {
        val paramsJson = params.map {
            case s: String => s""""$s""""
            case other => other.toString
        }.mkString("[", ",", "]")

        val body = s"""{"jsonrpc":"1.0","id":"rtm-sdk-scala","method":"$method","params":$paramsJson}"""

        val reqBuilder = HttpRequest.newBuilder()
            .uri(URI.create(url))
            .timeout(Duration.ofSeconds(30))
            .header("Content-Type", "application/json")
            .POST(HttpRequest.BodyPublishers.ofString(body))

        if (authHeader != null) {
            reqBuilder.header("Authorization", authHeader)
        }

        val response = httpClient.send(reqBuilder.build(), HttpResponse.BodyHandlers.ofString())

        if (response.statusCode() != 200 && response.statusCode() != 500) {
            throw new RuntimeException(s"HTTP Error: Received status code ${response.statusCode()}")
        }

        // Return raw JSON response
        response.body()
    }

    def getBlockchainInfo(): String = request("getblockchaininfo")
    def getBlockCount(): String = request("getblockcount")
    def getBalance(): String = request("getbalance")
}
