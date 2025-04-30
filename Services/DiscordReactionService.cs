using Discord;

namespace Services
{
    public class DiscordReactionService(DiscordRateLimitService rateLimitService)
    {
        private readonly DiscordRateLimitService _rateLimitService = rateLimitService;

        public async Task<IEnumerable<IUser>> GetReactionUsersAsync(IMessage message, IEmote emoji, int limit = 100)
        {
            string routeKey = $"reactions:{message.Channel.Id}";
            return await _rateLimitService.ExecuteWithRateLimitAsync(routeKey, async () =>
            {
                var options = new RequestOptions
                {
                    RetryMode = RetryMode.AlwaysRetry,
                    Timeout = 15000
                };

                var fetchedUsers = await message.GetReactionUsersAsync(emoji, limit, options).FlattenAsync();
                Logger.LogWithTimestamp($"Fetched {fetchedUsers.Count()} reaction users for emoji {emoji.Name}");
                return fetchedUsers;
            });
        }
    }
} 