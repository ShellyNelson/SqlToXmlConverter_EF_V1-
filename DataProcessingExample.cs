using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Services;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter;

/// <summary>
/// Example demonstrating how to use the DataProcessingService for complete workflow
/// </summary>
public class DataProcessingExample
{
    public static async Task RunEmployeeDataExampleAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<DataProcessingExample>();

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<XmlConverterService>();
            
            // Configure HttpClient for REST client
            services.AddHttpClient<RestClientService>(client =>
            {
                var timeout = TimeSpan.Parse(configuration["RestClient:Timeout"] ?? "00:00:30");
                client.Timeout = timeout;
            });
            
            services.AddSingleton<DataProcessingService>();
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();
            var dataProcessingService = serviceProvider.GetRequiredService<DataProcessingService>();

            logger.LogInformation("=== Employee Data Processing Example ===");

            // Example 1: Basic employee data processing
            logger.LogInformation("\n1. Processing employee data...");
            var result1 = await dataProcessingService.ProcessEmployeeDataAsync(saveToFile: true);
            DisplayResult("Employee Data Processing", result1);

            // Example 2: Process with custom REST endpoint
            logger.LogInformation("\n2. Processing with custom REST endpoint...");
            var customEndpoint = "https://httpbin.org/anything"; // This endpoint echoes back the request
            var result2 = await dataProcessingService.ProcessEmployeeDataAsync(saveToFile: false, customEndpoint: customEndpoint);
            DisplayResult("Custom Endpoint Processing", result2);

            // Example 3: Process with retry logic
            logger.LogInformation("\n3. Processing with retry logic...");
            var result3 = await dataProcessingService.ProcessEmployeeDataWithRetryAsync(maxRetries: 2, saveToFile: true);
            DisplayResult("Retry Logic Processing", result3);

            // Example 4: Test REST endpoint connectivity
            logger.LogInformation("\n4. Testing REST endpoint connectivity...");
            var isReachable = await dataProcessingService.TestRestEndpointAsync();
            logger.LogInformation("REST endpoint reachable: {IsReachable}", isReachable);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in the example");
        }
    }

    public static async Task RunCustomDataExampleAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<DataProcessingExample>();

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<XmlConverterService>();
            
            // Configure HttpClient for REST client
            services.AddHttpClient<RestClientService>(client =>
            {
                var timeout = TimeSpan.Parse(configuration["RestClient:Timeout"] ?? "00:00:30");
                client.Timeout = timeout;
            });
            
            services.AddSingleton<DataProcessingService>();
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();
            var dataProcessingService = serviceProvider.GetRequiredService<DataProcessingService>();

            logger.LogInformation("=== Custom Data Processing Example ===");

            // Example: Process custom data from any table
            logger.LogInformation("\nProcessing custom data from Employees table...");
            var result = await dataProcessingService.ProcessCustomDataAsync<Employee>(
                saveToFile: true, 
                customEndpoint: "https://httpbin.org/post");

            DisplayResult("Custom Data Processing", result);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in the custom data example");
        }
    }

    private static void DisplayResult(string title, DataProcessingResult result)
    {
        Console.WriteLine($"\n--- {title} ---");
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Employee Count: {result.EmployeeCount}");
        Console.WriteLine($"Steps Completed: {string.Join(", ", result.StepsCompleted)}");
        Console.WriteLine($"Processed At: {result.ProcessedAt:yyyy-MM-dd HH:mm:ss UTC}");
        
        if (!string.IsNullOrEmpty(result.OutputFilePath))
        {
            Console.WriteLine($"Output File: {result.OutputFilePath}");
        }
        
        if (result.RestResponse != null)
        {
            Console.WriteLine($"REST Response Status: {result.RestResponse.StatusCode}");
            Console.WriteLine($"REST Response Success: {result.RestResponse.IsSuccess}");
            if (!string.IsNullOrEmpty(result.RestResponse.Content))
            {
                Console.WriteLine($"REST Response Content: {result.RestResponse.Content.Substring(0, Math.Min(200, result.RestResponse.Content.Length))}...");
            }
        }
        
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }

        if (!string.IsNullOrEmpty(result.XmlContent))
        {
            Console.WriteLine($"XML Length: {result.XmlContent.Length} characters");
            Console.WriteLine($"XML Preview: {result.XmlContent.Substring(0, Math.Min(100, result.XmlContent.Length))}...");
        }
    }
}
