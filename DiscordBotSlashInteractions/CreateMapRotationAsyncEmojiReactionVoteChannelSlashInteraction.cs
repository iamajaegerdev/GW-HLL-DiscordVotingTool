using Discord.Interactions;
using ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel;
using Microsoft.Extensions.Options;
using GWHLLDiscordVotingTool;
using Services;

namespace DiscordBotSlashInteractions
{
    public class CreateMapRotationAsyncEmojiReactionVoteChannelSlashInteraction : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppSettings _appSettings;
        private readonly HellLetLooseMapData _hellLetLooseMapData;
        private readonly IBot _bot;
        private readonly CreateMapRotationVoteLogic _mapVoteLogic;

        public CreateMapRotationAsyncEmojiReactionVoteChannelSlashInteraction(
            IOptions<AppSettings> appSettings,
            IOptions<HellLetLooseMapData> hellLetLooseMapData,
            IBot bot)
        {
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
            _hellLetLooseMapData = hellLetLooseMapData.Value;
            _bot = bot;
            _mapVoteLogic = new CreateMapRotationVoteLogic(appSettings, hellLetLooseMapData, bot);
        }

        [SlashCommand("mapvote", "Create a new map vote channel and seed the options")]
        [RequireOwner(Group = "Permission")]
        public async Task HandleMapVoteCommand([Summary("date-range", "Optional date range for the vote (e.g. 03-15-to-03-29)")] string? dateRange = null)
        {
            // Defer the response since this operation might take a while
            await DeferAsync();

            try
            {
                await _mapVoteLogic.ExecuteMapVoteAsync(_appSettings, Context.Channel, null, Context.Guild, dateRange ?? "");
                // Delete the deferred response since we don't need it
                await DeleteOriginalResponseAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error in slash command: {ex}");
                await FollowupAsync("An error occurred while creating the map vote channel.", ephemeral: true);
            }
        }
    }
}