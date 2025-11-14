# Prerequisites

This document outlines the required and optional software, tools, and configurations necessary to develop, build, test, and deploy the BlackSlope.NET application. Ensure all prerequisites are properly installed and configured before proceeding with the [installation](/getting_started/installation.md) process.

## Required Software

### .NET 6.0 SDK

The BlackSlope.NET application is built on **.NET 6.0**, targeting the `net6.0` framework. Both the web API and console applications require the .NET 6.0 SDK for compilation and runtime execution.

**Note**: The project file contains legacy output path references to `net5.0` in the Debug and Release configurations, but the actual target framework is `net6.0`.

#### Installation

Download and install the .NET 6.0 SDK from the official Microsoft website:
- **Official Download**: https://dotnet.microsoft.com/download/dotnet/6.0

Choose the appropriate installer for your operating system:
- **Windows**: x64 or x86 installer
- **macOS**: x64 or ARM64 installer
- **Linux**: Package manager instructions or binary archives

#### Verification

After installation, verify the SDK is correctly installed by opening a terminal or command prompt and running:

```bash
dotnet --version
```

Expected output should show version `6.0.x`:

```
6.0.xxx
```

To list all installed SDKs:

```bash
dotnet --list-sdks
```

Expected output:

```
6.0.xxx [C:\Program Files\dotnet\sdk]
```

#### SDK Components

The .NET 6.0 SDK includes:
- **C# 10 compiler**: For language features and syntax
- **MSBuild**: Build engine for compiling projects
- **NuGet**: Package manager for dependency resolution
- **dotnet CLI**: Command-line interface for project management
- **ASP.NET Core 6.0 runtime**: For web API applications
- **.NET Runtime 6.0**: For console applications

### SQL Server Developer 2019

The application uses **Microsoft SQL Server** as its primary database, with Entity Framework Core 6.0.1 as the ORM layer. SQL Server Developer Edition is recommended for development environments.

#### Installation

1. Download SQL Server Developer 2019 from Microsoft:
   - **Official Download**: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

2. Run the installer and select **Developer Edition** (free for development and testing)

3. Choose installation type:
   - **Basic**: Quick installation with default settings (recommended for most developers)
   - **Custom**: Advanced configuration options
   - **Download Media**: For offline installation

4. During installation, note the following:
   - **Instance Name**: Default instance (MSSQLSERVER) or named instance
   - **Authentication Mode**: Mixed Mode (SQL Server and Windows Authentication) recommended
   - **SA Password**: Set a strong password for the system administrator account
   - **Data Directories**: Default locations are typically sufficient

#### Post-Installation Configuration

After installation, configure SQL Server for the BlackSlope.NET application:

1. **Enable TCP/IP Protocol** (if not enabled):
   - Open SQL Server Configuration Manager
   - Navigate to SQL Server Network Configuration → Protocols for [Instance Name]
   - Enable TCP/IP protocol
   - Restart SQL Server service

2. **Create Database**:
   
   Using SQL Server Management Studio (SSMS):
   ```sql
   CREATE DATABASE movies;
   GO
   ```

   Or using command-line tools (see Optional Tools section).

3. **Update Connection String**:
   
   Modify the connection string in `src/BlackSlope.Api/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "MoviesConnectionString": "Server=localhost;Database=movies;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
     }
   }
   ```

   **Connection String Parameters**:
   - `Server`: SQL Server instance name (e.g., `localhost`, `.\SQLEXPRESS`, or `server-name\instance-name`)
   - `Database`: Database name (`movies`)
   - `User Id`: SQL Server authentication username
   - `Password`: SQL Server authentication password
   - `TrustServerCertificate=True`: Required for SQL Server 2019+ with self-signed certificates

   **Alternative (Windows Authentication)**:
   ```json
   "MoviesConnectionString": "Server=localhost;Database=movies;Integrated Security=True;TrustServerCertificate=True;"
   ```

#### Database Provider Configuration

The application uses the following Entity Framework Core packages for SQL Server integration:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.3" />
```

**Note**: While `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.Design` are referenced in the tech stack description, they are not present in the `BlackSlope.Api.Common` shared library project. These packages are likely referenced in the main API project (`BlackSlope.Api.csproj`) instead.

The `Microsoft.Data.SqlClient` package provides:
- Modern SQL Server data provider with enhanced security and performance
- Enhanced connection resiliency and performance features
- Support for Azure Active Directory authentication

### Docker Desktop

Docker support is integrated into the BlackSlope.NET application for containerized deployments. The project includes Docker configuration with **Windows containers** as the default target OS.

#### Installation

1. Download Docker Desktop from the official website:
   - **Official Download**: https://www.docker.com/products/docker-desktop

2. System Requirements:
   - **Windows**: Windows 10 64-bit Pro, Enterprise, or Education (Build 19041 or higher) with Hyper-V and WSL 2
   - **macOS**: macOS 10.15 or newer
   - **Linux**: 64-bit distribution with KVM virtualization support

3. Install Docker Desktop and follow the setup wizard

4. **Windows-Specific Configuration**:
   - Enable Hyper-V and Containers Windows features
   - Install WSL 2 (Windows Subsystem for Linux)
   - Configure Docker to use WSL 2 backend (recommended) or Hyper-V

#### Verification

After installation, verify Docker is running:

```bash
docker --version
```

Expected output:

```
Docker version 20.x.x, build xxxxxxx
```

Test Docker functionality:

```bash
docker run hello-world
```

This command downloads a test image and runs a container, confirming Docker is properly configured.

#### Docker Configuration for BlackSlope.NET

The application includes a multi-stage Dockerfile optimized for .NET 6.0 applications:

```dockerfile
# Build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
WORKDIR /BlackSlope.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**Dockerfile Breakdown**:
- **Build Stage**: Uses `mcr.microsoft.com/dotnet/sdk:6.0` image containing the full SDK for compilation
- **Runtime Stage**: Uses `mcr.microsoft.com/dotnet/aspnet:6.0` image containing only the ASP.NET Core runtime (smaller image size)
- **Exposed Ports**: 80 (HTTP) and 443 (HTTPS)
- **Entry Point**: Launches the compiled BlackSlope.Api.dll

**Container Tools Integration**:

The project includes Visual Studio container tools support:

```xml
<PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.14.0" />
```

This package enables:
- F5 debugging in Docker containers from Visual Studio
- Docker Compose orchestration
- Container build and publish integration

For detailed Docker setup and deployment instructions, see [Docker Deployment Guide](/deployment/docker.md).

### Development IDE

A modern IDE with .NET 6.0 support is required for development. Choose one of the following:

#### Visual Studio 2022 (Recommended for Windows)

- **Edition**: Community (free), Professional, or Enterprise
- **Workloads Required**:
  - ASP.NET and web development
  - .NET desktop development
  - Azure development (for Azure integration features)
- **Extensions Recommended**:
  - ReSharper or CodeMaid (code quality and refactoring)
  - GitHub Copilot (AI-assisted coding)
  - Docker Tools (integrated container management)

**Features Utilized**:
- Integrated debugging with Docker containers
- Entity Framework Core Power Tools
- Built-in StyleCop and Code Analysis integration
- User Secrets management UI

#### Visual Studio Code (Cross-Platform)

- **Version**: Latest stable release
- **Required Extensions**:
  - C# (Microsoft) - Language support and debugging
  - C# Dev Kit (Microsoft) - Enhanced C# development experience
  - Docker (Microsoft) - Container management
  - REST Client or Thunder Client - API testing
- **Optional Extensions**:
  - GitLens - Enhanced Git integration
  - EditorConfig for VS Code - Code style enforcement
  - NuGet Package Manager - Package management UI

**Configuration**:

Create or update `.vscode/launch.json` for debugging:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/BlackSlope.Api/bin/Debug/net6.0/BlackSlope.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/BlackSlope.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

#### JetBrains Rider (Cross-Platform Alternative)

- **Version**: 2022.3 or later
- **Built-in Features**:
  - Excellent .NET 6.0 support
  - Integrated database tools
  - Docker and Docker Compose support
  - Advanced refactoring capabilities

## Optional Tools

### Entity Framework Core CLI Tools

The EF Core command-line tools are essential for database migrations and scaffolding operations.

#### Installation

Install the global tool using the .NET CLI:

```bash
dotnet tool install --global dotnet-ef
```

#### Verification

Verify installation:

```bash
dotnet ef --version
```

Expected output:

```
Entity Framework Core .NET Command-line Tools
6.0.x
```

#### Common Commands

**Create Migration**:
```bash
dotnet ef migrations add MigrationName --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Update Database**:
```bash
dotnet ef database update --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**List Migrations**:
```bash
dotnet ef migrations list --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Generate SQL Script**:
```bash
dotnet ef migrations script --project ./src/BlackSlope.Api/BlackSlope.Api.csproj --output migration.sql
```

**Remove Last Migration** (if not applied):
```bash
dotnet ef migrations remove --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

#### Initial Database Setup

After installing SQL Server and configuring the connection string, initialize the database:

```bash
cd /path/to/repository
dotnet ef database update --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

Expected output:

```
Build started...
Build succeeded.
Applying migration '20190814225754_initialized'.
Applying migration '20190814225910_seeded'.
Done.
```

This command applies all pending migrations and seeds initial data.

### mssql-cli

A cross-platform command-line tool for SQL Server, providing an alternative to SQL Server Management Studio for database management.

#### Installation

**Using pip (Python package manager)**:

```bash
pip install mssql-cli
```

**Prerequisites**:
- Python 3.6 or higher
- pip package manager

#### Usage

**Connect to SQL Server**:

```bash
mssql-cli -S localhost -U sa -P YourPassword
```

**Create Database**:

```sql
CREATE DATABASE movies;
GO
USE movies;
GO
```

**Query Database**:

```sql
SELECT * FROM Movies;
GO
```

**Features**:
- IntelliSense and auto-completion
- Syntax highlighting
- Multi-line editing
- Query history
- Cross-platform support (Windows, macOS, Linux)

**Alternative**: Azure Data Studio (GUI-based, cross-platform SQL Server management tool)

### SpecFlow IDE Extensions

**Note**: As of .NET 6.0, SpecFlow integration test projects have been temporarily removed from the solution pending full .NET 6 support. These extensions will be required when SpecFlow projects are re-integrated.

SpecFlow enables Behavior-Driven Development (BDD) with Gherkin syntax for integration testing.

#### Visual Studio Extension

Install from Visual Studio Marketplace:
- **Extension Name**: SpecFlow for Visual Studio 2022
- **Features**:
  - Gherkin syntax highlighting
  - Step definition navigation
  - Feature file templates
  - Test execution integration

#### VS Code Extension

Install from VS Code Marketplace:
- **Extension Name**: Cucumber (Gherkin) Full Support
- **Features**:
  - Syntax highlighting for .feature files
  - Step definition auto-completion
  - Snippet support

#### Integration Test Projects

The solution includes two SpecFlow-based integration test projects (currently disabled):

1. **BlackSlope.Api.Tests.IntegrationTests**
   - Uses `System.Net.Http.HttpClient` for API testing
   - Direct HTTP client implementation

2. **BlackSlope.Api.Tests.RestSharpIntegrationTests**
   - Uses RestSharp library for API testing
   - Simplified HTTP client with fluent API

**Configuration** (when re-enabled):

Update `appsettings.test.json` in the integration test project:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=movies_test;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:51385"
  }
}
```

**Running Integration Tests**:

```bash
dotnet test ./src/BlackSlope.Api.Tests.IntegrationTests/
```

## Additional Development Tools

### Logging and Diagnostics

The project uses **Serilog** for structured logging with multiple output sinks:

**Package References**:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="4.1.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="3.3.0" />
<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="3.1.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="4.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

**Logging Capabilities**:
- **Serilog.AspNetCore**: ASP.NET Core integration with request logging
- **Serilog.Settings.Configuration**: Configuration from `appsettings.json`
- **Serilog.Sinks.ApplicationInsights**: Azure Application Insights integration for cloud telemetry
- **Serilog.Sinks.Console**: Console output for development
- **Serilog.Sinks.File**: File-based logging for persistent logs

**Configuration Example** (`appsettings.json`):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Code Quality and Analysis

The project enforces code quality through two analyzer systems:

#### StyleCop Analyzers

**Configuration**: `stylecop.json` at project level

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "Slalom Build",
      "copyrightText": "Copyright (c) {companyName}. All rights reserved."
    }
  }
}
```

**Package Reference** (from `BlackSlope.Api.Common.csproj`):

```xml
<PackageReference Include="StyleCop.Analyzers" Version="1.1.118">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Suppressed Rules** (defined in `GlobalSuppressions.cs`):

| Rule ID | Description |
|---------|-------------|
| SA1101  | Prefix local calls with this |
| SA1309  | Field names should not begin with underscore |
| SA1600  | Elements should be documented |
| SA1614  | Element parameter documentation must have text |
| SA1616  | Element return value documentation must have text |
| SA1629  | Documentation text should end with period |
| SA1633  | File should have header |

#### Microsoft Code Analysis (NetAnalyzers)

**Package Reference** (from `BlackSlope.Api.Common.csproj`):

```xml
<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Configuration**: `.editorconfig` at solution or project level

**Suppressed Rules**:

| Rule ID | Description | Scope |
|---------|-------------|-------|
| CA1031  | Do not catch general exception types | ExceptionHandlingMiddleware only |
| CA1710  | Identifiers should have correct suffix | CompositeValidator class |

### Additional Libraries

The `BlackSlope.Api.Common` shared library includes several additional packages:

**Authentication**:
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.1" />
```
- JWT Bearer token authentication for securing API endpoints
- Integration with Azure Active Directory and other OAuth 2.0 providers

**JSON Serialization**:
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="6.0.1" />
```
- Newtonsoft.Json (Json.NET) integration for ASP.NET Core MVC
- Alternative to System.Text.Json with more flexible serialization options

**Swagger/OpenAPI**:
```xml
<PackageReference Include="Swashbuckle.AspNetCore.Swagger" Version="6.2.3" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerGen" Version="6.2.3" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="6.3.0" />
```
- Complete Swagger/OpenAPI documentation generation
- Interactive API explorer UI
- Automatic schema generation from controllers and models

**File System Abstraction**:
```xml
<PackageReference Include="System.IO.Abstractions" Version="14.0.13" />
```
- Testable file system operations
- Enables mocking of file I/O for unit tests

### Build Scripts

The project includes shell scripts for common build operations:

**Build Script** (`scripts/build.sh`):

```bash
cd ../src
dotnet build BlackSlope.NET.sln
```

**Usage**:

```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

### User Secrets Management

For secure local development, the project uses ASP.NET Core User Secrets:

**User Secrets ID** (defined in `BlackSlope.Api.csproj`):

```xml
<UserSecretsId>eeaaec3a-f784-4d04-8b1d-8fe6d9637231</UserSecretsId>
```

**Initialize User Secrets**:

```bash
dotnet user-secrets init --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Set Secret**:

```bash
dotnet user-secrets set "ConnectionStrings:MoviesConnectionString" "Server=localhost;Database=movies;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**List Secrets**:

```bash
dotnet user-secrets list --project ./src/BlackSlope.Api/BlackSlope.Api.csproj
```

User secrets are stored outside the project directory and are never committed to source control.

## Environment Configuration

### Development Environment Variables

Set the following environment variables for local development:

**Windows (PowerShell)**:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
```

**macOS/Linux (Bash)**:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
```

### Health Check Configuration

The application includes health check endpoints for monitoring:

**Packages**:

```xml
<PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="5.0.3" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="6.0.1" />
```

**Health Check Endpoints**:
- `/health`: Overall application health
- `/health/ready`: Readiness probe (database connectivity)
- `/health/live`: Liveness probe (application responsiveness)

These endpoints are essential for container orchestration and production monitoring.

## Verification Checklist

Before proceeding to [installation](/getting_started/installation.md), verify the following:

- [ ] .NET 6.0 SDK installed and verified (`dotnet --version`)
- [ ] SQL Server Developer 2019 installed and running
- [ ] Database created (`movies`)
- [ ] Connection string configured in `appsettings.json`
- [ ] Docker Desktop installed and running (`docker --version`)
- [ ] IDE installed with required extensions
- [ ] Entity Framework Core CLI tools installed (`dotnet ef --version`)
- [ ] Repository cloned locally
- [ ] Build script executes successfully (`dotnet build`)
- [ ] User secrets initialized (optional but recommended)

## Next Steps

Once all prerequisites are installed and verified:

1. Proceed to [Installation Guide](/getting_started/installation.md) for detailed setup instructions
2. Review [Introduction](/getting_started/introduction.md) for architecture overview
3. Consult [Docker Deployment](/deployment/docker.md) for containerization details

## Troubleshooting

### Common Issues

**Issue**: `dotnet` command not recognized
- **Solution**: Ensure .NET SDK is in system PATH; restart terminal after installation

**Issue**: SQL Server connection fails
- **Solution**: Verify SQL Server service is running; check firewall settings; confirm TCP/IP protocol is enabled

**Issue**: Docker build fails with "no matching manifest"
- **Solution**: Ensure Docker is configured for Windows containers (if using Windows); pull base images manually

**Issue**: Entity Framework migrations fail
- **Solution**: Verify connection string; ensure database exists; check SQL Server authentication mode

**Issue**: StyleCop warnings overwhelming
- **Solution**: Review `GlobalSuppressions.cs`; adjust `.editorconfig` severity levels; suppress specific rules as needed

For additional support, consult the project's README.md or contact the development team.