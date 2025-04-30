using Discord.Interactions;
using Microsoft.Extensions.Options;
using Services;
using ResponseLogic.TallyAsyncEmojiReactionVotes;
using GWHLLDiscordVotingTool;

namespace DiscordBotSlashInteractions
{
    public class TallyAsyncEmojiReactionVotesSlashInteraction : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppSettings _appSettings;
        private readonly DiscordReactionService _reactionService;
        private readonly TallyAsyncEmojiReactionVotesLogic _tallyLogic;

        public TallyAsyncEmojiReactionVotesSlashInteraction(IOptions<AppSettings> appSettings, DiscordReactionService reactionService)
        {
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
            _reactionService = reactionService;
            _tallyLogic = new TallyAsyncEmojiReactionVotesLogic(appSettings, reactionService);
        }

        [SlashCommand("votetally", "Tally the votes in the current channel")]
        [RequireOwner(Group = "Permission")]
        public async Task HandleTallyCommand()
        {
            // Defer the response since this operation might take a while
            await DeferAsync();

            try
            {
                await _tallyLogic.ExecuteTallyAsync(Context.Channel, null, Context.Guild);
                // Delete the deferred response since we don't need it
                await DeleteOriginalResponseAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error in slash command: {ex}");
                await FollowupAsync("An error occurred while tallying votes.", ephemeral: true);
            }
        }
    }
}