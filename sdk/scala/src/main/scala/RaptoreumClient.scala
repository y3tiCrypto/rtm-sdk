package org.raptoreum

import java.net.URI
import java.net.http.{HttpClient, HttpRequest, HttpResponse}
import java.nio.charset.StandardCharsets
import java.time.Duration
import java.util.Base64

class RaptoreumRPCException(val code: Int, message: String) extends RuntimeException(s"RPC Error [$code]: $message")

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
        "Basic " + Base64.getEncoder().encodeToString(credentials.getBytes(StandardCharsets.UTF_8))
    } else null

    private val httpClient = HttpClient.newBuilder()
        .connectTimeout(Duration.ofSeconds(10))
        .build()

    def request(method: String, params: Any*): String = {
        val paramsJson = params.map {
            case s: String => s""""$s""""
            case m: Map[_, _] => m.map {
                case (k, v: String) => s""""$k":"$v""""
                case (k, v) => s""""$k":$v"""
            }.mkString("{", ",", "}")
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

        val respBody = response.body()
        if (respBody.contains("\"error\":") && !respBody.contains("\"error\":null") && !respBody.contains("\"error\": null")) {
            var code = -1
            var msg = "Unknown RPC Error"
            val codeIndex = respBody.indexOf("\"code\":")
            if (codeIndex != -1) {
                val nextComma = respBody.indexOf(",", codeIndex)
                val codeStr = respBody.substring(codeIndex + 7, if (nextComma != -1) nextComma else respBody.indexOf("}", codeIndex)).trim
                try { code = codeStr.toInt } catch { case _: Exception => }
            }
            val msgIndex = respBody.indexOf("\"message\":")
            if (msgIndex != -1) {
                val startQuote = respBody.indexOf("\"", msgIndex + 10)
                val endQuote = respBody.indexOf("\"", startQuote + 1)
                if (startQuote != -1 && endQuote != -1) {
                    msg = respBody.substring(startQuote + 1, endQuote)
                }
            }
            throw new RaptoreumRPCException(code, msg)
        }

        respBody
    }

    def getBlockchainInfo(): String = request("getblockchaininfo")
    def getBlockCount(): String = request("getblockcount")
    def getBalance(): String = request("getbalance")
    def validateaddress(address: String): String = request("validateaddress", address)
    def sendmany(amounts: Map[String, Double], minconf: Int = 1, comment: String = ""): String = {
        request("sendmany", "", amounts, minconf, comment)
    }
    def listassets(mine: Boolean = false): String = request("listassets", mine)
    def createasset(name: String, amount: Double, options: Map[String, Any] = Map()): String = request("createasset", name, amount, options)
    def sendasset(assetId: String, amount: Double, address: String): String = request("sendasset", assetId, amount, address)
}
