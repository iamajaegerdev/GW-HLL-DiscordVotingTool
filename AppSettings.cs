using Services;

namespace GWHLLDiscordVotingTool
{

    public class AppSettings
    {
        //command triggers
        public string PingTextCommandTrigger { get; set; } = string.Empty;
        public string CreateMapRotationVoteTextChannelTextCommandTrigger { get; set; } = string.Empty;
        public string TallyReactionVotesTextCommandTrigger { get; set; } = string.Empty;
        public string InstallSlashInteractionsTextCommandTrigger { get; set; } = string.Empty;  

        //discord targets
        public ulong GuildId { get; set; }
        public ulong CategoryId { get; set; }
        public ulong VotingRoleId1 { get; set; }
        public ulong VotingRoleId2 { get; set; }

        //voting rules
        public int MaxVotesPerVoter { get; set; }
        public int NumberOfWinners { get; set; }

        //hll map variant options
        public bool EnableDawn { get; set; }
        public bool EnableDay { get; set; }
        public bool EnableDusk { get; set; }
        public bool EnableNight { get; set; }
        public bool EnableFog { get; set; }
        public bool EnableOvercast { get; set; }
        public bool EnableRain { get; set; }
        public bool EnableSandstorm { get; set; }
        public bool EnableSnowstorm { get; set; }

        //oauth2
        public bool EnableOAuth2WebService { get; set; }
        public string RedirectUri { get; set; } = string.Empty;
#pragma warning disable IDE0301 // Simplify collection initialization
        public string[] Scopes { get; set; } = Array.Empty<string>();
#pragma warning restore IDE0301 // Simplify collection initialization

        //application personalization
        public bool AppendRemainderToChannel { get; set; }
        public bool AutoAppendDateToChannelAfterRemainder { get; set; }

        
        public void ValidateAll()
        {
            ValidateTextCommandTriggers();
            ValidateDiscordTargets();
            ValidateVotingRequirements();
        }
        public void ValidateTextCommandTriggers()
        {
            if (string.IsNullOrWhiteSpace(PingTextCommandTrigger))
            {
                Logger.LogWithTimestamp("FATAL ERROR: PingTextCommandTrigger must not be null or white space (empty)");
                Console.ReadLine();
                throw new InvalidOperationException("PingTextCommandTrigger must not be null or white space (empty)");
            }
            if (string.IsNullOrWhiteSpace(CreateMapRotationVoteTextChannelTextCommandTrigger))
            {
                Logger.LogWithTimestamp("FATAL ERROR: CreateMapRotationVoteTextChannelTextCommandTrigger must not be null or white space (empty)");
                Console.ReadLine();
                throw new InvalidOperationException("CreateMapRotationVoteTextChannelTextCommandTrigger must not be null or white space (empty)");
            }
            ;
            if (string.IsNullOrWhiteSpace(TallyReactionVotesTextCommandTrigger))
            {
                Logger.LogWithTimestamp("FATAL ERROR: TallyReactionVotesTextCommandTrigger must not be null or white space (empty)");
                Console.ReadLine();
                throw new InvalidOperationException("TallyReactionVotesTextCommandTrigger must not be null or white space (empty)");
            }
            ;
            if (string.IsNullOrWhiteSpace(InstallSlashInteractionsTextCommandTrigger))
            {
                Logger.LogWithTimestamp("FATAL ERROR: TallyReactionVotesTextCommandTrigger must not be null or white space (empty)");
                Console.ReadLine();
                throw new InvalidOperationException("TallyReactionVotesTextCommandTrigger must not be null or white space (empty)");
            }
            ;
        }
        public void ValidateDiscordTargets()
        {
            if (GuildId == 0)
            {
                Logger.LogWithTimestamp("FATAL ERROR: GuildId must not be null or white space (empty).");
                Console.ReadLine();
                throw new InvalidOperationException("GuildId must not be null.");
            }
            if (CategoryId == 0)
            {
                Logger.LogWithTimestamp("FATAL ERROR: CategoryId must not be null or white space (empty).");
                Console.ReadLine();
                throw new InvalidOperationException("CategoryId must not be null.");
            }
            if (VotingRoleId1 == 0)
            {
                Logger.LogWithTimestamp("FATAL ERROR: VotingRoleId1 must not be null or white space (empty).");
                Console.ReadLine();
                throw new InvalidOperationException("VotingRoleId1 must not be null.");
            }
            if (VotingRoleId2 == 0)
            {
                Logger.LogWithTimestamp("FATAL ERROR: VotingRoleId2 must not be null or white space (empty).");
                Console.ReadLine();
                throw new InvalidOperationException("VotingRoleId2 must not be null.");
            }
        }

        public void ValidateVotingRequirements()
        {
            if (MaxVotesPerVoter == 0 | MaxVotesPerVoter < 0 | MaxVotesPerVoter > 32)
            {
                Logger.LogWithTimestamp("FATAL ERROR: MaxVotesPerVoter must be above 0 and below 32.");
                Console.ReadLine();
                throw new InvalidOperationException("MaxVotesPerVoter must be above 0 and below 32.");
            }
            if (NumberOfWinners == 0 | NumberOfWinners < 0 | NumberOfWinners > 32)
            {
                Logger.LogWithTimestamp("FATAL ERROR: NumberOfWinners must be above 0 and below 32.");
                Console.ReadLine();
                throw new InvalidOperationException("NumberOfWinners must be above 0 and below 32.");
            }
        }
    }
}
