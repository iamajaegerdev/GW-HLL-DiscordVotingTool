using Discord;

namespace Utilities
{
    public static class ColorUtilities
    {
        public static Color GetDiscordColorFromString(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "blue" => Color.Blue,
                "red" => Color.Red,
                "green" => Color.Green,
                "orange" => Color.Orange,
                "purple" => Color.Purple,
                "magenta" => Color.Magenta,
                "teal" => Color.Teal,
                "gold" => Color.Gold,
                "darkblue" => Color.DarkBlue,
                "darkred" => Color.DarkRed,
                "darkgreen" => Color.DarkGreen,
                "darkorange" => Color.DarkOrange,
                "darkpurple" => Color.DarkPurple,
                "darkmagenta" => Color.DarkMagenta,
                "darkteal" => Color.DarkTeal,
                "darkgray" => Color.DarkGrey,
                "lightgray" => Color.LightGrey,
                "lightorange" => Color.LightOrange,
                "lightergray" => Color.LighterGrey,
                // Add more color mappings as needed
                _ => Color.Default
            };
        }
    }
} 