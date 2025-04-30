using Discord;
using Discord.Commands;

namespace DiscordBotTextCommands
{
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Responds with Pong.")]
        [RequireOwner(Group = "Permission")]
        public async Task PingAsync()
        {

            await Context.Message.DeleteAsync();

            IUserMessage response = await ReplyAsync("Pong");

            await Task.Delay(3000);

            await response.DeleteAsync();
        }
    }
}