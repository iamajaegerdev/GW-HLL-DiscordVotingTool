using Microsoft.AspNetCore.Mvc;
using Services;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Text;

namespace Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuth2CallbackController(OAuth2Service oauth2Service, ILogger<OAuth2CallbackController> logger) : ControllerBase
    {
        private readonly OAuth2Service _oauth2Service = oauth2Service;
        private readonly ILogger<OAuth2CallbackController> _logger = logger;

        [HttpGet("callback")]
        public async Task<IActionResult> HandleCallback(
            [FromQuery] string code, 
            [FromQuery] string? guild_id = null,
            [FromQuery] string? permissions = null)
        {
            _logger.LogInformation(message: $"""Received callback with code: {code}, guild_id: {guild_id}""");
            _logger.LogInformation(message: $"Using redirect URI: {_oauth2Service.GetRedirectUri()}");
            string messageScope = $"""Using scopes: {string.Join(", ", _oauth2Service.GetScopes())}""";
            _logger.LogInformation(message: messageScope);
            
            try
            {
                var tokenResponse = await _oauth2Service.ExchangeCodeForTokenAsync(code);
                _logger.LogInformation("Successfully exchanged code for tokens");
                
                var htmlResponse = @"
<!DOCTYPE html>
<html>
<head>
    <title>Authorization Successful</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background-color: #f0f2f5;
        }
        .messageScope {
            text-align: center;
            padding: 2rem;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            max-width: 400px;
        }
        h1 { color: #43b581; }
        p { color: #4f545c; }
        .details {
            margin-top: 1rem;
            padding: 1rem;
            background: #f8f9fa;
            border-radius: 4px;
            font-size: 0.9em;
            text-align: left;
        }
        .debug-info {
            margin-top: 1rem;
            padding: 1rem;
            background: #e3f2fd;
            border-radius: 4px;
            font-size: 0.8em;
            text-align: left;
            font-family: monospace;
            white-space: pre-wrap;
            word-break: break-all;
        }
    </style>
</head>
<body>
    <div class='messageScope'>
        <h1>Authorization Successful!</h1>
        <p>You can now close this window and return to the application.</p>
        <div class='details'>
            <p><strong>Guild ID:</strong> " + (guild_id ?? "Not provided") + @"</p>
            <p><strong>Permissions:</strong> " + (permissions ?? "Not provided") + @"</p>
        </div>
        <div class='debug-info'>
            <p><strong>Debug Information:</strong></p>
            <p>Redirect URI: " + HttpUtility.HtmlEncode(_oauth2Service.GetRedirectUri()) + @"</p>
            <p>Scopes: " + HttpUtility.HtmlEncode(string.Join(", ", _oauth2Service.GetScopes())) + @"</p>
            <p>Code: " + HttpUtility.HtmlEncode(code) + @"</p>
        </div>
    </div>
    <script>
        setTimeout(() => window.close(), 3000);
    </script>
</body>
</html>";
                
                return Content(htmlResponse, "text/html", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OAuth2 callback");
                var errorHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Authorization Failed</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background-color: #f0f2f5;
        }}
        .messageScope {{
            text-align: center;
            padding: 2rem;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            max-width: 400px;
        }}
        h1 {{ color: #f04747; }}
        p {{ color: #4f545c; }}
        .error-details {{
            margin-top: 1rem;
            padding: 1rem;
            background: #fff5f5;
            border-radius: 4px;
            font-size: 0.9em;
            text-align: left;
            color: #f04747;
        }}
        .redirect-uri {{
            margin-top: 1rem;
            padding: 1rem;
            background: #f8f9fa;
            border-radius: 4px;
            font-size: 0.9em;
            text-align: left;
            word-break: break-all;
        }}
        .debug-info {{
            margin-top: 1rem;
            padding: 1rem;
            background: #e3f2fd;
            border-radius: 4px;
            font-size: 0.8em;
            text-align: left;
            font-family: monospace;
            white-space: pre-wrap;
            word-break: break-all;
        }}
    </style>
</head>
<body>
    <div class='messageScope'>
        <h1>Authorization Failed</h1>
        <p>Error: {HttpUtility.HtmlEncode(ex.Message)}</p>
        <div class='error-details'>
            <p><strong>Please check:</strong></p>
            <ul>
                <li>Your Discord application's OAuth2 redirect URI matches exactly</li>
                <li>Your client ID and client secret are correct</li>
                <li>You have the required scopes enabled</li>
            </ul>
        </div>
        <div class='redirect-uri'>
            <p><strong>Current Redirect URI:</strong></p>
            <p>{HttpUtility.HtmlEncode(_oauth2Service.GetRedirectUri())}</p>
        </div>
        <div class='debug-info'>
            <p><strong>Debug Information:</strong></p>
            <p>Code: {HttpUtility.HtmlEncode(code)}</p>
            <p>Guild ID: {HttpUtility.HtmlEncode(guild_id ?? "Not provided")}</p>
            <p>Permissions: {HttpUtility.HtmlEncode(permissions ?? "Not provided")}</p>
            <p>Scopes: {HttpUtility.HtmlEncode(string.Join(", ", _oauth2Service.GetScopes()))}</p>
        </div>
    </div>
</body>
</html>";
                
                return Content(errorHtml, "text/html", Encoding.UTF8);
            }
        }
    }
} 