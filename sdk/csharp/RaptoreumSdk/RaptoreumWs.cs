#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RaptoreumSdk
{
    public class RaptoreumWebSocketClient
    {
        private readonly Uri _uri;
        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;

        public RaptoreumWebSocketClient(string url)
        {
            _uri = new Uri(url);
        }

        public async Task ConnectAsync(Action<string> messageCallback)
        {
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            await _ws.ConnectAsync(_uri, _cts.Token);

            _ = Task.Run(async () =>
            {
                byte[] buffer = new byte[4096];
                try
                {
                    while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                    {
                        var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
                            break;
                        }

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            messageCallback(message);
                        }
                    }
                }
                catch (Exception)
                {
                    // Connection closed
                }
            }, _cts.Token);
        }

        public async Task CloseAsync()
        {
            if (_cts != null) _cts.Cancel();
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}
