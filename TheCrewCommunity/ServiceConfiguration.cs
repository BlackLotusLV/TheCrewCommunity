﻿using System.Security.Claims;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.InteractionNamingPolicies;
using DSharpPlus.Commands.Processors.UserCommands;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.LiveBot;
using TheCrewCommunity.LiveBot.DiscordEventHandlers;
using TheCrewCommunity.Services;


namespace TheCrewCommunity;

public static class ServiceConfiguration
{
    public static IServiceCollection AddMyServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        ConfigurationManager configuration = builder.Configuration;
        string token = configuration.GetSection("Discord")["BotToken"] ?? throw new InvalidOperationException("Bot token not provided!");
        services.AddDistributedMemoryCache();
        services.AddDiscordClient(token, DiscordIntents.All);
        services.AddHostedService<LiveBotService>();
        services.AddHostedService<ModMailCleanupService>();
        services.AddSingleton<StreamNotificationService>();
        services.AddHostedService(provider => provider.GetRequiredService<StreamNotificationService>());
        services.AddHostedService<WebRoleManagerService>();
        services.AddHostedService<ThisOrThatLeaderboardService>();
        services.AddSingleton<IThisOrThatLeaderboardService>(provider => 
        {
            return (ThisOrThatLeaderboardService)provider.GetServices<IHostedService>()
                .First(service => service is ThisOrThatLeaderboardService);
        });
        services.AddHostedService<ThisOrThatDailyVoteService>();
        services.AddSingleton<IThisOrThatDailyVoteService>(provider =>
        {
            return (ThisOrThatDailyVoteService)provider.GetServices<IHostedService>()
                .First(service => service is ThisOrThatDailyVoteService);
        });
        
        services.AddSingleton<IModeratorLoggingService, ModeratorLoggingService>();
        services.AddSingleton<IModeratorWarningService, ModeratorWarningService>();
        services.AddSingleton<IModMailService, ModMailService>();
        services.AddSingleton<IDatabaseMethodService, DatabaseMethodService>();
        services.AddSingleton<ICloudFlareImageService, CloudFlareImageService>();
        services.AddSingleton<IUserActivityService, UserActivityService>();

        services.AddSingleton<GeneralUtils>();
        services.AddScoped<BrowserService>();

        services.AddHttpClient();
        services.AddLogging();
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddAntiforgery();
                
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Default connection string not provided!");
                
        services.AddPooledDbContextFactory<LiveBotDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<LiveBotDbContext>>();
            return factory.CreateDbContext();
        });
        
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LiveBotDbContext>();
        services.AddCascadingAuthenticationState();
        
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = IdentityConstants.ExternalScheme;
                options.DefaultChallengeScheme = "Discord";
            })
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Cookie.Name = "TCC";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddDiscord(options =>
            {
                options.ClientId = builder.Configuration["Discord:ClientId"]??throw new Exception("Discord Client ID not found");
                options.ClientSecret = builder.Configuration["Discord:ClientSecret"]??throw new Exception("Discord Client Secret not found");
                options.Scope.Add("identify");
                options.Scope.Add("guilds");
                options.Scope.Add("email");
                options.SaveTokens = true;
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        var identity = (ClaimsIdentity)context.Principal.Identity;
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
               
                        ulong discordId = ulong.Parse(identity.FindFirst(ClaimTypes.NameIdentifier).Value);
                        ApplicationUser? user = await userManager.Users.SingleOrDefaultAsync(u => u.DiscordId == discordId);

                        if (user != null)
                        {
                            // Add role claims to the identity
                            var roles = await userManager.GetRolesAsync(user);
                            foreach (string role in roles)
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            }
                        }
                    },
                    OnTicketReceived = context =>
                    {
                        context.ReturnUri = "/Account/Registering";
                        if (context.Properties is null) return Task.CompletedTask;
                        
                        context.Properties.IsPersistent = true;
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                        return Task.CompletedTask;
                    }
                };
            });
        services.Configure<IdentityOptions>(o =>
        {
            o.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
        });
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromDays(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.AddServerSideBlazor().AddCircuitOptions(o =>
        {
            o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromDays(1);
            o.DetailedErrors = builder.Environment.IsDevelopment();
        });

        services.Configure<DiscordConfiguration>(config =>
        {
            config.LogUnknownAuditlogs = false;
            config.LogUnknownEvents = false;
        });
        services.ConfigureEventHandlers(
            eventHandlingBuilder => eventHandlingBuilder
                .HandleSessionCreated(SystemEvents.SessionCreated)
                .HandleGuildAvailable(SystemEvents.GuildAvailable)
                .HandleGuildAuditLogCreated(AuditLogEvents.OnAuditLogCreated)
                .HandleGuildMemberUpdated(MembershipScreening.OnAcceptRules)
                .HandleGuildMemberAdded(MemberFlow.OnJoin)
                .HandleGuildMemberAdded(MemberFlow.LogJoin)
                .HandleGuildMemberRemoved(MemberFlow.OnLeave)
                .HandleGuildMemberRemoved(MemberFlow.LogLeave)
                .HandleMessageDeleted(DeleteLog.OnMessageDeleted)
                .HandleMessagesBulkDeleted(DeleteLog.OnBulkDelete)
                .HandlePresenceUpdated(LivestreamNotifications.OnPresenceChange)
                .HandleMessageCreated(MediaOnlyFilter.OnMessageCreated)
                .HandleMessageCreated(FloodFilter.OnMessageCreated)
                .HandleVoiceStateUpdated(VoiceActivityLog.OnVoiceStateUpdated)
                .HandleMessageCreated(EveryoneTagFilter.OnMessageCreated)
                .HandleMessageCreated(DiscordInviteFilter.OnMessageCreated)
                .HandleComponentInteractionCreated(LiveBot.DiscordEventHandlers.ComponentInteractionCreated.HandleEvent.OnButtonPress)
                .HandleMessageCreated(LiveBot.DiscordEventHandlers.MessageCreated.HandleEvent.OnMessageCreated)
        );
        ulong guildId = 0;
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            guildId = 282478449539678210;
        }
        CommandsConfiguration commandsConfiguration = new()
        {
            DebugGuildId = guildId,
            RegisterDefaultCommandProcessors = false,
        };
        SlashCommandConfiguration slashCommandConfiguration = new()
        {
            NamingPolicy = new KebabCaseNamingPolicy()
        };
        
        services.AddCommandsExtension((_, commands) =>
            {
                commands.CommandExecuted += SystemEvents.CommandExecuted;
                commands.CommandErrored += SystemEvents.CommandErrored;
                commands.AddProcessors(
                    new SlashCommandProcessor(slashCommandConfiguration),
                    new UserCommandProcessor(),
                    new MessageCommandProcessor()
                );
                commands.AddCommands(typeof(LiveBotService).Assembly);
            },
            commandsConfiguration);
        services.AddInteractivityExtension();
        services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        services.AddConfiguredDataProtection(builder);
        services.AddDataProtection().SetDefaultKeyLifetime(TimeSpan.FromDays(15));

        services.AddControllers();

        
        return services;
    }
}