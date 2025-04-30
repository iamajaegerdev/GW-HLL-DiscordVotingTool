using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using Services;
using DiscordBotTextCommands;
using DiscordBotSlashInteractions;
using ResponseLogic.CreateMapRotationAsyncEmojiReactionVoteChannel;

namespace GWHLLDiscordVotingTool
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddJsonFile("Resources/hellletloosemapdata.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("Resources/englishalphabetimageurls.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("Resources/initialvotecreationembeddata.json", optional: false, reloadOnChange: true)
                        .AddJsonFile("Resources/completedvotecreationembeddata.json", optional: false, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        var useCertificate = false; // Set this to true when you have your certificate ready

                        if (useCertificate)
                        {
                            try
                            {
                                // HTTPS configuration - commented out until certificate is ready
                                options.ListenLocalhost(5000, listenOptions =>
                                {
                                    // Load certificate from file
                                    var certPath = "/path/to/your-certificate.pfx";
                                    var certPassword = "your-certificate-password";

                                    var certificate = new X509Certificate2(certPath, certPassword);
                                    listenOptions.UseHttps(certificate);
                                });

                                Console.WriteLine("HTTPS configured successfully on port 5000");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to configure HTTPS: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("HTTPS not configured - certificate not installed");
                        }

                        // Always set up HTTP fallback
                        options.ListenLocalhost(5001);
                        Console.WriteLine("HTTP configured on port 5001");
                    })
                    .UseUrls("https://localhost:5000", "http://localhost:5001")
                    .Configure(app =>
                    {
                        // Enable detailed error pages in development
                        app.UseDeveloperExceptionPage();
                        
                        // Add response compression
                        app.UseResponseCompression();
                        
                        // Add security headers middleware
                        app.Use(async (context, next) =>
                        {
                            // Add security headers middleware
                            app.Use(async (context, next) =>
                            {
                                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                                context.Response.Headers.Append("X-Frame-Options", "DENY");
                                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                                context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
                                await next();
                            });
                            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                            context.Response.Headers.Append("X-Frame-Options", "DENY");
                            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
                            await next();
                        });

                        // Global error handling middleware
                        app.Use(async (context, next) =>
                        {
                            try
                            {
                                Logger.LogWithTimestamp($"Request started: {context.Request.Method} {context.Request.Path}");
                                await next();
                                Logger.LogWithTimestamp($"Request completed: {context.Request.Method} {context.Request.Path} - Status: {context.Response.StatusCode}");
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWithTimestamp($"Request error: {context.Request.Method} {context.Request.Path} - Error: {ex}");
                                throw;
                            }
                        });

                        // Add core middleware in the correct order
                        app.UseHttpsRedirection();
                        app.UseRouting();
                        app.UseStaticFiles();
                        
                        app.UseAuthentication();
                        app.UseAuthorization();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            
                            // Add a test endpoint
                            endpoints.MapGet("/test", async context =>
                            {
                                context.Response.ContentType = "text/plain";
                                await context.Response.WriteAsync("Web server is running!");
                            });

                            // Add a catch-all endpoint for debugging
                            endpoints.MapFallback(async context =>
                            {
                                Logger.LogWithTimestamp($"Fallback handler hit for: {context.Request.Method} {context.Request.Path}");
                                context.Response.ContentType = "text/plain";
                                await context.Response.WriteAsync("Endpoint not found");
                            });
                        });
                    });
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure HTTPS with enhanced security settings
                    services.AddHttpsRedirection(options =>
                    {
                        options.HttpsPort = 5000;
                        options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    });

                    // Configure HSTS with proper settings
                    services.AddHsts(options =>
                    {
                        options.MaxAge = TimeSpan.FromDays(365);
                        options.IncludeSubDomains = true;
                        options.Preload = true;
                        options.ExcludedHosts.Add("localhost");
                    });

                    // Add response compression
                    services.AddResponseCompression(options =>
                    {
                        options.EnableForHttps = true;
                        options.Providers.Add<BrotliCompressionProvider>();
                        options.Providers.Add<GzipCompressionProvider>();
                    });

                    // Add MVC services with detailed error handling
                    services.AddMvc()
                        .ConfigureApiBehaviorOptions(options =>
                        {
                            options.SuppressModelStateInvalidFilter = false;
                            options.InvalidModelStateResponseFactory = actionContext =>
                            {
                                Logger.LogWithTimestamp($"Model validation failed: {string.Join(", ", actionContext.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                                return new BadRequestObjectResult(actionContext.ModelState);
                            };
                        });

                    // Add core services
                    services.AddAuthentication();
                    services.AddAuthorization();

                    // Make sure controllers are added before other services
                    services.AddControllers()
                        .AddApplicationPart(typeof(OAuth2CallbackController).Assembly);
                    
                    // Add web-related services first
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();

                    // Bind configuration section to a strongly typed class
                    services.Configure<HellLetLooseMapData>(context.Configuration);
                    services.Configure<AppSettings>(context.Configuration.GetSection("TextCommandTriggers"));
                    services.Configure<AppSettings>(context.Configuration.GetSection("DiscordTargets"));
                    services.Configure<AppSettings>(context.Configuration.GetSection("VotingRequirements"));
                    services.Configure<AppSettings>(context.Configuration.GetSection("ApplicationPersonalization"));
                    services.Configure<AppSettings>(context.Configuration.GetSection("HLLMapVariantOptions"));
                    services.Configure<OAuth2Config>(context.Configuration.GetSection("OAuth2"));

                    // Register your service that will use the configuration
                    services.AddTransient<CreateMapRotationAsyncEmojiReactionVoteChannelTextCommand>();
                    services.AddTransient<CreateMapRotationAsyncEmojiReactionVoteChannelSlashInteraction>();
                    services.AddTransient<TallyAsyncEmojiReactionVotesTextCommand>();
                    services.AddTransient<TallyAsyncEmojiReactionVotesSlashInteraction>();

                    // Add Discord services
                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<InteractionService>(sp => 
                        new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));
                    
                    // Add BotConfig using Windows Credential Manager
                    services.AddSingleton<BotConfig>(sp =>
                    {
                        var token = CredentialManager.RetrieveCredential("HLLMapRotationVotingTool");
                        if (string.IsNullOrEmpty(token))
                        {
                            throw new InvalidOperationException("Bot token not found in Windows Credential Manager. Please run the application normally first to set up credentials.");
                        }
                        return new BotConfig(token);
                    });
                    
                    services.AddSingleton<IBot, Bot>();
                    services.AddSingleton<CommandHandler>();
                    services.AddSingleton<InteractionHandler>();
                    services.AddTransient<InstallSlashInteractionsTextCommand>();

                    // Add other services
                    services.AddSingleton<DiscordRateLimitService>();
                    services.AddSingleton<DiscordReactionService>();
                    services.AddSingleton<OAuth2Service>();

                    // We'll register BotConfig later when we have the token
                })
                .Build();

                // Start both web host and application
                var appSettings = host.Services.GetRequiredService<IOptions<AppSettings>>().Value;
                
                if (appSettings.EnableOAuth2WebService)
                {
                    Logger.LogWithTimestamp("Starting web server...");
                    await host.StartAsync();
                    Logger.LogWithTimestamp("Web server started successfully at https://localhost:5000");

                    // Test the web server is actually listening
                    using var httpClient = new HttpClient();
                    try
                    {
                        var response = await httpClient.GetAsync("https://localhost:5000/test");
                        if (response.IsSuccessStatusCode)
                        {
                            Logger.LogWithTimestamp("Web server test endpoint responded successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp($"Failed to connect to test endpoint: {ex.Message}");
                    }
                }
                else
                {
                    Logger.LogWithTimestamp("OAuth2 web service is disabled. Skipping web server startup.");
                }

                using (var serviceScope = host.Services.CreateScope())
                {
                    var services = serviceScope.ServiceProvider;

                    var configuration = services.GetRequiredService<IConfiguration>();

                    Logger.LogWithTimestamp($"Begin startup checks");

                    // Log configuration values
                    Logger.LogWithTimestamp("Checking configuration values:");
                    Logger.LogWithTimestamp($"- TextCommandTriggers exists: {configuration.GetSection("TextCommandTriggers").Exists()}");
                    Logger.LogWithTimestamp($"- DiscordTargets exists: {configuration.GetSection("DiscordTargets").Exists()}");
                    Logger.LogWithTimestamp($"- VotingRequirements exists: {configuration.GetSection("VotingRequirements").Exists()}");
                    Logger.LogWithTimestamp($"- ApplicationPersonalization exists: {configuration.GetSection("ApplicationPersonalization").Exists()}");

                    // Debug configuration values
                    Logger.LogWithTimestamp("Debugging configuration:");
                    Logger.LogWithTimestamp($"- Configuration root: {configuration.GetType().Name}");
                    Logger.LogWithTimestamp($"- Configuration children: {string.Join(", ", configuration.GetChildren().Select(c => c.Key))}");
                    
                    // Try to get the Maps section directly
                    var mapsSection = configuration.GetSection("Maps");
                    Logger.LogWithTimestamp($"- Maps section exists: {mapsSection.Exists()}");
                    if (mapsSection.Exists())
                    {
                        Logger.LogWithTimestamp($"- Maps section value: {mapsSection.Value}");
                    }

                    // Check HellLetLooseMapData configuration
                    var hellLetLooseMapData = configuration.Get<HellLetLooseMapData>();
                    if (hellLetLooseMapData == null)
                    {
                        Logger.LogWithTimestamp("Configuration error: HellLetLooseMapData could not be loaded from configuration.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                    Logger.LogWithTimestamp($"HellLetLooseMapData loaded successfully. Number of maps: {hellLetLooseMapData.Maps?.Count ?? 0}");

                    //load textcommandtriggers
                    var textCommandTriggers = configuration.GetSection("TextCommandTriggers").Get<AppSettings>();

                    //load discordtarget settings
                    var discordTargets = configuration.GetSection("DiscordTargets").Get<AppSettings>();

                    //load votingrequirement settings
                    var votingRequirements = configuration.GetSection("VotingRequirements").Get<AppSettings>();

                    //load application personalization settings
                    var applicationPersonalization = configuration.GetSection("ApplicationPersonalization").Get<AppSettings>();

                    if (textCommandTriggers == null) {
                        Logger.LogWithTimestamp("Configuration error: TextCommandTriggers section is missing in the configuration file.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }

                    if (discordTargets == null)
                    {
                        Logger.LogWithTimestamp("Configuration error: DiscordTargets section is missing in the configuration file.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }

                    if (votingRequirements == null)
                    {
                        Logger.LogWithTimestamp("Configuration error: VotingRequirements section is missing in the configuration file.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }

                    if (applicationPersonalization == null)
                    {
                        Logger.LogWithTimestamp("Configuration error: ApplicationPersonalization section is missing in the configuration file.");
                        Console.ReadLine();
                        Environment.Exit(1);
                    }             

                    Logger.LogWithTimestamp("AppSettings loaded.");
                    Logger.LogWithTimestamp($"Guild Target:{discordTargets.GuildId}");
                    Logger.LogWithTimestamp($"Category Target: {discordTargets.CategoryId}");
                    Logger.LogWithTimestamp($"VotingRoleId1:{discordTargets.VotingRoleId1}");
                    Logger.LogWithTimestamp($"VotingRoleId2:{discordTargets.VotingRoleId2}");
                    Logger.LogWithTimestamp($"Max Votes:{votingRequirements.MaxVotesPerVoter}");
                    Logger.LogWithTimestamp($"Number of Winners:{votingRequirements.NumberOfWinners}");
                    Logger.LogWithTimestamp($"Max Votes:{votingRequirements.MaxVotesPerVoter}");
                    Logger.LogWithTimestamp($"Append Remainder to Channel Name:{applicationPersonalization.AppendRemainderToChannel}");
                    Logger.LogWithTimestamp($"Auto Append Date to Map Vote Channel After Remainder:{applicationPersonalization.AutoAppendDateToChannelAfterRemainder}");
                    Logger.LogWithTimestamp($"Available commands: {textCommandTriggers.PingTextCommandTrigger}, {textCommandTriggers.InstallSlashInteractionsTextCommandTrigger}, {textCommandTriggers.CreateMapRotationVoteTextChannelTextCommandTrigger}, {textCommandTriggers.TallyReactionVotesTextCommandTrigger}");

                    // Log map variant options
                    var mapVariantOptions = configuration.GetSection("HLLMapVariantOptions").Get<AppSettings>();
                    if (mapVariantOptions != null)
                    {
                        Logger.LogWithTimestamp("Map Variant Options:");
                        Logger.LogWithTimestamp($"- EnableDawn: {mapVariantOptions.EnableDawn}");
                        Logger.LogWithTimestamp($"- EnableDay: {mapVariantOptions.EnableDay}");
                        Logger.LogWithTimestamp($"- EnableDusk: {mapVariantOptions.EnableDusk}");
                        Logger.LogWithTimestamp($"- EnableNight: {mapVariantOptions.EnableNight}");
                        Logger.LogWithTimestamp($"- EnableRain: {mapVariantOptions.EnableRain}");
                        Logger.LogWithTimestamp($"- EnableSandstorm: {mapVariantOptions.EnableSandstorm}");
                        Logger.LogWithTimestamp($"- EnableSnowstorm: {mapVariantOptions.EnableSnowstorm}");
                    }
                    else
                    {
                        Logger.LogWithTimestamp("Warning: HLLMapVariantOptions section not found in configuration");
                    }

                    // Check bot token
                    string retrievedToken = "sensitive token";
                    Logger.LogWithTimestamp("Checking for secure bot token.");
                    bool hasToken = CredentialChecker.CheckCredentials("HLLMapRotationVotingTool");
                    if (hasToken)
                    {
                        Logger.LogWithTimestamp("Secure bot token found.");
                        retrievedToken = CredentialManager.RetrieveCredential("HLLMapRotationVotingTool");
                        Logger.LogWithTimestamp("Retrieved secure bot token.");
                    }
                    else
                    {
                        Logger.LogWithTimestamp("Error: Secure bot token not found.");
                        Console.Write("Enter your bot token: ");

                        string token = ReadTokenFromConsole();
                        Logger.LogWithTimestamp("\nBot token entered successfully.");

                        CredentialManager.SaveCredential("HLLMapRotationVotingTool", token);
                        Logger.LogWithTimestamp("Bot token stored securely.");

                        retrievedToken = CredentialManager.RetrieveCredential("HLLMapRotationVotingTool");
                        Logger.LogWithTimestamp("Retrieved secure bot token.");
                    }

                    if (appSettings.EnableOAuth2WebService)
                    {
                        // Check OAuth2 credentials
                        Logger.LogWithTimestamp("Checking for secure OAuth2 credentials.");
                        if (!OAuth2CredentialManager.CheckOAuth2Credentials())
                        {
                            Logger.LogWithTimestamp("OAuth2 credentials not found. Please enter them now.");
                            OAuth2CredentialManager.PromptAndSaveOAuth2Credentials();
                        }
                        else
                        {
                            Logger.LogWithTimestamp("OAuth2 credentials found.");
                        } 
                    }

                    // Create a new service collection with all the existing services
                    var serviceCollection = new ServiceCollection();
                    
                    // Add all the configuration bindings
                    serviceCollection.Configure<HellLetLooseMapData>(configuration);
                    serviceCollection.Configure<AppSettings>(configuration.GetSection("TextCommandTriggers"));
                    serviceCollection.Configure<AppSettings>(configuration.GetSection("DiscordTargets"));
                    serviceCollection.Configure<AppSettings>(configuration.GetSection("VotingRequirements"));
                    serviceCollection.Configure<AppSettings>(configuration.GetSection("ApplicationPersonalization"));
                    serviceCollection.Configure<AppSettings>(configuration.GetSection("HLLMapVariantOptions"));
                    serviceCollection.Configure<OAuth2Config>(configuration.GetSection("OAuth2"));

                    // Add all the services
                    serviceCollection.AddTransient<CreateMapRotationAsyncEmojiReactionVoteChannelTextCommand>();
                    serviceCollection.AddTransient<CreateMapRotationAsyncEmojiReactionVoteChannelSlashInteraction>();
                    serviceCollection.AddTransient<TallyAsyncEmojiReactionVotesTextCommand>();
                    serviceCollection.AddTransient<TallyAsyncEmojiReactionVotesSlashInteraction>();

                    // Add BotConfig with the actual token first
                    serviceCollection.AddSingleton(new BotConfig(retrievedToken));
                    
                    // Now register DiscordSocketClient with the config
                    serviceCollection.AddSingleton(sp => 
                        new DiscordSocketClient(sp.GetRequiredService<BotConfig>().SocketConfig));
                    
                    serviceCollection.AddSingleton<CommandService>();
                    serviceCollection.AddSingleton<InteractionService>(sp => 
                        new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));
                    serviceCollection.AddSingleton<IBot, Bot>();
                    serviceCollection.AddSingleton<CommandHandler>();
                    serviceCollection.AddSingleton<InteractionHandler>();
                    serviceCollection.AddSingleton<DiscordRateLimitService>();
                    serviceCollection.AddSingleton<DiscordReactionService>();
                    serviceCollection.AddSingleton<OAuth2Service>();

                    // Build a new service provider
                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    try
                    {
                        Logger.LogWithTimestamp("Application starting.");

                        var bot = serviceProvider.GetRequiredService<IBot>();
                        await bot.StartAsync(serviceProvider);

                        // Initialize both command and interaction handlers
                        await serviceProvider.GetRequiredService<CommandHandler>().InstallCommandsAsync();
                        await serviceProvider.GetRequiredService<InteractionHandler>().InitializeAsync();

                        Logger.LogWithTimestamp("Bot started successfully.");

                        while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                        {
                            // Check for shutdown command (Q key)
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(intercept: true);
                                if (key.Key == ConsoleKey.Q)
                                {
                                    await ShutdownApplicationAsync(bot);
                                    break;
                                }
                            }
                        }
                        
                        Logger.LogWithTimestamp("Shutdown initiated by user.");
                        await bot.StopAsync();

                        Logger.LogWithTimestamp("Bot stopped successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWithTimestamp("An unhandled exception occurred. Exiting the application.");
                        Logger.LogWithTimestamp($"{ex}");
                        Environment.Exit(-1);
                    }
                }

                await host.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Critical error starting application: {ex}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        private static async Task ShutdownApplicationAsync(IBot bot)
        {
            try
            {
                Logger.LogWithTimestamp("Initiating graceful shutdown...");
                
                // Stop the Discord bot
                await bot.StopAsync();
                Logger.LogWithTimestamp("Discord bot stopped successfully.");

                // Give a moment for any pending operations to complete
                await Task.Delay(1000);
                
                Logger.LogWithTimestamp("Application shutdown complete. Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Logger.LogWithTimestamp($"Error during shutdown: {ex.Message}");
                Logger.LogWithTimestamp("Forcing application exit...");
                Environment.Exit(1);
            }
        }

        private static string ReadTokenFromConsole()
        {
            string token = string.Empty;
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key != ConsoleKey.Enter && keyInfo.Key != ConsoleKey.Backspace)
                {
                    token += keyInfo.KeyChar;
                    Console.Write("*");
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && token.Length > 0)
                {
                    token = token[..^1];
                    Console.Write("\b \b");
                }
            }
            while (keyInfo.Key != ConsoleKey.Enter);
            return token;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<AppSettings>(context.Configuration.GetSection("DiscordTargets"));
                    services.AddTransient<CreateMapRotationAsyncEmojiReactionVoteChannelTextCommand>();
                });
    }
}