using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;
using Utilities;
using GWHLLDiscordVotingTool;

namespace ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel
{
    public class CompletedVoteCreationBookendEmbedData
    {
        public string TopBookendTitle { get; set; }
        public string TopBookendDescription { get; set; }
        public string TopBookendFieldRulesTitle { get; set; }
        public string TopBookendFieldRulesText { get; set; }
        public string TopBookendFieldHowToTitle { get; set; }
        public string TopBookendFieldHowToText { get; set; }
        public string TopBookendFieldEmojiKeyTitle { get; set; }
        public string TopBookendFieldEmojiKeyText { get; set; }
        public string TopBookendFieldCurrentMetaMapsTitle { get; set; }
        public string TopBookendFieldCurrentMetaMapsText { get; set; }
        public string TopBookendFieldWarningTitle { get; set; }
        public string TopBookendFieldWarningText { get; set; }
        public string TopBookendFieldJumpTitle { get; set; }
        public string TopBookendImageUrl { get; set; }
        public string TopBookendThumbnailUrl { get; set; }
        public Color TopBookendColor { get; set; }

        public string BottomBookendTitle { get; set; }
        public string BottomBookendDescription { get; set; }
        public string BottomBookendFieldJumpTitle { get; set; }
        public string BottomBookendFieldJumpValue { get; set; }
        public string BottomBookendFieldWarningTitle { get; set; }
        public string BottomBookendFieldWarningText { get; set; }
        public string BottomBookendImageUrl { get; set; }
        public string BottomBookendThumbnailUrl { get; set; }
        public Color BottomBookendColor { get; set; }

        public string ThreadHeaderTitle { get; set; }
        public string ThreadHeaderDescription { get; set; }
        public string ThreadHeaderFieldMapRotationGuideTitle { get; set; }
        public string ThreadHeaderFieldMapRotationGuideText { get; set; }
        public string ThreadHeaderFieldMetaMapsExplainedTitle { get; set; }
        public string ThreadHeaderFieldMetaMapsExplainedText {  get; set; }
        public string ThreadHeaderFieldCurrentMetaMapsTitle {  get; set; }
        public string ThreadHeaderFieldCurrentMetaMapsText { get; set; }

        // JSON configuration model classes
        private class EmbedConfig
        {
            [JsonPropertyName("topBookend")]
            public TopBookendConfig TopBookend { get; set; } = new TopBookendConfig();

            [JsonPropertyName("bottomBookend")]
            public BottomBookendConfig BottomBookend { get; set; } = new BottomBookendConfig();

            [JsonPropertyName("threadHeader")]
            public ThreadHeaderConfig ThreadHeader { get; set; } = new ThreadHeaderConfig();
        }

        private class TopBookendConfig
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("fieldRulesTitle")]
            public string FieldRulesTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldRulesText")]
            public string FieldRulesText { get; set; } = string.Empty;

            [JsonPropertyName("fieldHowToTitle")]
            public string FieldHowToTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldHowToText")]
            public string FieldHowToText { get; set; } = string.Empty;

            [JsonPropertyName("fieldEmojiKeyTitle")]
            public string FieldEmojiKeyTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldEmojiKeyText")]
            public string FieldEmojiKeyText { get; set; } = string.Empty;

            [JsonPropertyName("fieldCurrentMetaMapsTitle")]
            public string FieldCurrentMetaMapsTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldCurrentMetaMapsText")]
            public string FieldCurrentMetaMapsText { get; set; } = string.Empty;

            [JsonPropertyName("fieldWarningTitle")]
            public string FieldWarningTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldWarningText")]
            public string FieldWarningText { get; set; } = string.Empty;

            [JsonPropertyName("fieldJumpTitle")]
            public string FieldJumpTitle { get; set; } = string.Empty;

            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; } = string.Empty;

            [JsonPropertyName("thumbnailUrl")]
            public string ThumbnailUrl { get; set; } = string.Empty;

            [JsonPropertyName("color")]
            public string Color { get; set; } = string.Empty;
        }

        private class BottomBookendConfig
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("fieldJumpTitle")]
            public string FieldJumpTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldJumpValue")]
            public string FieldJumpValue { get; set; } = string.Empty;

            [JsonPropertyName("fieldWarningTitle")]
            public string FieldWarningTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldWarningText")]
            public string FieldWarningText { get; set; } = string.Empty;

            [JsonPropertyName("imageUrl")]
            public string ImageUrl { get; set; } = string.Empty;

            [JsonPropertyName("thumbnailUrl")]
            public string ThumbnailUrl { get; set; } = string.Empty;

            [JsonPropertyName("color")]
            public string Color { get; set; } = string.Empty;
        }

        private class ThreadHeaderConfig
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("fieldMapRotationGuideTitle")]
            public string FieldMapRotationGuideTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldMapRotationGuideText")]
            public string FieldMapRotationGuideText { get; set; } = string.Empty;

            [JsonPropertyName("fieldMetaMapsExplainedTitle")]
            public string FieldMetaMapsExplainedTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldMetaMapsExplainedText")]
            public string FieldMetaMapsExplainedText { get; set; } = string.Empty;

            [JsonPropertyName("fieldCurrentMetaMapsTitle")]
            public string FieldCurrentMetaMapsTitle { get; set; } = string.Empty;

            [JsonPropertyName("fieldCurrentMetaMapsText")]
            public string FieldCurrentMetaMapsText { get; set; } = string.Empty;
        }


        // Constructor that loads from JSON
        public CompletedVoteCreationBookendEmbedData(string jsonFilePath, Emoji alertEmoji, Emoji dawnEmoji, Emoji dayEmoji, Emoji duskEmoji, Emoji nightEmoji, Emoji overcastEmoji, Emoji rainEmoji, Emoji sandEmoji, Emoji snowEmoji, AppSettings appSettings, List<string> currentMetaMaps, HashSet<string> enabledVariants)
        {
            var jsonString = File.ReadAllText(jsonFilePath);
            var config = JsonSerializer.Deserialize<EmbedConfig>(jsonString);

            if (config?.TopBookend?.Description != null)
            {
                config.TopBookend.Description = config.TopBookend.Description.Replace("{maxVotesPerVoter}", appSettings.MaxVotesPerVoter.ToString());
            }

            if (config?.TopBookend?.FieldRulesText != null)
            {
                config.TopBookend.FieldRulesText = config.TopBookend.FieldRulesText.Replace("{maxVotesPerVoter}", appSettings.MaxVotesPerVoter.ToString());
            }

            if (config?.TopBookend?.FieldWarningTitle != null)
            {
                config.TopBookend.FieldWarningTitle = config.TopBookend.FieldWarningTitle.Replace("{alertEmoji}", alertEmoji.ToString());
            }

            if (config?.BottomBookend?.FieldWarningTitle != null)
            {
                config.BottomBookend.FieldWarningTitle = config.BottomBookend.FieldWarningTitle.Replace("{alertEmoji}", alertEmoji.ToString());
            }

            // Load top bookend values
            TopBookendTitle = config?.TopBookend?.Title ?? string.Empty;
            TopBookendDescription = config?.TopBookend?.Description ?? string.Empty;
            TopBookendFieldRulesTitle = config?.TopBookend?.FieldRulesTitle ?? string.Empty;
            TopBookendFieldRulesText = config?.TopBookend?.FieldRulesText ?? string.Empty;
            TopBookendFieldHowToTitle = config?.TopBookend?.FieldHowToTitle ?? string.Empty;
            TopBookendFieldHowToText = config?.TopBookend?.FieldHowToText ?? string.Empty;
            TopBookendFieldEmojiKeyTitle = config?.TopBookend?.FieldEmojiKeyTitle ?? string.Empty;

            // Create emoji key text dynamically based on enabled variants
            var emojiKeyLines = new List<string>();
            if (enabledVariants.Contains("Dawn")) emojiKeyLines.Add($"{dawnEmoji} is Dawn.");
            if (enabledVariants.Contains("Day")) emojiKeyLines.Add($"{dayEmoji} is Day.");
            if (enabledVariants.Contains("Dusk")) emojiKeyLines.Add($"{duskEmoji} is Dusk.");
            if (enabledVariants.Contains("Night")) emojiKeyLines.Add($"{nightEmoji} is Night.");
            if (enabledVariants.Contains("Overcast")) emojiKeyLines.Add($"{overcastEmoji} is Overcast.");
            if (enabledVariants.Contains("Rain")) emojiKeyLines.Add($"{rainEmoji} is Rain.");
            if (enabledVariants.Contains("Sandstorm")) emojiKeyLines.Add($"{sandEmoji} is Sandstorm.");
            if (enabledVariants.Contains("Snowstorm")) emojiKeyLines.Add($"{snowEmoji} is Snowstorm.");
            TopBookendFieldEmojiKeyText = string.Join("\n", emojiKeyLines);

            TopBookendFieldCurrentMetaMapsTitle = config?.TopBookend?.FieldCurrentMetaMapsTitle ?? string.Empty;
            TopBookendFieldCurrentMetaMapsText = string.Join("\n", currentMetaMaps);
            TopBookendFieldWarningTitle = config?.TopBookend?.FieldWarningTitle ?? string.Empty;
            TopBookendFieldWarningText = config?.TopBookend?.FieldWarningText ?? string.Empty;
            TopBookendFieldJumpTitle = config?.TopBookend?.FieldJumpTitle ?? string.Empty;
            TopBookendImageUrl = config?.TopBookend?.ImageUrl ?? string.Empty;
            TopBookendThumbnailUrl = config?.TopBookend?.ThumbnailUrl ?? string.Empty;
            TopBookendColor = ColorUtilities.GetDiscordColorFromString((config?.TopBookend?.Color ?? string.Empty));
            BottomBookendColor = ColorUtilities.GetDiscordColorFromString((config?.BottomBookend?.Color ?? string.Empty));
            TopBookendColor = ColorUtilities.GetDiscordColorFromString((config?.TopBookend?.Color ?? string.Empty));

            // Load bottom bookend values
            BottomBookendTitle = config?.BottomBookend?.Title ?? string.Empty;
            BottomBookendDescription = config?.BottomBookend?.Description ?? string.Empty;
            BottomBookendFieldJumpTitle = config?.BottomBookend?.FieldJumpTitle ?? string.Empty;
            BottomBookendFieldJumpValue = config?.BottomBookend?.FieldJumpValue ?? string.Empty;
            BottomBookendFieldWarningTitle = config?.BottomBookend?.FieldWarningTitle ?? string.Empty;
            BottomBookendFieldWarningText = config?.BottomBookend?.FieldWarningText ?? string.Empty;
            BottomBookendImageUrl = config?.BottomBookend?.ImageUrl ?? string.Empty;
            BottomBookendThumbnailUrl = config?.BottomBookend?.ThumbnailUrl ?? string.Empty;
            BottomBookendColor = ColorUtilities.GetDiscordColorFromString((config?.BottomBookend?.Color ?? string.Empty));

            // Load thread header values
            ThreadHeaderTitle = config?.ThreadHeader?.Title ?? string.Empty;
            ThreadHeaderDescription = config?.ThreadHeader?.Description ?? string.Empty;
            ThreadHeaderFieldMapRotationGuideTitle = config?.ThreadHeader?.FieldMapRotationGuideTitle ?? string.Empty;
            ThreadHeaderFieldMapRotationGuideText = config?.ThreadHeader?.FieldMapRotationGuideText ?? string.Empty;
            ThreadHeaderFieldMetaMapsExplainedTitle = config?.ThreadHeader?.FieldMetaMapsExplainedTitle ?? string.Empty;
            ThreadHeaderFieldMetaMapsExplainedText = config?.ThreadHeader?.FieldMetaMapsExplainedText ?? string.Empty;
            ThreadHeaderFieldCurrentMetaMapsTitle = config?.ThreadHeader?.FieldCurrentMetaMapsTitle ?? string.Empty;
            ThreadHeaderFieldCurrentMetaMapsText = string.Join("\n", currentMetaMaps);
        }
    }
}
