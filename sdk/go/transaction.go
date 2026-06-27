package raptoreum

import (
	"bytes"
	"crypto/sha256"
	"encoding/binary"
	"encoding/hex"
	"errors"
	"fmt"
	"math"
	"math/big"
	"strings"
)

type TxInput struct {
	Txid         string
	Vout         uint32
	ScriptPubKey []byte
	Amount       uint64
	ScriptSig    []byte
}

type TxOutput struct {
	Value  uint64
	Script []byte
}

type UTXO struct {
	Txid         string  `json:"txid"`
	Vout         uint32  `json:"vout"`
	Amount       float64 `json:"amount"`
	ScriptPubKey string  `json:"scriptPubKey"`
}

type RaptoreumTransactionBuilder struct {
	Inputs   []*TxInput
	Outputs  []*TxOutput
	Locktime uint32
	Version  uint32
}

func NewRaptoreumTransactionBuilder() *RaptoreumTransactionBuilder {
	return &RaptoreumTransactionBuilder{
		Inputs:   make([]*TxInput, 0),
		Outputs:  make([]*TxOutput, 0),
		Locktime: 0,
		Version:  1,
	}
}

func (tb *RaptoreumTransactionBuilder) AddInput(txid string, vout uint32, scriptPubKey string, amountRtm float64) error {
	spkBytes, err := hex.DecodeString(scriptPubKey)
	if err != nil {
		return err
	}
	tb.Inputs = append(tb.Inputs, &TxInput{
		Txid:         txid,
		Vout:         vout,
		ScriptPubKey: spkBytes,
		Amount:       uint64(math.Round(amountRtm * 100000000)),
		ScriptSig:    make([]byte, 0),
	})
	return nil
}

func (tb *RaptoreumTransactionBuilder) AddOutput(address string, amountRtm float64) error {
	hash160, err := addressToHash160(address)
	if err != nil {
		return err
	}
	// P2PKH scriptPubKey: OP_DUP OP_HASH160 <hash160> OP_EQUALVERIFY OP_CHECKSIG
	script := append([]byte{0x76, 0xa9, 0x14}, hash160...)
	script = append(script, 0x88, 0xac)

	tb.Outputs = append(tb.Outputs, &TxOutput{
		Value:  uint64(math.Round(amountRtm * 100000000)),
		Script: script,
	})
	return nil
}

func addressToHash160(address string) ([]byte, error) {
	B58 := "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
	n := big.NewInt(0)
	fiftyEight := big.NewInt(58)
	for _, char := range address {
		idx := strings.IndexRune(B58, char)
		if idx == -1 {
			return nil, errors.New("invalid address character")
		}
		n.Mul(n, fiftyEight)
		n.Add(n, big.NewInt(int64(idx)))
	}
	bytes := n.Bytes()
	if len(bytes) < 25 {
		padded := make([]byte, 25)
		copy(padded[25-len(bytes):], bytes)
		bytes = padded
	}
	payload := bytes[:21]
	checksum := bytes[21:]

	h1 := sha256.Sum256(payload)
	h2 := sha256.Sum256(h1[:])
	if !bytesEqual(h2[:4], checksum) {
		return nil, errors.New("invalid checksum")
	}
	return payload[1:], nil
}

func bytesEqual(a, b []byte) bool {
	if len(a) != len(b) {
		return false
	}
	for i, v := range a {
		if v != b[i] {
			return false
		}
	}
	return true
}

func encodeVarInt(val int) []byte {
	if val < 0xfd {
		return []byte{byte(val)}
	} else if val <= 0xffff {
		buf := make([]byte, 3)
		buf[0] = 0xfd
		binary.LittleEndian.PutUint16(buf[1:], uint16(val))
		return buf
	} else if val <= 0xffffffff {
		buf := make([]byte, 5)
		buf[0] = 0xfe
		binary.LittleEndian.PutUint32(buf[1:], uint32(val))
		return buf
	} else {
		buf := make([]byte, 9)
		buf[0] = 0xff
		binary.LittleEndian.PutUint64(buf[1:], uint64(val))
		return buf
	}
}

func (tb *RaptoreumTransactionBuilder) Serialize() ([]byte, error) {
	buf := new(bytes.Buffer)

	// Version
	err := binary.Write(buf, binary.LittleEndian, tb.Version)
	if err != nil {
		return nil, err
	}

	// Input Count
	buf.Write(encodeVarInt(len(tb.Inputs)))

	for _, in := range tb.Inputs {
		// Txid (reversed byte order / little endian hex)
		txidBytes, err := hex.DecodeString(in.Txid)
		if err != nil {
			return nil, err
		}
		for i := len(txidBytes) - 1; i >= 0; i-- {
			buf.WriteByte(txidBytes[i])
		}

		// Vout
		err = binary.Write(buf, binary.LittleEndian, in.Vout)
		if err != nil {
			return nil, err
		}

		// ScriptSig size & ScriptSig
		buf.Write(encodeVarInt(len(in.ScriptSig)))
		buf.Write(in.ScriptSig)

		// Sequence (0xffffffff)
		err = binary.Write(buf, binary.LittleEndian, uint32(0xffffffff))
		if err != nil {
			return nil, err
		}
	}

	// Output Count
	buf.Write(encodeVarInt(len(tb.Outputs)))

	for _, out := range tb.Outputs {
		// Value (Satoshis)
		err = binary.Write(buf, binary.LittleEndian, out.Value)
		if err != nil {
			return nil, err
		}

		// ScriptPubKey size & ScriptPubKey
		buf.Write(encodeVarInt(len(out.Script)))
		buf.Write(out.Script)
	}

	// Locktime
	err = binary.Write(buf, binary.LittleEndian, tb.Locktime)
	if err != nil {
		return nil, err
	}

	return buf.Bytes(), nil
}

func (tb *RaptoreumTransactionBuilder) Sign(privateKeyBytes []byte) error {
	wallet := &RaptoreumWallet{}
	
	// Derive compressed public key
	k := new(big.Int).SetBytes(privateKeyBytes)
	pubPoint := ecMul(G, k)
	var prefix byte
	if pubPoint.Y.Bit(0) == 0 {
		prefix = 0x02
	} else {
		prefix = 0x03
	}
	pubBytes := make([]byte, 33)
	pubBytes[0] = prefix
	pubPoint.X.FillBytes(pubBytes[1:])

	for i, in := range tb.Inputs {
		// Store original scriptSigs
		originalSigs := make([][]byte, len(tb.Inputs))
		for j, txIn := range tb.Inputs {
			originalSigs[j] = txIn.ScriptSig
		}

		// Prepare temporary state for signing input i
		for j, txIn := range tb.Inputs {
			if j == i {
				txIn.ScriptSig = txIn.ScriptPubKey
			} else {
				txIn.ScriptSig = make([]byte, 0)
			}
		}

		serialized, err := tb.Serialize()
		if err != nil {
			return err
		}

		// Append SIGHASH_ALL
		preimage := append(serialized, 0x01, 0x00, 0x00, 0x00)

		sig, err := wallet.SignMessage(privateKeyBytes, preimage)
		if err != nil {
			return err
		}

		// Append hash type byte
		sigWithHash := append(sig, 0x01)

		// scriptSig: OP_DATA_SIG <sig> OP_DATA_PUBKEY <pubkey>
		scriptSig := append([]byte{byte(len(sigWithHash))}, sigWithHash...)
		scriptSig = append(scriptSig, byte(len(pubBytes)))
		scriptSig = append(scriptSig, pubBytes...)

		// Restore scriptSigs and assign to i
		for j, txIn := range tb.Inputs {
			txIn.ScriptSig = originalSigs[j]
		}
		in.ScriptSig = scriptSig
	}

	return nil
}

func SelectInputs(utxos []UTXO, targetAmountRtm float64, feeRateSatByte uint64) ([]UTXO, uint64, error) {
	targetSat := uint64(math.Round(targetAmountRtm * 100000000))
	var accumulated uint64
	selected := make([]UTXO, 0)
	numOutputs := uint64(2)

	for _, utxo := range utxos {
		selected = append(selected, utxo)
		accumulated += uint64(math.Round(utxo.Amount * 100000000))

		size := 148*uint64(len(selected)) + 34*numOutputs + 10
		fee := size * feeRateSatByte

		if accumulated >= targetSat+fee {
			return selected, fee, nil
		}
	}

	return nil, 0, errors.New("insufficient funds")
}
