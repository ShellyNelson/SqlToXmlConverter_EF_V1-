@echo off
echo Starting SQL to XML Converter...
echo.

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET 8 SDK is not installed or not in PATH.
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Restore packages
echo Restoring NuGet packages...
dotnet restore --source https://api.nuget.org/v3/index.json
if %errorlevel% neq 0 (
    echo Error: Failed to restore packages.
    pause
    exit /b 1
)

REM Build the application
echo Building application...
dotnet build --source https://api.nuget.org/v3/index.json
if %errorlevel% neq 0 (
    echo Error: Build failed.
    pause
    exit /b 1
)

REM Run the application
echo.
echo Running application...
echo.
dotnet run

pause
