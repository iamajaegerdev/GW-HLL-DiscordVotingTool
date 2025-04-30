namespace Services
{
    public class OAuth2CredentialManager
    {
        private const string CLIENT_ID_KEY = "HLLMapRotationVotingTool_OAuth2_ClientId";
        private const string CLIENT_SECRET_KEY = "HLLMapRotationVotingTool_OAuth2_ClientSecret";

        public static void SaveOAuth2Credentials(string clientId, string clientSecret)
        {
            CredentialManager.SaveCredential(CLIENT_ID_KEY, clientId);
            CredentialManager.SaveCredential(CLIENT_SECRET_KEY, clientSecret);
        }

        public static (string ClientId, string ClientSecret) RetrieveOAuth2Credentials()
        {
            try
            {
                string clientId = CredentialManager.RetrieveCredential(CLIENT_ID_KEY);
                string clientSecret = CredentialManager.RetrieveCredential(CLIENT_SECRET_KEY);
                return (clientId, clientSecret);
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Failed to retrieve OAuth2 credentials: {ex.Message}");
                throw;
            }
        }

        public static bool CheckOAuth2Credentials()
        {
            bool hasClientId = CredentialChecker.CheckCredentials(CLIENT_ID_KEY);
            bool hasClientSecret = CredentialChecker.CheckCredentials(CLIENT_SECRET_KEY);
            return hasClientId && hasClientSecret;
        }

        public static void PromptAndSaveOAuth2Credentials()
        {
            Console.Write("Enter your OAuth2 Client ID: ");
            string clientId = ReadSecureInput();
            Console.Write("\nEnter your OAuth2 Client Secret: ");
            string clientSecret = ReadSecureInput();

            SaveOAuth2Credentials(clientId, clientSecret);
            Logger.LogWithTimestamp("OAuth2 credentials stored securely.");
        }

        private static string ReadSecureInput()
        {
            string input = string.Empty;
            ConsoleKeyInfo keyInfo;

            do
            {
                keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key != ConsoleKey.Enter && keyInfo.Key != ConsoleKey.Backspace)
                {
                    input += keyInfo.KeyChar;
                    Console.Write("*");
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input[..^1];
                    Console.Write("\b \b");
                }
            }
            while (keyInfo.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return input;
        }
    }
} 