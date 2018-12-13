using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Varvarin.Api
{
    public class Processor
    {
        private readonly Guid _processGuid;
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _messageQueues;

        public Processor(Guid processGuid, ConcurrentDictionary<Guid, ConcurrentQueue<string>> messageQueues)
        {
            _messageQueues = messageQueues;
            _processGuid = processGuid;
        }

        public void Add(WebSocket webSocket)
        {
            var isReading = true;
            var reader = new Thread(() =>
            {
                var buffer = new byte[1024 * 4];
                var result = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();
                while (!result.CloseStatus.HasValue)
                {
                    foreach (var queue in _messageQueues.Values)
                    {
                        queue.Enqueue(Encoding.ASCII.GetString(buffer).Substring(0, result.Count));
                    }
                    result = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter().GetResult();
                }
                isReading = false;
                _messageQueues.Remove(_processGuid, out var removedQueue);
                webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None).GetAwaiter().GetResult();
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
                        webSocket.SendAsync(Encoding.ASCII.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                }
            });
            writer.Start();
            reader.Start();
            reader.Join();
            writer.Join();
        }
    }
}
