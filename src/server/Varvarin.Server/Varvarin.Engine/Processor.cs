using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Varvarin.Engine.Extensions;

namespace Varvarin.Engine
{
    public class Processor
    {
        private readonly Guid _processGuid;
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _messageQueues;
        private readonly WebSocket _webSocket;
        private readonly byte[] _buffer;
        private readonly int _bufferSize;

        public Processor(Guid processGuid, ConcurrentDictionary<Guid, ConcurrentQueue<string>> messageQueues, WebSocket webSocket, int bufferSize)
        {
            _messageQueues = messageQueues;
            _processGuid = processGuid;
            _webSocket = webSocket;
            _buffer = new byte[bufferSize];
            _bufferSize = bufferSize;
        }

        public void Start()
        {
            var isReading = true;
            var reader = new Thread(() =>
            {
                var result = _webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None).GetAwaiter().GetResult();
                while (!result.CloseStatus.HasValue)
                {
                    foreach (var queue in _messageQueues.Values)
                    {
                        queue.Enqueue(_buffer.ConvertToString(result.Count));
                    }
                    result = _webSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), CancellationToken.None).GetAwaiter().GetResult();
                }
                isReading = false;
                _messageQueues.Remove(_processGuid, out var removedQueue);
                _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None).GetAwaiter().GetResult();
            });

            var writer = new Thread(() =>
            {
                while (isReading)
                {
                    var processMessageQueue = _messageQueues.GetValueOrDefault(_processGuid);
                    if (processMessageQueue == null)
                    {
                        continue;
                    }
                    var hasMessage = processMessageQueue.TryDequeue(out var message);
                    if (!hasMessage)
                    {
                        continue;
                    }
                    if(isReading)
                        _webSocket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                }
            });
            writer.Start();
            reader.Start();
            reader.Join();
            writer.Join();
        }
    }
}
