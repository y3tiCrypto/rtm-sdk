package raptoreum

import (
	"crypto/rand"
	"crypto/sha256"
	"math/big"

	"golang.org/x/crypto/ripemd160"
)

// secp256k1 parameters
var (
	secp256k1_P, _  = new(big.Int).SetString("fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f", 16)
	secp256k1_N, _  = new(big.Int).SetString("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141", 16)
	secp256k1_Gx, _ = new(big.Int).SetString("79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798", 16)
	secp256k1_Gy, _ = new(big.Int).SetString("483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8", 16)
)

type Point struct {
	X, Y *big.Int
}

var G = &Point{secp256k1_Gx, secp256k1_Gy}

func inv(a, n *big.Int) *big.Int {
	return new(big.Int).ModInverse(a, n)
}

func ecAdd(p1, p2 *Point) *Point {
	if p1 == nil {
		return p2
	}
	if p2 == nil {
		return p1
	}
	if p1.X.Cmp(p2.X) == 0 && p1.Y.Cmp(p2.Y) != 0 {
		return nil
	}
	m := new(big.Int)
	if p1.X.Cmp(p2.X) == 0 && p1.Y.Cmp(p2.Y) == 0 {
		num := new(big.Int).Mul(big.NewInt(3), new(big.Int).Mul(p1.X, p1.X))
		den := new(big.Int).Mul(big.NewInt(2), p1.Y)
		m.Mul(num, inv(den, secp256k1_P)).Mod(m, secp256k1_P)
	} else {
		num := new(big.Int).Sub(p2.Y, p1.Y)
		den := new(big.Int).Sub(p2.X, p1.X)
		m.Mul(num, inv(den, secp256k1_P)).Mod(m, secp256k1_P)
	}
	rx := new(big.Int).Mul(m, m)
	rx.Sub(rx, p1.X).Sub(rx, p2.X).Mod(rx, secp256k1_P)

	ry := new(big.Int).Sub(p1.X, rx)
	ry.Mul(m, ry).Sub(ry, p1.Y).Mod(ry, secp256k1_P)

	return &Point{rx, ry}
}

func ecMul(p *Point, k *big.Int) *Point {
	var r *Point
	b := p
	kCopy := new(big.Int).Set(k)
	for kCopy.Sign() > 0 {
		if kCopy.Bit(0) == 1 {
			r = ecAdd(r, b)
		}
		b = ecAdd(b, b)
		kCopy.Rsh(kCopy, 1)
	}
	return r
}

type RaptoreumWallet struct{}

func (w *RaptoreumWallet) GeneratePrivateKey() ([]byte, error) {
	for {
		kBytes := make([]byte, 32)
		_, err := rand.Read(kBytes)
		if err != nil {
			return nil, err
		}
		k := new(big.Int).SetBytes(kBytes)
		if k.Cmp(big.NewInt(0)) > 0 && k.Cmp(secp256k1_N) < 0 {
			return kBytes, nil
		}
	}
}

func (w *RaptoreumWallet) PrivateKeyToAddress(privKey []byte) (string, error) {
	k := new(big.Int).SetBytes(privKey)
	pubPoint := ecMul(G, k)

	// Compressed PubKey format
	var prefix byte
	if pubPoint.Y.Bit(0) == 0 {
		prefix = 0x02
	} else {
		prefix = 0x03
	}

	pubBytes := make([]byte, 33)
	pubBytes[0] = prefix
	pubPoint.X.FillBytes(pubBytes[1:])

	// Hash160
	sha := sha256.Sum256(pubBytes)
	r160 := ripemd160.New()
	r160.Write(sha[:])
	h160 := r160.Sum(nil)

	// Raptoreum version prefix is 0x3c (60)
	payload := append([]byte{0x3c}, h160...)
	hash1 := sha256.Sum256(payload)
	hash2 := sha256.Sum256(hash1[:])
	checksum := hash2[:4]

	fullPayload := append(payload, checksum...)

	// Base58 Check Encoding
	B58 := "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"
	n := new(big.Int).SetBytes(fullPayload)
	var res string
	mod := new(big.Int)
	fiftyEight := big.NewInt(58)
	for n.Sign() > 0 {
		n.DivMod(n, fiftyEight, mod)
		res = string(B58[mod.Int64()]) + res
	}

	pad := 0
	for _, b := range fullPayload {
		if b == 0 {
			pad++
		} else {
			break
		}
	}
	for i := 0; i < pad; i++ {
		res = "1" + res
	}

	return res, nil
}

func (w *RaptoreumWallet) SignMessage(privKey []byte, message []byte) ([]byte, error) {
	h1 := sha256.Sum256(message)
	h2 := sha256.Sum256(h1[:])
	z := new(big.Int).SetBytes(h2[:])

	kPriv := new(big.Int).SetBytes(privKey)

	for {
		kBytes := make([]byte, 32)
		_, err := rand.Read(kBytes)
		if err != nil {
			return nil, err
		}
		k := new(big.Int).SetBytes(kBytes)
		if k.Cmp(big.NewInt(0)) <= 0 || k.Cmp(secp256k1_N) >= 0 {
			continue
		}

		rPoint := ecMul(G, k)
		r := new(big.Int).Mod(rPoint.X, secp256k1_N)
		if r.Sign() == 0 {
			continue
		}

		// s = k^-1 * (z + r * kPriv) mod N
		s := new(big.Int).Mul(r, kPriv)
		s.Add(s, z)
		s.Mul(s, inv(k, secp256k1_N)).Mod(s, secp256k1_N)

		if s.Sign() == 0 {
			continue
		}

		// Low S constraint (BIP-62)
		halfN := new(big.Int).Rsh(secp256k1_N, 1)
		if s.Cmp(halfN) > 0 {
			s.Sub(secp256k1_N, s)
		}

		// DER Encoding
		rBytes := r.Bytes()
		if len(rBytes) > 0 && rBytes[0] >= 0x80 {
			rBytes = append([]byte{0x00}, rBytes...)
		}
		sBytes := s.Bytes()
		if len(sBytes) > 0 && sBytes[0] >= 0x80 {
			sBytes = append([]byte{0x00}, sBytes...)
		}

		der := []byte{0x30, byte(len(rBytes) + len(sBytes) + 4), 0x02, byte(len(rBytes))}
		der = append(der, rBytes...)
		der = append(der, 0x02, byte(len(sBytes)))
		der = append(der, sBytes...)

		return der, nil
	}
}
