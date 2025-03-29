using Serilog;
using Serilog.Events;
using TheCrewCommunity;
using TheCrewCommunity.Components;
using TheCrewCommunity.LiveBot.LogEnrichers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog()
    .UseConsoleLifetime();

builder.Services.AddMyServices(builder);

builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With(new EventIdEnricher())
    .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Error, outputTemplate: "[{Timestamp:yyyy:MM:dd HH:mm:ss} {Level:u3}] [{FormattedEventId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration.GetSection("Seq")["Url"]!, apiKey:builder.Configuration.GetSection("Seq")["Key"])
    .CreateLogger();
builder.Logging.AddSerilog();

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

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapRazorPages();

app.UseForwardedHeaders(new ForwardedHeadersOptions()
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All
});

app.UseHttpsRedirection();

app.MapControllers();

app.MapReverseProxy();

app.Run();