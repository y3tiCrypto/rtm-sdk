package org.raptoreum

object Example {
    def main(args: Array[String]): Unit = {
        val host = sys.env.getOrElse("RTM_RPC_HOST", "127.0.0.1")
        val port = sys.env.get("RTM_RPC_PORT").flatMap(_.toIntOption).getOrElse(8766)
        val user = sys.env.getOrElse("RTM_RPC_USER", "rtm_rpc_user")
        val pass = sys.env.getOrElse("RTM_RPC_PASS", "rtm_rpc_secure_password_98231")

        println(s"Connecting to Raptoreum Node at http://$host:$port (Scala)...")
        val client = new RaptoreumClient(host, port, user, pass, false)

        try {
            val info = client.getBlockchainInfo()
            println("\nConnection Successful!")
            println(s"Response: $info")
        } catch {
            case e: Exception => println(s"\nCould not connect to node: ${e.getMessage}")
        }
    }
}
