#include "raptoreum_client.hpp"
#include <iostream>
#include <cstdlib>

int main() {
    const char* host_env = std::getenv("RTM_RPC_HOST");
    const char* port_env = std::getenv("RTM_RPC_PORT");
    const char* user_env = std::getenv("RTM_RPC_USER");
    const char* pass_env = std::getenv("RTM_RPC_PASS");

    std::string host = host_env ? host_env : "127.0.0.1";
    int port = port_env ? std::atoi(port_env) : 8766;
    std::string user = user_env ? user_env : "rtm_rpc_user";
    std::string pass = pass_env ? pass_env : "rtm_rpc_secure_password_98231";

    std::cout << "Connecting to Raptoreum Node at http://" << host << ":" << port << " (C++)..." << std::endl;
    
    try {
        raptoreum::RaptoreumClient client(host, port, user, pass, false);
        std::string info = client.getblockchaininfo();
        std::cout << "\nConnection Successful!" << std::endl;
        std::cout << "Response:\n" << info << std::endl;
    } catch (const std::exception& e) {
        std::cerr << "\nCould not connect to node: " << e.what() << std::endl;
    }

    return 0;
}
