using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter.Services;

/// <summary>
/// Orchestration service that coordinates database retrieval, XML conversion, and REST posting
/// </summary>
public class DataProcessingService
{
    private readonly DatabaseService _databaseService;
    private readonly XmlConverterService _xmlConverterService;
    private readonly RestClientService _restClientService;
    private readonly ILogger<DataProcessingService> _logger;
    private readonly IConfiguration _configuration;

    public DataProcessingService(
        DatabaseService databaseService,
        XmlConverterService xmlConverterService,
        RestClientService restClientService,
        ILogger<DataProcessingService> logger,
        IConfiguration configuration)
    {
        _databaseService = databaseService;
        _xmlConverterService = xmlConverterService;
        _restClientService = restClientService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Processes employee data: retrieves from database, converts to XML, and posts to REST endpoint
    /// </summary>
    /// <param name="saveToFile">Whether to save XML to a local file (default: true)</param>
    /// <param name="customEndpoint">Optional custom REST endpoint (uses configured endpoint if null)</param>
    /// <returns>Processing result with details about the operation</returns>
    public async Task<DataProcessingResult> ProcessEmployeeDataAsync(bool saveToFile = true, string? customEndpoint = null)
    {
        var result = new DataProcessingResult();
        
        try
        {
            _logger.LogInformation("Starting employee data processing workflow...");

            // Step 1: Retrieve employee data from database
            _logger.LogInformation("Step 1: Retrieving employee data from database...");
            var employees = await _databaseService.GetEmployeesAsync();
            result.EmployeeCount = employees.Count;
            result.StepsCompleted.Add("Database Retrieval");

            if (employees.Count == 0)
            {
                _logger.LogWarning("No employee data found in database");
                result.IsSuccess = false;
                result.ErrorMessage = "No employee data found in database";
                return result;
            }

            _logger.LogInformation("Successfully retrieved {Count} employees from database", employees.Count);

            // Step 2: Convert employee data to XML
            _logger.LogInformation("Step 2: Converting employee data to XML...");
            var xmlContent = _xmlConverterService.ConvertToXml(employees, "Employees");
            result.XmlContent = xmlContent;
            result.StepsCompleted.Add("XML Conversion");

            _logger.LogInformation("Successfully converted employee data to XML ({Length} characters)", xmlContent.Length);

            // Step 3: Save XML to file (if requested)
            if (saveToFile)
            {
                _logger.LogInformation("Step 3: Saving XML to file...");
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "output.xml");
                await _xmlConverterService.SaveXmlToFileAsync(xmlContent, outputPath);
                result.OutputFilePath = outputPath;
                result.StepsCompleted.Add("File Save");
                _logger.LogInformation("XML saved to file: {FilePath}", outputPath);
            }

            // Step 4: Post XML to REST endpoint
            _logger.LogInformation("Step 4: Posting XML to REST endpoint...");
            var restResponse = await _restClientService.PostXmlWithRetryAsync(xmlContent, customEndpoint);
            result.RestResponse = restResponse;
            result.StepsCompleted.Add("REST Posting");

            if (restResponse.IsSuccess)
            {
                _logger.LogInformation("Successfully posted XML to REST endpoint. Status: {StatusCode}", restResponse.StatusCode);
                result.IsSuccess = true;
            }
            else
            {
                _logger.LogWarning("Failed to post XML to REST endpoint. Status: {StatusCode}, Error: {Error}", 
                    restResponse.StatusCode, restResponse.Content);
                result.IsSuccess = false;
                result.ErrorMessage = $"REST posting failed: {restResponse.StatusCode} - {restResponse.Content}";
            }

            _logger.LogInformation("Employee data processing workflow completed. Success: {Success}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during employee data processing workflow");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Processes custom data from any entity using Entity Framework
    /// </summary>
    /// <typeparam name="T">Type of entity to process</typeparam>
    /// <param name="saveToFile">Whether to save XML to a local file</param>
    /// <param name="customEndpoint">Optional custom REST endpoint</param>
    /// <returns>Processing result with details about the operation</returns>
    public async Task<DataProcessingResult> ProcessCustomDataAsync<T>(
        bool saveToFile = true, 
        string? customEndpoint = null) where T : class
    {
        var result = new DataProcessingResult();
        
        try
        {
            _logger.LogInformation("Starting custom data processing workflow for entity: {EntityType}", typeof(T).Name);

            // Step 1: Retrieve custom data from database
            _logger.LogInformation("Step 1: Retrieving data from entity {EntityType}...", typeof(T).Name);
            var data = await _databaseService.GetDataAsync<T>();
            result.EmployeeCount = data.Count;
            result.StepsCompleted.Add("Database Retrieval");

            if (data.Count == 0)
            {
                _logger.LogWarning("No data found in entity {EntityType}", typeof(T).Name);
                result.IsSuccess = false;
                result.ErrorMessage = $"No data found in entity {typeof(T).Name}";
                return result;
            }

            _logger.LogInformation("Successfully retrieved {Count} records from entity {EntityType}", data.Count, typeof(T).Name);

            // Step 2: Convert data to XML
            _logger.LogInformation("Step 2: Converting data to XML...");
            var xmlContent = _xmlConverterService.ConvertToXml(data, typeof(T).Name);
            result.XmlContent = xmlContent;
            result.StepsCompleted.Add("XML Conversion");

            _logger.LogInformation("Successfully converted data to XML ({Length} characters)", xmlContent.Length);

            // Step 3: Save XML to file (if requested)
            if (saveToFile)
            {
                _logger.LogInformation("Step 3: Saving XML to file...");
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{typeof(T).Name}_output.xml");
                await _xmlConverterService.SaveXmlToFileAsync(xmlContent, outputPath);
                result.OutputFilePath = outputPath;
                result.StepsCompleted.Add("File Save");
                _logger.LogInformation("XML saved to file: {FilePath}", outputPath);
            }

            // Step 4: Post XML to REST endpoint
            _logger.LogInformation("Step 4: Posting XML to REST endpoint...");
            var restResponse = await _restClientService.PostXmlWithRetryAsync(xmlContent, customEndpoint);
            result.RestResponse = restResponse;
            result.StepsCompleted.Add("REST Posting");

            if (restResponse.IsSuccess)
            {
                _logger.LogInformation("Successfully posted XML to REST endpoint. Status: {StatusCode}", restResponse.StatusCode);
                result.IsSuccess = true;
            }
            else
            {
                _logger.LogWarning("Failed to post XML to REST endpoint. Status: {StatusCode}, Error: {Error}", 
                    restResponse.StatusCode, restResponse.Content);
                result.IsSuccess = false;
                result.ErrorMessage = $"REST posting failed: {restResponse.StatusCode} - {restResponse.Content}";
            }

            _logger.LogInformation("Custom data processing workflow completed. Success: {Success}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during custom data processing workflow");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Tests the REST endpoint connectivity
    /// </summary>
    /// <param name="customEndpoint">Optional custom endpoint to test</param>
    /// <returns>True if endpoint is reachable, false otherwise</returns>
    public async Task<bool> TestRestEndpointAsync(string? customEndpoint = null)
    {
        try
        {
            _logger.LogInformation("Testing REST endpoint connectivity...");
            var isReachable = await _restClientService.TestConnectionAsync(customEndpoint);
            
            if (isReachable)
            {
                _logger.LogInformation("REST endpoint is reachable");
            }
            else
            {
                _logger.LogWarning("REST endpoint is not reachable");
            }
            
            return isReachable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while testing REST endpoint");
            return false;
        }
    }

    /// <summary>
    /// Processes employee data with retry logic for REST posting
    /// </summary>
    /// <param name="maxRetries">Maximum number of retries for REST posting</param>
    /// <param name="saveToFile">Whether to save XML to a local file</param>
    /// <param name="customEndpoint">Optional custom REST endpoint</param>
    /// <returns>Processing result with details about the operation</returns>
    public async Task<DataProcessingResult> ProcessEmployeeDataWithRetryAsync(
        int maxRetries = 3, 
        bool saveToFile = true, 
        string? customEndpoint = null)
    {
        var result = new DataProcessingResult();
        
        try
        {
            _logger.LogInformation("Starting employee data processing with retry logic (max retries: {MaxRetries})...", maxRetries);

            // Steps 1-3: Database retrieval, XML conversion, and file saving (no retry needed)
            var tempResult = await ProcessEmployeeDataAsync(saveToFile, null);
            if (!tempResult.StepsCompleted.Contains("Database Retrieval") || 
                !tempResult.StepsCompleted.Contains("XML Conversion"))
            {
                return tempResult; // Return early if database or XML conversion failed
            }

            result.EmployeeCount = tempResult.EmployeeCount;
            result.XmlContent = tempResult.XmlContent;
            result.OutputFilePath = tempResult.OutputFilePath;
            result.StepsCompleted.AddRange(tempResult.StepsCompleted);

            // Step 4: REST posting with retry logic
            _logger.LogInformation("Step 4: Posting XML to REST endpoint with retry logic...");
            var restResponse = await _restClientService.PostXmlWithRetryAsync(tempResult.XmlContent!, customEndpoint, maxRetries);
            result.RestResponse = restResponse;
            result.StepsCompleted.Add("REST Posting");

            if (restResponse.IsSuccess)
            {
                _logger.LogInformation("Successfully posted XML to REST endpoint after retries. Status: {StatusCode}", restResponse.StatusCode);
                result.IsSuccess = true;
            }
            else
            {
                _logger.LogWarning("Failed to post XML to REST endpoint after {MaxRetries} retries. Status: {StatusCode}, Error: {Error}", 
                    maxRetries, restResponse.StatusCode, restResponse.Content);
                result.IsSuccess = false;
                result.ErrorMessage = $"REST posting failed after {maxRetries} retries: {restResponse.StatusCode} - {restResponse.Content}";
            }

            _logger.LogInformation("Employee data processing with retry completed. Success: {Success}", result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during employee data processing with retry");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

/// <summary>
/// Result of data processing operations
/// </summary>
public class DataProcessingResult
{
    public bool IsSuccess { get; set; }
    public int EmployeeCount { get; set; }
    public string? XmlContent { get; set; }
    public string? OutputFilePath { get; set; }
    public RestClientResponse? RestResponse { get; set; }
    public List<string> StepsCompleted { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
