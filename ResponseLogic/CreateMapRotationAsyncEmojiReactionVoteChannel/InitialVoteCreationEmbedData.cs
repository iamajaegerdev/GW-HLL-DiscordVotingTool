using Discord;
using System.Text.Json;

namespace ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel
{
    public class InitialVoteCreationEmbedData
    {
        // Properties
        public string InitialEmbedTitle { get; set; } = string.Empty;
        public string InitialEmbedDescription { get; set; } = string.Empty;
        public string InitialEmbedWarningFieldTitle { get; set; } = string.Empty;
        public string InitialEmbedWarningFieldValue { get; set; } = string.Empty;
        public string InitialEmbedImageUrl { get; set; } = string.Empty;
        public string InitialEmbedThumbnailUrl { get; set; } = string.Empty;
        public string InitialEmbedFooterText { get; set; } = string.Empty;
        public string InitialEmbedFooterIconUrl { get; set; } = string.Empty;
        public string InitialEmbedColor { get; set; } = string.Empty;

        // Parameterless constructor for JSON deserialization
        public InitialVoteCreationEmbedData()
        {
        }

        // Constructor that loads from JSON
        public InitialVoteCreationEmbedData(string jsonFilePath, Emoji alertEmoji)
        {
            var jsonString = File.ReadAllText(jsonFilePath);
            var config = JsonSerializer.Deserialize<InitialVoteCreationEmbedData>(jsonString);

            if (config != null)
            {
                InitialEmbedTitle = config.InitialEmbedTitle ?? string.Empty;
                InitialEmbedDescription = config.InitialEmbedDescription ?? string.Empty;
                InitialEmbedWarningFieldTitle = config.InitialEmbedWarningFieldTitle?.Replace("⚠️", alertEmoji.ToString()) ?? string.Empty;
                InitialEmbedWarningFieldValue = config.InitialEmbedWarningFieldValue ?? string.Empty;
                InitialEmbedImageUrl = config.InitialEmbedImageUrl ?? string.Empty;
                InitialEmbedThumbnailUrl = config.InitialEmbedThumbnailUrl ?? string.Empty;
                InitialEmbedFooterText = config.InitialEmbedFooterText ?? string.Empty;
                InitialEmbedFooterIconUrl = config.InitialEmbedFooterIconUrl ?? string.Empty;
                InitialEmbedColor = config.InitialEmbedColor ?? string.Empty;
            }
        }
    }
}
