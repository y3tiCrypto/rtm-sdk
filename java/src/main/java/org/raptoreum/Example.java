package org.raptoreum;

public class Example {
    public static void main(String[] args) {
        String host = System.getenv("RTM_RPC_HOST");
        if (host == null) host = "127.0.0.1";
        String portStr = System.getenv("RTM_RPC_PORT");
        int port = 8766;
        if (portStr != null) {
            try { port = Integer.parseInt(portStr); } catch (Exception e) {}
        }
        String user = System.getenv("RTM_RPC_USER");
        if (user == null) user = "rtm_rpc_user";
        String pass = System.getenv("RTM_RPC_PASS");
        if (pass == null) pass = "rtm_rpc_secure_password_98231";

        System.out.println("Connecting to Raptoreum Node at http://" + host + ":" + port + " (Java)...");
        RaptoreumClient client = new RaptoreumClient(host, port, user, pass, false);

        try {
            var info = client.getBlockchainInfo();
            System.out.println("\nConnection Successful!");
            System.out.println("Response: " + info.toString());
        } catch (Exception e) {
            System.out.println("\nCould not connect to node: " + e.getMessage());
        }
    }
}
