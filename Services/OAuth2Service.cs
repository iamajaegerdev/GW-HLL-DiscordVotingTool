using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;

namespace Services
{
    public class OAuth2Service
    {
        private readonly HttpClient _httpClient;
        private readonly OAuth2Config _config;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string DISCORD_API_ENDPOINT = "https://discord.com/api/v10";
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        public OAuth2Service(IOptions<OAuth2Config> config)
        {
            _config = config.Value;
            (_clientId, _clientSecret) = OAuth2CredentialManager.RetrieveOAuth2Credentials();

            _httpClient = new HttpClient();

            // Add default headers
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GWHLLDiscordVotingTool/1.0");

            Logger.LogWithTimestamp($"OAuth2Service initialized with client ID: {_clientId}");
            Logger.LogWithTimestamp($"Using redirect URI: {_config.RedirectUri}");
            Logger.LogWithTimestamp($"Using scopes: {string.Join(", ", _config.Scopes)}");
        }

        public string GetAuthorizationUrl(string state)
        {
            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["redirect_uri"] = _config.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = string.Join(" ", _config.Scopes),
                ["state"] = state
            };

            var queryString = string.Join("&", queryParams.Select(kvp =>
                $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

            return $"https://discord.com/oauth2/authorize?{queryString}";
        }

        public async Task<OAuth2TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var tokenRequestParams = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _config.RedirectUri
            };

            var content = new FormUrlEncodedContent(tokenRequestParams);

            // Add proper headers
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            try
            {
                Logger.LogWithTimestamp($"Making token request to Discord API with redirect URI: {_config.RedirectUri}");
                Logger.LogWithTimestamp($"Request parameters: client_id={_clientId}, grant_type=authorization_code, code={code}");

                // Use the full URL for the token endpoint
                var tokenEndpoint = "https://discord.com/api/v10/oauth2/token";
                Logger.LogWithTimestamp($"Token endpoint: {tokenEndpoint}");

                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Logger.LogWithTimestamp($"Discord API Response Status: {response.StatusCode}");
                Logger.LogWithTimestamp($"Discord API Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
                Logger.LogWithTimestamp($"Discord API Response Body: {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = $"Discord API returned {response.StatusCode}: {responseString}";
                    Logger.LogWithTimestamp($"Error: {errorMessage}");
                    throw new Exception(errorMessage);
                }

                try
                {
                    var tokenResponse = JsonSerializer.Deserialize<OAuth2TokenResponse>(responseString, _jsonSerializerOptions);
                    if (tokenResponse == null)
                    {
                        var nullError = "Token response was null";
                        Logger.LogWithTimestamp($"Error: {nullError}");
                        throw new Exception(nullError);
                    }

                    Logger.LogWithTimestamp("Successfully parsed token response");
                    return tokenResponse;
                }
                catch (JsonException jsonEx)
                {
                    var jsonError = $"Failed to parse JSON response: {jsonEx.Message}";
                    Logger.LogWithTimestamp($"Error: {jsonError}");
                    Logger.LogWithTimestamp($"Raw response: {responseString}");
                    throw new Exception(jsonError);
                }
            }
            catch (HttpRequestException httpEx)
            {
                var httpError = $"HTTP request failed: {httpEx.Message}";
                Logger.LogWithTimestamp($"Error: {httpError}");
                throw new Exception(httpError);
            }
        }

        public async Task<OAuth2TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var refreshParams = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(refreshParams);
            var response = await _httpClient.PostAsync("/oauth2/token", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to refresh token: {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OAuth2TokenResponse>(responseString, _jsonSerializerOptions)
                ?? throw new Exception("Failed to deserialize token response");
        }

        public async Task RevokeTokenAsync(string token, string tokenType = "access_token")
        {
            var revokeParams = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["token"] = token,
                ["token_type_hint"] = tokenType
            };

            var content = new FormUrlEncodedContent(revokeParams);
            var response = await _httpClient.PostAsync("/oauth2/token/revoke", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to revoke token: {error}");
            }
        }

        public string GetRedirectUri() => _config.RedirectUri;
        public string[] GetScopes() => _config.Scopes;
    }
} 