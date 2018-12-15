using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCBot
{
    public static class TCEvents
    {
        public static async Task AnnounceUserJoinedAsync(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.DefaultChannel;
            await channel.SendMessageAsync($"Welcome, {user.Mention}");

        }
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = (SocketUserMessage)arg;
            if (msg == null || msg.Author.IsBot) return;

            int argPos = 0;
            if (msg.HasCharPrefix('!', ref argPos) || msg.HasMentionPrefix(TCProgram.client.CurrentUser, ref argPos))
            {
                var contex = new SocketCommandContext(TCProgram.client, msg);

                var result = await TCProgram.service.ExecuteAsync(contex, argPos, TCProgram.provider);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    await contex.Channel.SendMessageAsync(result.ErrorReason);
                    Console.WriteLine($"{contex.User.Username} snet an invalid command, {result.ErrorReason}.");
                }

            }
        }
        public static async Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            await Task.CompletedTask;
        }
    }
}
