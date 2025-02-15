using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace TheCrewCommunity;

public static class DataProtectionExtensions
{
    public static IServiceCollection AddConfiguredDataProtection(this IServiceCollection services,
        WebApplicationBuilder builder)
    {
        string? keyStoragePath = builder.Configuration.GetSection("DataProtection:KeyPath").Value;
        
        if (string.IsNullOrEmpty(keyStoragePath))
        {
            Log.Fatal("DataProtection:KeyPath is not configured in appsettings.json");
            throw new InvalidOperationException("Data Protection key storage path is not configured.");
        }

        try
        {
            Directory.CreateDirectory(keyStoragePath);
            
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoragePath))
                .SetApplicationName("TheCrewCommunity");

            Log.Information("Data protection configured successfully with key path: {KeyPath}", keyStoragePath);
            
            return services;
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Fatal(ex, "Unable to access or create directory for data protection keys at {KeyPath}. " +
                          "Ensure the application has write permissions to this location.", keyStoragePath);
            throw new InvalidOperationException(
                "Cannot access data protection key storage location. Check application permissions.", ex);
        }
        catch (IOException ex)
        {
            Log.Fatal(ex, "IO error while configuring data protection key storage at {KeyPath}", keyStoragePath);
            throw new InvalidOperationException(
                "Failed to configure data protection due to IO error. Check disk space and permissions.", ex);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unexpected error while configuring data protection at {KeyPath}", keyStoragePath);
            throw new InvalidOperationException(
                "Critical error during data protection configuration.", ex);
        }


    }
}