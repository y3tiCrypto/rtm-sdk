using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace RaptoreumSdk
{
    public class TxInput
    {
        public string Txid { get; set; } = "";
        public uint Vout { get; set; }
        public byte[] ScriptPubKey { get; set; } = Array.Empty<byte>();
        public ulong Amount { get; set; }
        public byte[] ScriptSig { get; set; } = Array.Empty<byte>();
    }

    public class TxOutput
    {
        public ulong Value { get; set; }
        public byte[] Script { get; set; } = Array.Empty<byte>();
    }

    public class UTXO
    {
        public string txid { get; set; } = "";
        public uint vout { get; set; }
        public double amount { get; set; }
        public string scriptPubKey { get; set; } = "";
    }

    public class RaptoreumTransactionBuilder
    {
        public List<TxInput> Inputs { get; } = new List<TxInput>();
        public List<TxOutput> Outputs { get; } = new List<TxOutput>();
        public uint Locktime { get; set; } = 0;
        public uint Version { get; set; } = 1;

        public void AddInput(string txid, uint vout, string scriptPubKey, double amountRtm)
        {
            Inputs.Add(new TxInput
            {
                Txid = txid,
                Vout = vout,
                ScriptPubKey = HexDecode(scriptPubKey),
                Amount = (ulong)Math.Round(amountRtm * 100000000)
            });
        }

        public void AddOutput(string address, double amountRtm)
        {
            byte[] hash160 = AddressToHash160(address);
            // P2PKH scriptPubKey: OP_DUP OP_HASH160 <hash160> OP_EQUALVERIFY OP_CHECKSIG
            byte[] script = new byte[25];
            script[0] = 0x76;
            script[1] = 0xa9;
            script[2] = 0x14;
            Array.Copy(hash160, 0, script, 3, 20);
            script[23] = 0x88;
            script[24] = 0xac;

            Outputs.Add(new TxOutput
            {
                Value = (ulong)Math.Round(amountRtm * 100000000),
                Script = script
            });
        }

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            // Version
            byte[] verBytes = BitConverter.GetBytes(Version);
            if (!BitConverter.IsLittleEndian) Array.Reverse(verBytes);
            ms.Write(verBytes, 0, 4);

            // Inputs
            byte[] inCount = EncodeVarInt(Inputs.Count);
            ms.Write(inCount, 0, inCount.Length);

            foreach (var input in Inputs)
            {
                byte[] txidBytes = HexDecode(input.Txid);
                Array.Reverse(txidBytes); // Little endian
                ms.Write(txidBytes, 0, txidBytes.Length);

                byte[] voutBytes = BitConverter.GetBytes(input.Vout);
                if (!BitConverter.IsLittleEndian) Array.Reverse(voutBytes);
                ms.Write(voutBytes, 0, 4);

                byte[] scriptSigLen = EncodeVarInt(input.ScriptSig.Length);
                ms.Write(scriptSigLen, 0, scriptSigLen.Length);
                ms.Write(input.ScriptSig, 0, input.ScriptSig.Length);

                byte[] seqBytes = BitConverter.GetBytes(0xffffffff);
                ms.Write(seqBytes, 0, 4);
            }

            // Outputs
            byte[] outCount = EncodeVarInt(Outputs.Count);
            ms.Write(outCount, 0, outCount.Length);

            foreach (var output in Outputs)
            {
                byte[] valBytes = BitConverter.GetBytes(output.Value);
                if (!BitConverter.IsLittleEndian) Array.Reverse(valBytes);
                ms.Write(valBytes, 0, 8);

                byte[] scriptLen = EncodeVarInt(output.Script.Length);
                ms.Write(scriptLen, 0, scriptLen.Length);
                ms.Write(output.Script, 0, output.Script.Length);
            }

            // Locktime
            byte[] ltBytes = BitConverter.GetBytes(Locktime);
            if (!BitConverter.IsLittleEndian) Array.Reverse(ltBytes);
            ms.Write(ltBytes, 0, 4);

            return ms.ToArray();
        }

        public void Sign(byte[] privateKeyBytes)
        {
            // Derive compressed public key
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName("secp256k1"),
                D = privateKeyBytes
            });
            var parameters = ecdsa.ExportParameters(false);
            var qX = parameters.Q.X ?? throw new InvalidOperationException("Invalid X");
            var qY = parameters.Q.Y ?? throw new InvalidOperationException("Invalid Y");

            byte prefix = (qY[qY.Length - 1] % 2 == 0) ? (byte)0x02 : (byte)0x03;
            byte[] pubBytes = new byte[33];
            pubBytes[0] = prefix;
            Array.Copy(qX, 0, pubBytes, 1, 32);

            for (int i = 0; i < Inputs.Count; i++)
            {
                byte[][] originalScriptSigs = new byte[Inputs.Count][];
                for (int j = 0; j < Inputs.Count; j++)
                {
                    originalScriptSigs[j] = Inputs[j].ScriptSig;
                }

                for (int j = 0; j < Inputs.Count; j++)
                {
                    if (j == i)
                    {
                        Inputs[j].ScriptSig = Inputs[j].ScriptPubKey;
                    }
                    else
                    {
                        Inputs[j].ScriptSig = Array.Empty<byte>();
                    }
                }

                byte[] serialized = Serialize();
                byte[] preimage = new byte[serialized.Length + 4];
                Array.Copy(serialized, preimage, serialized.Length);
                preimage[serialized.Length] = 0x01; // SIGHASH_ALL

                byte[] sig = RaptoreumWallet.SignMessage(privateKeyBytes, preimage);
                byte[] sigWithHash = new byte[sig.Length + 1];
                Array.Copy(sig, sigWithHash, sig.Length);
                sigWithHash[sig.Length] = 0x01; // SIGHASH_ALL

                byte[] scriptSig = new byte[1 + sigWithHash.Length + 1 + pubBytes.Length];
                scriptSig[0] = (byte)sigWithHash.Length;
                Array.Copy(sigWithHash, 0, scriptSig, 1, sigWithHash.Length);
                scriptSig[1 + sigWithHash.Length] = (byte)pubBytes.Length;
                Array.Copy(pubBytes, 0, scriptSig, 1 + sigWithHash.Length + 1, pubBytes.Length);

                for (int j = 0; j < Inputs.Count; j++)
                {
                    Inputs[j].ScriptSig = originalScriptSigs[j];
                }
                Inputs[i].ScriptSig = scriptSig;
            }
        }

        public static (List<UTXO> selected, double fee) SelectInputs(List<UTXO> utxos, double targetAmountRtm, uint feeRateSatByte = 1)
        {
            ulong targetSat = (ulong)Math.Round(targetAmountRtm * 100000000);
            ulong accumulated = 0;
            List<UTXO> selected = new List<UTXO>();
            uint numOutputs = 2;

            foreach (var utxo in utxos)
            {
                selected.Add(utxo);
                accumulated += (ulong)Math.Round(utxo.amount * 100000000);

                uint size = 148 * (uint)selected.Count + 34 * numOutputs + 10;
                ulong fee = size * feeRateSatByte;

                if (accumulated >= targetSat + fee)
                {
                    return (selected, (double)fee);
                }
            }

            throw new InvalidOperationException("Insufficient funds");
        }

        private static byte[] EncodeVarInt(long n)
        {
            if (n < 0xfd)
            {
                return new byte[] { (byte)n };
            }
            else if (n <= 0xffff)
            {
                byte[] buf = new byte[3];
                buf[0] = 0xfd;
                byte[] temp = BitConverter.GetBytes((ushort)n);
                if (!BitConverter.IsLittleEndian) Array.Reverse(temp);
                Array.Copy(temp, 0, buf, 1, 2);
                return buf;
            }
            else if (n <= 0xffffffff)
            {
                byte[] buf = new byte[5];
                buf[0] = 0xfe;
                byte[] temp = BitConverter.GetBytes((uint)n);
                if (!BitConverter.IsLittleEndian) Array.Reverse(temp);
                Array.Copy(temp, 0, buf, 1, 4);
                return buf;
            }
            else
            {
                byte[] buf = new byte[9];
                buf[0] = 0xff;
                byte[] temp = BitConverter.GetBytes((ulong)n);
                if (!BitConverter.IsLittleEndian) Array.Reverse(temp);
                Array.Copy(temp, 0, buf, 1, 8);
                return buf;
            }
        }

        private static byte[] AddressToHash160(string address)
        {
            const string B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            System.Numerics.BigInteger n = 0;
            for (int i = 0; i < address.Length; i++)
            {
                int idx = B58.indexOf_custom(address[i]); // wait! custom index helper since .NET Standard 2.0 indexof can be case-sensitive or different
                if (idx == -1) throw new FormatException("Invalid Base58 char");
                n = n * 58 + idx;
            }
            byte[] bytes = n.ToByteArray();
            Array.Reverse(bytes);
            if (bytes.Length > 25 && bytes[0] == 0)
            {
                byte[] temp = new byte[bytes.Length - 1];
                Array.Copy(bytes, 1, temp, 0, temp.Length);
                bytes = temp;
            }
            if (bytes.Length < 25)
            {
                byte[] padded = new byte[25];
                Array.Copy(bytes, 0, padded, 25 - bytes.Length, bytes.Length);
                bytes = padded;
            }
            byte[] payload = new byte[21];
            byte[] checksum = new byte[4];
            Array.Copy(bytes, 0, payload, 0, 21);
            Array.Copy(bytes, 21, checksum, 0, 4);

            using var sha = SHA256.Create();
            byte[] h1 = sha.ComputeHash(payload);
            byte[] h2 = sha.ComputeHash(h1);

            for (int i = 0; i < 4; i++)
            {
                if (h2[i] != checksum[i]) throw new FormatException("Invalid address checksum");
            }

            byte[] hash160 = new byte[20];
            Array.Copy(payload, 1, hash160, 0, 20);
            return hash160;
        }

        private static byte[] HexDecode(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }

    internal static class StringExtensions
    {
        public static int indexOf_custom(this string s, char c)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c) return i;
            }
            return -1;
        }
    }
}
