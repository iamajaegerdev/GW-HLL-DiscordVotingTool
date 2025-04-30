using Discord.Commands;
using Microsoft.Extensions.Options;
using Services;
using ResponseLogic.TallyAsyncEmojiReactionVotes;
using GWHLLDiscordVotingTool;


namespace DiscordBotTextCommands
{
    public class TallyAsyncEmojiReactionVotesTextCommand : ModuleBase<SocketCommandContext>
    {
        private readonly AppSettings _appSettings;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly DiscordReactionService _reactionService;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly TallyAsyncEmojiReactionVotesLogic _tallyLogic;

        public TallyAsyncEmojiReactionVotesTextCommand(IOptions<AppSettings> appSettings, DiscordReactionService reactionService)
        {
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
            _reactionService = reactionService;
            _tallyLogic = new TallyAsyncEmojiReactionVotesLogic(appSettings, reactionService);
        }

        [Command("votetally")]
        [Discord.Commands.Summary("Tally votes")]
        [Discord.Commands.RequireOwner(Group = "Permission")]
        public async Task ExecuteAsync()
        {
            Logger.LogWithTimestamp("TallyAsyncEmojiReactionVotesTextCommand.ExecuteAsync");
            await _tallyLogic.ExecuteTallyAsync(Context.Channel, Context.Message, Context.Guild);
        }
    }
} 