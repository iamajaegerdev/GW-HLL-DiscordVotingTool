namespace Services
{
    public class OAuth2Config
    {
        public const string ConfigurationSection = "OAuth2";
        
        public string RedirectUri { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = [];

        public void Validate()
        {            
            if (string.IsNullOrEmpty(RedirectUri))
                throw new ArgumentException("RedirectUri is required", nameof(RedirectUri));
            
            if (Scopes == null || Scopes.Length == 0)
                throw new ArgumentException("At least one scope is required", nameof(Scopes));
        }
    }
} 