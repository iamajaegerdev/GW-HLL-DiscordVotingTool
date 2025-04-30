using Discord.Commands;
using Services;

namespace DiscordBotTextCommands
{
    public class InstallSlashInteractionsTextCommand(InteractionHandler interactionHandler) : ModuleBase<SocketCommandContext>
    {
        private readonly InteractionHandler _interactionHandler = interactionHandler;

        [Command("installslashinteractions")]
        [Summary("Install slash commands for the bot")]
        [RequireOwner(Group = "Permission")]
        public async Task ExecuteAsync()
        {
            try
            {
                Logger.LogWithTimestamp("Installing slash commands...");
                await _interactionHandler.InstallSlashCommandsAsync();
                await ReplyAsync("✅ Slash commands have been installed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Failed to install slash commands: {ex}");
                await ReplyAsync($"❌ Failed to install slash commands: {ex.Message}");
            }
        }
    }
} 