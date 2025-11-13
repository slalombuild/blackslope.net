# Build Scripts

The build scripts directory contains shell scripts that automate common development, database, and deployment tasks for the BlackSlope.NET application. These scripts provide a consistent, repeatable way to build, test, migrate databases, and deploy the application across different environments.

## Build Script Overview

The `scripts/` directory is organized at the root level of the repository and contains five primary automation scripts:

```
scripts/
├── build.sh                    # Compiles the entire solution
├── db-update.sh               # Applies Entity Framework Core migrations
├── publish.sh                 # Creates release builds for deployment
├── docker-image-build.sh      # Builds Docker images via docker-compose
└── docker-container-run.sh    # Orchestrates container startup and database initialization
```

### Script Purposes and Usage

Each script serves a specific purpose in the development and deployment workflow:

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `build.sh` | Compiles the solution in Debug mode | During active development, before running tests |
| `db-update.sh` | Applies pending EF Core migrations to the database | After creating new migrations, during environment setup |
| `publish.sh` | Creates optimized Release builds | Before deployment to staging/production |
| `docker-image-build.sh` | Builds Docker images for all services | When Dockerfile or dependencies change |
| `docker-container-run.sh` | Starts containers and initializes database | For local containerized development environment |

**Prerequisites:**
- Bash shell environment (Linux, macOS, WSL on Windows, or Git Bash)
- .NET 6.0 SDK installed and available in PATH
- Docker and Docker Compose (for container scripts)
- Appropriate database connection strings configured in `appsettings.json` or User Secrets

**Execution Permissions:**
All scripts must have execute permissions. If needed, run:
```bash
chmod +x scripts/*.sh
```

## build.sh

The `build.sh` script compiles the entire BlackSlope.NET solution, including both the web API and the RenameUtility console application.

### Script Contents

```bash
cd ../src
dotnet build BlackSlope.NET.sln
```

### Building the Solution

The script performs the following operations:

1. **Changes to Source Directory**: Navigates from `scripts/` to `src/` where the solution file resides
2. **Invokes .NET Build**: Executes `dotnet build` against the solution file, which:
   - Restores NuGet packages if not already cached
   - Compiles all projects in the solution (BlackSlope.Api and RenameUtility)
   - Runs code analyzers (StyleCop.Analyzers, Microsoft.CodeAnalysis.NetAnalyzers)
   - Validates project references and dependencies
   - Outputs build diagnostics and any warnings/errors

### Build Options

The script uses default build behavior, which means:

- **Configuration**: Debug (default when not specified)
- **Target Framework**: net6.0 (as defined in project files)
- **Output Path**: Default bin/Debug/net6.0/ for each project
- **Restore**: Implicit (NuGet packages restored automatically)

**Customizing the Build:**

To modify build behavior, you can extend the script with additional flags:

```bash
# Build in Release mode
dotnet build BlackSlope.NET.sln --configuration Release

# Build with verbose output for troubleshooting
dotnet build BlackSlope.NET.sln --verbosity detailed

# Build without restoring packages (if already restored)
dotnet build BlackSlope.NET.sln --no-restore

# Build for specific runtime
dotnet build BlackSlope.NET.sln --runtime win-x64
```

### Output Directories

After a successful build, compiled assemblies are located at:

```
src/
├── BlackSlope.Api/
│   └── bin/
│       └── Debug/
│           └── net6.0/
│               ├── BlackSlope.Api.dll
│               ├── BlackSlope.Api.pdb
│               ├── appsettings.json
│               └── [dependencies...]
└── RenameUtility/
    └── bin/
        └── Debug/
            └── net6.0/
                ├── RenameUtility.exe
                ├── RenameUtility.dll
                └── [dependencies...]
```

**Build Artifacts:**
- `.dll` files: Compiled assemblies
- `.pdb` files: Debug symbols for debugging
- `.deps.json`: Dependency manifest
- `.runtimeconfig.json`: Runtime configuration
- Configuration files (appsettings.json, etc.)

**Common Build Issues:**

| Issue | Cause | Solution |
|-------|-------|----------|
| Package restore failures | Network issues, missing NuGet sources | Run `dotnet restore` explicitly, check NuGet.config |
| StyleCop warnings | Code style violations | Review StyleCop rules, suppress if intentional |
| Missing SDK | .NET 6.0 SDK not installed | Install from https://dotnet.microsoft.com/download |
| Project reference errors | Circular dependencies, missing projects | Verify .csproj references are correct |

## db-update.sh

The `db-update.sh` script applies Entity Framework Core migrations to the database, ensuring the database schema matches the current model definitions.

### Script Contents

```bash
cd ../src/BlackSlope.Api
dotnet ef database update -v 
```

### Running Database Migrations

The script executes the following workflow:

1. **Navigates to API Project**: Changes to the `BlackSlope.Api` directory where the DbContext is defined
2. **Applies Migrations**: Runs `dotnet ef database update` with verbose output (`-v`)
3. **Updates Schema**: EF Core:
   - Connects to the database using the connection string from configuration
   - Checks the `__EFMigrationsHistory` table for applied migrations
   - Applies any pending migrations in chronological order
   - Updates the migrations history table

### Migration Workflow

**Prerequisites:**
- `Microsoft.EntityFrameworkCore.Design` package installed (version 6.0.1)
- Valid SQL Server connection string configured
- Database server accessible from the execution environment
- Appropriate database permissions (CREATE TABLE, ALTER TABLE, etc.)

**Connection String Resolution:**

EF Core resolves the connection string in this order:
1. Command-line argument: `--connection "connection_string"`
2. Environment variables
3. User Secrets (for Development environment)
4. `appsettings.json` or `appsettings.{Environment}.json`

**Verbose Output:**

The `-v` flag provides detailed migration information:
```
Build started...
Build succeeded.
Applying migration '20231015120000_InitialCreate'.
Applying migration '20231020143000_AddUserTable'.
Done.
```

**Advanced Migration Commands:**

While not included in the script, developers may need these commands:

```bash
# Apply migrations to a specific migration
dotnet ef database update SpecificMigrationName

# Revert all migrations (dangerous!)
dotnet ef database update 0

# Generate SQL script without applying
dotnet ef migrations script

# Apply migrations to a specific environment
dotnet ef database update --environment Production

# Use a specific connection string
dotnet ef database update --connection "Server=...;Database=...;"
```

**Migration Best Practices:**

1. **Always review migrations before applying**: Use `dotnet ef migrations script` to generate SQL and review changes
2. **Backup production databases**: Before applying migrations to production
3. **Test migrations**: Apply to a staging environment first
4. **Handle data migrations carefully**: Use custom migration code for data transformations
5. **Version control migrations**: Commit migration files to source control

**Common Migration Issues:**

| Issue | Cause | Solution |
|-------|-------|----------|
| "No migrations found" | No migrations created yet | Run `dotnet ef migrations add InitialCreate` |
| Connection timeout | Database server unreachable | Verify connection string, network connectivity |
| Permission denied | Insufficient database permissions | Grant DDL permissions to the database user |
| Migration already applied | Migration history out of sync | Check `__EFMigrationsHistory` table, resolve conflicts |
| Build errors | Project doesn't compile | Run `build.sh` first to ensure solution builds |

**Integration with CI/CD:**

For automated deployments, consider:
- Using `dotnet ef migrations script` to generate idempotent SQL scripts
- Applying scripts via SQL deployment tools (SSDT, Flyway, etc.)
- Implementing migration rollback strategies
- Monitoring migration execution time and failures

See [/database/migrations.md](/database/migrations.md) for comprehensive migration management documentation.

## publish.sh

The `publish.sh` script creates optimized, production-ready builds of the BlackSlope.Api application for deployment.

### Script Contents

```bash
rm -r ../publish
dotnet publish ../src/blackslope.api/blackslope.api.csproj --configuration RELEASE --output ../publish
```

### Publishing for Deployment

The script performs a two-step process:

1. **Cleans Previous Publish Output**: Removes the existing `publish/` directory to ensure a clean build
2. **Publishes Release Build**: Creates an optimized, self-contained deployment package

### Release Builds

The `dotnet publish` command with `--configuration RELEASE` applies several optimizations:

**Compiler Optimizations:**
- Code optimization enabled (IL-level optimizations)
- Debug symbols excluded or minimized
- Unused code elimination (tree shaking)
- Assembly trimming (if configured)

**Configuration Transformations:**
- Uses `appsettings.Production.json` settings (if present)
- Excludes development-only dependencies
- Applies conditional compilation symbols

**Output Characteristics:**
- Smaller assembly sizes
- Faster startup times
- Reduced memory footprint
- Production-ready error handling

### Output Configuration

The `--output ../publish` parameter specifies the deployment package location:

```
repository-root/
├── publish/                          # Created by publish.sh
│   ├── BlackSlope.Api.dll           # Main application assembly
│   ├── BlackSlope.Api.exe           # Windows executable (if applicable)
│   ├── appsettings.json             # Base configuration
│   ├── appsettings.Production.json  # Production overrides
│   ├── web.config                   # IIS configuration (if applicable)
│   ├── wwwroot/                     # Static files (if any)
│   └── [runtime dependencies]       # All required NuGet packages
├── scripts/
└── src/
```

**Deployment Package Contents:**

| File/Directory | Purpose |
|----------------|---------|
| `*.dll` | Compiled assemblies (application + dependencies) |
| `*.exe` | Native executable (Windows) |
| `appsettings*.json` | Configuration files |
| `web.config` | IIS hosting configuration |
| `wwwroot/` | Static web assets (Swagger UI, etc.) |
| `runtimes/` | Platform-specific native libraries |

**Publish Profiles:**

For more complex deployment scenarios, create publish profiles:

```xml
<!-- Properties/PublishProfiles/Production.pubxml -->
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishUrl>\\deployment-server\share\</PublishUrl>
    <DeleteExistingFiles>True</DeleteExistingFiles>
    <TargetFramework>net6.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
</Project>
```

Then publish using:
```bash
dotnet publish --profile Production
```

**Advanced Publish Options:**

```bash
# Self-contained deployment (includes .NET runtime)
dotnet publish --configuration Release --self-contained true --runtime win-x64

# Framework-dependent deployment (requires .NET runtime on target)
dotnet publish --configuration Release --self-contained false

# Single-file deployment
dotnet publish --configuration Release --runtime win-x64 \
  --self-contained true /p:PublishSingleFile=true

# Trimmed deployment (smaller size, may break reflection)
dotnet publish --configuration Release --self-contained true \
  /p:PublishTrimmed=true

# ReadyToRun compilation (faster startup)
dotnet publish --configuration Release /p:PublishReadyToRun=true
```

**Deployment Considerations:**

1. **Connection Strings**: Ensure production connection strings are configured via:
   - Environment variables
   - Azure App Configuration
   - Key Vault references
   - Configuration transformations

2. **Secrets Management**: Never include secrets in published output:
   - Use Azure Key Vault for production secrets
   - Configure managed identities for Azure resources
   - Implement proper secret rotation policies

3. **Health Checks**: The published application includes health check endpoints:
   - `/health` - Overall application health
   - `/health/ready` - Readiness probe (database connectivity)
   - `/health/live` - Liveness probe

4. **Logging**: Configure production logging:
   - Application Insights for Azure deployments
   - Structured logging with Serilog or NLog
   - Log aggregation services (ELK, Splunk, etc.)

**Post-Publish Validation:**

Before deploying, validate the publish output:

```bash
# Verify assembly versions
dotnet BlackSlope.Api.dll --version

# Test configuration loading
dotnet BlackSlope.Api.dll --environment Production --dry-run

# Check for missing dependencies
dotnet BlackSlope.Api.dll --verify-dependencies
```

**Deployment Targets:**

The published output can be deployed to:
- **IIS**: Copy to IIS application directory, configure app pool
- **Azure App Service**: Deploy via Azure DevOps, GitHub Actions, or FTP
- **Docker**: Use as base for Docker image (see Docker Scripts section)
- **Kubernetes**: Package in container, deploy via Helm charts
- **Windows Service**: Install using `sc.exe` or NSSM

## Docker Scripts

The Docker scripts automate containerization workflows, enabling consistent local development environments and production deployments.

### docker-image-build.sh

Builds Docker images for all services defined in the docker-compose configuration.

**Script Contents:**

```bash
cd ../src
docker-compose build
```

**Build Process:**

1. **Navigates to Source Directory**: Changes to `src/` where `docker-compose.yml` resides
2. **Invokes Docker Compose Build**: Builds all services defined in the compose file

**Docker Compose Configuration:**

The script expects a `docker-compose.yml` file in the `src/` directory:

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: BlackSlope.Api/Dockerfile
    image: blackslope-api:latest
    depends_on:
      - db
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=BlackSlope;User Id=sa;Password=YourStrong@Passw0rd;
    ports:
      - "5000:80"
    networks:
      - blackslope-network

  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - blackslope-network

volumes:
  sqlserver-data:

networks:
  blackslope-network:
    driver: bridge
```

**Dockerfile Structure:**

The API Dockerfile (referenced in docker-compose.yml) follows multi-stage build pattern:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
COPY ["RenameUtility/RenameUtility.csproj", "RenameUtility/"]
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"
COPY . .
WORKDIR "/src/BlackSlope.Api"
RUN dotnet build "BlackSlope.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "BlackSlope.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**Build Options:**

```bash
# Build with no cache (force rebuild)
docker-compose build --no-cache

# Build specific service
docker-compose build api

# Build with build arguments
docker-compose build --build-arg BUILD_VERSION=1.2.3

# Parallel build (faster)
docker-compose build --parallel
```

**Image Optimization:**

1. **Layer Caching**: Order Dockerfile commands from least to most frequently changed
2. **.dockerignore**: Exclude unnecessary files from build context:
   ```
   **/bin/
   **/obj/
   **/.vs/
   **/.git/
   **/node_modules/
   ```
3. **Multi-stage Builds**: Separate build and runtime stages to minimize image size
4. **Base Image Selection**: Use `aspnet` runtime image (smaller) vs `sdk` image (larger)

**Windows Container Considerations:**

The project uses `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` with Windows as the default target OS. For Windows containers:

```dockerfile
# Windows-specific base images
FROM mcr.microsoft.com/dotnet/aspnet:6.0-nanoserver-ltsc2022 AS final
```

**Switching to Linux containers** (recommended for production):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS final
```

### docker-container-run.sh

Orchestrates container startup and database initialization, ensuring the database is ready before applying migrations.

**Script Contents:**

```bash
cd ../src
echo "Docker Compose"
docker-compose up -d
cd ../scripts
echo -e "\e[32mdb container might not be ready yet, so wait a few seconds!\e[0m"
sleep 5
echo "Update database"
./db-update.sh
```

**Execution Flow:**

1. **Starts Containers**: Runs `docker-compose up -d` (detached mode)
2. **Waits for Database**: Sleeps 5 seconds to allow SQL Server container initialization
3. **Applies Migrations**: Executes `db-update.sh` to update database schema

**Container Startup:**

The `-d` flag runs containers in detached mode (background):
```bash
docker-compose up -d
```

**Output:**
```
Creating network "src_blackslope-network" ... done
Creating volume "src_sqlserver-data" ... done
Creating src_db_1 ... done
Creating src_api_1 ... done
```

**Database Initialization Wait:**

The 5-second sleep is a simple approach to handle SQL Server startup time. SQL Server containers typically need 10-30 seconds to fully initialize.

**Improved Wait Strategy:**

For production-grade reliability, implement a health check loop:

```bash
# Enhanced docker-container-run.sh
cd ../src
echo "Starting containers..."
docker-compose up -d

echo "Waiting for SQL Server to be ready..."
until docker-compose exec -T db /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1" &> /dev/null
do
  echo "SQL Server is unavailable - sleeping"
  sleep 2
done

echo "SQL Server is ready!"
cd ../scripts
echo "Applying migrations..."
./db-update.sh
```

**Docker Compose Health Checks:**

Add health checks to `docker-compose.yml`:

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "YourStrong@Passw0rd", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  api:
    depends_on:
      db:
        condition: service_healthy
```

**Container Management Commands:**

```bash
# View running containers
docker-compose ps

# View container logs
docker-compose logs -f api
docker-compose logs -f db

# Stop containers
docker-compose stop

# Stop and remove containers
docker-compose down

# Stop, remove containers, and delete volumes
docker-compose down -v

# Restart specific service
docker-compose restart api
```

**Troubleshooting Container Issues:**

| Issue | Diagnosis | Solution |
|-------|-----------|----------|
| API container exits immediately | Check logs: `docker-compose logs api` | Verify connection string, ensure db is ready |
| Database connection failures | SQL Server not fully initialized | Increase sleep time or implement health checks |
| Port conflicts | Port already in use | Change port mapping in docker-compose.yml |
| Volume permission errors | SELinux or file permissions | Add `:z` suffix to volume mounts (Linux) |
| Out of disk space | Docker images/volumes consuming space | Run `docker system prune -a --volumes` |

**Development Workflow:**

```bash
# Initial setup
./docker-image-build.sh
./docker-container-run.sh

# Code changes - rebuild and restart
./docker-image-build.sh
docker-compose restart api

# Database schema changes
./db-update.sh

# Clean restart
docker-compose down -v
./docker-container-run.sh
```

**Integration with CI/CD:**

For automated deployments:

```yaml
# Azure DevOps pipeline example
- task: Docker@2
  inputs:
    command: 'build'
    Dockerfile: 'src/BlackSlope.Api/Dockerfile'
    tags: '$(Build.BuildId)'

- task: Docker@2
  inputs:
    command: 'push'
    containerRegistry: 'myregistry.azurecr.io'
    repository: 'blackslope-api'
    tags: '$(Build.BuildId)'
```

See [/deployment/docker.md](/deployment/docker.md) for comprehensive Docker deployment documentation.

## Best Practices

### Script Portability

**Cross-Platform Considerations:**

1. **Line Endings**: Use LF (Unix) line endings, not CRLF (Windows)
   ```bash
   # Convert line endings
   dos2unix scripts/*.sh
   ```

2. **Shebang**: Add shebang for explicit shell specification
   ```bash
   #!/bin/bash
   # or
   #!/usr/bin/env bash
   ```

3. **Path Separators**: Use forward slashes (/) for paths
   ```bash
   # Good
   cd ../src/BlackSlope.Api
   
   # Avoid (Windows-specific)
   cd ..\src\BlackSlope.Api
   ```

4. **Environment Variables**: Use consistent variable syntax
   ```bash
   # POSIX-compliant
   ${VARIABLE_NAME}
   ```

**Windows Compatibility:**

For Windows developers without WSL:

1. **Git Bash**: Included with Git for Windows
2. **PowerShell Alternatives**: Create equivalent `.ps1` scripts
   ```powershell
   # build.ps1
   Set-Location ../src
   dotnet build BlackSlope.NET.sln
   ```

### Error Handling

**Exit on Error:**

Add error handling to prevent cascading failures:

```bash
#!/bin/bash
set -e  # Exit immediately if a command exits with non-zero status
set -u  # Treat unset variables as errors
set -o pipefail  # Pipeline fails if any command fails

cd ../src || exit 1
dotnet build BlackSlope.NET.sln || exit 1
```

**Graceful Error Messages:**

```bash
#!/bin/bash

# Function for error handling
error_exit() {
    echo "ERROR: $1" >&2
    exit 1
}

# Check prerequisites
command -v dotnet >/dev/null 2>&1 || error_exit ".NET SDK not found. Please install .NET 6.0 SDK."

# Navigate with error checking
cd ../src || error_exit "Failed to navigate to src directory"

# Build with error checking
echo "Building solution..."
dotnet build BlackSlope.NET.sln || error_exit "Build failed"

echo "Build completed successfully!"
```

**Rollback on Failure:**

For destructive operations (like `publish.sh`):

```bash
#!/bin/bash
set -e

BACKUP_DIR="../publish.backup"
PUBLISH_DIR="../publish"

# Backup existing publish directory
if [ -d "$PUBLISH_DIR" ]; then
    echo "Backing up existing publish directory..."
    mv "$PUBLISH_DIR" "$BACKUP_DIR"
fi

# Attempt publish
if dotnet publish ../src/blackslope.api/blackslope.api.csproj \
    --configuration RELEASE --output "$PUBLISH_DIR"; then
    echo "Publish successful!"
    rm -rf "$BACKUP_DIR"
else
    echo "Publish failed! Restoring backup..."
    rm -rf "$PUBLISH_DIR"
    mv "$BACKUP_DIR" "$PUBLISH_DIR"
    exit 1
fi
```

### Logging Output

**Structured Logging:**

```bash
#!/bin/bash

# Logging functions
log_info() {
    echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - $1" >&2
}

log_success() {
    echo "[SUCCESS] $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# Usage
log_info "Starting build process..."
dotnet build BlackSlope.NET.sln
log_success "Build completed successfully!"
```

**Log File Output:**

```bash
#!/bin/bash

LOG_FILE="../logs/build-$(date '+%Y%m%d-%H%M%S').log"
mkdir -p ../logs

# Redirect output to both console and log file
exec > >(tee -a "$LOG_FILE")
exec 2>&1

echo "Build started at $(date)"
dotnet build BlackSlope.NET.sln
echo "Build completed at $(date)"
```

**Colored Output:**

```bash
#!/bin/bash

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting build...${NC}"
if dotnet build BlackSlope.NET.sln; then
    echo -e "${GREEN}Build successful!${NC}"
else
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi
```

**Progress Indicators:**

```bash
#!/bin/bash

echo "Building solution..."
dotnet build BlackSlope.NET.sln --verbosity quiet &
BUILD_PID=$!

# Show spinner while building
spin='-\|/'
i=0
while kill -0 $BUILD_PID 2>/dev/null; do
    i=$(( (i+1) %4 ))
    printf "\r${spin:$i:1} Building..."
    sleep 0.1
done

wait $BUILD_PID
BUILD_EXIT_CODE=$?

if [ $BUILD_EXIT_CODE -eq 0 ]; then
    echo -e "\r✓ Build completed successfully!"
else
    echo -e "\r✗ Build failed!"
    exit $BUILD_EXIT_CODE
fi
```

**Script Execution Tracking:**

```bash
#!/bin/bash

SCRIPT_NAME=$(basename "$0")
START_TIME=$(date +%s)

echo "==================================="
echo "Script: $SCRIPT_NAME"
echo "Started: $(date)"
echo "==================================="

# Your script logic here
dotnet build BlackSlope.NET.sln

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo "==================================="
echo "Completed: $(date)"
echo "Duration: ${DURATION}s"
echo "==================================="
```

**Integration with Build Systems:**

For CI/CD integration, ensure scripts:
- Return appropriate exit codes (0 for success, non-zero for failure)
- Output parseable logs (JSON, XML, or structured text)
- Support environment variable configuration
- Provide verbose mode for debugging

```bash
#!/bin/bash

# Support verbose mode via environment variable
if [ "$VERBOSE" = "true" ]; then
    set -x  # Print commands as they execute
fi

# Support configuration via environment variables
CONFIGURATION=${BUILD_CONFIGURATION:-Release}
OUTPUT_DIR=${PUBLISH_OUTPUT:-../publish}

dotnet publish --configuration "$CONFIGURATION" --output "$OUTPUT_DIR"
```

See [/development/environment.md](/development/environment.md) for environment setup and configuration details.