#include "raptoreum_client.hpp"
#include <curl/curl.h>
#include <sstream>
#include <stdexcept>
#include <openssl/ec.h>
#include <openssl/ecdsa.h>
#include <openssl/obj_mac.h>
#include <openssl/rand.h>
#include <openssl/sha.h>
#include <openssl/ripemd.h>
#include <openssl/bn.h>

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

std::string RaptoreumClient::listassets(bool mine) {
    std::stringstream ss;
    ss << "[" << (mine ? "true" : "false") << "]";
    return request("listassets", ss.str());
}

std::string RaptoreumClient::createasset(const std::string& name, double amount, const std::string& options_json) {
    std::stringstream ss;
    ss << "[\"" << name << "\"," << amount << "," << options_json << "]";
    return request("createasset", ss.str());
}

std::string RaptoreumClient::sendasset(const std::string& asset_id, double amount, const std::string& address) {
    std::stringstream ss;
    ss << "[\"" << asset_id << "\"," << amount << ",\"" << address << "\"]";
    return request("sendasset", ss.str());
}

std::vector<unsigned char> RaptoreumWallet::generate_private_key() {
    std::vector<unsigned char> priv(32);
    EC_KEY* ec_key = EC_KEY_new_by_curve_name(NID_secp256k1);
    if (ec_key) {
        if (EC_KEY_generate_key(ec_key)) {
            const BIGNUM* priv_bn = EC_KEY_get0_private_key(ec_key);
            BN_bn2binpad(priv_bn, priv.data(), 32);
        }
        EC_KEY_free(ec_key);
    }
    return priv;
}

std::string RaptoreumWallet::private_key_to_address(const std::vector<unsigned char>& private_key_bytes) {
    EC_KEY* ec_key = EC_KEY_new_by_curve_name(NID_secp256k1);
    if (!ec_key) return "";

    BIGNUM* priv_bn = BN_bin2bn(private_key_bytes.data(), private_key_bytes.size(), nullptr);
    EC_KEY_set_private_key(ec_key, priv_bn);

    const EC_GROUP* group = EC_KEY_get0_group(ec_key);
    EC_POINT* pub_point = EC_POINT_new(group);
    EC_POINT_mul(group, pub_point, priv_bn, nullptr, nullptr, nullptr);
    EC_KEY_set_public_key(ec_key, pub_point);

    EC_KEY_set_conv_form(ec_key, POINT_CONVERSION_COMPRESSED);
    int pub_len = i2o_ECPublicKey(ec_key, nullptr);
    std::vector<unsigned char> pub_bytes(pub_len);
    unsigned char* p = pub_bytes.data();
    i2o_ECPublicKey(ec_key, &p);

    EC_POINT_free(pub_point);
    BN_free(priv_bn);
    EC_KEY_free(ec_key);

    unsigned char sha[SHA256_DIGEST_LENGTH];
    SHA256(pub_bytes.data(), pub_bytes.size(), sha);

    unsigned char r160[RIPEMD160_DIGEST_LENGTH];
    RIPEMD160(sha, SHA256_DIGEST_LENGTH, r160);

    std::vector<unsigned char> payload;
    payload.push_back(0x3c);
    payload.insert(payload.end(), r160, r160 + RIPEMD160_DIGEST_LENGTH);

    unsigned char hash1[SHA256_DIGEST_LENGTH];
    SHA256(payload.data(), payload.size(), hash1);
    unsigned char hash2[SHA256_DIGEST_LENGTH];
    SHA256(hash1, SHA256_DIGEST_LENGTH, hash2);

    payload.insert(payload.end(), hash2, hash2 + 4);

    const char* B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    BIGNUM* bn = BN_bin2bn(payload.data(), payload.size(), nullptr);
    std::string result = "";
    BIGNUM* bn58 = BN_new();
    BN_set_word(bn58, 58);
    BIGNUM* div = BN_new();
    BIGNUM* rem = BN_new();
    BN_CTX* ctx = BN_CTX_new();

    while (!BN_is_zero(bn)) {
        BN_div(div, rem, bn, bn58, ctx);
        unsigned long r = BN_get_word(rem);
        result = B58[r] + result;
        BN_copy(bn, div);
    }

    BN_free(rem);
    BN_free(div);
    BN_free(bn58);
    BN_free(bn);
    BN_CTX_free(ctx);

    int pad = 0;
    for (size_t i = 0; i < payload.size(); ++i) {
        if (payload[i] == 0) pad++;
        else break;
    }
    return std::string(pad, '1') + result;
}

std::vector<unsigned char> RaptoreumWallet::sign_message(const std::vector<unsigned char>& private_key_bytes, const std::vector<unsigned char>& message_bytes) {
    EC_KEY* ec_key = EC_KEY_new_by_curve_name(NID_secp256k1);
    if (!ec_key) return {};

    BIGNUM* priv_bn = BN_bin2bn(private_key_bytes.data(), private_key_bytes.size(), nullptr);
    EC_KEY_set_private_key(ec_key, priv_bn);

    unsigned char hash1[SHA256_DIGEST_LENGTH];
    SHA256(message_bytes.data(), message_bytes.size(), hash1);
    unsigned char hash2[SHA256_DIGEST_LENGTH];
    SHA256(hash1, SHA256_DIGEST_LENGTH, hash2);

    unsigned int sig_len = ECDSA_size(ec_key);
    std::vector<unsigned char> sig(sig_len);
    if (!ECDSA_sign(0, hash2, SHA256_DIGEST_LENGTH, sig.data(), &sig_len, ec_key)) {
        sig.clear();
    } else {
        sig.resize(sig_len);
    }

    BN_free(priv_bn);
    EC_KEY_free(ec_key);
    return sig;
}

} // namespace raptoreum
