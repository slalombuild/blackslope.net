# Common Issues

This document provides comprehensive troubleshooting guidance for common issues encountered when working with the BlackSlope.NET application. The application is built on .NET 6.0 with ASP.NET Core Web API, Entity Framework Core, and SQL Server, containerized using Docker with Windows containers.

## Database Connection Issues

### Connection String Errors

The application uses SQL Server as its primary database with Entity Framework Core 6.0.1 as the ORM. Connection string issues are among the most common problems during setup and deployment.

**Symptoms:**
- Application fails to start with database-related exceptions
- Health check endpoint (`/health`) reports unhealthy status
- Entity Framework migrations fail to execute

**Common Causes and Solutions:**

#### 1. Incorrect Connection String Format

The default connection string in `appsettings.json` uses Windows Integrated Security:

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
  }
}
```

**Solutions:**

For **SQL Server Authentication**, modify the connection string:
```json
"MoviesConnectionString": "data source=localhost,1433;initial catalog=movies;User Id=sa;Password=YourPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;"
```

For **Azure SQL Database**, use this format:
```json
"MoviesConnectionString": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=movies;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

For **Docker containers** connecting to host SQL Server:
```json
"MoviesConnectionString": "data source=host.docker.internal,1433;initial catalog=movies;User Id=sa;Password=YourPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;"
```

#### 2. Database Does Not Exist

The connection string references a database named `movies` that must be created before running migrations.

**Solution:**

Create the database manually using SSMS, Azure Data Studio, or mssql-cli:

```sql
CREATE DATABASE movies;
GO
```

Or use sqlcmd:
```bash
sqlcmd -S localhost -U sa -P YourPassword -Q "CREATE DATABASE movies"
```

#### 3. Connection String Not Found in Configuration

Ensure the configuration key path matches exactly: `BlackSlope.Api:MoviesConnectionString`

**Verification:**
```csharp
// In your DbContext configuration, verify the connection string is being read correctly
var connectionString = configuration.GetConnectionString("MoviesConnectionString") 
    ?? configuration["BlackSlope.Api:MoviesConnectionString"];
```

### SQL Server Not Running

**Symptoms:**
- "A network-related or instance-specific error occurred while establishing a connection to SQL Server"
- Connection timeout errors
- Health checks fail immediately

**Solutions:**

**Windows:**
```powershell
# Check SQL Server service status
Get-Service -Name "MSSQL*"

# Start SQL Server service
Start-Service -Name "MSSQLSERVER"

# Or use SQL Server Configuration Manager
```

**Linux/Docker:**
```bash
# Check if SQL Server container is running
docker ps | grep sql

# Start SQL Server container
docker start sql-server-container

# Check SQL Server logs
docker logs sql-server-container
```

**Verify SQL Server is listening on the correct port:**
```powershell
# Windows
netstat -an | findstr "1433"

# Linux
netstat -tuln | grep 1433
```

### Authentication Failures

The application supports multiple authentication mechanisms through Azure.Identity (1.14.2) and legacy ADAL (5.2.9).

**Symptoms:**
- "Login failed for user" errors
- Azure AD authentication errors
- JWT token validation failures

**Solutions:**

#### 1. SQL Server Authentication Issues

For Windows Integrated Security, ensure:
- The application pool identity (IIS) or process user has database access
- SQL Server is configured for Windows Authentication mode

```sql
-- Grant access to Windows user
USE movies;
CREATE USER [DOMAIN\Username] FROM LOGIN [DOMAIN\Username];
ALTER ROLE db_owner ADD MEMBER [DOMAIN\Username];
```

For SQL Authentication:
```sql
-- Create SQL login and user
CREATE LOGIN blackslope_user WITH PASSWORD = 'StrongPassword123!';
USE movies;
CREATE USER blackslope_user FROM LOGIN blackslope_user;
ALTER ROLE db_owner ADD MEMBER blackslope_user;
```

#### 2. Azure AD Authentication Issues

The application uses Azure AD for API authentication. Verify configuration in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "AzureAd": {
      "AadInstance": "https://login.microsoftonline.com/{0}",
      "Tenant": "[tenant-id]",
      "Audience": "https://[host-name]"
    }
  }
}
```

**Common fixes:**
- Replace `[tenant-id]` with your actual Azure AD tenant ID
- Replace `[host-name]` with your application's registered audience/client ID
- Ensure the application is registered in Azure AD
- Verify API permissions are granted and admin consent is provided

**Testing Azure AD authentication:**
```bash
# Obtain a token using Azure CLI
az login
az account get-access-token --resource https://[host-name]

# Test API with token
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:51385/api/movies
```

### Migration Errors

Entity Framework Core migrations may fail due to various reasons.

**Symptoms:**
- "The database does not exist" errors
- "Migration already applied" warnings
- Schema mismatch errors

**Solutions:**

#### 1. Initial Migration Setup

From the repository root, run:

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Update database with migrations
dotnet ef database update --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

Expected successful output:
```
Build started...
Build succeeded.
Applying migration '20190814225754_initialized'.
Applying migration '20190814225910_seeded'.
Done.
```

#### 2. Migration Script Execution

For production environments or restricted database access, generate SQL scripts:

```bash
# Generate SQL script for all migrations
dotnet ef migrations script --project=./src/BlackSlope.Api/BlackSlope.Api.csproj --output migrations.sql

# Generate script for specific migration range
dotnet ef migrations script FromMigration ToMigration --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

#### 3. Reset Migrations (Development Only)

If migrations are corrupted or need to be reset:

```bash
# Drop the database
dotnet ef database drop --project=./src/BlackSlope.Api/BlackSlope.Api.csproj

# Recreate and apply migrations
dotnet ef database update --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

#### 4. Connection String for Migrations

The migration script (`scripts/db-update.sh`) navigates to the API project:

```bash
cd ../src/BlackSlope.Api
dotnet ef database update -v
```

Ensure you run this from the `scripts` directory, or adjust paths accordingly.

**Troubleshooting migration connection issues:**
```bash
# Specify connection string explicitly
dotnet ef database update --connection "YourConnectionString" --project=./src/BlackSlope.Api/BlackSlope.Api.csproj

# Use verbose output for debugging
dotnet ef database update -v --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

For more information on database migrations, see [Database Migrations](/database/migrations.md).

## Build and Compilation Issues

### Missing Dependencies

**Symptoms:**
- Build errors referencing missing assemblies
- "The type or namespace name could not be found" errors
- NuGet package restore warnings

**Solutions:**

#### 1. Restore NuGet Packages

```bash
# Restore packages for entire solution
dotnet restore src/BlackSlope.NET.sln

# Clear NuGet cache if packages are corrupted
dotnet nuget locals all --clear
dotnet restore src/BlackSlope.NET.sln
```

#### 2. Verify Package Sources

Check your `NuGet.config` file or global configuration:

```bash
# List configured package sources
dotnet nuget list source

# Add nuget.org if missing
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

#### 3. Check for Package Version Conflicts

The project uses specific package versions. Common conflicts:

| Package | Version | Potential Conflict |
|---------|---------|-------------------|
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | Newer than other Microsoft.Extensions.* packages (6.0.x) |
| Microsoft.IdentityModel.JsonWebTokens | 7.7.1 | Must match System.IdentityModel.Tokens.Jwt version |
| System.Net.Http | 4.3.4 | Legacy version, may conflict with .NET 6 built-in version |

**Resolution:**
```xml
<!-- In .csproj file, add explicit package references -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

### Package Restore Failures

**Symptoms:**
- "Unable to find package" errors
- HTTP 401/403 errors when accessing package sources
- Timeout errors during restore

**Solutions:**

#### 1. Authentication Issues with Private Feeds

If using Azure Artifacts or private NuGet feeds:

```bash
# Install credential provider
iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"

# Or for Linux/Mac
wget -qO- https://aka.ms/install-artifacts-credprovider.sh | bash

# Restore with interactive authentication
dotnet restore src/BlackSlope.NET.sln --interactive
```

#### 2. Network/Proxy Issues

Configure proxy settings:

```bash
# Set proxy for NuGet
dotnet nuget config set http_proxy http://proxy.company.com:8080
dotnet nuget config set https_proxy https://proxy.company.com:8080
```

#### 3. Package Source Timeout

Increase timeout for slow connections:

```xml
<!-- In NuGet.config -->
<configuration>
  <config>
    <add key="http_timeout" value="600" />
  </config>
</configuration>
```

### Framework Version Mismatches

**Symptoms:**
- "The current .NET SDK does not support targeting .NET 6.0" errors
- Runtime version mismatch warnings
- Assembly binding errors

**Solutions:**

#### 1. Verify .NET SDK Version

```bash
# Check installed SDK versions
dotnet --list-sdks

# Check runtime versions
dotnet --list-runtimes

# Required versions for this project:
# SDK: 6.0.x or higher
# Runtime: Microsoft.AspNetCore.App 6.0.x
```

#### 2. Install Correct SDK

Download and install .NET 6.0 SDK from:
- https://dotnet.microsoft.com/download/dotnet/6.0

#### 3. Global.json Configuration

If the project has a `global.json` file, it may pin a specific SDK version:

```json
{
  "sdk": {
    "version": "6.0.100",
    "rollForward": "latestMinor"
  }
}
```

**Troubleshooting:**
- Remove or update `global.json` if the specified version is not installed
- Use `"rollForward": "latestMinor"` to allow newer patch versions

#### 4. Target Framework Mismatch

The project targets `net6.0`. Verify in `.csproj` files:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>
```

For the console application (RenameUtility):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>
```

For detailed installation instructions, see [Installation Guide](/getting_started/installation.md).

## Runtime Issues

### Port Conflicts

**Symptoms:**
- "Address already in use" errors
- "Failed to bind to address" exceptions
- Application starts but is not accessible

**Solutions:**

#### 1. Identify Port Usage

The application uses ports 80 and 443 (Docker) or 51385 (local development):

**Windows:**
```powershell
# Find process using port 51385
netstat -ano | findstr :51385
Get-Process -Id [PID]

# Kill the process
Stop-Process -Id [PID] -Force
```

**Linux/Mac:**
```bash
# Find process using port
lsof -i :51385
netstat -tuln | grep 51385

# Kill the process
kill -9 [PID]
```

#### 2. Change Application Port

Modify `launchSettings.json` for local development:

```json
{
  "profiles": {
    "BlackSlope.Api": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Or use command-line arguments:
```bash
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj --urls "http://localhost:5000"
```

#### 3. Docker Port Mapping

When running in Docker, map to different host ports:

```bash
# Map container port 80 to host port 8080
docker run -p 8080:80 -p 8443:443 blackslope.api
```

### Configuration Errors

**Symptoms:**
- "Configuration key not found" exceptions
- Null reference exceptions when accessing configuration values
- Application starts with default/incorrect settings

**Solutions:**

#### 1. Configuration File Hierarchy

ASP.NET Core loads configuration in this order:
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment variables
5. Command-line arguments

**Verify configuration loading:**
```csharp
// In Startup.cs or Program.cs
var config = builder.Configuration;
var connectionString = config["BlackSlope.Api:MoviesConnectionString"];
Console.WriteLine($"Connection String: {connectionString}");
```

#### 2. Nested Configuration Access

The application uses nested configuration sections. Access them correctly:

```csharp
// Correct - using section path
var baseUrl = configuration["BlackSlope.Api:BaseUrl"];

// Correct - using GetSection
var azureAdConfig = configuration.GetSection("BlackSlope.Api:AzureAd");
var tenant = azureAdConfig["Tenant"];

// Incorrect - missing parent section
var baseUrl = configuration["BaseUrl"]; // Returns null
```

#### 3. Configuration Binding

For strongly-typed configuration:

```csharp
public class BlackSlopeSettings
{
    public string BaseUrl { get; set; }
    public SwaggerSettings Swagger { get; set; }
    public AzureAdSettings AzureAd { get; set; }
    public string MoviesConnectionString { get; set; }
}

// In Startup.cs
services.Configure<BlackSlopeSettings>(
    configuration.GetSection("BlackSlope.Api"));
```

#### 4. Serilog Configuration Issues

The application uses Serilog with configuration in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "information",
      "FileName": "log.txt",
      "WriteToFile": "false",
      "WriteToAppInsights": "false",
      "WriteToConsole": "true"
    }
  }
}
```

**Common issues:**
- Boolean values as strings ("true"/"false") - ensure proper parsing
- Invalid log level names - use: verbose, debug, information, warning, error, fatal
- Missing Application Insights instrumentation key when `WriteToAppInsights` is true

### Missing appsettings

**Symptoms:**
- Application fails to start with configuration errors
- Default values are used instead of expected configuration
- Environment-specific settings not applied

**Solutions:**

#### 1. Ensure Required Files Exist

Required configuration files:
- `src/BlackSlope.Api/appsettings.json` (base configuration)
- `src/BlackSlope.Api/appsettings.Development.json` (optional, for local development)
- `src/BlackSlope.Api/appsettings.Production.json` (optional, for production)

#### 2. Copy to Output Directory

Verify `.csproj` includes configuration files:

```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="appsettings.*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <DependentUpon>appsettings.json</DependentUpon>
  </Content>
</ItemGroup>
```

#### 3. Docker Configuration

When building Docker images, ensure configuration files are copied:

```dockerfile
# From the Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
WORKDIR /BlackSlope.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app
```

The `COPY . .` command copies all files including `appsettings.json`. Verify the file exists in the published output:

```bash
# After building the image
docker run --rm blackslope.api ls /app
# Should show BlackSlope.Api.dll, appsettings.json, etc.
```

#### 4. User Secrets for Development

For sensitive configuration in development, use User Secrets:

```bash
# Initialize user secrets
dotnet user-secrets init --project src/BlackSlope.Api/BlackSlope.Api.csproj

# Set a secret
dotnet user-secrets set "BlackSlope.Api:MoviesConnectionString" "YourConnectionString" --project src/BlackSlope.Api/BlackSlope.Api.csproj

# List all secrets
dotnet user-secrets list --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

User secrets are stored outside the project directory and never committed to source control.

### Environment Variable Issues

**Symptoms:**
- Configuration values not overridden as expected
- Environment-specific behavior not working
- ASPNETCORE_ENVIRONMENT not recognized

**Solutions:**

#### 1. Set ASPNETCORE_ENVIRONMENT

**Windows (PowerShell):**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Linux/Mac:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Docker:**
```bash
docker run -e ASPNETCORE_ENVIRONMENT=Production blackslope.api
```

#### 2. Override Configuration with Environment Variables

ASP.NET Core uses double underscore (`__`) or colon (`:`) for nested configuration:

```bash
# Override connection string
export BlackSlope__Api__MoviesConnectionString="Server=prod-server;..."

# Or using colon (may require escaping in some shells)
export "BlackSlope:Api:MoviesConnectionString"="Server=prod-server;..."
```

**Docker example:**
```bash
docker run \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e BlackSlope__Api__MoviesConnectionString="Server=prod;..." \
  -e BlackSlope__Api__AzureAd__Tenant="your-tenant-id" \
  blackslope.api
```

#### 3. Verify Environment Variable Loading

Add logging to verify environment variables are loaded:

```csharp
// In Program.cs or Startup.cs
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Console.WriteLine($"Environment: {environment}");

var connectionString = Environment.GetEnvironmentVariable("BlackSlope__Api__MoviesConnectionString");
Console.WriteLine($"Connection String from env: {connectionString}");
```

## Docker Issues

### Container Startup Failures

**Symptoms:**
- Container exits immediately after starting
- "docker start" command fails
- Container status shows "Exited" or "Restarting"

**Solutions:**

#### 1. Check Container Logs

```bash
# View container logs
docker logs blackslope-container

# Follow logs in real-time
docker logs -f blackslope-container

# View last 100 lines
docker logs --tail 100 blackslope-container
```

Common startup errors:
- Missing configuration files
- Database connection failures
- Port binding errors
- Missing dependencies

#### 2. Verify Dockerfile Build

The Dockerfile uses multi-stage builds:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
WORKDIR /BlackSlope.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**Build the image with verbose output:**
```bash
cd src
docker build -t blackslope.api -f Dockerfile . --progress=plain
```

**Common build issues:**
- Incorrect WORKDIR path - ensure it matches your project structure
- Missing files during COPY - verify `.dockerignore` doesn't exclude required files
- Restore failures - check network connectivity and NuGet sources

#### 3. Interactive Debugging

Run the container interactively to debug startup issues:

```bash
# Override entrypoint to get a shell
docker run -it --entrypoint /bin/bash blackslope.api

# Inside container, manually start the application
cd /app
dotnet BlackSlope.Api.dll
```

#### 4. Health Check Configuration

The application includes health check endpoints. Verify they're accessible:

```bash
# Start container with port mapping
docker run -d -p 8080:80 --name blackslope-container blackslope.api

# Check health endpoint
curl http://localhost:8080/health

# Expected response: Healthy
```

Configure Docker health checks in `docker-compose.yml`:

```yaml
services:
  api:
    image: blackslope.api
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### Networking Problems

**Symptoms:**
- Cannot access application from host
- Container cannot connect to SQL Server
- Inter-container communication failures

**Solutions:**

#### 1. Port Mapping Issues

Verify port mappings when creating containers:

```bash
# Correct port mapping
docker run -d -p 8080:80 -p 8443:443 --name blackslope-container blackslope.api

# Verify port mappings
docker port blackslope-container

# Expected output:
# 80/tcp -> 0.0.0.0:8080
# 443/tcp -> 0.0.0.0:8443
```

**Test connectivity:**
```bash
# From host machine
curl http://localhost:8080/swagger

# Check if port is listening
netstat -an | grep 8080  # Linux/Mac
netstat -an | findstr 8080  # Windows
```

#### 2. Container-to-Host Communication

To connect from container to SQL Server on host:

**Windows (Docker Desktop):**
```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=host.docker.internal,1433;initial catalog=movies;User Id=sa;Password=YourPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  }
}
```

**Linux:**
```bash
# Use host IP address
docker run -e BlackSlope__Api__MoviesConnectionString="Server=172.17.0.1,1433;..." blackslope.api

# Or use host network mode (Linux only)
docker run --network host blackslope.api
```

#### 3. Docker Network Configuration

Create a custom network for multi-container setups:

```bash
# Create network
docker network create blackslope-network

# Run SQL Server container
docker run -d \
  --name sql-server \
  --network blackslope-network \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Password" \
  mcr.microsoft.com/mssql/server:2019-latest

# Run API container
docker run -d \
  --name blackslope-api \
  --network blackslope-network \
  -p 8080:80 \
  -e BlackSlope__Api__MoviesConnectionString="Server=sql-server,1433;..." \
  blackslope.api
```

#### 4. Firewall and Security Groups

**Windows Firewall:**
```powershell
# Allow Docker Desktop
New-NetFirewallRule -DisplayName "Docker Desktop" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8080
```

**Azure/Cloud:**
- Ensure Network Security Groups allow inbound traffic on required ports
- Verify container instances have proper network configuration
- Check Application Gateway or Load Balancer settings

### Volume Mounting Issues

**Symptoms:**
- Configuration files not found in container
- Logs not persisted to host
- Database files not accessible

**Solutions:**

#### 1. Mount Configuration Files

```bash
# Mount appsettings.json from host
docker run -d \
  -v /path/to/appsettings.json:/app/appsettings.json \
  -p 8080:80 \
  blackslope.api

# Windows example
docker run -d \
  -v C:\config\appsettings.json:/app/appsettings.json \
  -p 8080:80 \
  blackslope.api
```

#### 2. Persist Logs

```bash
# Mount log directory
docker run -d \
  -v /var/log/blackslope:/app/logs \
  -e BlackSlope__Api__Serilog__WriteToFile=true \
  -e BlackSlope__Api__Serilog__FileName=/app/logs/log.txt \
  blackslope.api
```

#### 3. Docker Compose Volume Configuration

```yaml
version: '3.8'
services:
  api:
    image: blackslope.api
    ports:
      - "8080:80"
    volumes:
      - ./config/appsettings.json:/app/appsettings.json:ro
      - ./logs:/app/logs
      - app-data:/app/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production

volumes:
  app-data:
```

#### 4. Volume Permission Issues (Linux)

```bash
# Check volume permissions
docker exec blackslope-container ls -la /app

# Fix permissions if needed
docker exec blackslope-container chown -R app:app /app/logs

# Or run container with specific user
docker run -d --user 1000:1000 -v /logs:/app/logs blackslope.api
```

## Testing Issues

### Test Execution Failures

**Symptoms:**
- Tests fail to discover or execute
- "No tests found" messages
- Test runner crashes or hangs

**Solutions:**

#### 1. Restore Test Dependencies

```bash
# Restore packages for test projects
dotnet restore src/BlackSlope.NET.sln

# Build test projects
dotnet build src/BlackSlope.Api.Tests/BlackSlope.Api.Tests.csproj
```

#### 2. Run Tests with Verbose Output

```bash
# Run all tests
dotnet test ./src/ --verbosity detailed

# Run specific test project
dotnet test src/BlackSlope.Api.Tests/BlackSlope.Api.Tests.csproj

# Run with logger for detailed output
dotnet test ./src/ --logger "console;verbosity=detailed"
```

#### 3. Test Discovery Issues

Verify test framework packages are installed:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
  <PackageReference Include="xUnit" Version="2.4.1" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
</ItemGroup>
```

#### 4. Parallel Test Execution

Disable parallel execution if tests conflict:

```csharp
// In test assembly
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

Or in `xunit.runner.json`:
```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

### SpecFlow .NET 6 Compatibility

**Important Note:** As documented in the README, integration test projects using SpecFlow have been removed from the solution until SpecFlow adds full support for .NET 6.

**Current Status:**
```
NOTE: Per 6.x, these projects have been removed from the Solution until 
SpecFlow adds support for the latest version of .NET 6
```

**Affected Projects:**
- `BlackSlope.Api.Tests.IntegrationTests`
- `BlackSlope.Api.Tests.RestSharpIntegrationTests`

**Symptoms:**
- SpecFlow tests fail to compile
- Runtime binding errors with SpecFlow
- Feature file generation issues

**Workarounds:**

#### 1. Use .NET 5 or Earlier

Temporarily target .NET 5 for integration tests:

```xml
<PropertyGroup>
  <TargetFramework>net5.0</TargetFramework>
</PropertyGroup>
```

#### 2. Check SpecFlow Version Compatibility

Monitor SpecFlow releases for .NET 6 support:
- https://github.com/SpecFlowOSS/SpecFlow/releases
- https://docs.specflow.org/projects/specflow/en/latest/

#### 3. Alternative Testing Approaches

While waiting for SpecFlow support, consider:

**Option A: Use xUnit/NUnit for integration tests**
```csharp
public class MovieApiIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly HttpClient _client;

    public MovieApiIntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMovies_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/movies");
        response.EnsureSuccessStatusCode();
    }
}
```

**Option B: Use BDDfy or LightBDD**
```csharp
[Fact]
public void GetMovieById()
{
    this.Given(_ => GivenAMovieExists())
        .When(_ => WhenIRequestTheMovie())
        .Then(_ => ThenTheMovieIsReturned())
        .BDDfy();
}
```

### Integration Test Setup

When SpecFlow support is restored, the integration tests require specific configuration.

**Configuration File:** `appsettings.test.json`

```json
{
  "BlackSlopeHost": "http://localhost:51385",
  "DBConnectionString": "data source=localhost,1433;initial catalog=movies_test;User Id=sa;Password=TestPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;"
}
```

**Environment Setup:** The `EnvironmentSetup.cs` file configures the test environment:

```csharp
[Binding]
public sealed class EnvironmentSetup
{
    private readonly IObjectContainer objectContainer;
    private readonly ITestOutputHelper _outputHelper;

    public EnvironmentSetup(IObjectContainer objectContainer, ITestOutputHelper outputHelper)
    {
        this.objectContainer = objectContainer;
        _outputHelper = outputHelper;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json")
            .Build();
        
        Environments.BaseUrl = configuration["BlackSlopeHost"];
        Environments.DBConnection = configuration["DBConnectionString"];
    }

    [BeforeScenario]
    public void InitializeWebServices()
    {
        var movieService = new MovieService(_outputHelper);
        objectContainer.RegisterInstanceAs<ITestServices>(movieService);
    }
}
```

**Setup Steps:**

1. **Create Test Database:**
```sql
CREATE DATABASE movies_test;
GO
```

2. **Update Configuration:**
   - Copy `appsettings.test.json.template` to `appsettings.test.json`
   - Update `BlackSlopeHost` with your API URL
   - Update `DBConnectionString` with test database connection

3. **Run API Separately:**
   - Integration tests require the API to be running
   - Start the API in a separate IDE instance or terminal:
   ```bash
   dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
   ```

4. **Install SpecFlow IDE Extensions:**
   - Visual Studio: SpecFlow for Visual Studio
   - VS Code: Cucumber (Gherkin) Full Support
   - Rider: SpecFlow Support

5. **Execute Tests:**
```bash
dotnet test src/BlackSlope.Api.IntegrationTests/BlackSlope.Api.IntegrationTests.csproj
```

**Common Integration Test Issues:**

| Issue | Solution |
|-------|----------|
| API not accessible | Verify API is running and `BlackSlopeHost` URL is correct |
| Database connection fails | Ensure test database exists and connection string is valid |
| Test data conflicts | Use separate test database and clean up after each test |
| Authentication failures | Configure test authentication tokens or disable auth for tests |
| Timeout errors | Increase test timeout or check API performance |

For more debugging techniques, see [Debugging Guide](/troubleshooting/debugging.md).

---

## Additional Resources

- **Installation Guide:** [/getting_started/installation.md](/getting_started/installation.md)
- **Database Migrations:** [/database/migrations.md](/database/migrations.md)
- **Debugging Guide:** [/troubleshooting/debugging.md](/troubleshooting/debugging.md)
- **BlackSlope Blog Series:**
  - [Introducing BlackSlope](https://medium.com/slalom-build/introducing-black-slope-a-dotnet-core-reference-architecture-from-slalom-build-3f1452eb62ef)
  - [Components Deep Dive](https://medium.com/slalom-build/blackslope-a-deeper-look-at-the-components-of-our-dotnet-reference-architecture-b7b3a9d6e43b)
  - [Usage Guide](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)

## Getting Help

If you encounter issues not covered in this document:

1. Check application logs (console output or log files)
2. Review health check endpoint: `http://localhost:51385/health`
3. Enable verbose logging in `appsettings.json`:
   ```json
   {
     "BlackSlope.Api": {
       "Serilog": {
         "MinimumLevel": "debug",
         "WriteToConsole": "true"
       }
     }
   }
   ```
4. Consult the debugging guide for advanced troubleshooting techniques