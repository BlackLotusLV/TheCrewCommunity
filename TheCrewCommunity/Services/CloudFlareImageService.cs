using System.Net.Http.Headers;
using System.Text.Json;

namespace TheCrewCommunity.Services;

public interface ICloudFlareImageService
{
    Task<PostImageResponse> PostImage(byte[] image, bool requireSignedUrls = false);
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

    public async Task<PostImageResponse> PostImage(byte[] image, bool requireSignedUrls = false)
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
    public CloudFlareImageResult? Result { get; init; }
    public required CloudFlareError[] Errors { get; init; }
    public required CloudFlareMessage[] Messages { get; init; }
    public required bool Success { get; init; }
}

public class CloudFlareImageResult
{
    public string FileName { get; init; }
    public string Id { get; init; }
    public object Meta { get; init; }
    public bool RequireSignedUrl { get; init; }
    public DateTime Uploaded { get; init; }
    public string[] Variants { get; init; }
}

public class CloudFlareError
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}

public class CloudFlareMessage
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}