using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using GWHLLDiscordVotingTool;

namespace Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;

        private readonly CommandService _commands;

        private readonly IServiceProvider _services;

        private readonly AppSettings _appSettings;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();

            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InstallCommandsAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private Task HandleCommandAsync(SocketMessage messageParam)
        {

            // Quick filtering checks
            if (messageParam is not SocketUserMessage message) return Task.CompletedTask;
            if (message.Author.IsBot) return Task.CompletedTask;

            var context = new SocketCommandContext(_client, message);
            if (context.Guild == null || context.Guild.Id != _appSettings.GuildId) return Task.CompletedTask;

            int argPos = 0;
            if (!message.HasStringPrefix("!", ref argPos) &&
                !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                return Task.CompletedTask;
            }

            // Execute and forget - don't await here
            var _ = Task.Run(async () =>
            {
                try
                {
                    await _commands.ExecuteAsync(context: context, argPos: argPos, services: _services);
                }
                catch (Exception ex)
                {
                    Logger.LogWithTimestamp($"Error executing command: {ex.Message}");
                }
            });

            return Task.CompletedTask;
        }
    }
}