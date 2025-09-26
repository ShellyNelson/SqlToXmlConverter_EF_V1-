using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Services;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter;

/// <summary>
/// Example demonstrating how to use the RestClientService to post XML data
/// </summary>
public class RestClientExample
{
    public static async Task RunExampleAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<RestClientExample>();

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<XmlConverterService>();
            
            // Configure HttpClient for REST client
            services.AddHttpClient<RestClientService>(client =>
            {
                var timeout = TimeSpan.Parse(configuration["RestClient:Timeout"] ?? "00:00:30");
                client.Timeout = timeout;
            });
            
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();

            // Get services
            var xmlConverterService = serviceProvider.GetRequiredService<XmlConverterService>();
            var restClientService = serviceProvider.GetRequiredService<RestClientService>();

            // Example 1: Post sample data as XML
            var sampleData = new List<Employee>
            {
                new Employee { Id = 1, FirstName = "John", LastName = "Doe", Department = "IT", Salary = 75000, HireDate = DateTime.Now },
                new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Department = "HR", Salary = 65000, HireDate = DateTime.Now }
            };

            var xmlContent = xmlConverterService.ConvertToXml(sampleData, "Employees");
            
            logger.LogInformation("Generated XML for posting:");
            Console.WriteLine(xmlContent);
            Console.WriteLine(new string('=', 50));

            // Test connection first
            logger.LogInformation("Testing connection to REST endpoint...");
            var connectionTest = await restClientService.TestConnectionAsync();
            logger.LogInformation("Connection test result: {Result}", connectionTest ? "Success" : "Failed");

            if (connectionTest)
            {
                // Post XML with retry logic
                logger.LogInformation("Posting XML to REST endpoint...");
                var response = await restClientService.PostXmlWithRetryAsync(xmlContent);
                
                if (response.IsSuccess)
                {
                    logger.LogInformation("✅ Successfully posted XML!");
                    logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
                    logger.LogInformation("Response Content: {Content}", response.Content);
                }
                else
                {
                    logger.LogWarning("❌ Failed to post XML");
                    logger.LogWarning("Status Code: {StatusCode}", response.StatusCode);
                    logger.LogWarning("Error Content: {Content}", response.Content);
                }
            }

            // Example 2: Post to a different endpoint
            logger.LogInformation("\n--- Example 2: Posting to different endpoint ---");
            var customEndpoint = "https://httpbin.org/anything"; // This endpoint echoes back the request
            var customResponse = await restClientService.PostXmlAsync(xmlContent, customEndpoint);
            
            logger.LogInformation("Custom endpoint response: {StatusCode}", customResponse.StatusCode);
            if (customResponse.IsSuccess)
            {
                logger.LogInformation("Custom endpoint success: {Content}", customResponse.Content);
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in the example");
        }
    }
}
