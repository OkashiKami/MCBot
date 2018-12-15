using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MCBot
{
    class TCProgram
    {
        static void Main(string[] args) => new TCProgram().RunAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static CommandService service;
        public static IServiceProvider provider;

        private async Task RunAsync()
        {

            Directory.CreateDirectory(Staff.path);
            client = new DiscordSocketClient();
            service = new CommandService();
            provider = new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton(service)
            .BuildServiceProvider();
            var botToken = "NTIyODY4NjIxMzI5MDM5MzY5.DvWqBw.fndExPvur2hmq37YLQkUmTyKEQY";

            // Event Subscription
            client.Log += TCEvents.Log;
            client.UserJoined += TCEvents.AnnounceUserJoinedAsync;
            client.MessageReceived += TCEvents.HandleCommandAsync;
            

            await service.AddModulesAsync(Assembly.GetEntryAssembly());
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            await Task.Delay(-1);
        }

    }
}
