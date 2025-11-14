# Installation and Setup

This guide provides comprehensive instructions for setting up the BlackSlope.NET application in your local development environment. The application consists of a .NET 6.0 Web API and a console utility (RenameUtility), both utilizing Microsoft SQL Server as the database backend.

## Prerequisites

Before beginning the installation process, ensure you have completed all requirements outlined in [Prerequisites](/getting_started/prerequisites.md). This includes having .NET 6.0 SDK, SQL Server, and Docker Desktop (if using containerization) installed on your development machine.

## Cloning the Repository

### Initial Repository Setup

Clone the BlackSlope.NET repository to your local development environment:

```bash
git clone <repository-url>
cd BlackSlope.NET
```

### Repository Structure

After cloning, familiarize yourself with the key directories:

```
BlackSlope.NET/
├── src/
│   ├── BlackSlope.Api/              # Main Web API project
│   ├── BlackSlope.Api.Common/       # Shared libraries and utilities
│   ├── RenameUtility/               # Console application
│   └── BlackSlope.NET.sln           # Solution file
├── scripts/
│   ├── build.sh                     # Build automation script
│   ├── db-update.sh                 # Database migration script
│   └── data.sql                     # Sample data seeding script
└── README.md
```

### User Secrets Configuration

The application uses User Secrets for secure local development configuration management. Initialize user secrets for the API project:

```bash
cd src/BlackSlope.Api
dotnet user-secrets init
```

This creates a secure storage location outside your repository for sensitive configuration values like connection strings and API keys.

## Database Setup

### Creating the SQL Server Database

The application requires Microsoft SQL Server (Developer Edition 2019 or later recommended) with a database named `movies`.

#### Option 1: Using SQL Server Management Studio (SSMS)

1. Open SQL Server Management Studio
2. Connect to your local SQL Server instance
3. Right-click on **Databases** in Object Explorer
4. Select **New Database**
5. Enter `movies` as the database name
6. Click **OK** to create the database

#### Option 2: Using Command-Line Tools (mssql-cli)

```bash
# Install mssql-cli if not already installed
pip install mssql-cli

# Connect to SQL Server
mssql-cli -S localhost -U sa

# Create the database
CREATE DATABASE movies;
GO
```

#### Option 3: Using Azure SQL Database

If deploying to Azure, create an Azure SQL Database instance and note the connection string for the next step.

### Configuring Connection Strings

The application uses Entity Framework Core 6.0.1 with the SQL Server provider. Connection strings are configured in `appsettings.json`.

#### Local Development Configuration

Edit `src/BlackSlope.Api/appsettings.json` and update the `MoviesConnectionString`:

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=localhost,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
  }
}
```

#### Connection String Parameters Explained

| Parameter | Description | Example Value |
|-----------|-------------|---------------|
| `data source` | SQL Server instance name and port | `localhost,1433` or `.,1433` |
| `initial catalog` | Database name | `movies` |
| `Integrated Security` | Use Windows Authentication | `true` (or use `User Id` and `Password` for SQL Auth) |
| `MultipleActiveResultSets` | Enable MARS for EF Core | `True` (required for certain EF Core operations) |

#### SQL Server Authentication Alternative

If using SQL Server authentication instead of Windows authentication:

```json
"MoviesConnectionString": "data source=localhost,1433;initial catalog=movies;User Id=your_username;Password=your_password;MultipleActiveResultSets=True;"
```

**Security Best Practice**: For production environments, store connection strings in Azure Key Vault or use Managed Identity with Azure SQL Database. For local development, consider using User Secrets:

```bash
cd src/BlackSlope.Api
dotnet user-secrets set "BlackSlope.Api:MoviesConnectionString" "your-connection-string-here"
```

#### Verifying Database Connectivity

The application includes health checks via `AspNetCore.HealthChecks.SqlServer` (5.0.3) and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (6.0.1). After configuration, you can verify connectivity by running the health check endpoint (covered in the [Running the Application](#running-the-application) section).

### Running Entity Framework Migrations

The application uses Entity Framework Core 6.0.1 with the Design package for migration support. Migrations create the database schema and seed initial data.

#### Installing the EF Core CLI Tool

Install the `dotnet-ef` global tool if not already installed:

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:

```bash
dotnet ef --version
# Expected output: Entity Framework Core .NET Command-line Tools 6.0.x
```

#### Applying Migrations

Navigate to the repository root and execute the migration command:

```bash
dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
```

**Alternative**: Use the provided shell script for Unix-based systems:

```bash
cd scripts
chmod +x db-update.sh
./db-update.sh
```

The `db-update.sh` script contains:

```bash
cd ../src/BlackSlope.Api
dotnet ef database update -v
```

The `-v` flag enables verbose output for troubleshooting.

#### Expected Migration Output

Successful migration execution produces output similar to:

```
Build started...
Build succeeded.
Applying migration '20190814225754_initialized'.
Applying migration '20190814225910_seeded'.
Done.
```

#### Migration Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "No DbContext was found" | Project reference issue | Ensure you're running from the correct directory and the project file path is correct |
| "Login failed for user" | Connection string authentication issue | Verify SQL Server credentials and authentication mode |
| "Cannot open database" | Database doesn't exist | Create the database manually first (see [Creating the SQL Server Database](#creating-the-sql-server-database)) |
| "A network-related error occurred" | SQL Server not running or firewall blocking | Start SQL Server service and check firewall rules |

For more details on managing migrations, see [Database Migrations](/database/migrations.md).

### Seeding Initial Data

The application includes a seeded migration that populates the `Movies` table with sample data. The seed data is also available in `scripts/data.sql`:

```sql
INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Shawshank Redemption','Two imprisoned men bond over a number of years, finding solace and eventual redemption through acts of common decency.')
GO

INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Godfather','The aging patriarch of an organized crime dynasty transfers control of his clandestine empire to his reluctant son.')
GO

INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Godfather: Part II','The early life and career of Vito Corleone in 1920s New York is portrayed while his son, Michael, expands and tightens his grip on the family crime syndicate.')
GO

INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Dark Knight','When the menace known as the Joker emerges from his mysterious past, he wreaks havoc and chaos on the people of Gotham, the Dark Knight must accept one of the greatest psychological and physical tests of his ability to fight injustice.')
GO

INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('12 Angry Men','A jury holdout attempts to prevent a miscarriage of justice by forcing his colleagues to reconsider the evidence.')
GO
```

This data is automatically applied during the `dotnet ef database update` process via the seeded migration. If you need to manually execute the seed script:

```bash
sqlcmd -S localhost -d movies -i scripts/data.sql
```

## Building the Solution

### Using .NET CLI Commands

The application uses the .NET 6.0 SDK with two project types:
- **BlackSlope.Api**: Web API using `Microsoft.NET.Sdk.Web`
- **RenameUtility**: Console application using `Microsoft.NET.Sdk`

#### Building the Entire Solution

From the repository root:

```bash
dotnet build src/BlackSlope.NET.sln
```

This command:
1. Restores all NuGet package dependencies
2. Compiles all projects in the solution
3. Runs StyleCop.Analyzers (1.1.118) and Microsoft.CodeAnalysis.NetAnalyzers (6.0.0) for code quality checks
4. Outputs binaries to `bin/Debug/net6.0/` for each project

#### Building Individual Projects

Build only the Web API:

```bash
dotnet build src/BlackSlope.Api/BlackSlope.Api.csproj
```

Build only the RenameUtility console application:

```bash
dotnet build src/RenameUtility/RenameUtility.csproj
```

#### Build Configuration Options

Specify the build configuration (Debug or Release):

```bash
# Debug build (default)
dotnet build src/BlackSlope.NET.sln --configuration Debug

# Release build (optimized for production)
dotnet build src/BlackSlope.NET.sln --configuration Release
```

Release builds enable optimizations and disable debug symbols, resulting in smaller, faster binaries suitable for production deployment.

### Build Scripts Overview

The repository includes a build automation script at `scripts/build.sh`:

```bash
cd ../src
dotnet build BlackSlope.NET.sln
```

#### Using the Build Script

On Unix-based systems (Linux, macOS):

```bash
cd scripts
chmod +x build.sh
./build.sh
```

On Windows, use PowerShell or Git Bash to execute the script, or run the commands directly:

```powershell
cd src
dotnet build BlackSlope.NET.sln
```

### Verifying Successful Build

A successful build produces output similar to:

```
Microsoft (R) Build Engine version 17.0.0+c9eb9dd64 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  All projects are up-to-date for restore.
  BlackSlope.Api.Common -> C:\BlackSlope.NET\src\BlackSlope.Api.Common\bin\Debug\net6.0\BlackSlope.Api.Common.dll
  BlackSlope.Api -> C:\BlackSlope.NET\src\BlackSlope.Api\bin\Debug\net6.0\BlackSlope.Api.dll
  RenameUtility -> C:\BlackSlope.NET\src\RenameUtility\bin\Debug\net6.0\RenameUtility.exe

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:15.23
```

#### Build Verification Checklist

- [ ] All projects compiled without errors
- [ ] No StyleCop analyzer warnings (SA rules)
- [ ] No CodeAnalysis warnings (CA rules)
- [ ] Output assemblies created in `bin/Debug/net6.0/` directories
- [ ] NuGet packages successfully restored

#### Common Build Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "The SDK 'Microsoft.NET.Sdk.Web' specified could not be found" | .NET 6.0 SDK not installed | Install .NET 6.0 SDK from https://dotnet.microsoft.com/download |
| "Package restore failed" | Network connectivity or NuGet source issue | Check internet connection and NuGet.config settings |
| StyleCop warnings (SA####) | Code style violations | Review StyleCop rules in `stylecop.json` or suppress specific rules in `GlobalSuppressions.cs` |
| "The type or namespace name could not be found" | Missing package reference | Run `dotnet restore` explicitly |

#### Code Quality Analyzers

The solution uses two analyzer packages:

**StyleCop.Analyzers (1.1.118)**: Enforces code style consistency
- Configuration: `stylecop.json` at project level
- Suppressed rules: SA1101, SA1309, SA1600, SA1614, SA1616, SA1629, SA1633

**Microsoft.CodeAnalysis.NetAnalyzers (6.0.0)**: Provides .NET code quality analysis
- Configuration: `.editorconfig` at solution/project level
- Suppressed rules: CA1031 (specific to ExceptionHandlingMiddleware), CA1710 (CompositeValidator)

Global suppressions are managed in `BlackSlope.Api.Common.GlobalSuppressions.cs`. For more information on analyzer configuration, see the [StyleCop Configuration](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/Configuration.md) and [.editorconfig Configuration](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/Analyzer%20Configuration.md) documentation.

## Running the Application

### Starting the API Locally

#### Using .NET CLI

From the repository root:

```bash
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

The application starts and listens on the configured port (default: 55644 for HTTP, as specified in `appsettings.json`).

#### Expected Startup Output

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:55644
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\BlackSlope.NET\src\BlackSlope.Api
```

#### Running with Specific Environment

Specify the environment using the `ASPNETCORE_ENVIRONMENT` variable:

```bash
# Development (default)
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj --environment Development

# Production
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj --environment Production
```

Environment-specific configuration files (`appsettings.Development.json`, `appsettings.Production.json`) override base settings in `appsettings.json`. See [Application Settings](/configuration/application_settings.md) for detailed configuration options.

#### Running with Hot Reload

.NET 6.0 supports hot reload for rapid development:

```bash
dotnet watch run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

This automatically rebuilds and restarts the application when source files change.

### Verifying the Application is Running

#### Health Check Endpoint

The application includes comprehensive health checks using `AspNetCore.HealthChecks.SqlServer` (5.0.3) and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (6.0.1).

Access the health check endpoint:

```bash
curl http://localhost:55644/health
```

Or navigate to `http://localhost:55644/health` in your browser.

**Healthy Response** (HTTP 200):

```json
{
  "status": "Healthy",
  "results": {
    "sqlserver": {
      "status": "Healthy",
      "description": "SQL Server is responsive"
    },
    "dbcontext": {
      "status": "Healthy",
      "description": "Entity Framework Core DbContext is healthy"
    }
  }
}
```

**Unhealthy Response** (HTTP 503):

```json
{
  "status": "Unhealthy",
  "results": {
    "sqlserver": {
      "status": "Unhealthy",
      "description": "A network-related or instance-specific error occurred while establishing a connection to SQL Server.",
      "exception": "..."
    }
  }
}
```

The health check endpoint is configured in `appsettings.json`:

```json
"HealthChecks": {
  "Endpoint": "/health"
}
```

#### Testing API Endpoints

Test a sample endpoint to verify the API is functioning:

```bash
# Get all movies
curl http://localhost:55644/api/movies

# Expected response (sample data from seeded migration)
[
  {
    "id": 1,
    "title": "The Shawshank Redemption",
    "description": "Two imprisoned men bond over a number of years..."
  },
  ...
]
```

### Accessing Swagger UI

The application uses Swashbuckle.AspNetCore.SwaggerUI (6.3.0) to provide interactive API documentation.

#### Opening Swagger UI

Navigate to the Swagger endpoint in your browser:

```
http://localhost:55644/swagger
```

#### Swagger Configuration

Swagger is configured in `appsettings.json`:

```json
"Swagger": {
  "Version": "1",
  "ApplicationName": "BlackSlope",
  "XmlFile": "BlackSlope.Api.xml"
}
```

The `XmlFile` setting enables XML documentation comments to appear in the Swagger UI. Ensure XML documentation generation is enabled in the project file:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>BlackSlope.Api.xml</DocumentationFile>
</PropertyGroup>
```

#### Using Swagger UI

The Swagger UI provides:
- **Interactive API testing**: Execute requests directly from the browser
- **Request/response schemas**: View data models and validation rules
- **Authentication testing**: Configure JWT tokens for secured endpoints
- **API versioning**: Navigate between different API versions

#### Swagger Authentication

For endpoints secured with Azure AD authentication (using Azure.Identity 1.14.2 and JWT tokens), configure the bearer token in Swagger UI:

1. Click the **Authorize** button in the top-right corner
2. Enter your JWT token in the format: `Bearer <your-token>`
3. Click **Authorize** to apply the token to all requests

For more information on authentication configuration, see [Application Settings - Azure AD Configuration](/configuration/application_settings.md#azure-ad-configuration).

### Docker Setup (Alternative Deployment)

The application includes Docker support via `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (1.14.0) with Windows containers as the default target.

#### Building the Docker Image

From the `src` directory:

```bash
cd src
docker build -t blackslope.api -f Dockerfile .
```

#### Verifying the Image

```bash
docker images
# Expected output includes:
# REPOSITORY       TAG       IMAGE ID       CREATED         SIZE
# blackslope.api   latest    <image-id>     X minutes ago   XXX MB
```

#### Creating and Running the Container

```bash
# Create the container
docker create --name blackslope-container blackslope.api

# Start the container
docker start blackslope-container

# View container logs
docker logs blackslope-container

# Stop the container
docker stop blackslope-container
```

#### Docker Networking Considerations

When running in Docker, the application needs to connect to SQL Server. Update the connection string to use the host machine's IP address or a Docker network:

```json
"MoviesConnectionString": "data source=host.docker.internal,1433;initial catalog=movies;User Id=sa;Password=YourPassword;MultipleActiveResultSets=True;"
```

For production deployments, use Docker Compose or Kubernetes to orchestrate multi-container applications with proper networking and service discovery.

#### Verifying Docker Deployment

Access the containerized application:

```bash
# If port mapping is configured (e.g., -p 8080:80)
curl http://localhost:8080/health
```

Visual inspection in Docker Desktop should show the `blackslope-container` running with a healthy status.

## Testing

Run the test suite to verify your installation:

```bash
dotnet test ./src/
```

### Integration Tests

**Note**: As of .NET 6.x, the Integration Test projects have been removed from the solution until SpecFlow adds support for .NET 6.

BlackSlope provides two SpecFlow-driven Integration Test projects for use by Quality Engineers (QE):

- `BlackSlope.Api.Tests.IntegrationTests` - using `System.Net.Http.HttpClient` implementation
- `BlackSlope.Api.Tests.RestSharpIntegrationTests` - using RestSharp HttpClient implementation

These can be executed in Test Explorer like regular Unit Tests. Choose the implementation that best suits your project.

#### Integration Test Setup

1. Ensure you've successfully completed the database setup and application build steps above
2. Update the `appsettings.test.json` file in your Integration Test project with:
   - The proper database connection string
   - The host URL for the BlackSlope API
   - **Note**: The BlackSlope API can run on localhost if desired, but must run in a separate IDE instance to allow tests to execute
3. Download the appropriate [SpecFlow plugins](https://docs.specflow.org/projects/specflow/en/latest/Installation/Installation.html) for your IDE

## Next Steps

After successfully installing and running the application:

1. **Quick Start Guide**: Follow the [Quick Start](/getting_started/quick_start.md) tutorial to build your first feature
2. **Configuration Deep Dive**: Review [Application Settings](/configuration/application_settings.md) for advanced configuration options including:
   - Azure AD authentication setup
   - Serilog logging configuration
   - Application Insights integration
   - Polly resilience policies
3. **Database Management**: Learn about [Database Migrations](/database/migrations.md) for schema evolution and data seeding strategies

## Troubleshooting

### Common Installation Issues

| Issue | Symptoms | Resolution |
|-------|----------|------------|
| Port already in use | "Failed to bind to address http://localhost:55644" | Change the port in `appsettings.json` or terminate the process using the port |
| Database connection timeout | Health check returns Unhealthy, "Connection Timeout Expired" | Verify SQL Server is running, check firewall rules, validate connection string |
| Missing dependencies | Build errors referencing missing packages | Run `dotnet restore` explicitly, clear NuGet cache with `dotnet nuget locals all --clear` |
| EF Core tool not found | "dotnet ef: command not found" | Install the global tool: `dotnet tool install --global dotnet-ef` |
| Swagger not loading | 404 error on /swagger endpoint | Verify Swashbuckle.AspNetCore.SwaggerUI is installed and configured in Startup.cs |

### Logging and Diagnostics

The application uses Serilog for structured logging with multiple sinks configured in `appsettings.json`:

```json
"Serilog": {
  "MinimumLevel": "information",
  "FileName": "log.txt",
  "WriteToFile": "false",
  "WriteToConsole": "true",
  "WriteToAppInsights": "false"
}
```

Enable file logging for troubleshooting:

```json
"WriteToFile": "true"
```

Logs are written to `log.txt` in the application directory. For production environments, enable Application Insights logging:

```json
"WriteToAppInsights": "true",
"ApplicationInsights": {
  "InstrumentationKey": "[your-instrumentation-key]"
}
```

### Performance Considerations

The application includes several performance optimizations:

- **In-memory caching**: `Microsoft.Extensions.Caching.Memory` (6.0.2) for frequently accessed data
- **Connection pooling**: Enabled by default in `Microsoft.Data.SqlClient` (5.1.3)
- **MARS (Multiple Active Result Sets)**: Enabled in connection string for concurrent query execution
- **Polly resilience policies**: Configured for HTTP client resilience with retry and circuit breaker patterns

Monitor application performance using the health check endpoint and Application Insights telemetry.

## Additional Resources

- **BlackSlope Blog Series**:
  - [Introducing BlackSlope](https://medium.com/slalom-build/introducing-black-slope-a-dotnet-core-reference-architecture-from-slalom-build-3f1452eb62ef)
  - [BlackSlope Components Deep Dive](https://medium.com/slalom-build/blackslope-a-deeper-look-at-the-components-of-our-dotnet-reference-architecture-b7b3a9d6e43b)
  - [BlackSlope in Action](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)

- **Microsoft Documentation**:
  - [.NET 6.0 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-6)
  - [Entity Framework Core 6.0](https://docs.microsoft.com/en-us/ef/core/)
  - [ASP.NET Core Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api/)

- **Third-Party Libraries**:
  - [Polly Documentation](https://github.com/App-vNext/Polly/wiki)
  - [AutoMapper Documentation](https://docs.automapper.org/)
  - [FluentValidation Documentation](https://docs.fluentvalidation.net/)