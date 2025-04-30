namespace ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel
{
    public class HellLetLooseMapData
    {
        public List<MapInfo> Maps { get; set; } = [];
    }

    public class MapInfo
    {
        public string MapName { get; set; } = string.Empty;
        public MapDetails MapDetails { get; set; } = new();
    }

    public class MapDetails
    {
        public string Enabled { get; set; } = string.Empty;
        public string Allies { get; set; } = string.Empty;
        public string Axis { get; set; } = string.Empty;
        public string Dawn { get; set; } = string.Empty;
        public string Day { get; set; } = string.Empty;
        public string Dusk { get; set; } = string.Empty;
        public string Night { get; set; } = string.Empty;
        public string Fog { get; set; } = string.Empty;
        public string Overcast { get; set; } = string.Empty;
        public string Rain { get; set; } = string.Empty;
        public string Sandstorm { get; set; } = string.Empty;
        public string Snowstorm { get; set; } = string.Empty;
    }
} 