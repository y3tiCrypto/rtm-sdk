package org.raptoreum

fun main() {
    val host = System.getenv("RTM_RPC_HOST") ?: "127.0.0.1"
    val portStr = System.getenv("RTM_RPC_PORT")
    val port = portStr?.toIntOrNull() ?: 8766
    val user = System.getenv("RTM_RPC_USER") ?: "rtm_rpc_user"
    val pass = System.getenv("RTM_RPC_PASS") ?: "rtm_rpc_secure_password_98231"

    println("Connecting to Raptoreum Node at http://$host:$port (Kotlin)...")
    val client = RaptoreumClient(host, port, user, pass, false)

    try {
        val info = client.getBlockchainInfo()
        println("\nConnection Successful!")
        println("Response: $info")
    } catch (e: Exception) {
        println("\nCould not connect to node: ${e.message}")
    }
}
