using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using GWHLLDiscordVotingTool;

namespace Services
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly AppSettings _appSettings;

        public InteractionHandler(
            DiscordSocketClient client,
            InteractionService interactions,
            IServiceProvider services,
            IOptions<AppSettings> appSettings)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Add the interaction modules from our assembly
                await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

                // Subscribe to the interaction created event
                _client.InteractionCreated += HandleInteractionAsync;
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error initializing interaction handler: {ex}");
            }
        }

        public async Task InstallSlashCommandsAsync()
        {
            try
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    // Register commands to a specific guild during development
                    await _interactions.RegisterCommandsToGuildAsync(_appSettings.GuildId);
                    Logger.LogWithTimestamp($"Registered slash commands to guild {_appSettings.GuildId}");
                }
                else
                {
                    throw new InvalidOperationException("Client is not connected. Cannot register slash commands.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error installing slash commands: {ex}");
                throw;
            }
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context for the interaction
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the interaction
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            await interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
                            break;
                        default:
                            Logger.LogWithTimestamp($"Error handling interaction: {result.Error} - {result.ErrorReason}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error executing interaction: {ex}");

                // If the interaction hasn't been responded to yet, respond with an error
                if (interaction.Type is InteractionType.ApplicationCommand)
                {
                    if (!interaction.HasResponded)
                    {
                        await interaction.RespondAsync("An error occurred while executing the command.", ephemeral: true);
                    }
                }
            }
        }
    }
} 