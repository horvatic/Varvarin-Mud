using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Varvarin.Engine;

namespace Varvarin.Api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            var messageQueues = new ConcurrentDictionary<Guid, ConcurrentQueue<string>>();
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var guid = Guid.NewGuid();
                    messageQueues.GetOrAdd(guid, new ConcurrentQueue<string>());
                    new Processor(guid, messageQueues, await context.WebSockets.AcceptWebSocketAsync(), 4 * 1024).Start(); ;
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });
        }
    }
}
