# SQL Server to XML Converter

A .NET 8 Core console application that reads data from SQL Server tables, converts it to XML format, and posts it to REST endpoints.

## Features

- Read data from any SQL Server table
- Convert data to well-formatted XML
- **NEW**: Post XML data to REST endpoints via HTTP POST
- **NEW**: Complete workflow orchestration with DataProcessingService
- Support for strongly-typed models and generic DataTable approach
- Configurable connection strings and REST endpoints
- Comprehensive logging
- Error handling with retry logic
- Connection testing for REST endpoints

## Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Visual Studio 2022 or VS Code (optional)

## Setup

1. **Update Configuration**
   Edit `appsettings.json` and update the connection string and REST endpoint:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server;Database=your-database;Trusted_Connection=true;TrustServerCertificate=true;"
     },
     "RestClient": {
       "Endpoint": "https://your-api-endpoint.com/api/xml",
       "Timeout": "00:00:30",
       "Headers": {
         "X-Custom-Header": "SqlToXmlConverter",
         "Accept": "application/xml"
       }
     }
   }
   ```

2. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

3. **Build the Application**
   ```bash
   dotnet build
   ```

## Usage

### Method 1: Using Strongly-Typed Models

1. **Create a Model Class**
   - Copy the `Employee.cs` example from `Models/Employee.cs`
   - Modify the properties to match your table structure
   - Add appropriate XML attributes for serialization

2. **Update the Database Query**
   - Modify the query in `DatabaseService.cs` to match your table and columns

3. **Run the Application**
   ```bash
   dotnet run
   ```

   The application will:
   - Read data from the database
   - Convert it to XML
   - Save it to a local file (`output.xml`)
   - **Post the XML to the configured REST endpoint**

### Method 2: Generic Approach (Any Table)

Use the `GenericExample.cs` class to convert any table:

```csharp
var genericExample = new GenericExample(configuration, logger);
var xmlContent = await genericExample.ConvertTableToXmlAsync("YourTableName", "output.xml");
```

### Method 3: REST Client Only

Use the `RestClientExample.cs` to test REST functionality with sample data:

```csharp
await RestClientExample.RunExampleAsync();
```

### Method 4: Complete Workflow with DataProcessingService

Use the new `DataProcessingService` for a complete orchestrated workflow:

```csharp
// Basic employee data processing
var result = await dataProcessingService.ProcessEmployeeDataAsync(saveToFile: true);

// Process with custom REST endpoint
var result = await dataProcessingService.ProcessEmployeeDataAsync(
    saveToFile: true, 
    customEndpoint: "https://your-custom-endpoint.com/api/data");

// Process with retry logic
var result = await dataProcessingService.ProcessEmployeeDataWithRetryAsync(
    maxRetries: 3, 
    saveToFile: true);

// Process custom data from any table
var result = await dataProcessingService.ProcessCustomDataAsync<Employee>(
    "Employees", 
    new[] { "Id", "FirstName", "LastName", "Email" }, 
    saveToFile: true);

// Test REST endpoint connectivity
var isReachable = await dataProcessingService.TestRestEndpointAsync();
```

The `DataProcessingService` provides:
- **Complete workflow orchestration**: Database → XML → REST in one call
- **Comprehensive result tracking**: Success status, step completion, error details
- **Flexible configuration**: Custom endpoints, retry logic, file saving options
- **Error handling**: Graceful failure handling with detailed error reporting
- **Logging**: Full workflow logging for debugging and monitoring

## Sample SQL Table

Here's a sample table structure you can use for testing:

```sql
CREATE TABLE Employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Department NVARCHAR(50),
    HireDate DATETIME2 NOT NULL,
    Salary DECIMAL(10,2)
);

-- Insert sample data
INSERT INTO Employees (FirstName, LastName, Email, Department, HireDate, Salary)
VALUES 
    ('John', 'Doe', 'john.doe@company.com', 'IT', '2023-01-15', 75000.00),
    ('Jane', 'Smith', 'jane.smith@company.com', 'HR', '2023-02-20', 65000.00),
    ('Bob', 'Johnson', 'bob.johnson@company.com', 'Finance', '2023-03-10', 80000.00);
```

## Output

The application will generate XML output like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Employees>
  <Employee>
    <Id>1</Id>
    <FirstName>John</FirstName>
    <LastName>Doe</LastName>
    <Email>john.doe@company.com</Email>
    <Department>IT</Department>
    <HireDate>2023-01-15T00:00:00</HireDate>
    <Salary>75000.00</Salary>
  </Employee>
  <!-- More employees... -->
</Employees>
```

## Configuration

- **Connection String**: Update in `appsettings.json`
- **REST Endpoint**: Configure in `appsettings.json` under `RestClient` section
- **Logging Level**: Configure in `appsettings.json`
- **Output File**: Modify the output path in `Program.cs`

### REST Client Configuration Options

- `Endpoint`: The URL where XML data will be posted
- `Timeout`: HTTP request timeout (default: 30 seconds)
- `Headers`: Custom headers to include with requests

## Error Handling

The application includes comprehensive error handling for:
- Database connection issues
- SQL query errors
- XML serialization problems
- File I/O operations
- **REST API communication errors**
- **Network connectivity issues**
- **HTTP timeout and retry logic**

All errors are logged with detailed information to help with troubleshooting.

### REST Client Features

- **Automatic Retry**: Configurable retry logic with exponential backoff
- **Connection Testing**: Test REST endpoint connectivity before posting
- **Custom Headers**: Support for authentication and custom headers
- **Timeout Handling**: Configurable request timeouts
- **Response Logging**: Detailed logging of HTTP responses

## Dependencies

- Microsoft.Data.SqlClient (6.1.1)
- Microsoft.Extensions.Configuration (9.0.9)
- Microsoft.Extensions.Logging (9.0.9)
- Microsoft.Extensions.Http (9.0.9)
- System.Data.Common (4.3.0)
