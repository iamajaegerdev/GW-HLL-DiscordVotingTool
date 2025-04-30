using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Services;
using Utilities;
using GWHLLDiscordVotingTool;
using ResponseLogic.Common;
using System.Text.Json;

namespace ResponseLogic.TallyAsyncEmojiReactionVotes
{
    public class TallyAsyncEmojiReactionVotesLogic
    {
        private readonly AppSettings _appSettings;
        private readonly DiscordReactionService _reactionService;
        private readonly Dictionary<string, string> _topWinnersEmbedTemplate;
        private readonly Dictionary<string, string> _placeholderEmbedTemplate;

        public TallyAsyncEmojiReactionVotesLogic(IOptions<AppSettings> appSettings, DiscordReactionService reactionService)
        {
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
            _reactionService = reactionService;

            // Load embed templates from JSON files
            string topWinnersJson = File.ReadAllText("Resources/topwinnersembeddata.json");
            string placeholderJson = File.ReadAllText("Resources/placeholderembeddata.json");
            _topWinnersEmbedTemplate = JsonSerializer.Deserialize<Dictionary<string, string>>(topWinnersJson) ?? throw new InvalidOperationException("Failed to load top winners embed template");
            _placeholderEmbedTemplate = JsonSerializer.Deserialize<Dictionary<string, string>>(placeholderJson) ?? throw new InvalidOperationException("Failed to load placeholder embed template");
        }

        public async Task ExecuteTallyAsync(IChannel channel, IMessage? triggerMessage, SocketGuild guild)
        {
            int numberOfWinners = _appSettings.NumberOfWinners;
            int maxVotes = _appSettings.MaxVotesPerVoter;

            Emoji dawnEmoji = MapVariantEmojis.Dawn;
            Emoji dayEmoji = MapVariantEmojis.Day;
            Emoji duskEmoji = MapVariantEmojis.Dusk;
            Emoji nightEmoji = MapVariantEmojis.Night;
            Emoji fogEmoji = MapVariantEmojis.Fog;
            Emoji overcastEmoji = MapVariantEmojis.Overcast;
            Emoji rainEmoji = MapVariantEmojis.Rain;
            Emoji sandEmoji = MapVariantEmojis.Sand;
            Emoji snowEmoji = MapVariantEmojis.Snow;

            Dictionary<Emoji, string> variants = new()
            {
                { dawnEmoji, "Dawn" },
                { dayEmoji, "Day" },
                { duskEmoji, "Dusk" },
                { nightEmoji, "Night" },
                { fogEmoji, "Fog" },
                { overcastEmoji, "Overcast" },
                { rainEmoji, "Rain" },
                { sandEmoji, "Sand" },
                { snowEmoji, "Snow" }
            };

            Dictionary<string, int> embedVariantReactionCounts = [];
            Dictionary<ulong, List<(Embed, string)>> userVotes = [];

            Logger.LogWithTimestamp("Triggered Tally Async Emoji Reaction Votes Command");
            DateTime startTime = DateTime.Now;

            try
            {
                if (channel is not ITextChannel textChannel)
                {
                    Logger.LogWithTimestamp("Channel is not a text channel.");
                    return;
                }

                // Delete trigger message if it exists (text command only)
                if (triggerMessage != null)
                {
                    try
                    {
                        await triggerMessage.DeleteAsync();
                        Logger.LogWithTimestamp("Triggering message deleted");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp("An error occurred while deleting the triggering message");
                        Logger.LogWithTimestamp($"{ex}");
                    }
                }

                //download messages
                IEnumerable<IMessage> messages = await textChannel.GetMessagesAsync().FlattenAsync();
                Logger.LogWithTimestamp($"Fetched {messages.Count()} messages");

                //build placeholder embed message using template
                Embed placeholderEmbed = new EmbedBuilder()
                    .WithTitle(_placeholderEmbedTemplate["Title"])
                    .WithDescription(_placeholderEmbedTemplate["Description"])
                    .WithImageUrl(_placeholderEmbedTemplate["ImageUrl"])
                    .WithThumbnailUrl(_placeholderEmbedTemplate["ThumbnailUrl"])
                    .WithColor(Color.Gold)
                    .Build();

                //send placeholder embed message
                IUserMessage placeholderEmbedMessage = await textChannel.SendMessageAsync(null, isTTS: false, placeholderEmbed);

                // Go through each message in downloaded messages
                foreach (IMessage message in messages)
                {
                    // Ensure that message.Embeds is not null
                    if (message.Embeds == null)
                    {
                        Logger.LogWithTimestamp("Message embeds are null.");
                        continue;
                    }

                    // Process each message asynchronously to avoid blocking the main thread
                    await Task.Run(async () =>
                    {
                        try
                        {
                            // Find embeds in each message
                            foreach (IEmbed embed in message.Embeds)
                            {
                                // Check if embed is not null
                                if (embed == null)
                                {
                                    Logger.LogWithTimestamp("Embed is null.");
                                    continue;
                                }

                                if (embed is Embed embedInstance)
                                {
                                    // Count emoji reaction votes
                                    HashSet<ulong> uniqueVoters = [];

                                    List<Task> processingTasks = [];

                                    foreach (var kvp in variants)
                                    {
                                        if (!message.Reactions.TryGetValue(kvp.Key, out var reactionMetadata))
                                        {
                                            continue;
                                        }

                                        // Create a task for each emoji variant, but don't await it yet
                                        Task emojiProcessingTask = Task.Run(async () =>
                                        {
                                            try
                                            {
                                                Logger.LogWithTimestamp($"Processing: Embed: {embedInstance.Title} ({kvp.Value}), Reactions: {reactionMetadata.ReactionCount}");

                                                // Use the new reaction service
                                                IEnumerable<IUser> users = await _reactionService.GetReactionUsersAsync(message, kvp.Key);
                                                Logger.LogWithTimestamp($"Emote key {kvp.Value}: {users.Count()} users");

                                                // Process users
                                                foreach (IUser user in users)
                                                {
                                                    // Ignore bots
                                                    if (!user.IsBot)
                                                    {
                                                        lock (uniqueVoters)
                                                        {
                                                            bool isAdded = uniqueVoters.Add(user.Id); // Try to add the user ID
                                                            Logger.LogWithTimestamp($"User ID {user.Id} added to Unique Voters: {isAdded}");
                                                        }

                                                        lock (userVotes)
                                                        {
                                                            if (!userVotes.TryGetValue(user.Id, out List<(Embed, string)>? value)) //if it doesnt contain user id, add it
                                                            {
                                                                value = [];
                                                                userVotes[user.Id] = value;
                                                                Logger.LogWithTimestamp($"Created Voter Log for {user.Id}");
                                                            }

                                                            value.Add((embedInstance, kvp.Value)); //record user's vote
                                                            Logger.LogWithTimestamp($"Recorded {embedInstance} {kvp.Value} for {user.Id}");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.LogWithTimestamp($"Error processing emoji {kvp.Value}: {ex.Message}");
                                            }
                                        });

                                        processingTasks.Add(emojiProcessingTask);
                                    }

                                    // Wait for all emoji processing tasks to complete
                                    await Task.WhenAll(processingTasks);
                                    Logger.LogWithTimestamp("All emoji reactions processed successfully");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWithTimestamp($"Error processing message: {ex.Message}");
                        }
                    });
                }

                Logger.LogWithTimestamp("Tally complete");

                //if voters have over 3 votes, discard 
                Random random = new();
                foreach (KeyValuePair<ulong, List<(Embed, string)>> userVote in userVotes)
                {
                    if (userVote.Value.Count > maxVotes)
                    {
                        Logger.LogWithTimestamp($"User {userVote.Key} has more than 3 votes. Removing excess votes.");
                        while (userVote.Value.Count > maxVotes)
                        {
                            int indexToRemove = random.Next(userVote.Value.Count);
                            userVote.Value.RemoveAt(indexToRemove);
                        }
                    }

                    //count map votes
                    foreach (var vote in userVote.Value)
                    {
                        string embedVariationReactionCountsKey = vote.Item1.Title + " " + vote.Item2;
                        if (embedVariantReactionCounts.TryGetValue(embedVariationReactionCountsKey, out int value))
                        {
                            embedVariantReactionCounts[embedVariationReactionCountsKey] = ++value;
                        }
                        else
                        {
                            embedVariantReactionCounts[embedVariationReactionCountsKey] = 1;
                        }
                    }
                }

                //tie breaker sorts by vote count then takes 3
                Random randomVariants = new();
                List<KeyValuePair<string, int>> topEmbedVariants = [.. (from x in embedVariantReactionCounts.OrderBy((x) => randomVariants.Next()).ToList()
                                                                    group x by x.Value into g
                                                                    orderby g.Key descending
                                                                    select g).SelectMany((g) => g).Take(numberOfWinners)];

                // Calculate the time it took to finish processing
                var processingTime = TimeCalculationUtility.CalculateProcessingTime(startTime);
                // Access individual values
                int minutes = processingTime.TotalMinutes;
                int seconds = processingTime.RemainingSeconds;
                // Or use the pre-formatted string
                string formatted = processingTime.FormattedString;

                // Get the total amount of votes from all voters
                int totalVotes = userVotes.Values.Sum(votes => votes.Count);

                //update footertext with processed calculations
                _topWinnersEmbedTemplate["FooterText"] = $"Processed {userVotes.Count} Voters with {totalVotes} votes in {formatted}.";
                Logger.LogWithTimestamp($"Processed {userVotes.Count} Voters with {totalVotes} votes in {formatted}.");
                Logger.LogWithTimestamp("Building top winners embed.");

                //build topWinners embed using template
                EmbedBuilder topWinnersEmbedBuilder = new EmbedBuilder()
                    .WithTitle(string.Format(_topWinnersEmbedTemplate["Title"], numberOfWinners))
                    .WithFooter(footer =>
                    {
                        footer.Text = _topWinnersEmbedTemplate["FooterText"];
                        footer.IconUrl = _topWinnersEmbedTemplate["FooterIconUrl"];
                    })
                    .WithImageUrl(_topWinnersEmbedTemplate["ImageUrl"])
                    .WithThumbnailUrl(_topWinnersEmbedTemplate["ThumbnailUrl"])
                    .WithColor(Color.Gold);

                for (int i = 0; i < topEmbedVariants.Count; i++)
                {
                    string embed = topEmbedVariants[i].Key;
                    int reactions = topEmbedVariants[i].Value;
                    topWinnersEmbedBuilder.AddField($"Winner #{i + 1}", $"**Map:** {embed}\n**Votes:** {reactions}");
                }

                //send top winners embed
                Logger.LogWithTimestamp("Sending top winners embed.");
                await textChannel.SendMessageAsync(null, isTTS: false, topWinnersEmbedBuilder.Build());

                //create full vote results thread
                Logger.LogWithTimestamp("Creating full vote results thread.");
                IThreadChannel thread = await textChannel.CreateThreadAsync("Full Vote Results");

                // Order all maps descending by votes
                List<KeyValuePair<string, int>> fullResults = [.. embedVariantReactionCounts.OrderByDescending(x => x.Value)];

                // Full results embed
                int embedCountFullResults = (int)Math.Ceiling(fullResults.Count / 25.0);

                for (int i = 0; i < embedCountFullResults; i++)
                {
                    EmbedBuilder fullResultsEmbedBuilder = new EmbedBuilder().WithTitle("Full Vote Results").WithColor(Color.DarkerGrey);

                    for (int j = 0; j < 25 && fullResults.Count > 0; j++)
                    {
                        KeyValuePair<string, int> result = fullResults[0];
                        fullResultsEmbedBuilder.AddField("**Map:** " + result.Key, $"**Votes:** {result.Value}");
                        fullResults.RemoveAt(0);
                    }

                    await thread.SendMessageAsync(null, isTTS: false, fullResultsEmbedBuilder.Build());
                }

                Logger.LogWithTimestamp("Posting full results. Please wait.");

                // Final embed after all entries are processed
                if (fullResults.Count == 0)
                {
                    EmbedBuilder finalEmbedBuilder = new EmbedBuilder().WithTitle("Final Vote Results").WithColor(Color.DarkerGrey);
                    finalEmbedBuilder.AddField("All entries processed", "All maps and their votes have been processed.");
                    await thread.SendMessageAsync(null, isTTS: false, finalEmbedBuilder.Build());
                }

                // Per user embed
                int embedCountUserLogs = (int)Math.Ceiling(userVotes.Count / 25.0);

                for (int i = 0; i < embedCountUserLogs; i++)
                {
                    EmbedBuilder userVoteLogsEmbedBuilder = new EmbedBuilder().WithTitle("User Vote Logs").WithColor(Color.LightGrey);

                    for (int j = 0; j < 25 && userVotes.Count > 0; j++)
                    {
                        KeyValuePair<ulong, List<(Embed, string)>> userVote = userVotes.First();
                        ulong userId = userVote.Key;
                        List<(Embed, string)> votesList = userVote.Value;

                        string username = guild.GetUser(userId)?.Username ?? "Unknown User";
                        Console.WriteLine($"User ID: {userId}, Username: {username}");
                        if (username == "Unknown User")
                        {
                            username = $"{userId}";
                        }

                        string votes = string.Join(", ", votesList.Select(v => v.Item1.Title + " (" + v.Item2 + ")"));
                        userVoteLogsEmbedBuilder.AddField(username, votes);

                        userVotes.Remove(userId);
                    }

                    Logger.LogWithTimestamp("Posting user vote logs. Please wait.");
                    await thread.SendMessageAsync(null, isTTS: false, userVoteLogsEmbedBuilder.Build());
                }

                // Final embed after all entries are processed
                if (userVotes.Count == 0)
                {
                    EmbedBuilder finalEmbedBuilder = new EmbedBuilder().WithTitle("Final User Vote Logs").WithColor(Color.LightGrey);
                    finalEmbedBuilder.AddField("All entries processed", "All user votes have been processed.");
                    await thread.SendMessageAsync(null, isTTS: false, finalEmbedBuilder.Build());
                }

                //delete placeholder
                await placeholderEmbedMessage.DeleteAsync();

                Logger.LogWithTimestamp("Tally reaction votes command completed");
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp("An error occurred while executing the tally command");
                Logger.LogWithTimestamp($"{ex}");
            }
        }
    }
}