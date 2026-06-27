#pragma once
#include <string>

namespace raptoreum {

class RaptoreumClient {
public:
    RaptoreumClient(const std::string& host = "127.0.0.1", int port = 8766,
                    const std::string& user = "", const std::string& password = "",
                    bool use_ssl = false);
    ~RaptoreumClient();

    std::string request(const std::string& method, const std::string& params_json = "[]");

    // Blockchain API
    std::string getblockchaininfo();
    std::string getblockcount();
    std::string getblockhash(int height);
    std::string getblock(const std::string& blockhash, int verbosity = 1);
    std::string getbestblockhash();

    // Wallet API
    std::string getbalance();
    std::string getnewaddress(const std::string& label = "", const std::string& address_type = "legacy");
    std::string sendtoaddress(const std::string& address, double amount);

private:
    std::string host_;
    int port_;
    std::string user_;
    std::string password_;
    bool use_ssl_;
    std::string url_;
};

} // namespace raptoreum
