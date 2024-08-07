﻿using System.Security.Claims;
using DSharpPlus;
using DSharpPlus.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
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
        
        services.AddSingleton<IModeratorLoggingService, ModeratorLoggingService>();
        services.AddSingleton<IModeratorWarningService, ModeratorWarningService>();
        services.AddSingleton<IModMailService, ModMailService>();
        services.AddSingleton<IDatabaseMethodService, DatabaseMethodService>();
        services.AddSingleton<ICloudFlareImageService, CloudFlareImageService>();
        services.AddSingleton<IUserActivityService, UserActivityService>();

        services.AddSingleton<GeneralUtils>();

        services.AddHttpClient();
        services.AddLogging();
        services.AddRazorPages();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
                
                
        services.AddPooledDbContextFactory<LiveBotDbContext>(options => options.UseNpgsql(services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
        services.AddDbContext<LiveBotDbContext>(options => options.UseNpgsql(services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
        
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LiveBotDbContext>();
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
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
                    OnTicketReceived = async context =>
                    {
                        string? discordId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<LiveBotDbContext>();
                        ApplicationUser? user = await dbContext.ApplicationUsers.FirstOrDefaultAsync(x=>x.DiscordId.ToString() == discordId);
                        context.ReturnUri = "/Account/Registering";
                    }
                };
            });
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromSeconds(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
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
        return services;
    }
}