# Docker Deployment

This document provides comprehensive guidance for deploying the BlackSlope.NET application using Docker containers. The application supports both standalone Docker deployment and Docker Compose orchestration for multi-container environments.

## Dockerfile Configuration

The application uses a multi-stage Docker build process optimized for .NET 6.0 applications, balancing build efficiency with runtime performance.

### Multi-Stage Build Architecture

The Dockerfile implements a two-stage build pattern that separates the build environment from the runtime environment:

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

**Stage 1: Build Stage**
- **Base Image**: `mcr.microsoft.com/dotnet/sdk:6.0` - Full SDK image containing all build tools, compilers, and dependencies
- **Purpose**: Compiles the application and resolves all NuGet package dependencies
- **Process**:
  1. Copies entire source tree into the container
  2. Sets working directory to `/BlackSlope.Api` project folder
  3. Executes `dotnet restore` to download all NuGet packages
  4. Runs `dotnet publish` with Release configuration, outputting to `/app`
- **Optimization**: Uses `--no-restore` flag during publish to avoid redundant package downloads

**Stage 2: Runtime Stage**
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:6.0` - Lightweight runtime-only image (~200MB smaller than SDK)
- **Purpose**: Creates minimal production container with only runtime dependencies
- **Process**:
  1. Sets working directory to `/app`
  2. Exposes ports 80 (HTTP) and 443 (HTTPS) for web traffic
  3. Copies compiled artifacts from build stage using `COPY --from=build`
  4. Configures entry point to execute the compiled API DLL

### Base Images

The application leverages official Microsoft container images optimized for .NET 6.0:

| Image | Purpose | Size | Use Case |
|-------|---------|------|----------|
| `mcr.microsoft.com/dotnet/sdk:6.0` | Build environment | ~700MB | Compilation, testing, package restoration |
| `mcr.microsoft.com/dotnet/aspnet:6.0` | Runtime environment | ~200MB | Production deployment, minimal attack surface |

**Key Considerations**:
- Both images are based on Debian Linux by default
- Windows container support is available via `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (version 1.14.0)
- Images receive regular security updates from Microsoft
- Runtime image excludes build tools, reducing container size by ~70%

### Build Optimization

The multi-stage approach provides several optimization benefits:

**Layer Caching Strategy**:
- Dependency restoration occurs before source code changes, maximizing cache hits
- Separate `dotnet restore` and `dotnet publish` commands allow Docker to cache the restore layer
- Source code changes only invalidate layers after the COPY instruction

**Size Optimization**:
- Final image contains only runtime dependencies and compiled binaries
- Build artifacts, source code, and SDK tools are excluded from production image
- Typical final image size: ~210MB (runtime + application)

**Security Hardening**:
- Minimal runtime image reduces attack surface
- No build tools or compilers present in production container
- Follows principle of least privilege for container contents

**Recommended Improvements**:
```dockerfile
# Enhanced Dockerfile with additional optimizations
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy only project files first for better caching
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
COPY ["BlackSlope.Api.Common/BlackSlope.Api.Common.csproj", "BlackSlope.Api.Common/"]
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"

# Copy remaining source files
COPY . .
WORKDIR "/src/BlackSlope.Api"
RUN dotnet publish "BlackSlope.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Run as non-root user for security
USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

## Prerequisites

Before building and running Docker containers, install Docker Desktop:

1. Download [Docker Desktop](https://www.docker.com/products/docker-desktop) for your operating system
2. Install and start Docker Desktop
3. Verify installation: `docker --version`

## Building Docker Images

The application can be built using Docker Compose or directly with Docker CLI commands.

### Manual Docker Build (from README)

To build the Docker image directly using the Docker CLI:

```bash
# Navigate to the src directory
cd src

# Build the Docker image
docker build -t blackslope.api -f Dockerfile .

# Verify the image was created
docker images

# Create a container from the image
docker create --name blackslope-container blackslope.api

# Start the container
docker start blackslope-container
```

**Note**: The manual build process uses the image name `blackslope.api`, while the Docker Compose configuration uses `blackslope/api-app`. Choose the naming convention that fits your deployment strategy.

### docker-image-build.sh Script

Located at `scripts/docker-image-build.sh`, this script orchestrates the Docker Compose build process:

```bash
cd ../src
docker-compose build
```

**Script Behavior**:
1. Changes directory to `/src` where `docker-compose.yml` and `Dockerfile` reside
2. Executes `docker-compose build` to build all services defined in the compose file
3. Builds the `apiapp` service using the Dockerfile configuration

**Usage**:
```bash
# From the scripts directory
./docker-image-build.sh

# Or with explicit permissions
chmod +x docker-image-build.sh
./docker-image-build.sh
```

### Image Tagging Strategy

The Docker Compose configuration defines the following image naming:

```yaml
apiapp:
  container_name: apiapp
  image: blackslope/api-app
  build: 
    context: .
```

**Default Tagging**:
- **Docker Compose Image Name**: `blackslope/api-app`
- **Manual Build Image Name** (from README): `blackslope.api`
- **Tag**: `latest` (implicit when not specified)
- **Full Reference**: `blackslope/api-app:latest` or `blackslope.api:latest`

**Recommended Tagging Practices**:

For production deployments, implement semantic versioning and environment-specific tags:

```bash
# Build with version tag
docker build -t blackslope/api-app:1.0.0 -f src/Dockerfile src/

# Tag for different environments
docker tag blackslope/api-app:1.0.0 blackslope/api-app:production
docker tag blackslope/api-app:1.0.0 blackslope/api-app:staging

# Tag with git commit SHA for traceability
docker build -t blackslope/api-app:$(git rev-parse --short HEAD) -f src/Dockerfile src/

# Multi-tag build
docker build \
  -t blackslope/api-app:latest \
  -t blackslope/api-app:1.0.0 \
  -t blackslope/api-app:production \
  -f src/Dockerfile src/
```

### Build Context

The build context is set to the `/src` directory, which contains:

```
src/
├── BlackSlope.Api/              # Main API project
├── BlackSlope.Api.Common/       # Shared libraries
├── BlackSlope.Api.Tests/        # Unit tests
├── Dockerfile                   # Container definition
├── docker-compose.yml           # Multi-container orchestration
└── BlackSlope.NET.sln          # Solution file
```

**Context Implications**:
- All files in `/src` are sent to Docker daemon during build
- `.dockerignore` file should exclude unnecessary files (bin/, obj/, .git/)
- Large context directories increase build time and network transfer

**Recommended .dockerignore**:
```
# Create src/.dockerignore with:
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/*.user
**/.git/
**/node_modules/
**/.dockerignore
**/.gitignore
**/README.md
**/docker-compose*.yml
```

### Alternative Build Commands

For scenarios using the Docker Compose naming convention:

```bash
# Build from src directory
cd src
docker build -t blackslope/api-app -f Dockerfile .

# Build with build arguments
docker build \
  --build-arg ASPNETCORE_ENVIRONMENT=Production \
  -t blackslope/api-app:production \
  -f Dockerfile .

# Build with no cache (force rebuild)
docker build --no-cache -t blackslope/api-app -f Dockerfile .

# Build and push to registry
docker build -t myregistry.azurecr.io/blackslope/api-app:1.0.0 -f Dockerfile .
docker push myregistry.azurecr.io/blackslope/api-app:1.0.0
```

## Running Containers

The application provides orchestrated container execution through Docker Compose, managing both the API application and SQL Server database.

### docker-container-run.sh Script

Located at `scripts/docker-container-run.sh`, this script handles complete environment startup:

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

**Script Workflow**:
1. **Navigate to Source**: Changes to `/src` directory containing `docker-compose.yml`
2. **Start Containers**: Executes `docker-compose up -d` in detached mode
3. **Wait for Database**: Implements 5-second delay for SQL Server initialization
4. **Apply Migrations**: Runs `db-update.sh` to apply Entity Framework migrations

**Critical Timing Consideration**:
The 5-second sleep is a simple but imperfect solution for database readiness. SQL Server 2019 container initialization includes:
- Service startup (~3-5 seconds)
- Database engine initialization (~2-3 seconds)
- Network listener activation (~1-2 seconds)

**Production-Ready Alternative**:
```bash
#!/bin/bash
cd ../src
echo "Starting Docker Compose services..."
docker-compose up -d

echo "Waiting for SQL Server to be ready..."
# Wait for SQL Server to accept connections
until docker exec db /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" \
  -Q "SELECT 1" &> /dev/null
do
  echo "SQL Server is unavailable - sleeping"
  sleep 2
done

echo "SQL Server is up - executing database migrations"
cd ../scripts
./db-update.sh
```

### Port Mapping

The Docker Compose configuration exposes services on the following ports:

```yaml
services: 
  db: 
    ports: 
      - "1401:1433"  # SQL Server
  apiapp:
    ports: 
      - "5010:80"    # API HTTP endpoint
```

**Port Mapping Details**:

| Service | Host Port | Container Port | Protocol | Purpose |
|---------|-----------|----------------|----------|---------|
| db | 1401 | 1433 | TCP | SQL Server database connections |
| apiapp | 5010 | 80 | HTTP | API web service |

**Connection Strings**:
- **From Host Machine**: `Server=localhost,1401;Database=movies;User Id=sa;Password=YourStrong!Passw0rd`
- **From API Container**: `Server=db,1433;Database=movies;User Id=sa;Password=YourStrong!Passw0rd`
- **API Endpoint**: `http://localhost:5010/swagger`

**Port Conflict Resolution**:
If default ports are unavailable, modify `docker-compose.yml`:

```yaml
services: 
  db: 
    ports: 
      - "1402:1433"  # Changed from 1401
  apiapp:
    ports: 
      - "5011:80"    # Changed from 5010
```

### Environment Variables

The Docker Compose configuration injects environment-specific settings:

**Database Container (SQL Server)**:
```yaml
db:
  environment:
    SA_PASSWORD: "YourStrong!Passw0rd"  # SA account password
    ACCEPT_EULA: "Y"                     # Accept SQL Server license
    MSSQL_PID: Developer                 # SQL Server edition
```

**API Container**:
```yaml
apiapp:
  environment:
    ASPNETCORE_ENVIRONMENT: "docker"     # Environment name
```

**Environment Variable Behavior**:

1. **ASPNETCORE_ENVIRONMENT**: 
   - Determines which `appsettings.{Environment}.json` file is loaded
   - Expected file: `appsettings.docker.json`
   - Affects logging levels, connection strings, and feature flags
   - See [Application Settings](/configuration/application_settings.md) for configuration details

2. **SA_PASSWORD**:
   - Must meet SQL Server complexity requirements (uppercase, lowercase, digits, symbols)
   - Used for initial SA account creation
   - Should be overridden via environment variables or secrets in production

3. **MSSQL_PID**:
   - `Developer`: Full-featured, non-production license
   - `Express`: Limited features, free license
   - `Standard`, `Enterprise`: Production licenses (require valid license key)

**Security Best Practices**:

For production deployments, externalize sensitive configuration:

```yaml
# docker-compose.prod.yml
services:
  db:
    environment:
      SA_PASSWORD: ${SQL_SA_PASSWORD}  # From environment variable
      ACCEPT_EULA: "Y"
      MSSQL_PID: ${SQL_EDITION}
  apiapp:
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENV}
      ConnectionStrings__MoviesConnectionString: ${MOVIES_DB_CONNECTION}
```

```bash
# Set environment variables before running
export SQL_SA_PASSWORD="$(openssl rand -base64 32)"
export SQL_EDITION="Standard"
export ASPNETCORE_ENV="Production"
export MOVIES_DB_CONNECTION="Server=db,1433;Database=movies;User Id=sa;Password=${SQL_SA_PASSWORD}"

docker-compose -f docker-compose.prod.yml up -d
```

**Azure Key Vault Integration**:

For Azure deployments, leverage Azure.Identity (version 1.14.2) for managed identity authentication:

```csharp
// In Program.cs or Startup.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Volume Mounting

The current Docker Compose configuration does not define persistent volumes, meaning database data is lost when containers are removed.

**Current Behavior**:
- Database data stored in container's writable layer
- Data persists during container stop/start
- Data lost on container removal (`docker-compose down`)

**Recommended Volume Configuration**:

```yaml
version: '3.8'
services: 
  db: 
    container_name: db
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "YourStrong!Passw0rd"
      ACCEPT_EULA: "Y"
      MSSQL_PID: Developer    
    ports: 
      - "1401:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql  # Persist database files
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql:ro  # Initialization script
  
  apiapp:
    container_name: apiapp
    image: blackslope/api-app
    build: 
      context: .
    depends_on:
      - db
    ports: 
      - "5010:80"
    environment:
      ASPNETCORE_ENVIRONMENT: "docker"
    volumes:
      - ./logs:/app/logs  # Persist application logs

volumes:
  sqlserver-data:  # Named volume for database persistence
```

**Volume Types**:

1. **Named Volumes** (`sqlserver-data`):
   - Managed by Docker
   - Persist across container lifecycle
   - Stored in Docker's volume directory
   - Best for database files

2. **Bind Mounts** (`./logs:/app/logs`):
   - Direct mapping to host filesystem
   - Useful for logs, configuration files
   - Easier to access from host machine

**Volume Management Commands**:
```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect src_sqlserver-data

# Backup database volume
docker run --rm \
  -v src_sqlserver-data:/source:ro \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/sqlserver-backup-$(date +%Y%m%d).tar.gz -C /source .

# Restore database volume
docker run --rm \
  -v src_sqlserver-data:/target \
  -v $(pwd)/backups:/backup \
  alpine tar xzf /backup/sqlserver-backup-20240101.tar.gz -C /target
```

## Docker Compose

The application uses Docker Compose for multi-container orchestration, simplifying development and deployment workflows.

### Multi-Container Setup

The `docker-compose.yml` file defines a two-service architecture:

```yaml
version: '3.8'
services: 
  db: 
    container_name: db
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "YourStrong!Passw0rd"
      ACCEPT_EULA: "Y"
      MSSQL_PID: Developer    
    ports: 
      - "1401:1433"
  apiapp:
    container_name: apiapp
    image: blackslope/api-app
    build: 
      context: .
    depends_on:
      - db
    ports: 
      - "5010:80"
    environment:
      ASPNETCORE_ENVIRONMENT: "docker"
```

**Compose File Version**:
- Uses Compose file format version 3.8
- Compatible with Docker Engine 19.03.0+
- Supports all features required for this application

### Service Definitions

**Database Service (`db`)**:
- **Image**: Official Microsoft SQL Server 2019 container
- **Container Name**: Fixed as `db` for consistent DNS resolution
- **Purpose**: Provides relational database for Entity Framework Core
- **Initialization**: Automatic on first startup
- **Data Persistence**: Ephemeral (see Volume Mounting section for persistence)

**API Service (`apiapp`)**:
- **Image**: Custom-built from local Dockerfile
- **Build Context**: Current directory (`.` = `/src`)
- **Container Name**: Fixed as `apiapp`
- **Purpose**: Hosts ASP.NET Core Web API
- **Dependencies**: Requires `db` service to be started first

### Networking

Docker Compose automatically creates a default bridge network for service communication.

**Network Architecture**:
```
┌─────────────────────────────────────────┐
│  Docker Compose Network (src_default)   │
│                                          │
│  ┌──────────┐         ┌──────────┐     │
│  │    db    │◄────────┤  apiapp  │     │
│  │  (1433)  │         │   (80)   │     │
│  └────┬─────┘         └────┬─────┘     │
│       │                    │            │
└───────┼────────────────────┼────────────┘
        │                    │
        │ Port 1401          │ Port 5010
        ▼                    ▼
    Host Machine         Host Machine
```

**DNS Resolution**:
- Services can reference each other by service name
- `db` resolves to the database container's IP address
- `apiapp` resolves to the API container's IP address

**Connection String Example**:
```json
// appsettings.docker.json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=db,1433;Database=movies;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  }
}
```

**Network Isolation**:
- Containers within the network can communicate freely
- External access only through explicitly mapped ports
- Host machine accesses services via `localhost:{mapped_port}`

**Custom Network Configuration**:

For advanced scenarios, define explicit networks:

```yaml
version: '3.8'
services: 
  db: 
    container_name: db
    image: mcr.microsoft.com/mssql/server:2019-latest
    networks:
      - backend
    # ... other configuration
  
  apiapp:
    container_name: apiapp
    image: blackslope/api-app
    networks:
      - backend
      - frontend
    # ... other configuration

networks:
  backend:
    driver: bridge
    internal: true  # No external access
  frontend:
    driver: bridge
```

**Health Checks Integration**:

The application includes health check endpoints (via `AspNetCore.HealthChecks.SqlServer` 5.0.3 and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` 6.0.1). Integrate with Docker Compose:

```yaml
apiapp:
  container_name: apiapp
  image: blackslope/api-app
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost/health"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
  depends_on:
    db:
      condition: service_healthy

db:
  container_name: db
  image: mcr.microsoft.com/mssql/server:2019-latest
  healthcheck:
    test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "SELECT 1" || exit 1
    interval: 10s
    timeout: 3s
    retries: 10
    start_period: 10s
```

### Common Docker Compose Commands

```bash
# Start all services in detached mode
docker-compose up -d

# Start and rebuild images
docker-compose up -d --build

# View service logs
docker-compose logs -f apiapp
docker-compose logs -f db

# Stop services (containers remain)
docker-compose stop

# Start stopped services
docker-compose start

# Stop and remove containers
docker-compose down

# Stop, remove containers, and delete volumes
docker-compose down -v

# Scale services (not applicable with fixed container names)
docker-compose up -d --scale apiapp=3

# Execute command in running container
docker-compose exec apiapp bash
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd"

# View service status
docker-compose ps

# Validate compose file syntax
docker-compose config
```

## Best Practices

### Image Optimization

**1. Multi-Stage Build Efficiency**:
```dockerfile
# Optimize layer caching by copying project files first
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project files separately to leverage cache
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
COPY ["BlackSlope.Api.Common/BlackSlope.Api.Common.csproj", "BlackSlope.Api.Common/"]
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"

# Copy source code after restore
COPY . .
WORKDIR "/src/BlackSlope.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore
```

**2. Minimize Image Layers**:
```dockerfile
# Combine related commands to reduce layers
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*
```

**3. Use .dockerignore**:
```
# Exclude unnecessary files from build context
**/bin/
**/obj/
**/.vs/
**/.git/
**/node_modules/
**/*.md
**/docker-compose*.yml
```

**4. Leverage Build Cache**:
- Order Dockerfile instructions from least to most frequently changing
- Place `COPY` instructions for source code after dependency restoration
- Use `--no-cache` flag only when necessary

**5. Optimize Runtime Image**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime
# Alpine variant is ~50MB smaller than Debian-based image
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

### Security Scanning

**1. Vulnerability Scanning with Docker Scout**:
```bash
# Enable Docker Scout
docker scout quickview blackslope/api-app:latest

# Detailed vulnerability report
docker scout cves blackslope/api-app:latest

# Compare with base image
docker scout compare --to mcr.microsoft.com/dotnet/aspnet:6.0 blackslope/api-app:latest
```

**2. Trivy Security Scanner**:
```bash
# Install Trivy
brew install aquasecurity/trivy/trivy  # macOS
# or
apt-get install trivy  # Debian/Ubuntu

# Scan image for vulnerabilities
trivy image blackslope/api-app:latest

# Scan with severity filtering
trivy image --severity HIGH,CRITICAL blackslope/api-app:latest

# Generate JSON report
trivy image -f json -o report.json blackslope/api-app:latest
```

**3. Dependency Scanning**:
```bash
# Scan NuGet packages for known vulnerabilities
dotnet list package --vulnerable --include-transitive

# Update vulnerable packages
dotnet add package Microsoft.Data.SqlClient --version 5.1.5
```

**4. Runtime Security**:
```dockerfile
# Run as non-root user
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN groupadd -r appuser && useradd -r -g appuser appuser
WORKDIR /app
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**5. Secrets Management**:
```yaml
# Use Docker secrets instead of environment variables
version: '3.8'
services:
  apiapp:
    secrets:
      - db_password
    environment:
      ConnectionStrings__MoviesConnectionString: "Server=db;Database=movies;User Id=sa;Password_File=/run/secrets/db_password"

secrets:
  db_password:
    file: ./secrets/db_password.txt
```

### Layer Caching

**1. Optimal Dockerfile Layer Order**:
```dockerfile
# 1. Base image (rarely changes)
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# 2. Working directory (rarely changes)
WORKDIR /src

# 3. Project files (changes occasionally)
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
COPY ["BlackSlope.Api.Common/BlackSlope.Api.Common.csproj", "BlackSlope.Api.Common/"]

# 4. Dependency restoration (changes when packages update)
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"

# 5. Source code (changes frequently)
COPY . .

# 6. Build and publish (changes with source code)
WORKDIR "/src/BlackSlope.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore
```

**2. BuildKit for Enhanced Caching**:
```bash
# Enable BuildKit for improved caching
export DOCKER_BUILDKIT=1

# Build with cache mount for NuGet packages
docker build \
  --build-arg BUILDKIT_INLINE_CACHE=1 \
  -t blackslope/api-app:latest \
  -f src/Dockerfile src/
```

**3. Cache from Remote Registry**:
```bash
# Pull previous image for cache
docker pull blackslope/api-app:latest

# Build using previous image as cache
docker build \
  --cache-from blackslope/api-app:latest \
  -t blackslope/api-app:latest \
  -f src/Dockerfile src/
```

**4. Multi-Stage Cache Optimization**:
```dockerfile
# Cache NuGet packages in separate stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS restore
WORKDIR /src
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"

# Build stage uses restore cache
FROM restore AS build
COPY . .
WORKDIR "/src/BlackSlope.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore
```

**5. CI/CD Pipeline Caching**:
```yaml
# GitHub Actions example
- name: Build Docker image
  uses: docker/build-push-action@v4
  with:
    context: ./src
    file: ./src/Dockerfile
    push: true
    tags: blackslope/api-app:latest
    cache-from: type=registry,ref=blackslope/api-app:buildcache
    cache-to: type=registry,ref=blackslope/api-app:buildcache,mode=max
```

### Production Deployment Checklist

- [ ] **Remove hardcoded credentials** from docker-compose.yml
- [ ] **Implement persistent volumes** for database data
- [ ] **Configure health checks** for all services
- [ ] **Enable HTTPS** with valid SSL certificates
- [ ] **Set resource limits** (CPU, memory) for containers
- [ ] **Implement logging** to external aggregation service
- [ ] **Configure restart policies** (`restart: unless-stopped`)
- [ ] **Use specific image tags** instead of `latest`
- [ ] **Scan images** for vulnerabilities before deployment
- [ ] **Implement backup strategy** for database volumes
- [ ] **Configure monitoring** and alerting
- [ ] **Document rollback procedures**
- [ ] **Test disaster recovery** scenarios

### Integration with Kubernetes

For production-scale deployments, consider migrating to Kubernetes. See [Kubernetes Deployment](/deployment/kubernetes.md) for orchestration at scale.

**Docker to Kubernetes Migration Path**:
1. Convert Docker Compose to Kubernetes manifests using Kompose
2. Implement Kubernetes Secrets for sensitive configuration
3. Configure Persistent Volume Claims for database storage
4. Set up Ingress controllers for external access
5. Implement Horizontal Pod Autoscaling for the API service

### Related Documentation

- [Application Settings Configuration](/configuration/application_settings.md) - Environment-specific configuration management
- [Prerequisites](/getting_started/prerequisites.md) - Required tools and dependencies
- [Kubernetes Deployment](/deployment/kubernetes.md) - Container orchestration for production