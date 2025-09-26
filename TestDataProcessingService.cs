using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Services;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter;

/// <summary>
/// Test the DataProcessingService with sample data (no database required)
/// </summary>
public class TestDataProcessingService
{
    public static async Task RunTestAsync()
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<TestDataProcessingService>();

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
            var xmlConverterService = serviceProvider.GetRequiredService<XmlConverterService>();
            var restClientService = serviceProvider.GetRequiredService<RestClientService>();

            logger.LogInformation("=== Testing DataProcessingService with Sample Data ===");

            // Create sample employee data
            var sampleEmployees = new List<Employee>
            {
                new Employee 
                { 
                    Id = 1, 
                    FirstName = "John", 
                    LastName = "Doe", 
                    Email = "john.doe@company.com", 
                    Department = "IT", 
                    Salary = 75000, 
                    HireDate = DateTime.Now.AddYears(-2) 
                },
                new Employee 
                { 
                    Id = 2, 
                    FirstName = "Jane", 
                    LastName = "Smith", 
                    Email = "jane.smith@company.com", 
                    Department = "HR", 
                    Salary = 65000, 
                    HireDate = DateTime.Now.AddYears(-1) 
                },
                new Employee 
                { 
                    Id = 3, 
                    FirstName = "Bob", 
                    LastName = "Johnson", 
                    Email = "bob.johnson@company.com", 
                    Department = "Finance", 
                    Salary = 80000, 
                    HireDate = DateTime.Now.AddMonths(-6) 
                }
            };

            logger.LogInformation("Created {Count} sample employees", sampleEmployees.Count);

            // Test 1: Convert to XML
            logger.LogInformation("\n1. Converting sample data to XML...");
            var xmlContent = xmlConverterService.ConvertToXml(sampleEmployees, "Employees");
            logger.LogInformation("XML generated successfully ({Length} characters)", xmlContent.Length);

            // Test 2: Save to file
            logger.LogInformation("\n2. Saving XML to file...");
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "sample_output.xml");
            await xmlConverterService.SaveXmlToFileAsync(xmlContent, outputPath);
            logger.LogInformation("XML saved to: {FilePath}", outputPath);

            // Test 3: Post to REST endpoint
            logger.LogInformation("\n3. Posting XML to REST endpoint...");
            var restResponse = await restClientService.PostXmlWithRetryAsync(xmlContent);
            
            if (restResponse.IsSuccess)
            {
                logger.LogInformation("Successfully posted XML to REST endpoint!");
                logger.LogInformation("Response Status: {StatusCode}", restResponse.StatusCode);
                logger.LogInformation("Response Content: {Content}", restResponse.Content?.Substring(0, Math.Min(200, restResponse.Content?.Length ?? 0)));
            }
            else
            {
                logger.LogWarning("Failed to post XML to REST endpoint");
                logger.LogWarning("Status: {StatusCode}, Error: {Error}", restResponse.StatusCode, restResponse.Content);
            }

            // Test 4: Test REST endpoint connectivity
            logger.LogInformation("\n4. Testing REST endpoint connectivity...");
            var isReachable = await restClientService.TestConnectionAsync();
            logger.LogInformation("REST endpoint reachable: {IsReachable}", isReachable);

            // Display XML content
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("GENERATED XML CONTENT");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(xmlContent);

            logger.LogInformation("\n=== Test completed successfully! ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the test");
        }
    }
}
