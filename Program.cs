using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Data;
using SqlToXmlConverter.Services;

namespace SqlToXmlConverter;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<Program>();

        // Check if we should run the test instead
        if (args.Length > 0 && args[0].Equals("test", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Running test with sample data...");
            await TestDataProcessingService.RunTestAsync();
            return;
        }

        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add Entity Framework
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
            services.AddScoped<DatabaseService>();
            services.AddSingleton<XmlConverterService>();
            
            // Configure HttpClient for REST client
            services.AddHttpClient<RestClientService>(client =>
            {
                var timeout = TimeSpan.Parse(configuration["RestClient:Timeout"] ?? "00:00:30");
                client.Timeout = timeout;
            });
            
            // Add the new orchestration service
            services.AddSingleton<DataProcessingService>();
            
            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();

            // Get the orchestration service
            var dataProcessingService = serviceProvider.GetRequiredService<DataProcessingService>();

            logger.LogInformation("Starting employee data processing workflow...");

            // Test REST endpoint connectivity first
            logger.LogInformation("Testing REST endpoint connectivity...");
            var isEndpointReachable = await dataProcessingService.TestRestEndpointAsync();
            if (!isEndpointReachable)
            {
                logger.LogWarning("REST endpoint is not reachable, but continuing with processing...");
            }

            // Process employee data using the orchestration service
            var result = await dataProcessingService.ProcessEmployeeDataAsync(saveToFile: true);

            // Display results
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("PROCESSING RESULTS");
            Console.WriteLine(new string('=', 60));
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
            }
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }

            // Display XML content if available
            if (!string.IsNullOrEmpty(result.XmlContent))
            {
                Console.WriteLine("\nGenerated XML:");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine(result.XmlContent);
            }

            if (result.IsSuccess)
            {
                logger.LogInformation("Employee data processing workflow completed successfully!");
            }
            else
            {
                logger.LogError("Employee data processing workflow failed: {Error}", result.ErrorMessage);
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the process");
            Environment.Exit(1);
        }
    }
}
