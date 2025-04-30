using System.Text.Json;
using Services;

namespace ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel
{
    public class EnglishAlphabetImageUrls
    {
        public Dictionary<char, string> AlphabetIndexImages { get; set; } = [];
        public bool IsUsingDefaultUrls { get; private set; }

        public EnglishAlphabetImageUrls()
        {
            LoadFromJson();
        }

        private void LoadFromJson()
        {
            try
            {
                // Try multiple possible locations for the JSON file
                string[] possiblePaths = [
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "englishalphabetimageurls.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "englishalphabetimageurls.json"),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "englishalphabetimageurls.json")
                ];

                string? jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        jsonPath = path;
                        Logger.LogWithTimestamp($"Found alphabet image URLs configuration file at: {jsonPath}");
                        break;
                    }
                    else
                    {
                        Logger.LogWithTimestamp($"Alphabet image URLs configuration file not found at: {path}");
                    }
                }

                if (jsonPath == null)
                {
                    throw new FileNotFoundException($"Alphabet image URLs configuration file not found in any of the expected locations");
                }

                string jsonContent = File.ReadAllText(jsonPath);
                Logger.LogWithTimestamp("Successfully read JSON content");
                
                var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                Logger.LogWithTimestamp("Successfully deserialized JSON content");
                
                AlphabetIndexImages = [];
                var alphabetImages = jsonData.GetProperty("alphabetIndexImages");
                
                foreach (var property in alphabetImages.EnumerateObject())
                {
                    if (property.Name.Length == 1)
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        AlphabetIndexImages.Add(property.Name[0], property.Value.GetString());
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }

                if (AlphabetIndexImages.Count == 0)
                {
                    throw new InvalidOperationException("No valid alphabet image URLs were loaded from the configuration file.");
                }

                IsUsingDefaultUrls = false;
                Logger.LogWithTimestamp($"Successfully loaded {AlphabetIndexImages.Count} custom alphabet image URLs");
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error loading alphabet image URLs from JSON: {ex.Message}");
                Logger.LogWithTimestamp($"Stack trace: {ex.StackTrace}");
                Logger.LogWithTimestamp("Falling back to default alphabet image URLs.");
                InitializeDefaultUrls();
                IsUsingDefaultUrls = true;
            }
        }

        private void InitializeDefaultUrls()
        {
            AlphabetIndexImages = new Dictionary<char, string>
            {
                { 'A', "https://i.imgur.com/POhK1wg.png" },
                { 'B', "https://i.imgur.com/dPSb43g.png" },
                { 'C', "https://i.imgur.com/0ZqSEoF.png" },
                { 'D', "https://i.imgur.com/QhX8yyq.png" },
                { 'E', "https://i.imgur.com/05Qptv0.png" },
                { 'F', "https://i.imgur.com/0o6XxLP.png" },
                { 'G', "https://i.imgur.com/VnLBUh4.png" },
                { 'H', "https://i.imgur.com/XQqLMqz.png" },
                { 'I', "https://i.imgur.com/Zvm6C3y.png" },
                { 'J', "https://i.imgur.com/DKyyX7X.png" },
                { 'K', "https://i.imgur.com/4avkjXI.png" },
                { 'L', "https://i.imgur.com/Thopl4h.png" },
                { 'M', "https://i.imgur.com/GpmrghY.png" },
                { 'N', "https://i.imgur.com/S5rtZpu.png" },
                { 'O', "https://i.imgur.com/JQnRkpf.png" },
                { 'P', "https://i.imgur.com/m0mJLDh.png" },
                { 'Q', "https://i.imgur.com/C4J5lwg.png" },
                { 'R', "https://i.imgur.com/dKAHBu6.png" },
                { 'S', "https://i.imgur.com/zhNa1Xa.png" },
                { 'T', "https://i.imgur.com/Xwn7NvD.png" },
                { 'U', "https://i.imgur.com/8PGSkKi.png" },
                { 'V', "https://i.imgur.com/qsWPk9P.png" },
                { 'W', "https://i.imgur.com/eyUv28A.png" },
                { 'X', "https://i.imgur.com/TwMskEW.png" },
                { 'Y', "https://i.imgur.com/khOlTn3.png" },
                { 'Z', "https://i.imgur.com/viRUrC9.png" }
            };
        }
    }
}
