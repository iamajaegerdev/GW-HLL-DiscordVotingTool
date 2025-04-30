using Discord;
using Discord.WebSocket;
using Discord.Net;
using Services;

namespace GWHLLDiscordVotingTool
{
    public class Bot : IBot
    {
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _config;
        private bool _isShuttingDown;
        private CancellationTokenSource _shutdownCts;

        public Bot(DiscordSocketClient client, BotConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _client = client ?? new DiscordSocketClient(_config.SocketConfig);
            _isShuttingDown = false;
            _shutdownCts = new CancellationTokenSource();

            _client.Disconnected += exception =>
            {
                Logger.LogWithTimestamp($"Bot disconnected: {exception?.Message ?? "Unknown reason"}");

                // Don't attempt to reconnect if we're shutting down
                if (_isShuttingDown)
                {
                    return Task.CompletedTask;
                }

                // Check if the error is related to disallowed intents (WebSocketClosedException with code 4014)
                if (exception?.InnerException is WebSocketClosedException wsCloseException && 
                    wsCloseException.CloseCode == 4014)
                {
                    Logger.LogWithTimestamp("Disallowed intents detected (Error 4014). Application will shut down in 5 seconds...");
                    Logger.LogWithTimestamp("Please check your bot's privileged gateway intents configuration in the Discord Developer Portal.");
                    _isShuttingDown = true;
                    _shutdownCts.Cancel();
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(5000); // Wait 5 seconds before shutting down
                        Environment.Exit(1); // Exit the application
                    });
                    return Task.CompletedTask;
                }

                // For other disconnection reasons, attempt to reconnect
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000); // Wait before reconnecting

                    try
                    {
                        if (!_isShuttingDown) // Double check we're not shutting down
                        {
                            await _client.LoginAsync(TokenType.Bot, _config.Token);
                            await _client.StartAsync();
                            Logger.LogWithTimestamp("Reconnection attempt completed");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp($"Failed to reconnect: {ex.Message}");
                    }
                });

                return Task.CompletedTask;
            };
        }

        public async Task StartAsync(IServiceProvider serviceProvider)
        {
            _isShuttingDown = false;
            _shutdownCts = new CancellationTokenSource();
            _client.Log += LogAsync;
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
        }

        public async Task StopAsync()
        {
            try
            {
                // Set shutdown flag first to prevent reconnection attempts
                _isShuttingDown = true;
                
                // Cancel any pending operations
                _shutdownCts.Cancel();
                
                // Log that we're starting the shutdown process
                Logger.LogWithTimestamp("Starting bot shutdown process...");

                // Remove event handlers
                _client.Log -= LogAsync;
                
                // Stop the client
                await _client.StopAsync();
                
                // Set status to offline before completely disconnecting
                await _client.SetStatusAsync(UserStatus.Offline);
                
                // Close the WebSocket connection
                await _client.LogoutAsync();
                
                Logger.LogWithTimestamp("Bot shutdown completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error during bot shutdown: {ex.Message}");
                throw;
            }
            finally
            {
                _shutdownCts.Dispose();
            }
        }

        public CancellationToken ShutdownToken => _shutdownCts.Token;

        private Task LogAsync(LogMessage logMessage)
        {
            Logger.LogWithTimestamp(logMessage.ToString(null, fullException: true, prependTimestamp: true, DateTimeKind.Local, 11));
            return Task.CompletedTask;
        }
    }
}