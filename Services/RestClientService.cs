using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace SqlToXmlConverter.Services;

public class RestClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestClientService> _logger;
    private readonly IConfiguration _configuration;

    public RestClientService(HttpClient httpClient, ILogger<RestClientService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        // Configure default headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SqlToXmlConverter/1.0");
    }

    public async Task<RestClientResponse> PostXmlAsync(string xmlContent, string? endpoint = null)
    {
        try
        {
            var url = endpoint ?? _configuration["RestClient:Endpoint"] ?? throw new InvalidOperationException("REST endpoint not configured");
            
            _logger.LogInformation("Posting XML to endpoint: {Endpoint}", url);
            
            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
            
            // Create request message to handle headers properly
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add custom headers to the request
            var customHeaders = _configuration.GetSection("RestClient:Headers").Get<Dictionary<string, string>>();
            if (customHeaders != null)
            {
                foreach (var header in customHeaders)
                {
                    if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(header.Value));
                    }
                    else
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("REST API response: {StatusCode} - {ResponseContent}", 
                response.StatusCode, 
                responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

            return new RestClientResponse
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = responseContent,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while posting XML");
            return new RestClientResponse
            {
                IsSuccess = false,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Content = ex.Message,
                Headers = new Dictionary<string, string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while posting XML");
            return new RestClientResponse
            {
                IsSuccess = false,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Content = ex.Message,
                Headers = new Dictionary<string, string>()
            };
        }
    }

    public async Task<RestClientResponse> PostXmlWithRetryAsync(string xmlContent, string? endpoint = null, int maxRetries = 3)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                var result = await PostXmlAsync(xmlContent, endpoint);
                if (result.IsSuccess)
                {
                    return result;
                }

                // If it's a server error (5xx), retry
                if ((int)result.StatusCode >= 500)
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
                        _logger.LogWarning("Server error {StatusCode}, retrying in {Delay} seconds (attempt {RetryCount}/{MaxRetries})", 
                            result.StatusCode, delay.TotalSeconds, retryCount, maxRetries);
                        await Task.Delay(delay);
                    }
                }
                else
                {
                    // Client error (4xx), don't retry
                    return result;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                if (retryCount < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    _logger.LogWarning(ex, "Request failed, retrying in {Delay} seconds (attempt {RetryCount}/{MaxRetries})", 
                        delay.TotalSeconds, retryCount, maxRetries);
                    await Task.Delay(delay);
                }
            }
        }

        // All retries failed
        _logger.LogError(lastException, "All retry attempts failed");
        return new RestClientResponse
        {
            IsSuccess = false,
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
            Content = lastException?.Message ?? "All retry attempts failed",
            Headers = new Dictionary<string, string>()
        };
    }

    public async Task<bool> TestConnectionAsync(string? endpoint = null)
    {
        try
        {
            var url = endpoint ?? _configuration["RestClient:Endpoint"] ?? throw new InvalidOperationException("REST endpoint not configured");
            
            _logger.LogInformation("Testing connection to: {Endpoint}", url);
            
            var response = await _httpClient.GetAsync(url);
            var isSuccess = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Connection test result: {StatusCode}", response.StatusCode);
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }
}

public class RestClientResponse
{
    public bool IsSuccess { get; set; }
    public System.Net.HttpStatusCode StatusCode { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}
