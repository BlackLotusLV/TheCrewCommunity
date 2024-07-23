using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheCrewCommunity.Services;

public interface ICloudFlareImageService
{
    Task<PostImageResponse> PostImageAsync(byte[] image, bool requireSignedUrls = false);
    Task<DeleteImageResponse> DeleteImageAsync(Guid imageId);
}

public class CloudFlareImageService : ICloudFlareImageService
{
    private readonly HttpClient _httpClient;
    private readonly Uri _cfBaseUrl;
    private readonly ILogger<CloudFlareImageService> _logger;
    public CloudFlareImageService(HttpClient httpClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _logger = loggerFactory.CreateLogger<CloudFlareImageService>();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["CloudFlare:Token"] ?? "");
        _cfBaseUrl = new Uri($"https://api.cloudflare.com/client/v4/accounts/{configuration["CloudFlare:AccountId"]}/images/v1");
    }

    public async Task<PostImageResponse> PostImageAsync(byte[] image, bool requireSignedUrls = false)
    {
        using MultipartFormDataContent content = new();
        content.Add(new ByteArrayContent(image),"file");
        content.Add(new StringContent(requireSignedUrls.ToString()),"requireSignedURLs");
        HttpResponseMessage responseMessage = await _httpClient.PostAsync(_cfBaseUrl, content);

        if (!responseMessage.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Request failed with status code {responseMessage.StatusCode}");
        }

        Stream jsonResponse = await responseMessage.Content.ReadAsStreamAsync();
        string test = await responseMessage.Content.ReadAsStringAsync();
        Console.WriteLine(test);
        var result = await JsonSerializer.DeserializeAsync<PostImageResponse>(jsonResponse);
        if(result == null)
        {
            throw new JsonException("Response could not be deserialized to UploadResponse");
        }

        return result;
    }

    public async Task<DeleteImageResponse> DeleteImageAsync(Guid imageId)
    {
        _logger.LogDebug(CustomLogEvents.CloudFlare,"Deletion process started for iamge: {Id}",imageId);
        var deleteUrl = new Uri(_cfBaseUrl, $"v1/{imageId}");
        _logger.LogDebug(CustomLogEvents.CloudFlare,"Delete url:{Url}", deleteUrl);
        HttpResponseMessage responseMessage = await _httpClient.DeleteAsync(deleteUrl);
        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.LogDebug(CustomLogEvents.CloudFlare,"Response failed");
            throw new HttpRequestException($"Request failed with status code {responseMessage.StatusCode}");
        }
        _logger.LogDebug(CustomLogEvents.CloudFlare,"Reading response to stream");
        Stream jsonResponse = await responseMessage.Content.ReadAsStreamAsync();
        _logger.LogDebug(CustomLogEvents.CloudFlare,"Deserializing json response to result object");
        var result = await JsonSerializer.DeserializeAsync<DeleteImageResponse>(jsonResponse);
        if (result is null)
        {
            throw new JsonException("Response could not be deserialized to DeleteImageResponse");
        }

        return result;
    }
}

public class PostImageResponse
{
    [JsonPropertyName("result")]
    public CloudFlareImageResult? Result { get; init; }
    [JsonPropertyName("errors")]
    public required CodeMessageObject[] Errors { get; init; }
    [JsonPropertyName("messages")]
    public required CodeMessageObject[] Messages { get; init; }
    [JsonPropertyName("success")]
    public required bool Success { get; init; }
}

public abstract class CloudFlareImageResult
{
    [JsonPropertyName("filename")]
    public string? FileName { get; init; }
    [JsonPropertyName("id")]
    public string Id { get; init; }
    [JsonPropertyName("meta")]
    public object Meta { get; init; }
    [JsonPropertyName("requireSignedURLs")]
    public bool RequireSignedUrl { get; init; }
    [JsonPropertyName("uploaded")]
    public DateTime Uploaded { get; init; }
    [JsonPropertyName("variants")]
    public string[] Variants { get; init; }
}

public class DeleteImageResponse
{
    [JsonPropertyName("result")]
    public required object? Result { get; init; }
    [JsonPropertyName("errors")]
    public required CodeMessageObject[] Errors { get; init; }
    [JsonPropertyName("messages")]
    public required CodeMessageObject[] Messages { get; init; }
    [JsonPropertyName("success")] public required bool Success { get; init; }
}

public abstract class CodeMessageObject
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}