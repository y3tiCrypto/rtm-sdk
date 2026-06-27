using System;
using System.Security.Cryptography;

namespace RaptoreumSdk
{
    public class RaptoreumWallet
    {
        private static byte[] HashSHA256(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        public static byte[] GeneratePrivateKey()
        {
            using var ecdsa = ECDsa.Create(ECCurve.CreateFromFriendlyName("secp256k1"));
            var parameters = ecdsa.ExportParameters(true);
            return parameters.D ?? throw new InvalidOperationException("Failed to export D");
        }

        public static string PrivateKeyToAddress(byte[] privateKeyBytes)
        {
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

            byte[] sha = HashSHA256(pubBytes);
            byte[] h160 = PureRipemd160.ComputeHash(sha);

            // Raptoreum version byte is 0x3c (60)
            byte[] payload = new byte[21];
            payload[0] = 0x3c;
            Array.Copy(h160, 0, payload, 1, 20);

            byte[] hash1 = HashSHA256(payload);
            byte[] hash2 = HashSHA256(hash1);
            byte[] checksum = new byte[4];
            Array.Copy(hash2, 0, checksum, 0, 4);

            byte[] fullPayload = new byte[25];
            Array.Copy(payload, 0, fullPayload, 0, 21);
            Array.Copy(checksum, 0, fullPayload, 21, 4);

            return Base58Encode(fullPayload);
        }

        public static byte[] SignMessage(byte[] privateKeyBytes, byte[] messageBytes)
        {
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.CreateFromFriendlyName("secp256k1"),
                D = privateKeyBytes
            });
            byte[] hash1 = HashSHA256(messageBytes);
            byte[] hash2 = HashSHA256(hash1);

            return ecdsa.SignHash(hash2);
        }

        private static string Base58Encode(byte[] data)
        {
            const string B58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            System.Numerics.BigInteger n = 0;
            for (int i = 0; i < data.Length; i++)
            {
                n = n * 256 + data[i];
            }
            string result = "";
            while (n > 0)
            {
                n = System.Numerics.BigInteger.DivRem(n, 58, out var rem);
                result = B58[(int)rem] + result;
            }
            int pad = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0) pad++;
                else break;
            }
            return new string('1', pad) + result;
        }
    }

    internal static class PureRipemd160
    {
        private static readonly uint[] r = new uint[] {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            7, 4, 13, 1, 10, 6, 15, 3, 12, 0, 9, 5, 2, 14, 11, 8,
            3, 10, 14, 4, 9, 15, 8, 1, 2, 7, 0, 6, 13, 11, 5, 12,
            1, 9, 11, 10, 0, 8, 12, 4, 13, 3, 7, 15, 14, 5, 6, 2,
            4, 0, 5, 9, 7, 12, 2, 10, 14, 1, 3, 8, 11, 6, 15, 13
        };

        private static readonly uint[] rp = new uint[] {
            5, 14, 7, 0, 9, 2, 11, 4, 13, 6, 15, 8, 1, 10, 3, 12,
            6, 11, 3, 7, 0, 13, 5, 10, 14, 15, 8, 12, 4, 9, 1, 2,
            15, 5, 1, 3, 7, 14, 6, 9, 11, 8, 12, 2, 10, 0, 4, 13,
            8, 6, 4, 1, 3, 11, 15, 0, 5, 12, 2, 13, 9, 7, 10, 14,
            12, 15, 10, 4, 1, 5, 8, 7, 6, 2, 13, 14, 0, 3, 9, 11
        };

        private static readonly int[] s = new int[] {
            11, 14, 15, 12, 5, 8, 7, 9, 11, 13, 14, 15, 6, 7, 9, 8,
            7, 6, 8, 13, 11, 9, 7, 15, 7, 12, 15, 9, 11, 7, 13, 12,
            11, 13, 6, 7, 14, 9, 13, 15, 14, 8, 13, 6, 5, 12, 7, 5,
            11, 12, 14, 15, 14, 15, 9, 8, 9, 14, 5, 6, 8, 6, 5, 12,
            9, 15, 5, 11, 6, 8, 13, 12, 5, 12, 13, 14, 11, 8, 5, 6
        };

        private static readonly int[] sp = new int[] {
            8, 9, 9, 11, 13, 15, 15, 5, 7, 7, 8, 11, 14, 14, 12, 6,
            9, 13, 15, 7, 12, 8, 9, 11, 7, 7, 12, 7, 6, 15, 13, 11,
            9, 7, 15, 11, 8, 6, 6, 14, 12, 13, 5, 14, 13, 13, 7, 5,
            15, 5, 8, 11, 14, 14, 6, 14, 6, 9, 12, 9, 12, 5, 15, 8,
            8, 5, 12, 9, 12, 5, 14, 6, 8, 13, 6, 5, 15, 13, 11, 11
        };

        private static uint Rol(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        public static byte[] ComputeHash(byte[] input)
        {
            uint[] X = new uint[16];
            for (int i = 0; i < 8; i++)
            {
                X[i] = BitConverter.ToUInt32(input, i * 4);
            }
            X[8] = 0x80;
            X[9] = 0; X[10] = 0; X[11] = 0; X[12] = 0; X[13] = 0;
            X[14] = 256;
            X[15] = 0;

            uint h0 = 0x67452301;
            uint h1 = 0xefcdab89;
            uint h2 = 0x98badcfe;
            uint h3 = 0x10325476;
            uint h4 = 0xc3d2e1f0;

            uint A = h0, B = h1, C = h2, D = h3, E = h4;
            uint Ap = h0, Bp = h1, Cp = h2, Dp = h3, Ep = h4;

            for (int j = 0; j < 80; j++)
            {
                uint f = 0, K = 0;
                if (j < 16) {
                    f = B ^ C ^ D;
                    K = 0x00000000;
                } else if (j < 32) {
                    f = (B & C) | (~B & D);
                    K = 0x5a827999;
                } else if (j < 48) {
                    f = (B | ~C) ^ D;
                    K = 0x6ed9eba1;
                } else if (j < 64) {
                    f = (B & D) | (C & ~D);
                    K = 0x8f1bbcdc;
                } else {
                    f = B ^ (C | ~D);
                    K = 0xa6c302ff;
                }

                uint T = Rol(A + f + X[r[j]] + K, s[j]) + E;
                A = E; E = D; D = Rol(C, 10); C = B; B = T;

                uint fp = 0, Kp = 0;
                if (j < 16) {
                    fp = Bp ^ (Cp | ~Dp);
                    Kp = 0x50a28be6;
                } else if (j < 32) {
                    fp = (Bp & Dp) | (Cp & ~Dp);
                    Kp = 0x5c4dd124;
                } else if (j < 48) {
                    fp = (Bp | ~Cp) ^ Dp;
                    Kp = 0x6d703ef3;
                } else if (j < 64) {
                    fp = (Bp & Cp) | (~Bp & Dp);
                    Kp = 0x7a6d76e9;
                } else {
                    fp = Bp ^ Cp ^ Dp;
                    Kp = 0x00000000;
                }

                uint Tp = Rol(Ap + fp + X[rp[j]] + Kp, sp[j]) + Ep;
                Ap = Ep; Ep = Dp; Dp = Rol(Cp, 10); Cp = Bp; Bp = Tp;
            }

            uint temp = h1 + C + Dp;
            h1 = h2 + D + Ep;
            h2 = h3 + E + Ap;
            h3 = h4 + A + Bp;
            h4 = h0 + B + Cp;
            h0 = temp;

            byte[] result = new byte[20];
            Array.Copy(BitConverter.GetBytes(h0), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(h1), 0, result, 4, 4);
            Array.Copy(BitConverter.GetBytes(h2), 0, result, 8, 4);
            Array.Copy(BitConverter.GetBytes(h3), 0, result, 12, 4);
            Array.Copy(BitConverter.GetBytes(h4), 0, result, 16, 4);
            return result;
        }
    }
}
