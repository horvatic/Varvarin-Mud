using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace Varvarin.Ui
{
    class Process
    {
        public void Start()
        {
            var client = new ClientWebSocket();
            var safeToEnd = true;
            var safeToWrite = true;
            var clientGuid = Guid.NewGuid();
            client.ConnectAsync(new Uri("ws://localhost:5001/ws"), CancellationToken.None).GetAwaiter().GetResult();
            Console.CancelKeyPress += new ConsoleCancelEventHandler((object sender, ConsoleCancelEventArgs args) =>
            {
                while (!safeToEnd) {}
                safeToWrite = false;
                client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).GetAwaiter().GetResult();
            });
            var cnt = 0;
            while (true)
            {
                safeToEnd = false;
                if (!safeToWrite)
                    continue;
                client.SendAsync(Encoding.ASCII.GetBytes("Hello From Client " + cnt + " " + clientGuid), WebSocketMessageType.Text, true, CancellationToken.None).GetAwaiter().GetResult();
                var buffer = new byte[1024 * 4];
                var result = client.ReceiveAsync(buffer, CancellationToken.None).GetAwaiter().GetResult();
                var message = Encoding.ASCII.GetString(buffer).Substring(0, result.Count);
                Console.WriteLine(message);
                cnt++;
                safeToEnd = true;
                Thread.Sleep(1000);
            }
        }
    }
}
