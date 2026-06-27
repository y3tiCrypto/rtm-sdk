#pragma once
#include <string>
#include <vector>
#include <utility>

namespace raptoreum {

struct TxInput {
    std::string txid;
    uint32_t vout;
    std::vector<unsigned char> script_pub_key;
    uint64_t amount;
    std::vector<unsigned char> script_sig;
};

struct TxOutput {
    uint64_t value;
    std::vector<unsigned char> script;
};

struct UTXO {
    std::string txid;
    uint32_t vout;
    double amount;
    std::string script_pub_key;
};

class RaptoreumTransactionBuilder {
public:
    RaptoreumTransactionBuilder();
    void add_input(const std::string& txid, uint32_t vout, const std::string& script_pub_key, double amount_rtm);
    void add_output(const std::string& address, double amount_rtm);
    std::vector<unsigned char> serialize();
    void sign(const std::vector<unsigned char>& private_key_bytes);

    static std::pair<std::vector<UTXO>, uint64_t> select_inputs(const std::vector<UTXO>& utxos, double target_amount_rtm, uint64_t fee_rate_sat_byte = 1);

    std::vector<TxInput> inputs;
    std::vector<TxOutput> outputs;
    uint32_t locktime;
    uint32_t version;
};

} // namespace raptoreum
