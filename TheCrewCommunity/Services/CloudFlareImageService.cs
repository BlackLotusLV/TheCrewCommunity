using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheCrewCommunity.Services;

public interface ICloudFlareImageService
{
    Task<PostImageResponse> PostImageAsync(byte[] image, bool requireSignedUrls = false);
}

public class CloudFlareImageService : ICloudFlareImageService
{
    private readonly HttpClient _httpClient;
    private readonly Uri _uploadImageUri;
    private readonly ILogger<CloudFlareImageService> _logger;
    public CloudFlareImageService(HttpClient httpClient, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _logger = loggerFactory.CreateLogger<CloudFlareImageService>();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["CloudFlare:Token"] ?? "");
        _uploadImageUri = new Uri($"https://api.cloudflare.com/client/v4/accounts/{configuration["CloudFlare:AccountId"]}/images/v1");
    }

    public async Task<PostImageResponse> PostImageAsync(byte[] image, bool requireSignedUrls = false)
    {
        using MultipartFormDataContent content = new();
        content.Add(new ByteArrayContent(image),"file");
        content.Add(new StringContent(requireSignedUrls.ToString()),"requireSignedURLs");
        HttpResponseMessage responseMessage = await _httpClient.PostAsync(_uploadImageUri, content);

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
}

public class PostImageResponse
{
    [JsonPropertyName("result")]
    public CloudFlareImageResult? Result { get; init; }
    [JsonPropertyName("errors")]
    public required CloudFlareError[] Errors { get; init; }
    [JsonPropertyName("messages")]
    public required CloudFlareMessage[] Messages { get; init; }
    [JsonPropertyName("success")]
    public required bool Success { get; init; }
}

public class CloudFlareImageResult
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

public class CloudFlareError
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

public class CloudFlareMessage
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}