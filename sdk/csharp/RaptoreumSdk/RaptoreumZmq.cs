#nullable enable
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaptoreumSdk
{
    public class RaptoreumZmqListener
    {
        public string Host { get; }
        public int Port { get; }
        private TcpClient? _client;
        private CancellationTokenSource? _cts;

        public RaptoreumZmqListener(string host = "127.0.0.1", int port = 28332)
        {
            Host = host;
            Port = port;
        }

        public async Task StartAsync(Action<string, byte[]> callback)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(Host, Port);
            _cts = new CancellationTokenSource();

            var stream = _client.GetStream();

            // ZMTP 3.0 Signature
            byte[] sig = new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0x7f };
            await stream.WriteAsync(sig, 0, sig.Length);

            // ZMTP 3.0 Details
            byte[] details = new byte[24];
            details[0] = 0x03;
            details[1] = 0x00;
            details[2] = (byte)'N';
            details[3] = (byte)'U';
            details[4] = (byte)'L';
            details[5] = (byte)'L';
            details[23] = 0x00;

            await stream.WriteAsync(details, 0, details.Length);

            byte[] greeting = new byte[64];
            int read = 0;
            while (read < 64)
            {
                int r = await stream.ReadAsync(greeting, read, 64 - read);
                if (r <= 0) throw new IOException("Connection closed during greeting");
                read += r;
            }

            // READY command SUB
            byte[] ready = new byte[] {
                0x04,
                20,
                5, (byte)'R', (byte)'E', (byte)'A', (byte)'D', (byte)'Y',
                11, (byte)'S', (byte)'o', (byte)'c', (byte)'k', (byte)'e', (byte)'t', (byte)'-', (byte)'T', (byte)'y', (byte)'p', (byte)'e',
                0, 3, (byte)'S', (byte)'U', (byte)'B'
            };
            await stream.WriteAsync(ready, 0, ready.Length);

            // Subscribe messages
            string[] topics = new string[] { "rawtx", "rawblock", "hashblock", "hashtx" };
            foreach (var topic in topics)
            {
                byte[] topicBytes = Encoding.UTF8.GetBytes(topic);
                byte[] payload = new byte[1 + topicBytes.Length];
                payload[0] = 0x01;
                Array.Copy(topicBytes, 0, payload, 1, topicBytes.Length);

                byte[] subCmd = new byte[2 + payload.Length];
                subCmd[0] = 0x00;
                subCmd[1] = (byte)payload.Length;
                Array.Copy(payload, 0, subCmd, 2, payload.Length);

                await stream.WriteAsync(subCmd, 0, subCmd.Length);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && _client.Connected)
                    {
                        byte[] flagBuf = new byte[1];
                        if (await ReadExactAsync(stream, flagBuf) <= 0) break;
                        byte flags = flagBuf[0];

                        ulong length;
                        if ((flags & 0x02) != 0)
                        {
                            byte[] lenBuf = new byte[8];
                            await ReadExactAsync(stream, lenBuf);
                            if (BitConverter.IsLittleEndian) Array.Reverse(lenBuf);
                            length = BitConverter.ToUInt64(lenBuf, 0);
                        }
                        else
                        {
                            byte[] lenBuf = new byte[1];
                            await ReadExactAsync(stream, lenBuf);
                            length = lenBuf[0];
                        }

                        byte[] payload = new byte[length];
                        await ReadExactAsync(stream, payload);

                        if ((flags & 0x01) != 0)
                        {
                            string topicStr = Encoding.UTF8.GetString(payload);

                            byte[] nextFlagBuf = new byte[1];
                            await ReadExactAsync(stream, nextFlagBuf);
                            byte nextFlags = nextFlagBuf[0];

                            ulong nextLen;
                            if ((nextFlags & 0x02) != 0)
                            {
                                byte[] lenBuf = new byte[8];
                                await ReadExactAsync(stream, lenBuf);
                                if (BitConverter.IsLittleEndian) Array.Reverse(lenBuf);
                                nextLen = BitConverter.ToUInt64(lenBuf, 0);
                            }
                            else
                            {
                                byte[] lenBuf = new byte[1];
                                await ReadExactAsync(stream, lenBuf);
                                nextLen = lenBuf[0];
                            }

                            byte[] body = new byte[nextLen];
                            await ReadExactAsync(stream, body);

                            callback(topicStr, body);

                            if ((nextFlags & 0x01) != 0)
                            {
                                byte[] thirdFlagBuf = new byte[1];
                                await ReadExactAsync(stream, thirdFlagBuf);
                                byte thirdFlags = thirdFlagBuf[0];

                                ulong thirdLen;
                                if ((thirdFlags & 0x02) != 0)
                                {
                                    byte[] lenBuf = new byte[8];
                                    await ReadExactAsync(stream, lenBuf);
                                    if (BitConverter.IsLittleEndian) Array.Reverse(lenBuf);
                                    thirdLen = BitConverter.ToUInt64(lenBuf, 0);
                                }
                                else
                                {
                                    byte[] lenBuf = new byte[1];
                                    await ReadExactAsync(stream, lenBuf);
                                    thirdLen = lenBuf[0];
                                }
                                byte[] thirdPayload = new byte[thirdLen];
                                await ReadExactAsync(stream, thirdPayload);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Error or close
                }
            }, _cts.Token);
        }

        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int r = await stream.ReadAsync(buffer, total, buffer.Length - total);
                if (r <= 0) return 0;
                total += r;
            }
            return total;
        }

        public void Stop()
        {
            if (_cts != null) _cts.Cancel();
            if (_client != null) _client.Close();
        }
    }
}
