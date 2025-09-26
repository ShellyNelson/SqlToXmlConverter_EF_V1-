using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlToXmlConverter.Data;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter.Services;

public class DatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Employee>> GetEmployeesAsync()
    {
        try
        {
            var employees = await _context.Employees
                .OrderBy(e => e.Id)
                .ToListAsync();

            _logger.LogInformation("Successfully retrieved {Count} employees from database", employees.Count);
            return employees;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving employees from database");
            throw;
        }
    }

    public async Task<List<T>> GetDataAsync<T>() where T : class
    {
        try
        {
            var items = await _context.Set<T>().ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} records from {EntityType}", items.Count, typeof(T).Name);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving data from {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public async Task<List<T>> GetDataAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            var items = await _context.Set<T>()
                .Where(predicate)
                .ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} records from {EntityType} with predicate", items.Count, typeof(T).Name);
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving data from {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }
}
