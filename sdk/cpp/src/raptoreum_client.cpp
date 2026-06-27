#include "raptoreum_client.hpp"
#include <curl/curl.h>
#include <sstream>
#include <stdexcept>

namespace raptoreum {

static size_t WriteCallback(void* contents, size_t size, size_t nmemb, void* userp) {
    ((std::string*)userp)->append((char*)contents, size * nmemb);
    return size * nmemb;
}

RaptoreumClient::RaptoreumClient(const std::string& host, int port,
                                 const std::string& user, const std::string& password,
                                 bool use_ssl)
    : host_(host), port_(port), user_(user), password_(password), use_ssl_(use_ssl) {
    curl_global_init(CURL_GLOBAL_DEFAULT);
    std::string scheme = use_ssl_ ? "https" : "http";
    std::stringstream ss;
    ss << scheme << "://" << host_ << ":" << port_ << "/";
    url_ = ss.str();
}

RaptoreumClient::~RaptoreumClient() {
    curl_global_cleanup();
}

std::string RaptoreumClient::request(const std::string& method, const std::string& params_json) {
    CURL* curl = curl_easy_init();
    if (!curl) {
        throw std::runtime_error("Failed to initialize cURL");
    }

    std::string readBuffer;
    struct curl_slist* headers = NULL;
    headers = curl_slist_append(headers, "Content-Type: application/json");

    std::stringstream payload;
    payload << "{\"jsonrpc\":\"1.0\",\"id\":\"rtm-sdk-cpp\",\"method\":\""
            << method << "\",\"params\":" << params_json << "}";
    std::string payload_str = payload.str();

    curl_easy_setopt(curl, CURLOPT_URL, url_.c_str());
    curl_easy_setopt(curl, CURLOPT_POST, 1L);
    curl_easy_setopt(curl, CURLOPT_POSTFIELDS, payload_str.c_str());
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &readBuffer);
    curl_easy_setopt(curl, CURLOPT_TIMEOUT, 30L);

    if (!user_.empty() || !password_.empty()) {
        std::string auth = user_ + ":" + password_;
        curl_easy_setopt(curl, CURLOPT_USERPWD, auth.c_str());
    }

    if (!use_ssl_) {
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 0L);
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 0L);
    }

    CURLcode res = curl_easy_perform(curl);
    long http_code = 0;
    curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &http_code);

    curl_slist_free_all(headers);
    curl_easy_cleanup(curl);

    if (res != CURLE_OK) {
        throw std::runtime_error(std::string("cURL error: ") + curl_easy_strerror(res));
    }

    if (http_code != 200 && http_code != 500) {
        std::stringstream err;
        err << "HTTP error: " << http_code << " response: " << readBuffer;
        throw std::runtime_error(err.str());
    }

    size_t error_pos = readBuffer.find("\"error\":");
    if (error_pos != std::string::npos) {
        size_t null_pos = readBuffer.find("null", error_pos);
        if (null_pos == std::string::npos || null_pos - error_pos > 12) {
            int code = -1;
            std::string msg = "Unknown RPC Error";
            size_t code_pos = readBuffer.find("\"code\":");
            if (code_pos != std::string::npos) {
                code = std::stoi(readBuffer.substr(code_pos + 7));
            }
            size_t msg_pos = readBuffer.find("\"message\":");
            if (msg_pos != std::string::npos) {
                size_t start_quote = readBuffer.find("\"", msg_pos + 10);
                size_t end_quote = readBuffer.find("\"", start_quote + 1);
                if (start_quote != std::string::npos && end_quote != std::string::npos) {
                    msg = readBuffer.substr(start_quote + 1, end_quote - start_quote - 1);
                }
            }
            throw RaptoreumRPCException(code, msg);
        }
    }

    return readBuffer;
}

std::string RaptoreumClient::getblockchaininfo() {
    return request("getblockchaininfo");
}

std::string RaptoreumClient::getblockcount() {
    return request("getblockcount");
}

std::string RaptoreumClient::getblockhash(int height) {
    std::stringstream ss;
    ss << "[" << height << "]";
    return request("getblockhash", ss.str());
}

std::string RaptoreumClient::getblock(const std::string& blockhash, int verbosity) {
    std::stringstream ss;
    ss << "[\"" << blockhash << "\"," << verbosity << "]";
    return request("getblock", ss.str());
}

std::string RaptoreumClient::getbestblockhash() {
    return request("getbestblockhash");
}

std::string RaptoreumClient::getbalance() {
    return request("getbalance");
}

std::string RaptoreumClient::getnewaddress(const std::string& label, const std::string& address_type) {
    std::stringstream ss;
    ss << "[\"" << label << "\",\"" << address_type << "\"]";
    return request("getnewaddress", ss.str());
}

std::string RaptoreumClient::sendtoaddress(const std::string& address, double amount) {
    std::stringstream ss;
    ss << "[\"" << address << "\"," << amount << "]";
    return request("sendtoaddress", ss.str());
}

std::string RaptoreumClient::validateaddress(const std::string& address) {
    std::stringstream ss;
    ss << "[\"" << address << "\"]";
    return request("validateaddress", ss.str());
}

std::string RaptoreumClient::sendmany(const std::string& amounts_json, int minconf, const std::string& comment) {
    std::stringstream ss;
    ss << "[\"\", " << amounts_json << ", " << minconf << ", \"" << comment << "\"]";
    return request("sendmany", ss.str());
}

} // namespace raptoreum
