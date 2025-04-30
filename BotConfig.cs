using Discord;
using Discord.WebSocket;

namespace GWHLLDiscordVotingTool
{
    public class BotConfig
    {
        public string Token { get; }
        public DiscordSocketConfig SocketConfig { get; }

        public BotConfig(string token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));

            SocketConfig = new DiscordSocketConfig
            {
                ConnectionTimeout = 30000, //milliseconds (30 seconds default)
                HandlerTimeout = 1500, //milliseconds (15 seconds default)

                // Include all necessary intents for both text and slash commands
                GatewayIntents = GatewayIntents.GuildMessages 
                    | GatewayIntents.GuildMembers 
                    | GatewayIntents.Guilds // Required for slash commands
                    | GatewayIntents.MessageContent // Required for message content
            };
        }
    }
}
