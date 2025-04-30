using Discord.Commands;
using ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel;
using Microsoft.Extensions.Options;
using GWHLLDiscordVotingTool;
using Services;

namespace DiscordBotTextCommands
{
    public class CreateMapRotationAsyncEmojiReactionVoteChannelTextCommand(
        IOptions<AppSettings> appSettings,
        IOptions<HellLetLooseMapData> hellLetLooseMapData,
        IBot bot) : ModuleBase<SocketCommandContext>
    {
        private readonly CreateMapRotationVoteLogic _mapVoteLogic = new(appSettings, hellLetLooseMapData, bot);
        private readonly AppSettings _appSettings = appSettings.Value;

        [Command($"mapvote")]
        [Discord.Commands.Summary("Create a new map vote channel and seed the options")]
        [Discord.Commands.RequireOwner(Group = "Permission")]
        public async Task ExecuteAsync([Remainder][Discord.Commands.Summary("An optional end date")] string remainder = "")
        {
            Logger.LogWithTimestamp("CreateMapRotationAsyncEmojiReactionVoteChannelTextCommand.ExecuteAsync");
            await _mapVoteLogic.ExecuteMapVoteAsync(_appSettings, Context.Channel, Context.Message, Context.Guild, remainder);
        }
    }
} 