namespace GWHLLDiscordVotingTool
{
    public interface IBot
    {
        Task StartAsync(IServiceProvider serviceProvider);

        Task StopAsync();

        CancellationToken ShutdownToken { get; }
    }
}
