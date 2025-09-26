using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Services;
using System.Data;

namespace SqlToXmlConverter;

/// <summary>
/// Generic example showing how to read from any SQL Server table and convert to XML
/// </summary>
public class GenericExample
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenericExample> _logger;

    public GenericExample(IConfiguration configuration, ILogger<GenericExample> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> ConvertTableToXmlAsync(string tableName, string outputFileName = "output.xml")
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found.");

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get all columns from the table
            var columnsQuery = $@"
                SELECT COLUMN_NAME, DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @tableName 
                ORDER BY ORDINAL_POSITION";

            using var columnsCommand = new SqlCommand(columnsQuery, connection);
            columnsCommand.Parameters.AddWithValue("@tableName", tableName);
            
            var columns = new List<string>();
            using var columnsReader = await columnsCommand.ExecuteReaderAsync();
            while (await columnsReader.ReadAsync())
            {
                columns.Add(columnsReader.GetString("COLUMN_NAME"));
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException($"Table '{tableName}' not found or has no columns.");
            }

            // Read all data from the table
            var dataQuery = $"SELECT * FROM {tableName}";
            using var dataCommand = new SqlCommand(dataQuery, connection);
            using var adapter = new SqlDataAdapter(dataCommand);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            // Convert to XML
            var xmlConverterService = new XmlConverterService(
                new Logger<XmlConverterService>(new LoggerFactory()));
            
            var xmlContent = xmlConverterService.ConvertDataTableToXml(dataTable, tableName);

            // Save to file
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), outputFileName);
            await File.WriteAllTextAsync(outputPath, xmlContent);

            _logger.LogInformation("Successfully converted table '{TableName}' to XML. Output saved to: {OutputPath}", 
                tableName, outputPath);

            return xmlContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while converting table '{TableName}' to XML", tableName);
            throw;
        }
    }
}
