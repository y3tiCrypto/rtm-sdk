#pragma once
#include <string>
#include <stdexcept>

namespace raptoreum {

class RaptoreumRPCException : public std::runtime_error {
public:
    RaptoreumRPCException(int code, const std::string& message)
        : std::runtime_error("RPC Error [" + std::to_string(code) + "]: " + message), code_(code), message_(message) {}
    
    int code() const { return code_; }
    std::string message() const { return message_; }

private:
    int code_;
    std::string message_;
};

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
    std::string validateaddress(const std::string& address);
    std::string sendmany(const std::string& amounts_json, int minconf = 1, const std::string& comment = "");

private:
    std::string host_;
    int port_;
    std::string user_;
    std::string password_;
    bool use_ssl_;
    std::string url_;
};

} // namespace raptoreum
