#include "raptoreum_tx.hpp"
#include "raptoreum_client.hpp"
#include <openssl/sha.h>
#include <openssl/bn.h>
#include <openssl/ec.h>
#include <openssl/obj_mac.h>
#include <sstream>
#include <iomanip>
#include <algorithm>
#include <stdexcept>
#include <cmath>
#include <cstring>

namespace raptoreum {

static std::vector<unsigned char> hex_decode(const std::string& hex) {
    std::vector<unsigned char> bytes;
    for (unsigned int i = 0; i < hex.length(); i += 2) {
        std::string byteString = hex.substr(i, 2);
        unsigned char byte = (unsigned char) strtol(byteString.c_str(), nullptr, 16);
        bytes.push_back(byte);
    }
    return bytes;
}

static std::vector<unsigned char> encode_varint(uint64_t val) {
    std::vector<unsigned char> res;
    if (val < 0xfd) {
        res.push_back((unsigned char)val);
    } else if (val <= 0xffff) {
        res.push_back(0xfd);
        res.push_back((unsigned char)(val & 0xff));
        res.push_back((unsigned char)((val >> 8) & 0xff));
    } else if (val <= 0xffffffff) {
        res.push_back(0xfe);
        for (int i = 0; i < 4; ++i) {
            res.push_back((unsigned char)((val >> (i * 8)) & 0xff));
        }
    } else {
        res.push_back(0xff);
        for (int i = 0; i < 8; ++i) {
            res.push_back((unsigned char)((val >> (i * 8)) & 0xff));
        }
    }
    return res;
}

static std::vector<unsigned char> address_to_hash160(const std::string& address) {
    const char* B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    BIGNUM* bn = BN_new();
    BIGNUM* bn58 = BN_new();
    BN_set_word(bn58, 58);
    BIGNUM* idx_bn = BN_new();
    BN_CTX* ctx = BN_CTX_new();

    BN_zero(bn);

    for (char c : address) {
        const char* p = strchr(B58, c);
        if (!p) {
            BN_free(bn); BN_free(bn58); BN_free(idx_bn); BN_CTX_free(ctx);
            throw std::runtime_error("Invalid Base58 char");
        }
        int idx = p - B58;
        BN_mul(bn, bn, bn58, ctx);
        BN_set_word(idx_bn, idx);
        BN_add(bn, bn, idx_bn);
    }

    int num_bytes = BN_num_bytes(bn);
    std::vector<unsigned char> bytes(num_bytes);
    BN_bn2bin(bn, bytes.data());

    BN_free(bn); BN_free(bn58); BN_free(idx_bn); BN_CTX_free(ctx);

    if (bytes.size() < 25) {
        std::vector<unsigned char> padded(25, 0);
        std::copy(bytes.begin(), bytes.end(), padded.begin() + (25 - bytes.size()));
        bytes = padded;
    }

    std::vector<unsigned char> payload(bytes.begin(), bytes.begin() + 21);
    std::vector<unsigned char> checksum(bytes.begin() + 21, bytes.end());

    unsigned char h1[SHA256_DIGEST_LENGTH];
    SHA256(payload.data(), payload.size(), h1);
    unsigned char h2[SHA256_DIGEST_LENGTH];
    SHA256(h1, SHA256_DIGEST_LENGTH, h2);

    if (std::equal(checksum.begin(), checksum.end(), h2) == false) {
        throw std::runtime_error("Invalid address checksum");
    }

    return std::vector<unsigned char>(payload.begin() + 1, payload.end());
}

RaptoreumTransactionBuilder::RaptoreumTransactionBuilder()
    : locktime(0), version(1) {}

void RaptoreumTransactionBuilder::add_input(const std::string& txid, uint32_t vout, const std::string& script_pub_key, double amount_rtm) {
    TxInput in;
    in.txid = txid;
    in.vout = vout;
    in.script_pub_key = hex_decode(script_pub_key);
    in.amount = (uint64_t)std::round(amount_rtm * 100000000.0);
    inputs.push_back(in);
}

void RaptoreumTransactionBuilder::add_output(const std::string& address, double amount_rtm) {
    std::vector<unsigned char> hash160 = address_to_hash160(address);
    std::vector<unsigned char> script = {0x76, 0xa9, 0x14};
    script.insert(script.end(), hash160.begin(), hash160.end());
    script.push_back(0x88);
    script.push_back(0xac);

    TxOutput out;
    out.value = (uint64_t)std::round(amount_rtm * 100000000.0);
    out.script = script;
    outputs.push_back(out);
}

std::vector<unsigned char> RaptoreumTransactionBuilder::serialize() {
    std::vector<unsigned char> res;
    
    // Version
    for (int i = 0; i < 4; ++i) {
        res.push_back((unsigned char)((version >> (i * 8)) & 0xff));
    }

    // Input Count
    std::vector<unsigned char> in_count_var = encode_varint(inputs.size());
    res.insert(res.end(), in_count_var.begin(), in_count_var.end());

    for (const auto& input : inputs) {
        std::vector<unsigned char> txid_bytes = hex_decode(input.txid);
        std::reverse(txid_bytes.begin(), txid_bytes.end());
        res.insert(res.end(), txid_bytes.begin(), txid_bytes.end());

        for (int i = 0; i < 4; ++i) {
            res.push_back((unsigned char)((input.vout >> (i * 8)) & 0xff));
        }

        std::vector<unsigned char> sig_len_var = encode_varint(input.script_sig.size());
        res.insert(res.end(), sig_len_var.begin(), sig_len_var.end());
        res.insert(res.end(), input.script_sig.begin(), input.script_sig.end());

        for (int i = 0; i < 4; ++i) {
            res.push_back(0xff);
        }
    }

    // Output Count
    std::vector<unsigned char> out_count_var = encode_varint(outputs.size());
    res.insert(res.end(), out_count_var.begin(), out_count_var.end());

    for (const auto& output : outputs) {
        for (int i = 0; i < 8; ++i) {
            res.push_back((unsigned char)((output.value >> (i * 8)) & 0xff));
        }

        std::vector<unsigned char> scr_len_var = encode_varint(output.script.size());
        res.insert(res.end(), scr_len_var.begin(), scr_len_var.end());
        res.insert(res.end(), output.script.begin(), output.script.end());
    }

    // Locktime
    for (int i = 0; i < 4; ++i) {
        res.push_back((unsigned char)((locktime >> (i * 8)) & 0xff));
    }

    return res;
}

void RaptoreumTransactionBuilder::sign(const std::vector<unsigned char>& private_key_bytes) {
    EC_KEY* ec_key = EC_KEY_new_by_curve_name(NID_secp256k1);
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

    for (size_t i = 0; i < inputs.size(); ++i) {
        std::vector<std::vector<unsigned char>> original_sigs;
        for (const auto& in : inputs) {
            original_sigs.push_back(in.script_sig);
        }

        for (size_t j = 0; j < inputs.size(); ++j) {
            if (j == i) {
                inputs[j].script_sig = inputs[j].script_pub_key;
            } else {
                inputs[j].script_sig.clear();
            }
        }

        std::vector<unsigned char> preimage = serialize();
        preimage.push_back(0x01);
        preimage.push_back(0x00);
        preimage.push_back(0x00);
        preimage.push_back(0x00); // SIGHASH_ALL

        std::vector<unsigned char> sig = RaptoreumWallet::sign_message(private_key_bytes, preimage);
        std::vector<unsigned char> sig_with_hash = sig;
        sig_with_hash.push_back(0x01); // SIGHASH_ALL

        std::vector<unsigned char> script_sig;
        script_sig.push_back(sig_with_hash.size());
        script_sig.insert(script_sig.end(), sig_with_hash.begin(), sig_with_hash.end());
        script_sig.push_back(pub_bytes.size());
        script_sig.insert(script_sig.end(), pub_bytes.begin(), pub_bytes.end());

        for (size_t j = 0; j < inputs.size(); ++j) {
            inputs[j].script_sig = original_sigs[j];
        }
        inputs[i].script_sig = script_sig;
    }
}

std::pair<std::vector<UTXO>, uint64_t> RaptoreumTransactionBuilder::select_inputs(const std::vector<UTXO>& utxos, double target_amount_rtm, uint64_t fee_rate_sat_byte) {
    uint64_t target_sat = (uint64_t)std::round(target_amount_rtm * 100000000.0);
    uint64_t accumulated = 0;
    std::vector<UTXO> selected;
    uint64_t num_outputs = 2;

    for (const auto& utxo : utxos) {
        selected.push_back(utxo);
        accumulated += (uint64_t)std::round(utxo.amount * 100000000.0);

        uint64_t size = 148 * selected.size() + 34 * num_outputs + 10;
        uint64_t fee = size * fee_rate_sat_byte;

        if (accumulated >= target_sat + fee) {
            return {selected, fee};
        }
    }

    throw std::runtime_error("Insufficient funds");
}

} // namespace raptoreum
