# PowerShell script to run the SQL to XML Converter
Write-Host "Starting SQL to XML Converter..." -ForegroundColor Green
Write-Host ""

# Check if .NET 8 is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Yellow
} catch {
    Write-Host "Error: .NET 8 SDK is not installed or not in PATH." -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Run the application
Write-Host ""
Write-Host "Running application..." -ForegroundColor Green
Write-Host ""
dotnet run

Read-Host "Press Enter to exit"
