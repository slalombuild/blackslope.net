# Kubernetes Deployment

This document provides comprehensive guidance for deploying the BlackSlope.NET application to Kubernetes clusters. The application consists of a .NET 6.0 Web API with integrated health checks, SQL Server database connectivity, and Azure AD authentication.

## Kubernetes Overview

Kubernetes provides container orchestration for the BlackSlope application, managing deployment, scaling, and operations of the containerized .NET 6.0 Web API.

### Core Kubernetes Concepts

**Pods**: The smallest deployable units in Kubernetes. Each pod runs one or more containers (in our case, the BlackSlope.Api Docker container) and shares networking and storage resources.

**Services**: Provide stable network endpoints for accessing pods. Services enable load balancing across multiple pod replicas and abstract the underlying pod IP addresses.

**Deployments**: Manage the desired state of pods, handling rolling updates, rollbacks, and replica management. Deployments ensure the specified number of pod replicas are running at all times.

**ConfigMaps and Secrets**: Store configuration data and sensitive information separately from container images. For BlackSlope, these manage connection strings, Azure AD credentials, and application settings.

### Architecture Considerations

The BlackSlope application architecture includes:

- **Stateless API Layer**: The Web API is stateless, making it ideal for horizontal scaling in Kubernetes
- **External Database**: SQL Server runs outside the Kubernetes cluster (or in a separate StatefulSet)
- **Health Check Endpoints**: Native health checks at `/health` enable Kubernetes probes
- **Azure Integration**: Azure AD authentication requires proper secret management

## Deployment Configuration

### Deployment Manifest

Create a deployment manifest for the BlackSlope API:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api
  namespace: blackslope
  labels:
    app: blackslope-api
    version: v1
spec:
  replicas: 3
  selector:
    matchLabels:
      app: blackslope-api
  template:
    metadata:
      labels:
        app: blackslope-api
        version: v1
    spec:
      containers:
      - name: api
        image: <your-registry>/blackslope-api:latest
        imagePullPolicy: Always
        ports:
        - containerPort: 80
          name: http
          protocol: TCP
        - containerPort: 443
          name: https
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:80"
        envFrom:
        - configMapRef:
            name: blackslope-config
        - secretRef:
            name: blackslope-secrets
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
            scheme: HTTP
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          successThreshold: 1
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health
            port: 80
            scheme: HTTP
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health
            port: 80
            scheme: HTTP
          initialDelaySeconds: 0
          periodSeconds: 5
          timeoutSeconds: 3
          successThreshold: 1
          failureThreshold: 30
      restartPolicy: Always
```

**Key Configuration Points**:

- **Replicas**: Set to 3 for high availability; adjust based on load requirements
- **Image Pull Policy**: `Always` ensures latest image is pulled; use `IfNotPresent` for stable releases
- **Ports**: Exposes both HTTP (80) and HTTPS (443) as defined in the Dockerfile
- **Environment Variables**: `ASPNETCORE_ENVIRONMENT` controls configuration loading; `ASPNETCORE_URLS` binds to all interfaces

### Service Definitions

#### ClusterIP Service (Internal)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: blackslope-api-internal
  namespace: blackslope
  labels:
    app: blackslope-api
spec:
  type: ClusterIP
  selector:
    app: blackslope-api
  ports:
  - name: http
    port: 80
    targetPort: 80
    protocol: TCP
  sessionAffinity: None
```

#### LoadBalancer Service (External)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: blackslope-api
  namespace: blackslope
  labels:
    app: blackslope-api
  annotations:
    service.beta.kubernetes.io/azure-load-balancer-internal: "false"
spec:
  type: LoadBalancer
  selector:
    app: blackslope-api
  ports:
  - name: http
    port: 80
    targetPort: 80
    protocol: TCP
  - name: https
    port: 443
    targetPort: 443
    protocol: TCP
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 10800
```

**Service Type Selection**:

- **ClusterIP**: Use for internal microservice communication
- **LoadBalancer**: Use for external access; creates cloud provider load balancer
- **Session Affinity**: `ClientIP` maintains sticky sessions for 3 hours (useful if implementing session state)

### ConfigMaps and Secrets

#### ConfigMap for Application Settings

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: blackslope-config
  namespace: blackslope
data:
  BlackSlope.Api__BaseUrl: "https://api.blackslope.example.com"
  BlackSlope.Api__Swagger__Version: "1"
  BlackSlope.Api__Swagger__ApplicationName: "BlackSlope"
  BlackSlope.Api__Swagger__XmlFile: "BlackSlope.Api.xml"
  BlackSlope.Api__Serilog__MinimumLevel: "information"
  BlackSlope.Api__Serilog__WriteToConsole: "true"
  BlackSlope.Api__Serilog__WriteToFile: "false"
  BlackSlope.Api__Serilog__WriteToAppInsights: "true"
  BlackSlope.Api__HealthChecks__Endpoint: "/health"
  AllowedHosts: "*"
```

**Configuration Hierarchy**: The double underscore (`__`) syntax maps to nested JSON configuration sections in `appsettings.json`. For example, `BlackSlope.Api__Serilog__MinimumLevel` maps to:

```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "information"
    }
  }
}
```

#### Secrets for Sensitive Data

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: blackslope-secrets
  namespace: blackslope
type: Opaque
stringData:
  BlackSlope.Api__MoviesConnectionString: "data source=sql-server.database.windows.net,1433;initial catalog=movies;User ID=blackslope_user;Password=<password>;MultipleActiveResultSets=True;Encrypt=True;"
  BlackSlope.Api__AzureAd__Tenant: "<tenant-id>"
  BlackSlope.Api__AzureAd__Audience: "https://api.blackslope.example.com"
  BlackSlope.Api__AzureAd__AadInstance: "https://login.microsoftonline.com/{0}"
  BlackSlope.Api__ApplicationInsights__InstrumentationKey: "<instrumentation-key>"
```

**Security Best Practices**:

- Never commit secrets to source control
- Use `stringData` for plain text (Kubernetes base64 encodes automatically)
- Consider using Azure Key Vault with the [Secrets Store CSI Driver](https://azure.github.io/secrets-store-csi-driver-provider-azure/)
- Rotate secrets regularly using Kubernetes secret rotation mechanisms
- Use RBAC to restrict secret access to specific service accounts

#### Creating Secrets from Files

```bash
# Create secret from connection string file
kubectl create secret generic blackslope-secrets \
  --from-literal=BlackSlope.Api__MoviesConnectionString="$(cat connection-string.txt)" \
  --namespace=blackslope

# Create secret from Azure AD configuration
kubectl create secret generic blackslope-azure-ad \
  --from-file=tenant-id=./azure-ad/tenant-id.txt \
  --from-file=audience=./azure-ad/audience.txt \
  --namespace=blackslope
```

## Health Check Integration

The BlackSlope application implements comprehensive health checks through the `HealthCheckStartup` class, providing multiple endpoints for Kubernetes probes.

### Health Check Architecture

The application exposes the following health check endpoints:

- `/health` - Overall application health (all checks)
- `/health/movies` - Movies-specific checks (database and API)
- `/health/database` - Database connectivity checks only
- `/health/api` - External API dependency checks only

### Health Check Implementation

From `HealthCheckStartup.cs`:

```csharp
public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<MovieRepositoryConfiguration>();

    services.AddHealthChecks()
        .AddSqlServer(config.MoviesConnectionString, 
            name: "MOVIES.DB", 
            tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
        .AddCheck<MoviesHealthCheck>("MOVIES.API", 
            tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
}
```

The `MoviesHealthCheck` class validates external API dependencies:

```csharp
public class MoviesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MoviesHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientFactory.CreateClient("movies")
                .GetAsync("api/version", cancellationToken).ConfigureAwait(false);
            
            return result.IsSuccessStatusCode
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(HealthStatus.Unhealthy);
        }
        catch (Exception e)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, exception: e);
        }
    }
}
```

### Liveness Probes

Liveness probes determine if a container is running properly. If a liveness probe fails, Kubernetes kills and restarts the container.

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
    scheme: HTTP
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 5
  successThreshold: 1
  failureThreshold: 3
```

**Configuration Rationale**:

- **initialDelaySeconds: 30** - Allows .NET runtime initialization, dependency injection container setup, and initial database connection pool creation
- **periodSeconds: 10** - Checks every 10 seconds; balances responsiveness with overhead
- **timeoutSeconds: 5** - Sufficient for database query and HTTP client timeout (considering Polly retry policies)
- **failureThreshold: 3** - Requires 3 consecutive failures (30 seconds) before restart; prevents flapping from transient issues

### Readiness Probes

Readiness probes determine if a container is ready to accept traffic. Failed readiness probes remove the pod from service endpoints without restarting it.

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 80
    scheme: HTTP
  initialDelaySeconds: 10
  periodSeconds: 5
  timeoutSeconds: 3
  successThreshold: 1
  failureThreshold: 3
```

**Configuration Rationale**:

- **initialDelaySeconds: 10** - Shorter than liveness; application may be running but not ready for traffic
- **periodSeconds: 5** - More frequent checks ensure rapid traffic routing updates
- **failureThreshold: 3** - 15 seconds of failures before removing from load balancer

**Use Case**: During database connection issues, the readiness probe fails, removing the pod from the service while the liveness probe allows the application to continue running and potentially recover.

### Startup Probes

Startup probes provide additional time for slow-starting applications. While the startup probe is active, liveness and readiness probes are disabled.

```yaml
startupProbe:
  httpGet:
    path: /health
    port: 80
    scheme: HTTP
  initialDelaySeconds: 0
  periodSeconds: 5
  timeoutSeconds: 3
  successThreshold: 1
  failureThreshold: 30
```

**Configuration Rationale**:

- **failureThreshold: 30** with **periodSeconds: 5** = 150 seconds maximum startup time
- Accommodates Entity Framework Core migrations, connection pool initialization, and Azure AD token acquisition
- Once successful, liveness and readiness probes take over

### Tagged Health Check Endpoints

For granular monitoring, use tagged endpoints in different probe types:

```yaml
# Check only database connectivity
livenessProbe:
  httpGet:
    path: /health/database
    port: 80

# Check all dependencies including external APIs
readinessProbe:
  httpGet:
    path: /health
    port: 80
```

**Strategy**: Use `/health/database` for liveness (core functionality) and `/health` for readiness (including external dependencies). This prevents pod restarts due to external API failures while still removing pods from load balancing.

### Health Check Response Format

The custom `HealthCheckResponseWriter` returns detailed JSON responses:

```json
{
  "status": "Healthy",
  "details": [
    {
      "key": "MOVIES.DB",
      "value": "Healthy",
      "description": null,
      "duration": "00:00:00.0234567",
      "exception": null
    },
    {
      "key": "MOVIES.API",
      "value": "Healthy",
      "description": null,
      "duration": "00:00:00.1234567",
      "exception": null
    }
  ]
}
```

This detailed response aids in debugging probe failures through Kubernetes events and logs.

## Scaling and Updates

### Horizontal Pod Autoscaling

Horizontal Pod Autoscaler (HPA) automatically scales the number of pods based on observed metrics.

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: blackslope-api-hpa
  namespace: blackslope
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: blackslope-api
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
      - type: Pods
        value: 2
        periodSeconds: 60
      selectPolicy: Min
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
      - type: Pods
        value: 4
        periodSeconds: 30
      selectPolicy: Max
```

**Scaling Behavior**:

- **Scale Up**: Aggressive (doubles pods or adds 4, whichever is greater) to handle traffic spikes quickly
- **Scale Down**: Conservative (5-minute stabilization window) to prevent flapping and maintain capacity during intermittent load
- **CPU Threshold**: 70% allows headroom for request bursts while maximizing resource utilization
- **Memory Threshold**: 80% accounts for .NET garbage collection patterns

**Metrics Server Requirement**: HPA requires the Kubernetes Metrics Server:

```bash
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

### Custom Metrics Autoscaling

For advanced scenarios, scale based on application-specific metrics using Azure Monitor or Prometheus:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: blackslope-api-custom-hpa
  namespace: blackslope
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: blackslope-api
  minReplicas: 3
  maxReplicas: 15
  metrics:
  - type: External
    external:
      metric:
        name: azure_application_insights_request_rate
        selector:
          matchLabels:
            app: blackslope-api
      target:
        type: AverageValue
        averageValue: "1000"
```

This scales based on Application Insights request rate, useful for request-heavy workloads.

### Rolling Updates

Configure rolling update strategy in the deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    # ... container spec
```

**Strategy Configuration**:

- **maxSurge: 1** - Creates 1 additional pod during updates (4 total with 3 replicas)
- **maxUnavailable: 0** - Ensures no pods are terminated until new pods are ready
- **Zero-downtime deployment** - Traffic continues flowing to old pods until new pods pass readiness checks

**Update Process**:

1. New pod is created (4 pods total)
2. New pod passes startup probe
3. New pod passes readiness probe and receives traffic
4. Old pod is terminated
5. Process repeats until all pods are updated

### Deployment Rollout Commands

```bash
# Apply updated deployment
kubectl apply -f blackslope-deployment.yaml

# Monitor rollout status
kubectl rollout status deployment/blackslope-api -n blackslope

# View rollout history
kubectl rollout history deployment/blackslope-api -n blackslope

# Rollback to previous version
kubectl rollout undo deployment/blackslope-api -n blackslope

# Rollback to specific revision
kubectl rollout undo deployment/blackslope-api --to-revision=2 -n blackslope

# Pause rollout (for canary testing)
kubectl rollout pause deployment/blackslope-api -n blackslope

# Resume rollout
kubectl rollout resume deployment/blackslope-api -n blackslope
```

### Blue-Green Deployment Strategy

For zero-risk deployments, use blue-green strategy with service selector switching:

```yaml
# Blue deployment (current)
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api-blue
spec:
  replicas: 3
  selector:
    matchLabels:
      app: blackslope-api
      version: blue
  template:
    metadata:
      labels:
        app: blackslope-api
        version: blue
    # ... container spec

---
# Green deployment (new)
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api-green
spec:
  replicas: 3
  selector:
    matchLabels:
      app: blackslope-api
      version: green
  template:
    metadata:
      labels:
        app: blackslope-api
        version: green
    # ... container spec with new image

---
# Service (switch selector to cutover)
apiVersion: v1
kind: Service
metadata:
  name: blackslope-api
spec:
  selector:
    app: blackslope-api
    version: blue  # Change to 'green' to cutover
  ports:
  - port: 80
    targetPort: 80
```

**Cutover Process**:

1. Deploy green deployment alongside blue
2. Verify green deployment health
3. Update service selector from `version: blue` to `version: green`
4. Monitor for issues
5. Delete blue deployment after validation period

### Resource Limits

Resource limits prevent pods from consuming excessive cluster resources:

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

**Request vs. Limit**:

- **Requests**: Guaranteed resources; used for scheduling decisions
- **Limits**: Maximum resources; pod is throttled (CPU) or killed (memory) if exceeded

**Sizing Guidance for .NET 6.0**:

- **Memory Request**: 256Mi accommodates .NET runtime, JIT compilation, and moderate request load
- **Memory Limit**: 512Mi provides headroom for garbage collection and peak load
- **CPU Request**: 250m (0.25 cores) sufficient for typical API request processing
- **CPU Limit**: 500m prevents CPU starvation of other pods

**Monitoring Resource Usage**:

```bash
# View pod resource usage
kubectl top pods -n blackslope

# View node resource usage
kubectl top nodes

# Describe pod to see resource allocation
kubectl describe pod <pod-name> -n blackslope
```

Adjust limits based on observed usage patterns from Application Insights and Kubernetes metrics.

## Best Practices

### Resource Management

#### Quality of Service (QoS) Classes

Kubernetes assigns QoS classes based on resource configuration:

```yaml
# Guaranteed QoS (highest priority)
resources:
  requests:
    memory: "512Mi"
    cpu: "500m"
  limits:
    memory: "512Mi"
    cpu: "500m"

# Burstable QoS (medium priority)
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"

# BestEffort QoS (lowest priority)
# No resources specified
```

**Recommendation**: Use **Burstable** QoS for the BlackSlope API to allow burst capacity while guaranteeing baseline resources.

#### Resource Quotas

Limit namespace resource consumption:

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: blackslope-quota
  namespace: blackslope
spec:
  hard:
    requests.cpu: "4"
    requests.memory: "8Gi"
    limits.cpu: "8"
    limits.memory: "16Gi"
    pods: "20"
    services: "5"
    persistentvolumeclaims: "5"
```

#### Limit Ranges

Set default resource limits for pods without explicit specifications:

```yaml
apiVersion: v1
kind: LimitRange
metadata:
  name: blackslope-limits
  namespace: blackslope
spec:
  limits:
  - max:
      cpu: "2"
      memory: "2Gi"
    min:
      cpu: "100m"
      memory: "128Mi"
    default:
      cpu: "500m"
      memory: "512Mi"
    defaultRequest:
      cpu: "250m"
      memory: "256Mi"
    type: Container
```

### Security Contexts

#### Pod Security Context

```yaml
spec:
  securityContext:
    runAsNonRoot: true
    runAsUser: 1000
    runAsGroup: 3000
    fsGroup: 2000
    seccompProfile:
      type: RuntimeDefault
```

**Security Measures**:

- **runAsNonRoot**: Prevents container from running as root user
- **runAsUser/runAsGroup**: Specifies non-privileged user/group IDs
- **fsGroup**: Sets group ownership for mounted volumes
- **seccompProfile**: Enables secure computing mode to restrict syscalls

#### Container Security Context

```yaml
containers:
- name: api
  securityContext:
    allowPrivilegeEscalation: false
    readOnlyRootFilesystem: true
    capabilities:
      drop:
      - ALL
```

**Hardening Measures**:

- **allowPrivilegeEscalation: false**: Prevents gaining additional privileges
- **readOnlyRootFilesystem: true**: Makes container filesystem read-only (requires writable volumes for temp files)
- **capabilities.drop: ALL**: Removes all Linux capabilities

**Note**: `readOnlyRootFilesystem` requires mounting writable volumes for ASP.NET Core temporary files:

```yaml
volumeMounts:
- name: tmp
  mountPath: /tmp
- name: aspnet-temp
  mountPath: /app/temp

volumes:
- name: tmp
  emptyDir: {}
- name: aspnet-temp
  emptyDir: {}
```

#### Network Policies

Restrict pod network access:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: blackslope-api-netpol
  namespace: blackslope
spec:
  podSelector:
    matchLabels:
      app: blackslope-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 80
  egress:
  - to:
    - namespaceSelector: {}
      podSelector:
        matchLabels:
          app: sql-server
    ports:
    - protocol: TCP
      port: 1433
  - to:
    - namespaceSelector:
        matchLabels:
          name: kube-system
      podSelector:
        matchLabels:
          k8s-app: kube-dns
    ports:
    - protocol: UDP
      port: 53
  - to:
    - podSelector: {}
    ports:
    - protocol: TCP
      port: 443  # Azure AD, Application Insights
```

**Policy Explanation**:

- **Ingress**: Only allows traffic from ingress controller namespace
- **Egress**: Allows traffic to SQL Server, DNS, and HTTPS (for Azure services)
- Blocks all other traffic by default

### Monitoring Integration

#### Application Insights Integration

The BlackSlope application integrates with Azure Application Insights for telemetry. Ensure the instrumentation key is configured:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: blackslope-secrets
stringData:
  BlackSlope.Api__ApplicationInsights__InstrumentationKey: "<key>"
  BlackSlope.Api__Serilog__WriteToAppInsights: "true"
```

#### Prometheus Metrics

Expose ASP.NET Core metrics for Prometheus scraping using `prometheus-net.AspNetCore`:

```bash
# Add NuGet package
dotnet add package prometheus-net.AspNetCore
```

Configure in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseHttpMetrics();  // Capture HTTP metrics
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapMetrics();  // Expose /metrics endpoint
        endpoints.MapControllers();
    });
}
```

Add Prometheus annotations to deployment:

```yaml
metadata:
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "80"
    prometheus.io/path: "/metrics"
```

#### Logging Best Practices

Configure Serilog for structured logging to stdout (captured by Kubernetes):

```yaml
BlackSlope.Api__Serilog__WriteToConsole: "true"
BlackSlope.Api__Serilog__WriteToFile: "false"
BlackSlope.Api__Serilog__MinimumLevel: "information"
```

**Log Aggregation**: Use Fluentd, Fluent Bit, or Azure Monitor to aggregate logs from all pods.

View logs:

```bash
# Stream logs from all pods
kubectl logs -f deployment/blackslope-api -n blackslope

# View logs from specific pod
kubectl logs <pod-name> -n blackslope

# View previous container logs (after crash)
kubectl logs <pod-name> -n blackslope --previous
```

### Database Connection Management

#### Connection String Configuration

Use Kubernetes secrets for SQL Server connection strings:

```yaml
stringData:
  BlackSlope.Api__MoviesConnectionString: |
    data source=sql-server.database.windows.net,1433;
    initial catalog=movies;
    User ID=blackslope_user;
    Password=<password>;
    MultipleActiveResultSets=True;
    Encrypt=True;
    TrustServerCertificate=False;
    Connection Timeout=30;
    Max Pool Size=100;
    Min Pool Size=5;
```

**Connection Pool Settings**:

- **Max Pool Size: 100**: Limits connections per pod (adjust based on replica count and database limits)
- **Min Pool Size: 5**: Maintains warm connections for faster request processing
- **Connection Timeout: 30**: Aligns with health check timeout settings

#### Database Initialization

For Entity Framework Core migrations, use an init container or Kubernetes Job:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: blackslope-db-migration
  namespace: blackslope
spec:
  template:
    spec:
      containers:
      - name: migration
        image: <your-registry>/blackslope-api:latest
        command: ["dotnet", "ef", "database", "update"]
        envFrom:
        - secretRef:
            name: blackslope-secrets
      restartPolicy: OnFailure
  backoffLimit: 3
```

Run before deploying the application:

```bash
kubectl apply -f db-migration-job.yaml
kubectl wait --for=condition=complete job/blackslope-db-migration -n blackslope
kubectl apply -f blackslope-deployment.yaml
```

### High Availability Considerations

#### Pod Disruption Budgets

Ensure minimum availability during voluntary disruptions (node maintenance, cluster upgrades):

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: blackslope-api-pdb
  namespace: blackslope
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: blackslope-api
```

**Configuration**: With 3 replicas and `minAvailable: 2`, Kubernetes ensures at least 2 pods remain running during voluntary disruptions.

#### Pod Anti-Affinity

Distribute pods across nodes for fault tolerance:

```yaml
spec:
  affinity:
    podAntiAffinity:
      preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchLabels:
              app: blackslope-api
          topologyKey: kubernetes.io/hostname
```

**Affinity Types**:

- **preferredDuringSchedulingIgnoredDuringExecution**: Soft constraint; schedules on same node if necessary
- **requiredDuringSchedulingIgnoredDuringExecution**: Hard constraint; fails scheduling if constraint cannot be met

#### Multi-Zone Deployment

For cloud providers supporting availability zones:

```yaml
spec:
  affinity:
    podAntiAffinity:
      preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchLabels:
              app: blackslope-api
          topologyKey: topology.kubernetes.io/zone
```

This distributes pods across availability zones for regional fault tolerance.

### Troubleshooting Common Issues

#### Pod Stuck in Pending State

```bash
# Check pod events
kubectl describe pod <pod-name> -n blackslope

# Common causes:
# - Insufficient cluster resources
# - Image pull errors
# - PersistentVolumeClaim not bound
```

#### CrashLoopBackOff

```bash
# View logs from crashed container
kubectl logs <pod-name> -n blackslope --previous

# Common causes:
# - Database connection failures (check connection string)
# - Missing configuration (check ConfigMap/Secret)
# - Application startup errors (check Application Insights)
```

#### Health Check Failures

```bash
# Test health endpoint directly
kubectl port-forward <pod-name> 8080:80 -n blackslope
curl http://localhost:8080/health

# Check health check configuration
kubectl describe pod <pod-name> -n blackslope | grep -A 10 "Liveness\|Readiness"
```

#### Image Pull Errors

```bash
# Check image pull secrets
kubectl get secrets -n blackslope

# Create image pull secret for private registry
kubectl create secret docker-registry regcred \
  --docker-server=<registry-url> \
  --docker-username=<username> \
  --docker-password=<password> \
  --namespace=blackslope

# Reference in deployment
spec:
  imagePullSecrets:
  - name: regcred
```

## Related Documentation

- [Docker Deployment](/deployment/docker.md) - Container image building and local testing
- [Health Checks](/features/health_checks.md) - Detailed health check implementation and customization
- [Monitoring](/deployment/monitoring.md) - Application Insights integration and observability practices

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [ASP.NET Core Health Checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Azure Kubernetes Service (AKS) Best Practices](https://docs.microsoft.com/en-us/azure/aks/best-practices)
- [.NET 6.0 Deployment Guide](https://docs.microsoft.com/en-us/dotnet/core/deploying/)