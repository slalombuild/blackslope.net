# Development Environment

This guide provides comprehensive instructions for setting up and configuring your local development environment for the BlackSlope.NET application. The application consists of a .NET 6.0 Web API and a console utility (RenameUtility), both utilizing SQL Server for data persistence.

## IDE Setup

### Visual Studio Configuration

BlackSlope.NET is optimized for development with Visual Studio 2022 or later, which includes native support for .NET 6.0.

#### Required Visual Studio Workloads

Install the following workloads through the Visual Studio Installer:

- **ASP.NET and web development** - Required for Web API development
- **Azure development** - Required for Azure Identity integration
- **.NET desktop development** - Required for the RenameUtility console application
- **Data storage and processing** - Recommended for SQL Server tooling

#### Visual Studio Extensions

Install these extensions to enhance your development experience:

1. **SpecFlow for Visual Studio** - Required for integration tests (when .NET 6 support is added)
   - Download from: Visual Studio Marketplace
   - Provides syntax highlighting and IntelliSense for `.feature` files

2. **Docker Tools** - Pre-installed with Visual Studio 2022
   - Enables container debugging and management
   - Configured via `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (v1.14.0)

3. **Entity Framework Core Power Tools** (Optional)
   - Provides visual database design and reverse engineering capabilities
   - Complements the EF Core Design tools (v6.0.1)

#### Visual Studio Settings

Configure the following settings for optimal development:

**Code Style and Analysis**

The project uses StyleCop.Analyzers (v1.1.118) and Microsoft.CodeAnalysis.NetAnalyzers (v6.0.0). Configure your IDE to respect these rules:

1. Navigate to **Tools > Options > Text Editor > C# > Code Style**
2. Ensure "Enable EditorConfig support" is checked
3. The project includes `.editorconfig` files at the solution level

**Suppressed Rules:**

The following StyleCop rules are globally suppressed in `BlackSlope.Api.Common.GlobalSuppressions`:

| Rule ID | Description | Rationale |
|---------|-------------|-----------|
| SA1101 | Prefix local calls with this | Modern C# convention |
| SA1309 | Field names should not begin with underscore | Conflicts with common naming patterns |
| SA1600 | Elements should be documented | Documentation enforced at public API level only |
| SA1614 | Element parameter documentation must have text | Reduces documentation overhead |
| SA1616 | Element return value documentation must have text | Reduces documentation overhead |
| SA1629 | Documentation text should end with period | Formatting preference |
| SA1633 | File should have header | Not required for this project |

**User Secrets Configuration**

For secure local development, configure User Secrets:

1. Right-click the `BlackSlope.Api` project in Solution Explorer
2. Select **Manage User Secrets**
3. Add sensitive configuration values (connection strings, API keys, etc.)

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=movies;Trusted_Connection=True;"
  },
  "AzureAd": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "TenantId": "your-tenant-id"
  }
}
```

### VS Code Setup

While Visual Studio is recommended, VS Code can be used for lightweight development tasks.

#### Required Extensions

Install the following extensions from the VS Code Marketplace:

1. **C# for Visual Studio Code** (powered by OmniSharp)
   - Provides IntelliSense, debugging, and code navigation
   
2. **C# Dev Kit** (Microsoft)
   - Enhanced C# development experience
   
3. **Docker** (Microsoft)
   - Container management and debugging
   
4. **REST Client** or **Thunder Client**
   - For testing API endpoints without leaving VS Code
   
5. **EditorConfig for VS Code**
   - Ensures consistent code formatting

#### VS Code Configuration

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
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/swagger"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": ".NET Core Attach",
      "type": "coreclr",
      "request": "attach"
    }
  ]
}
```

Create `.vscode/tasks.json` for build tasks:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/src/BlackSlope.NET.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "watch",
      "command": "dotnet",
      "type": "process",
      "args": [
        "watch",
        "run",
        "--project",
        "${workspaceFolder}/src/BlackSlope.Api/BlackSlope.Api.csproj"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

## Local Development

### Running the API Locally

#### Prerequisites

Before running the application, ensure you have:
1. Installed .NET Core (latest version for Windows/Linux/Mac) - https://dotnet.microsoft.com/download
2. Built the solution using `dotnet build src/BlackSlope.NET.sln`
3. Set up the SQL Server database - See the "Local SQL Server Setup" section below

#### Using the Command Line

Navigate to the repository root and execute:

```bash
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

Alternatively, you can use the provided convenience script:

```bash
./run.sh
```

The API will start and Swagger UI will be available at the `/swagger` endpoint (e.g., `http://localhost:51385/swagger`).

#### Using Visual Studio

1. Set `BlackSlope.Api` as the startup project
2. Select the desired launch profile from the dropdown:
   - **IIS Express** - Runs on `http://localhost:55644` (SSL: 44301)
   - **BlackSlope.Api** - Runs on `https://localhost:5001`
   - **Docker** - Runs in a Windows container
3. Press **F5** to start debugging or **Ctrl+F5** to run without debugging

#### Launch Profiles

The application includes three launch profiles defined in `src/BlackSlope.Api/Properties/launchSettings.json`:

**IIS Express Profile:**
```json
{
  "commandName": "IISExpress",
  "launchBrowser": true,
  "launchUrl": "swagger",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

**Kestrel Profile:**
```json
{
  "commandName": "Project",
  "launchBrowser": true,
  "launchUrl": "swagger",
  "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  },
  "applicationUrl": "https://localhost:5001;http://localhost:5000"
}
```

**Docker Profile:**
```json
{
  "commandName": "Docker",
  "launchBrowser": true,
  "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}/swagger"
}
```

#### Environment Configuration

The application uses environment-specific configuration files:

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides

Development logging configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  }
}
```

### Local SQL Server Setup

#### Installing SQL Server

1. Download **SQL Server 2019 Developer Edition** (free):
   - https://www.microsoft.com/en-us/sql-server/sql-server-downloads

2. Run the installer and select **Basic** installation type

3. Note the server name (typically `localhost` or `.\SQLEXPRESS`)

#### Database Creation

Create the `movies` database using one of the following methods:

**Using SQL Server Management Studio (SSMS):**

1. Connect to your SQL Server instance
2. Right-click **Databases** > **New Database**
3. Enter `movies` as the database name
4. Click **OK**

**Using mssql-cli:**

```bash
# Install mssql-cli
pip install mssql-cli

# Connect to SQL Server
mssql-cli -S localhost -U sa

# Create database
CREATE DATABASE movies;
GO
```

**Using sqlcmd:**

```bash
sqlcmd -S localhost -Q "CREATE DATABASE movies"
```

#### Connection String Configuration

Update the connection string in `appsettings.json` or User Secrets:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=movies;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

For SQL Server authentication:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=movies;User Id=your_username;Password=your_password;TrustServerCertificate=True;"
  }
}
```

**Note:** The `TrustServerCertificate=True` parameter is required for local development with SQL Server 2019+ to bypass certificate validation.

#### Running Entity Framework Migrations

1. Install the EF Core CLI tools globally:

```bash
dotnet tool install --global dotnet-ef
```

2. Verify installation:

```bash
dotnet ef --version
```

Expected output: `Entity Framework Core .NET Command-line Tools 6.0.x`

3. Navigate to the repository root and apply migrations:

```bash
dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
```

Successful output:

```
Build started...
Build succeeded.
Applying migration '20190814225754_initialized'.
Applying migration '20190814225910_seeded'.
Done.
```

4. Verify the database schema using SSMS or by querying:

```sql
SELECT * FROM sys.tables;
```

#### Health Check Verification

The application includes SQL Server health checks via `AspNetCore.HealthChecks.SqlServer` (v5.0.3). After starting the API, verify database connectivity by accessing the `/health` endpoint.

Expected response:

```json
{
  "status": "Healthy",
  "results": {
    "sqlserver": {
      "status": "Healthy",
      "description": "SQL Server is available"
    },
    "efcore": {
      "status": "Healthy",
      "description": "Entity Framework Core is available"
    }
  }
}
```

### Using Docker for Development

The application includes Docker support with Windows containers as the default target OS.

#### Prerequisites

1. Install **Docker Desktop for Windows**:
   - https://www.docker.com/products/docker-desktop

2. Ensure Docker is configured for Windows containers (right-click Docker tray icon > Switch to Windows containers)

3. Verify Docker installation:

```bash
docker --version
docker info
```

#### Building the Docker Image

Navigate to the `/src` directory and build the image:

```bash
cd src
docker build -t blackslope.api -f Dockerfile .
```

The Dockerfile uses multi-stage builds for optimization:

1. **Build stage** - Compiles the application
2. **Publish stage** - Creates a release build
3. **Runtime stage** - Creates the final lightweight image

#### Verifying the Image

List Docker images to confirm creation:

```bash
docker images
```

Expected output:

```
REPOSITORY        TAG       IMAGE ID       CREATED          SIZE
blackslope.api    latest    abc123def456   2 minutes ago    500MB
```

#### Creating and Running the Container

Create a container from the image:

```bash
docker create --name blackslope-container blackslope.api
```

Start the container:

```bash
docker start blackslope-container
```

View running containers:

```bash
docker ps
```

#### Container Networking

To access the API from your host machine, create the container with port mapping:

```bash
docker create --name blackslope-container -p 8080:80 -p 8443:443 blackslope.api
docker start blackslope-container
```

Access the API at:
- HTTP: `http://localhost:8080/swagger`
- HTTPS: `https://localhost:8443/swagger`

#### Database Connectivity from Docker

When running in Docker, update the connection string to use the host machine's IP address:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=host.docker.internal;Database=movies;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  }
}
```

**Note:** `host.docker.internal` is a special DNS name that resolves to the host machine's IP address from within a Docker container.

#### Docker Compose (Optional)

For a complete development environment with SQL Server, create a `docker-compose.yml`:

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

  api:
    build:
      context: ./src
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__MoviesConnectionString=Server=sqlserver;Database=movies;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;
    depends_on:
      - sqlserver

volumes:
  sqldata:
```

Start the entire stack:

```bash
docker-compose up -d
```

#### Visual Studio Docker Integration

When using the Docker launch profile in Visual Studio:

1. Visual Studio automatically builds the Docker image
2. Starts the container with debugging support
3. Attaches the debugger to the containerized application
4. Opens Swagger UI in your default browser

This is configured via `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (v1.14.0).

## Developer Tools

### Swagger UI for Testing

The application uses Swashbuckle.AspNetCore.SwaggerUI (v6.3.0) to provide interactive API documentation.

#### Accessing Swagger UI

Start the application and navigate to:

- **Development**: `https://localhost:5001/swagger`
- **IIS Express**: `http://localhost:55644/swagger`
- **Docker**: `http://localhost:8080/swagger`

#### Using Swagger UI

**Testing Endpoints:**

1. Expand an endpoint (e.g., `GET /api/movies`)
2. Click **Try it out**
3. Enter required parameters
4. Click **Execute**
5. View the response body, headers, and status code

**Authentication:**

If the API uses JWT authentication:

1. Click the **Authorize** button (lock icon)
2. Enter your bearer token: `Bearer <your-token>`
3. Click **Authorize**
4. All subsequent requests will include the authorization header

**Downloading OpenAPI Specification:**

Access the raw OpenAPI JSON at the `/swagger/v1/swagger.json` endpoint

This can be imported into other tools like Postman or used for code generation.

### Postman Collections

While Swagger UI is excellent for quick testing, Postman provides more advanced features for API testing.

#### Creating a Postman Collection

1. Open Postman
2. Click **Import** > **Link**
3. Enter your Swagger JSON URL (e.g., `http://localhost:51385/swagger/v1/swagger.json`)
4. Postman will generate a collection from the OpenAPI specification

#### Environment Variables

Create a Postman environment for local development:

```json
{
  "name": "BlackSlope Local",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:51385",
      "enabled": true
    },
    {
      "key": "token",
      "value": "",
      "enabled": true
    }
  ]
}
```

#### Pre-request Scripts

For endpoints requiring authentication, add a pre-request script to obtain a token:

```javascript
// Pre-request script for Azure AD authentication
const tokenUrl = 'https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token';

const requestBody = {
    client_id: pm.environment.get('clientId'),
    client_secret: pm.environment.get('clientSecret'),
    scope: 'api://{client-id}/.default',
    grant_type: 'client_credentials'
};

pm.sendRequest({
    url: tokenUrl,
    method: 'POST',
    header: 'Content-Type: application/x-www-form-urlencoded',
    body: {
        mode: 'urlencoded',
        urlencoded: Object.entries(requestBody).map(([key, value]) => ({key, value}))
    }
}, (err, response) => {
    if (!err) {
        const token = response.json().access_token;
        pm.environment.set('token', token);
    }
});
```

### Database Management Tools

#### SQL Server Management Studio (SSMS)

**Recommended for:**
- Visual database design
- Query execution and optimization
- Database administration

**Key Features:**
- Object Explorer for browsing database objects
- Query Editor with IntelliSense
- Execution plan analysis
- Database diagram designer

**Connecting to Local Database:**

1. Open SSMS
2. Server name: `localhost` or `.\SQLEXPRESS`
3. Authentication: Windows Authentication (or SQL Server Authentication)
4. Click **Connect**

#### Azure Data Studio

**Recommended for:**
- Cross-platform database management
- Notebook-style query execution
- Git integration

**Installation:**
- Download from: https://docs.microsoft.com/en-us/sql/azure-data-studio/download

**Extensions:**
- SQL Server Schema Compare
- SQL Server Dacpac
- SQL Server Import

#### mssql-cli

**Recommended for:**
- Command-line database operations
- Scripting and automation
- CI/CD pipelines

**Installation:**

```bash
pip install mssql-cli
```

**Usage:**

```bash
# Connect to database
mssql-cli -S localhost -d movies

# Execute query
SELECT * FROM Movies;

# Execute script file
mssql-cli -S localhost -d movies -i script.sql
```

#### Entity Framework Core Tools

The project includes `Microsoft.EntityFrameworkCore.Design` (v6.0.1) for database management via the CLI.

**Common Commands:**

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/BlackSlope.Api

# Update database to latest migration
dotnet ef database update --project src/BlackSlope.Api

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project src/BlackSlope.Api

# Generate SQL script for migration
dotnet ef migrations script --project src/BlackSlope.Api --output migration.sql

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/BlackSlope.Api

# View migration history
dotnet ef migrations list --project src/BlackSlope.Api
```

## Debugging

### Breakpoint Debugging

#### Visual Studio

**Setting Breakpoints:**

1. Click in the left margin next to a line of code (or press **F9**)
2. A red dot appears indicating a breakpoint
3. Start debugging (**F5**)
4. Execution pauses when the breakpoint is hit

**Conditional Breakpoints:**

1. Right-click a breakpoint
2. Select **Conditions**
3. Enter a condition (e.g., `movieId == 5`)
4. Execution only pauses when the condition is true

**Tracepoints:**

1. Right-click a breakpoint
2. Select **Actions**
3. Enter a message to log (e.g., `Movie ID: {movieId}`)
4. Check "Continue execution"
5. Messages appear in the Output window without pausing execution

**Data Breakpoints:**

For tracking when a specific variable changes:

1. Break at any point in code
2. Right-click a variable in the Locals or Watch window
3. Select **Break When Value Changes**

#### VS Code

**Setting Breakpoints:**

1. Click in the left margin (gutter) next to a line number
2. A red dot appears
3. Start debugging (**F5**)

**Logpoints:**

1. Right-click in the gutter
2. Select **Add Logpoint**
3. Enter a message (e.g., `Movie ID: {movieId}`)
4. Messages appear in the Debug Console

#### Debugging Entity Framework Queries

Enable sensitive data logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

Configure DbContext to log SQL queries:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors();
}
```

SQL queries will appear in the Output window during debugging.

### Logging Configuration

The application uses `Microsoft.Extensions.Logging` with multiple providers.

#### Log Levels

Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "BlackSlope": "Debug"
    }
  }
}
```

**Log Level Hierarchy:**
- **Trace** (0): Most detailed, typically only for development
- **Debug** (1): Debugging information
- **Information** (2): General informational messages
- **Warning** (3): Unexpected events that don't stop execution
- **Error** (4): Errors and exceptions
- **Critical** (5): Critical failures requiring immediate attention
- **None** (6): Disables logging

#### Logging Providers

**Debug Provider:**

The project includes `Microsoft.Extensions.Logging.Debug` (v6.0.0) for Visual Studio Output window logging.

**Console Provider:**

Enabled by default in ASP.NET Core 6.0, logs to the console/terminal.

**Custom Logging:**

Inject `ILogger<T>` into your classes:

```csharp
public class MovieService
{
    private readonly ILogger<MovieService> _logger;

    public MovieService(ILogger<MovieService> logger)
    {
        _logger = logger;
    }

    public async Task<Movie> GetMovieAsync(int id)
    {
        _logger.LogInformation("Retrieving movie with ID: {MovieId}", id);
        
        try
        {
            var movie = await _repository.GetByIdAsync(id);
            
            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found", id);
            }
            
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie with ID: {MovieId}", id);
            throw;
        }
    }
}
```

#### Structured Logging

Use structured logging for better queryability:

```csharp
// Good - Structured
_logger.LogInformation("User {UserId} created movie {MovieId}", userId, movieId);

// Bad - String interpolation
_logger.LogInformation($"User {userId} created movie {movieId}");
```

#### Log Filtering

Filter logs by category in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BlackSlope.Api.Services": "Debug",
      "BlackSlope.Api.Repositories": "Warning"
    }
  }
}
```

### Remote Debugging

#### Debugging in Docker Containers

Visual Studio 2022 supports remote debugging in Docker containers automatically when using the Docker launch profile.

**Manual Remote Debugging:**

1. Ensure the container is running with debugging enabled
2. In Visual Studio, select **Debug > Attach to Process**
3. Connection type: **Docker (Windows Container)**
4. Select the running container
5. Choose the `dotnet` process
6. Click **Attach**

#### Debugging on Remote Servers

For debugging on remote IIS or Azure App Service:

1. Install **Remote Tools for Visual Studio** on the remote server
2. Start the remote debugger (`msvsmon.exe`)
3. In Visual Studio, select **Debug > Attach to Process**
4. Connection type: **Remote (no authentication)** or **Remote (Windows Authentication)**
5. Enter the remote server address
6. Select the `w3wp.exe` or `dotnet.exe` process
7. Click **Attach**

**Azure App Service:**

1. In Visual Studio, open **Cloud Explorer**
2. Navigate to your App Service
3. Right-click and select **Attach Debugger**

**Note:** Remote debugging should only be used in non-production environments due to performance impact.

## Tips and Tricks

### Hot Reload

.NET 6.0 includes Hot Reload functionality for rapid development without restarting the application.

#### Using Hot Reload in Visual Studio

1. Start debugging (**F5**) or run without debugging (**Ctrl+F5**)
2. Make code changes
3. Save the file (**Ctrl+S**)
4. Changes are automatically applied without restarting

**Supported Changes:**
- Method body modifications
- Adding new methods
- Adding new properties
- Modifying lambda expressions

**Unsupported Changes:**
- Adding new types
- Changing method signatures
- Modifying generic type parameters
- Changes to `Program.cs` or `Startup.cs`

#### Using Hot Reload in VS Code

Hot Reload is not natively supported in VS Code. Use watch mode instead (see below).

#### Limitations

Hot Reload does not work with:
- Razor views (use `dotnet watch` instead)
- Static files (CSS, JavaScript)
- Configuration file changes

For these scenarios, restart the application or use watch mode.

### Watch Mode

Watch mode automatically rebuilds and restarts the application when files change.

#### Starting Watch Mode

```bash
dotnet watch run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

Or use the VS Code task defined earlier:

1. Press **Ctrl+Shift+P**
2. Select **Tasks: Run Task**
3. Select **watch**

#### Watch Mode Features

- Automatically detects file changes
- Rebuilds the project
- Restarts the application
- Refreshes the browser (with browser-link)

#### Excluding Files from Watch

Create or modify `BlackSlope.Api.csproj`:

```xml
<ItemGroup>
  <Watch Remove="**\*.tmp" />
  <Watch Remove="logs\**\*" />
</ItemGroup>
```

#### Watch Mode with Hot Reload

Combine watch mode with Hot Reload for the best development experience:

```bash
dotnet watch run --project src/BlackSlope.Api/BlackSlope.Api.csproj --no-hot-reload
```

The `--no-hot-reload` flag disables Hot Reload if you prefer full restarts.

### Quick Navigation

#### Visual Studio Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+,** | Navigate to file, type, or member |
| **Ctrl+T** | Go to all (files, types, members, symbols) |
| **F12** | Go to definition |
| **Ctrl+F12** | Go to implementation |
| **Shift+F12** | Find all references |
| **Ctrl+K, Ctrl+T** | View call hierarchy |
| **Alt+Home** | Navigate to base class or interface |
| **Ctrl+-** | Navigate backward |
| **Ctrl+Shift+-** | Navigate forward |
| **Ctrl+]** | Go to matching brace |
| **Ctrl+M, Ctrl+O** | Collapse all regions |
| **Ctrl+M, Ctrl+L** | Toggle all outlining |

#### VS Code Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+P** | Quick open file |
| **Ctrl+Shift+O** | Go to symbol in file |
| **Ctrl+T** | Go to symbol in workspace |
| **F12** | Go to definition |
| **Alt+F12** | Peek definition |
| **Shift+F12** | Find all references |
| **Ctrl+K F12** | Open definition to the side |
| **Alt+Left** | Navigate backward |
| **Alt+Right** | Navigate forward |

#### Code Snippets

Visual Studio includes built-in code snippets. Type the snippet shortcut and press **Tab** twice:

| Snippet | Expands To |
|---------|------------|
| `ctor` | Constructor |
| `prop` | Auto-property |
| `propfull` | Property with backing field |
| `if` | If statement |
| `for` | For loop |
| `foreach` | Foreach loop |
| `try` | Try-catch block |
| `class` | Class definition |
| `interface` | Interface definition |

#### Custom Snippets

Create custom snippets for common patterns:

1. **Tools > Code Snippets Manager**
2. Select **Visual C#**
3. Click **Add** to add a snippet directory
4. Create XML snippet files

Example snippet for a repository method:

```xml
<?xml version="1.0" encoding="utf-8"?>
<CodeSnippets xmlns="http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet">
  <CodeSnippet Format="1.0.0">
    <Header>
      <Title>Repository Method</Title>
      <Shortcut>repometh</Shortcut>
    </Header>
    <Snippet>
      <Code Language="csharp">
        <![CDATA[public async Task<$type$> $method$Async($params$)
        {
            _logger.LogInformation("$method$ called with parameters: {Params}", $params$);
            
            try
            {
                $end$
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in $method$");
                throw;
            }
        }]]>
      </Code>
    </Snippet>
  </CodeSnippet>
</CodeSnippets>
```

### Performance Profiling

#### Visual Studio Profiler

1. **Debug > Performance Profiler** (or **Alt+F2**)
2. Select profiling tools:
   - **CPU Usage** - Identify performance bottlenecks
   - **Memory Usage** - Detect memory leaks
   - **.NET Object Allocation** - Track object allocations
   - **Database** - Profile Entity Framework queries
3. Click **Start**
4. Use the application
5. Click **Stop Collection**
6. Analyze the results

#### Benchmarking with BenchmarkDotNet

For micro-benchmarking specific methods, consider adding BenchmarkDotNet:

```bash
dotnet add package BenchmarkDotNet
```

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class MovieServiceBenchmarks
{
    [Benchmark]
    public async Task GetMovieById()
    {
        // Benchmark code
    }
}

// Run benchmarks
BenchmarkRunner.Run<MovieServiceBenchmarks>();
```

### Testing Resilience Policies

The application uses Polly (v7.2.2) for resilience and transient-fault-handling. Test these policies during development:

#### Simulating Failures

Create a test endpoint that randomly fails:

```csharp
[HttpGet("test/flaky")]
public IActionResult FlakyEndpoint()
{
    if (Random.Shared.Next(0, 2) == 0)
    {
        return StatusCode(500, "Simulated failure");
    }
    
    return Ok("Success");
}
```

#### Observing Retry Behavior

Enable detailed logging for Polly:

```json
{
  "Logging": {
    "LogLevel": {
      "Polly": "Debug"
    }
  }
}
```

Call the flaky endpoint and observe retry attempts in the logs.

#### Testing Circuit Breaker

Configure a circuit breaker with a low threshold for testing:

```csharp
services.AddHttpClient("TestClient")
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 2,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) =>
            {
                _logger.LogWarning("Circuit breaker opened for {Duration}", duration);
            },
            onReset: () =>
            {
                _logger.LogInformation("Circuit breaker reset");
            }));
```

Make multiple failing requests to trigger the circuit breaker, then observe that subsequent requests fail immediately without attempting the call.

### Build Script Integration

For advanced build and deployment scenarios, refer to [/development/build_scripts.md](/development/build_scripts.md) for information on:

- Automated build pipelines
- Pre-build and post-build events
- Custom MSBuild targets
- CI/CD integration

### Common Gotchas

#### Entity Framework Tracking Issues

**Problem:** Changes to entities are not saved to the database.

**Solution:** Ensure entities are tracked by the DbContext:

```csharp
// Attach entity if not tracked
if (!_context.Entry(movie).IsKeySet)
{
    _context.Movies.Attach(movie);
}

_context.Entry(movie).State = EntityState.Modified;
await _context.SaveChangesAsync();
```

#### AutoMapper Configuration

**Problem:** `AutoMapper.AutoMapperMappingException: Missing type map configuration`

**Solution:** Ensure all mappings are registered in the AutoMapper profile:

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Movie, MovieDto>();
        CreateMap<MovieDto, Movie>();
    }
}
```

Register the profile in `Startup.cs`:

```csharp
services.AddAutoMapper(typeof(MappingProfile));
```

#### FluentValidation Not Executing

**Problem:** Validation rules are not being enforced.

**Solution:** Ensure validators are registered with dependency injection:

```csharp
services.AddFluentValidation(fv => 
{
    fv.RegisterValidatorsFromAssemblyContaining<MovieValidator>();
    fv.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
});
```

#### Azure Identity Authentication Failures

**Problem:** `Azure.Identity.AuthenticationFailedException` in local development.

**Solution:** Authenticate with Azure CLI:

```bash
az login
az account set --subscription "your-subscription-id"
```

Or use Visual Studio authentication:
1. **Tools > Options > Azure Service Authentication**
2. Select your account

#### Docker Container Cannot Connect to SQL Server

**Problem:** `SqlException: A network-related or instance-specific error occurred`

**Solution:** Use `host.docker.internal` instead of `localhost` in the connection string:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=host.docker.internal;Database=movies;..."
  }
}
```

#### StyleCop Warnings Blocking Build

**Problem:** Build fails due to StyleCop warnings.

**Solution:** Suppress specific rules in `.editorconfig` or `GlobalSuppressions.cs`:

```csharp
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Internal API")]
```

Or disable warnings as errors in the project file:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

---

This development environment guide should provide you with all the necessary information to set up, configure, and optimize your local development workflow. For additional information on prerequisites and installation, refer to the linked documentation pages. If you encounter issues not covered here, consult the project's README.md or reach out to the development team.