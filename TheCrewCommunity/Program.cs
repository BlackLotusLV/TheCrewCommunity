using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TheCrewCommunity;
using TheCrewCommunity.Data;
using TheCrewCommunity.LiveBot;
using TheCrewCommunity.LiveBot.EventHandlers;
using TheCrewCommunity.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddLogging();
builder.Services.AddSingleton<ILiveBotService,LiveBotService>();
builder.Services.AddHostedService<LiveBotService>();
builder.Services.AddSingleton<SystemEvents>();
builder.Services.AddSingleton<IDatabaseMethodService, DatabaseMethodService>();
builder.Services.AddPooledDbContextFactory<LiveBotDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IModeratorLoggingService, ModeratorLoggingService>();
builder.Services.AddSingleton<IModeratorWarningService, ModeratorWarningService>();
builder.Services.AddSingleton<IStreamNotificationService, StreamNotificationService>();
builder.Services.AddSingleton<IModMailService, ModMailService>();
builder.Services.AddSingleton<GeneralUtils>();
builder.Services.AddSingleton<HttpClient>();
builder.Host.UseSerilog()
    .UseConsoleLifetime();

builder.Services.AddDbContext<LiveBotDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LiveBotDbContext>();

builder.Services.AddAuthentication(options =>
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
        options.CallbackPath= "/Account/Callback";
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();