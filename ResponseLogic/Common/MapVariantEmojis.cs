using Discord;

namespace ResponseLogic.Common
{
    public static class MapVariantEmojis
    {
        public static readonly Emoji Warning = new("\u26A0");
        public static readonly Emoji Dawn = new("\uD83C\uDF04");
        public static readonly Emoji Day = new("\uD83C\uDF1E");
        public static readonly Emoji Dusk = new("\uD83C\uDF06");
        public static readonly Emoji Night = new("\uD83C\uDF1B");
        public static readonly Emoji Fog = new("\U0001F32B\uFE0F");
        public static readonly Emoji Overcast = new("\uD83C\uDF25");
        public static readonly Emoji Rain = new("\uD83D\uDCA7");
        public static readonly Emoji Sand = new("\uD83C\uDF2A\uFE0F");
        public static readonly Emoji Snow = new("\u2744");

        public static Emoji? GetEmoji(string variant)
        {
            return variant.ToLower() switch
            {
                "warning" => Warning,
                "dawn" => Dawn,
                "day" => Day,
                "dusk" => Dusk,
                "night" => Night,
                "fog" => Fog,
                "overcast" => Overcast,
                "rain" => Rain,
                "sand" => Sand,
                "snow" => Snow,
                _ => null
            };
        }
    }
} 