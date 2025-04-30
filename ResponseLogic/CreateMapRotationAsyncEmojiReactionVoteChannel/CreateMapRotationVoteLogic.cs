using Utilities;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Services;
using ResponseLogic.Common;
using GWHLLDiscordVotingTool;


namespace ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel
{
    // Shared logic between text and slash commands
    public class CreateMapRotationVoteLogic
    {
        private readonly AppSettings _appSettings;
        private readonly HellLetLooseMapData _hellLetLooseMapData;
        private readonly IBot _bot;

        public CreateMapRotationVoteLogic(IOptions<AppSettings> appSettings, IOptions<HellLetLooseMapData> hellLetLooseMapData, IBot bot)
        {
            _appSettings = appSettings.Value;
            _appSettings.ValidateAll();
            _hellLetLooseMapData = hellLetLooseMapData.Value;
            _bot = bot;
        }

        public async Task ExecuteMapVoteAsync(AppSettings _appsettings, IChannel channel, IMessage? triggerMessage, SocketGuild guild, string remainder)
        {
            ArgumentNullException.ThrowIfNull(channel);

            Logger.LogWithTimestamp("Triggered Create Map Rotation Vote Channel Command");
            DateTime startTime = DateTime.Now;
            try
            {
                if (triggerMessage != null)
                {
                    try
                    {
                        await triggerMessage.DeleteAsync();
                        Logger.LogWithTimestamp("Triggering message deleted");
                    }
                    catch (HttpException ex) when ((int?)ex.DiscordCode == 50013)
                    {
                        Logger.LogWithTimestamp("Missing permissions to delete trigger message. Continuing...");
                        Logger.LogWithTimestamp($"{ex}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp("An unhandled exception occurred. Exiting the application in 5 seconds.");
                        Logger.LogWithTimestamp($"{ex}");
                        await Task.Delay(5000);
                        Environment.Exit(-1);
                    }
                }

                #region Embed Resources

                Emoji warningEmoji = MapVariantEmojis.Warning;
                Emoji dawnEmoji = MapVariantEmojis.Dawn;
                Emoji dayEmoji = MapVariantEmojis.Day;
                Emoji duskEmoji = MapVariantEmojis.Dusk;
                Emoji nightEmoji = MapVariantEmojis.Night;
                Emoji fogEmoji = MapVariantEmojis.Fog;
                Emoji overcastEmoji = MapVariantEmojis.Overcast;
                Emoji rainEmoji = MapVariantEmojis.Rain;
                Emoji sandEmoji = MapVariantEmojis.Sand;
                Emoji snowEmoji = MapVariantEmojis.Snow;

                bool useFirstColor = true;
                Color color1 = new(1752220u);
                Color color2 = new(3447003u);
                #endregion Embed Resources

                (IMessageChannel, ITextChannel) newChannelTuple = await CreateAsyncEmojiReactionVoteTextChannel(remainder, guild);

                IMessageChannel messageChannel = newChannelTuple.Item1;
                ITextChannel textChannel = newChannelTuple.Item2;

                // Create initial embed with loading state using JSON configuration
                var initialEmbedData = new InitialVoteCreationEmbedData("Resources/initialvotecreationembeddata.json", warningEmoji);
                Embed initialEmbed = new EmbedBuilder().WithTitle(initialEmbedData.InitialEmbedTitle ?? "").WithDescription(initialEmbedData.InitialEmbedDescription ?? "").AddField(initialEmbedData.InitialEmbedWarningFieldTitle ?? "", initialEmbedData.InitialEmbedWarningFieldValue ?? "")
                    .WithImageUrl(initialEmbedData.InitialEmbedImageUrl ?? "")
                    .WithThumbnailUrl(initialEmbedData.InitialEmbedThumbnailUrl ?? "")
                    .WithFooter(delegate (EmbedFooterBuilder footer)
                    {
                        footer.WithText(initialEmbedData.InitialEmbedFooterText ?? "").WithIconUrl(initialEmbedData.InitialEmbedFooterIconUrl ?? "");
                    })
                    .WithColor(ColorUtilities.GetDiscordColorFromString(initialEmbedData.InitialEmbedColor))
                    .Build();

                IUserMessage initialMessage = await messageChannel.SendMessageAsync(null, isTTS: false, initialEmbed);
                Task.Delay(50).Wait();

                ulong initialMessageId = initialMessage.Id;
                string bottomBookendMessageUrl = $"[Jump to top.](https://discord.com/channels/{guild.Id}/{messageChannel.Id}/{initialMessageId})";
                string bottomBookendFieldJumpValue = bottomBookendMessageUrl;

                if (_hellLetLooseMapData?.Maps == null)
                {
                    Logger.LogWithTimestamp("Map data is not loaded or is null.");
                    return;
                }

                var currentMetaVoteVariants = new List<string>();
                var allEnabledVariants = new HashSet<string>();

                // Iterate through the maps and print details
                foreach (var map in _hellLetLooseMapData.Maps)
                {
                    // Check for shutdown
                    if (_bot.ShutdownToken.IsCancellationRequested)
                    {
                        Logger.LogWithTimestamp("Shutdown requested, stopping map population.");
                        break;
                    }

                    Color color = useFirstColor ? color1 : color2;

                    //get the list of meta variants and hashset of enabled variants by calling PopulateChannelWithVoteObjects
                    (List<string> metaVariants, HashSet<string> enabledVariants) = await PopulateChannelWithVoteObjects(_appsettings, messageChannel, map.MapName!, map.MapDetails!, color, dawnEmoji, dayEmoji, duskEmoji, nightEmoji, fogEmoji, overcastEmoji, rainEmoji, sandEmoji, snowEmoji);

                    //add the returned list to currentMetaVoteVariants
                    currentMetaVoteVariants.AddRange(metaVariants);

                    //add the returned hashset to allEnabledVariants
                    allEnabledVariants.UnionWith(enabledVariants);

                    Logger.LogWithTimestamp(map.MapName + " processed.");
                    useFirstColor = !useFirstColor;
                }

                // Check for shutdown before continuing
                if (_bot.ShutdownToken.IsCancellationRequested)
                {
                    Logger.LogWithTimestamp("Shutdown requested, stopping final embed creation.");
                    return;
                }

                Logger.LogWithTimestamp("All maps populated");

                // Create final embeds with all enabled variants using JSON configuration
                var completedVoteBookendEmbed = new CompletedVoteCreationBookendEmbedData(
                    "Resources/completedvotecreationembeddata.json",
                    warningEmoji, dawnEmoji, dayEmoji, duskEmoji, nightEmoji, overcastEmoji, rainEmoji, sandEmoji, snowEmoji, 
                    _appSettings, currentMetaVoteVariants, allEnabledVariants);

                Embed secondEmbed = new EmbedBuilder().WithTitle(completedVoteBookendEmbed.BottomBookendTitle).WithDescription(completedVoteBookendEmbed.BottomBookendDescription).AddField(completedVoteBookendEmbed.BottomBookendFieldWarningTitle, completedVoteBookendEmbed.BottomBookendFieldWarningText)
                    .AddField(completedVoteBookendEmbed.BottomBookendFieldJumpTitle, bottomBookendFieldJumpValue)
                    .WithImageUrl(completedVoteBookendEmbed.BottomBookendImageUrl)
                    .WithThumbnailUrl(completedVoteBookendEmbed.BottomBookendThumbnailUrl)
                    .WithColor(completedVoteBookendEmbed.BottomBookendColor)
                    .Build();

                IUserMessage secondMessage = await messageChannel.SendMessageAsync(null, isTTS: false, secondEmbed);

                ulong secondMessageId = secondMessage.Id;
                string topBookendMessageUrl = $"[Jump to bottom.](https://discord.com/channels/{guild.Id}/{messageChannel.Id}/{secondMessageId})";
                string topBookendFieldJumpText = topBookendMessageUrl;

                Embed editedInitialEmbed = new EmbedBuilder().WithTitle(completedVoteBookendEmbed.TopBookendTitle).WithDescription(completedVoteBookendEmbed.TopBookendDescription).AddField(completedVoteBookendEmbed.TopBookendFieldRulesTitle, completedVoteBookendEmbed.TopBookendFieldRulesText)
                    .AddField(completedVoteBookendEmbed.TopBookendFieldHowToTitle, completedVoteBookendEmbed.TopBookendFieldHowToText)
                    .AddField(completedVoteBookendEmbed.TopBookendFieldEmojiKeyTitle, completedVoteBookendEmbed.TopBookendFieldEmojiKeyText)
                    .AddField(completedVoteBookendEmbed.TopBookendFieldCurrentMetaMapsTitle, completedVoteBookendEmbed.TopBookendFieldCurrentMetaMapsText)
                    .AddField(completedVoteBookendEmbed.TopBookendFieldWarningTitle, completedVoteBookendEmbed.TopBookendFieldWarningText)
                    .AddField(completedVoteBookendEmbed.TopBookendFieldJumpTitle, topBookendFieldJumpText)
                    .WithImageUrl(completedVoteBookendEmbed.TopBookendImageUrl)
                    .WithThumbnailUrl(completedVoteBookendEmbed.TopBookendThumbnailUrl)
                    .WithColor(completedVoteBookendEmbed.TopBookendColor)
                    .Build();

                await initialMessage.ModifyAsync(delegate (MessageProperties msg)
                {
                    msg.Embed = editedInitialEmbed;
                });

                if (textChannel != null)
                {
                    IThreadChannel thread = await textChannel.CreateThreadAsync("Discussion Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
                    Logger.LogWithTimestamp($"Public thread created: ThreadId {thread.Id}");

                    // Create the embed with placeholder fields
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle(completedVoteBookendEmbed.ThreadHeaderTitle)
                        .WithDescription(completedVoteBookendEmbed.ThreadHeaderDescription)
                        .AddField(completedVoteBookendEmbed.ThreadHeaderFieldMetaMapsExplainedTitle, completedVoteBookendEmbed.ThreadHeaderFieldMetaMapsExplainedText)
                        .AddField(completedVoteBookendEmbed.ThreadHeaderFieldCurrentMetaMapsTitle, string.Join("\n", currentMetaVoteVariants.Select(map => "- " + map)))
                        .AddField(completedVoteBookendEmbed.ThreadHeaderFieldMapRotationGuideTitle, completedVoteBookendEmbed.ThreadHeaderFieldMapRotationGuideText)
                        .WithColor(Color.Blue);

                    // Send the embed in the thread
                    await thread.SendMessageAsync(embed: embedBuilder.Build());
                    Logger.LogWithTimestamp("Embed sent in the thread: ThreadId " + thread.Id);
                }
                else
                {
                    Logger.LogWithTimestamp($"Failed to create a thread: {textChannel} is not an appropriate channel");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"An error occurred while executing the mapvote command. Ex: {ex}");
                if (ex is InvalidOperationException)
                {
                    throw; // Re-throw InvalidOperationException to be handled by the command handler
                }
                throw new Exception("An unexpected error occurred while creating the map vote channel.", ex);
            }

            // Calculate the time it took to finish processing
            var processingTime = TimeCalculationUtility.CalculateProcessingTime(startTime);
            Logger.LogWithTimestamp($"Create Map Rotation Async Emoji Reaction Channel Command Completed in {processingTime.FormattedString}.");
        }

        private async Task<(IMessageChannel, ITextChannel)> CreateAsyncEmojiReactionVoteTextChannel(string remainder, SocketGuild guild)
        {
            try
            {
                #region declarations
                ulong votingRoleId1 = _appSettings.VotingRoleId1;
                ulong votingRoleId2 = _appSettings.VotingRoleId2;
                bool AutoAppendDateToChannelAfterRemainder = _appSettings.AutoAppendDateToChannelAfterRemainder;
                bool AppendRemainderToChannel = _appSettings.AppendRemainderToChannel;

                var everyoneRoleId = guild.EveryoneRole.Id;

                var votingRolePermissions = new OverwritePermissions(
                        addReactions: PermValue.Deny,
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Deny,
                        readMessageHistory: PermValue.Allow,
                        useExternalEmojis: PermValue.Allow,
                        usePublicThreads: PermValue.Allow,
                        usePrivateThreads: PermValue.Allow,
                        useExternalStickers: PermValue.Allow,
                        sendMessagesInThreads: PermValue.Allow
                    );

                var everyoneRolePermissions = new OverwritePermissions(
                        viewChannel: PermValue.Deny
                    );
                #endregion declarations
                #region diagnostic logging
                // Diagnostic logging for bot permissions
                var botUser = guild.CurrentUser;
                var botPermissions = botUser.GuildPermissions;
                Logger.LogWithTimestamp($"Bot permissions in guild:");
                Logger.LogWithTimestamp($"- Manage Channels: {botPermissions.ManageChannels}");
                Logger.LogWithTimestamp($"- View Channel: {botPermissions.ViewChannel}");
                Logger.LogWithTimestamp($"- Send Messages: {botPermissions.SendMessages}");

                // Log bot role position
                var botRole = guild.GetUser(botUser.Id).Roles.OrderByDescending(r => r.Position).FirstOrDefault();
                if (botRole != null)
                {
                    Logger.LogWithTimestamp($"Bot's highest role: {botRole.Name} (Position: {botRole.Position})");
                }

                // Get and validate category
                var categoryId = _appSettings.CategoryId;
                var category = guild.GetCategoryChannel(categoryId);
                
                if (category == null)
                {
                    Logger.LogWithTimestamp($"Category with ID {categoryId} not found!");
                    throw new InvalidOperationException($"Category with ID {categoryId} not found!");
                }

                // Log category permissions
                Logger.LogWithTimestamp($"Category permissions for {category.Name}:");
                foreach (var override_ in category.PermissionOverwrites)
                {
                    var targetType = override_.TargetType == PermissionTarget.Role ? "Role" : "User";
                    var target = override_.TargetType == PermissionTarget.Role 
                        ? guild.GetRole(override_.TargetId).Name 
                        : guild.GetUser(override_.TargetId)?.Username ?? override_.TargetId.ToString();
                    
                    Logger.LogWithTimestamp($"- {targetType} {target}:");
                    Logger.LogWithTimestamp($"  - Allowed: {override_.Permissions.AllowValue}");
                    Logger.LogWithTimestamp($"  - Denied: {override_.Permissions.DenyValue}");
                }

                // Check if bot can manage channels in the category
                var botPermissionsInCategory = botUser.GetPermissions(category);
                Logger.LogWithTimestamp($"Bot permissions in category {category.Name}:");
                Logger.LogWithTimestamp($"- Manage Channel: {botPermissionsInCategory.ManageChannel}");
                Logger.LogWithTimestamp($"- View Channel: {botPermissionsInCategory.ViewChannel}");
                Logger.LogWithTimestamp($"- Send Messages: {botPermissionsInCategory.SendMessages}");

                // Direct permission check before attempting channel creation
                if (!botPermissionsInCategory.ManageChannel)
                {
                    Logger.LogWithTimestamp("Bot lacks ManageChannel permission in the target category!");
                    Logger.LogWithTimestamp("Required permissions:");
                    Logger.LogWithTimestamp("1. Bot must have ManageChannels permission globally");
                    Logger.LogWithTimestamp("2. Bot's role must be higher than any roles with ManageChannel permission in the category");
                    Logger.LogWithTimestamp("3. Category must not have any permission overwrites that deny ManageChannel");
                    throw new InvalidOperationException("The bot lacks the required permissions to create channels in this category. Check logs for details.");
                }
                #endregion diagnostic logging
                Logger.LogWithTimestamp($"Category ID: {categoryId}");

                if (AppendRemainderToChannel) {

                    if (AutoAppendDateToChannelAfterRemainder)
                    {
                        DateTime today = DateTime.Now;
                        DateTime twoWeeksFromToday = today.AddDays(14.0);
                        DateTime fourWeeksFromToday = today.AddDays(28.0);
                        string twoWeeksDate = twoWeeksFromToday.ToString("MM-dd");
                        string fourWeeksDate = fourWeeksFromToday.ToString("MM-dd");
                        DateTime.Now.ToString("MM-dd");

                        string remainderAutoDate = remainder+"-"+twoWeeksDate+"-to-"+fourWeeksDate;

                        string channelNameToBeMade = "map-vote-"+remainderAutoDate;

                        if (channelNameToBeMade.Length > 100)
                        {
                            channelNameToBeMade = channelNameToBeMade[..100];
                        }

                        RestTextChannel channel = await guild.CreateTextChannelAsync(channelNameToBeMade, x =>
                        {
                            x.CategoryId = categoryId;
                            x.PermissionOverwrites = new List<Overwrite>
                            {
                                new(votingRoleId1, PermissionTarget.Role, votingRolePermissions),
                                new(votingRoleId2, PermissionTarget.Role, votingRolePermissions),
                                new(everyoneRoleId, PermissionTarget.Role, everyoneRolePermissions)
                            };
                        });
                        return (channel, channel);
                    }
                    else
                    {
                        string channelNameToBeMade = "map-vote-"+remainder;

                        if (channelNameToBeMade.Length > 100)
                        {
                            channelNameToBeMade = channelNameToBeMade[..100];
                        }

                        RestTextChannel channel = await guild.CreateTextChannelAsync(channelNameToBeMade, x =>
                        {
                            x.CategoryId = categoryId;
                            x.PermissionOverwrites = new List<Overwrite>
                            {
                                new(votingRoleId1, PermissionTarget.Role, votingRolePermissions),
                                new(votingRoleId2, PermissionTarget.Role, votingRolePermissions),
                                new(everyoneRoleId, PermissionTarget.Role, everyoneRolePermissions)
                            };
                        });
                        return (channel, channel);
                    }
                }
                else
                {
                    string channelNameToBeMade = "map-vote";
                    RestTextChannel channel = await guild.CreateTextChannelAsync(channelNameToBeMade, x =>
                    {
                        x.CategoryId = categoryId;
                        x.PermissionOverwrites = new List<Overwrite>
                        {
                            new(votingRoleId1, PermissionTarget.Role, votingRolePermissions),
                            new(votingRoleId2, PermissionTarget.Role, votingRolePermissions),
                            new(everyoneRoleId, PermissionTarget.Role, everyoneRolePermissions)
                        };
                    });
                    return (channel, channel);
                }
            }
            catch (HttpException ex) when ((int?)ex.DiscordCode == 50013)
            {
                Logger.LogWithTimestamp("Missing permissions to create channel. Please ensure the bot has 'Manage Channels' permission.");
                throw new InvalidOperationException("The bot lacks the required permissions to create channels. Please ensure it has the 'Manage Channels' permission in the server.");
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"An error occurred while creating the channel: {ex}");
                throw;
            }
        }


        private static async Task<(List<string> metaVariants, HashSet<string> enabledVariants)> PopulateChannelWithVoteObjects(AppSettings _appsettings, IMessageChannel newChannel, string mapName, MapDetails mapDetails, Color embedColor, Emoji dawnEmoji, Emoji dayEmoji, Emoji duskEmoji, Emoji nightEmoji, Emoji fogEmoji, Emoji overcastEmoji, Emoji rainEmoji, Emoji sandEmoji, Emoji snowEmoji)
        {
            try
            {
                if (mapDetails.Enabled == "enabled")
                {
                    var englishAlphabetImages = new EnglishAlphabetImageUrls();
                    char firstCharacter = mapName[0];

                    EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithTitle(mapName)
                        .WithDescription(mapDetails.Allies +"\n vs \n"+ mapDetails.Axis)
                        .WithColor(embedColor)
                        .WithThumbnailUrl(englishAlphabetImages.AlphabetIndexImages[firstCharacter]);

                    Logger.LogWithTimestamp(mapName + " Populating Emojis");
                    IUserMessage message = await newChannel.SendMessageAsync(null, isTTS: false, embedBuilder.Build());

                    (List<string> mapMetaVariants, HashSet<string> enabledVariants) = await ProcessVoteObjectVariantsAsEmojiReactions(_appsettings, mapName, mapDetails, message, dawnEmoji, dayEmoji, duskEmoji, nightEmoji, fogEmoji, overcastEmoji, rainEmoji, sandEmoji, snowEmoji);

                    Logger.LogWithTimestamp(mapName + " Completed Populating Emojis");
                    return (mapMetaVariants, enabledVariants);
                }
                else
                {
                    // Return an empty list if the map is not enabled
                    Logger.LogWithTimestamp(mapName + " is disabled. Not populating.");
                    return (new List<string>(), new HashSet<string>());
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"An error occurred while populating the maps. Ex: {ex}");

                // Return an empty list in case of an exception
                return (new List<string>(), new HashSet<string>());
            }
        }

        private static async Task<(List<string> mapMetaVariants, HashSet<string> enabledVariants)> ProcessVoteObjectVariantsAsEmojiReactions(AppSettings _appsettings, string mapName, MapDetails mapDetails, IUserMessage message, Emoji dawnEmoji, Emoji dayEmoji, Emoji duskEmoji, Emoji nightEmoji, Emoji fogEmoji, Emoji overcastEmoji, Emoji rainEmoji, Emoji sandEmoji, Emoji snowEmoji)
        {
            List<string> mapMetaVariants = [];
            HashSet<string> enabledVariants = [];
            if (_appsettings.EnableDawn)
            {
                if (mapDetails.Dawn != null)
                {
                    if (mapDetails.Dawn == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Dawn variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Dawn");
                        enabledVariants.Add("Dawn");
                    }
                    else if (mapDetails.Dawn == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Dawn", message, dawnEmoji);
                        enabledVariants.Add("Dawn");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Dawn variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Dawn variant is disabled");
            }

            if (_appsettings.EnableDay)
            {
                if (mapDetails.Day != null)
                {
                    if (mapDetails.Day == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Day variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Day");
                        enabledVariants.Add("Day");
                    }
                    else if (mapDetails.Day == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Day", message, dayEmoji);
                        enabledVariants.Add("Day");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Day variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Day variant is disabled");
            }

            if (_appsettings.EnableDusk)
            {
                if (mapDetails.Dusk != null)
                {
                    if (mapDetails.Dusk == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Dusk variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Dusk");
                        enabledVariants.Add("Dusk");
                    }
                    else if (mapDetails.Dusk == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Dusk", message, duskEmoji);
                        enabledVariants.Add("Dusk");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Dusk variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Dusk variant is disabled");
            }

            if (_appsettings.EnableNight)
            {
                if (mapDetails.Night != null)
                {
                    if (mapDetails.Night == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Night variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Night");
                        enabledVariants.Add("Night");
                    }
                    else if (mapDetails.Night == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Night", message, nightEmoji);
                        enabledVariants.Add("Night");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Night variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Night variant is disabled");
            }

            if (_appsettings.EnableFog)
            {
                if (mapDetails.Fog != null)
                {
                    if (mapDetails.Fog == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Fog variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Fog");
                        enabledVariants.Add("Fog");
                    }
                    else if (mapDetails.Fog == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Fog", message, fogEmoji);
                        enabledVariants.Add("Fog");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Fog variant is disabled");
                    }
                }
            }
            if (_appsettings.EnableOvercast)
            {
                if (mapDetails.Overcast != null)
                {
                    if (mapDetails.Overcast == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Overcast variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Overcast");
                        enabledVariants.Add("Overcast");
                    }
                    else if (mapDetails.Overcast == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Overcast", message, overcastEmoji);
                        enabledVariants.Add("Overcast");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Overcast variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Overcast variant is disabled");
            }

            if (_appsettings.EnableRain)
            {
                if (mapDetails.Rain != null)
                {
                    if (mapDetails.Rain == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Rain variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Rain");
                        enabledVariants.Add("Rain");
                    }
                    else if (mapDetails.Rain == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Rain", message, rainEmoji);
                        enabledVariants.Add("Rain");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Rain variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Rain variant is disabled");
            }

            if (_appsettings.EnableSandstorm) {
                if (mapDetails.Sandstorm != null)
                {
                    if (mapDetails.Sandstorm == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Sandstorm variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Sandstorm");
                        enabledVariants.Add("Sandstorm");
                    }
                    else if (mapDetails.Sandstorm == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Sandstorm", message, sandEmoji);
                        enabledVariants.Add("Sandstorm");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Sandstorm variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Sandstorm variant is disabled");
            }

            if (_appsettings.EnableSnowstorm)
            {
                if (mapDetails.Snowstorm != null)
                {
                    if (mapDetails.Snowstorm == "meta")
                    {
                        Logger.LogWithTimestamp(mapName + " Snowstorm variant is meta, not populating option");
                        mapMetaVariants.Add($"{mapName} Snowstorm");
                        enabledVariants.Add("Snowstorm");
                    }
                    else if (mapDetails.Snowstorm == "enabled")
                    {
                        await AddEmojiReactionWithLogging(mapName, "Snowstorm", message, snowEmoji);
                        enabledVariants.Add("Snowstorm");
                    }
                    else
                    {
                        Logger.LogWithTimestamp(mapName + " Snowstorm variant is disabled");
                    }
                }
            }
            else
            {
                Logger.LogWithTimestamp("Snowstorm variant is disabled");
            }

            return (mapMetaVariants, enabledVariants);
        }

        private static async Task AddEmojiReactionWithLogging(string mapName, string variant, IUserMessage message, Emoji emoji)
        {
            try
            {
                await message.AddReactionAsync(emoji);
                Logger.LogWithTimestamp(mapName + " " + variant + " variant option Emoji populated");
            }
            catch (RateLimitedException)
            {
                Logger.LogWithTimestamp("Rate limit hit. Waiting 5000ms.");
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"An error occurred while adding {variant} Emoji reaction. Ex: {ex}");
            }
        }
    }
}