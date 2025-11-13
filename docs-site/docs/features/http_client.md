# HTTP Client Patterns

This documentation covers the HTTP client patterns and practices implemented in the BlackSlope API, including the use of `HttpClientFactory`, decorator patterns, Polly resilience policies, and practical implementation examples.

## HttpClientFactory

The application leverages ASP.NET Core's `IHttpClientFactory` to manage HTTP client instances efficiently, avoiding common pitfalls such as socket exhaustion and DNS resolution issues.

### Named HTTP Clients

Named clients provide a way to configure and retrieve pre-configured `HttpClient` instances by name. This pattern is useful when you need multiple HTTP clients with different configurations.

**Configuration in Startup.cs:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Named client registration with decorator pattern
    services.AddHttpClient("movies", (provider, client) => 
        provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
}
```

**Key Benefits:**
- **Centralized Configuration**: All HTTP client settings are defined in one location
- **Dependency Injection**: Clients are resolved through the DI container
- **Lifetime Management**: The factory manages client lifetimes automatically (default 2 minutes)
- **Handler Pooling**: Connection handlers are pooled and reused efficiently

### Typed HTTP Clients

Typed clients encapsulate HTTP client logic within a dedicated class, providing a strongly-typed API for external service integration. This is the recommended approach for production applications.

**Implementation Example (FakeApiRepository):**

```csharp
using System.Net.Http;
using System.Threading.Tasks;

namespace BlackSlope.Repositories.FakeApi
{
    public class FakeApiRepository : IFakeApiRepository
    {
        private readonly HttpClient _httpClient;

        // HttpClient is injected by HttpClientFactory
        public FakeApiRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<dynamic> GetExponentialBackoff()
        {
            return await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
        }
    }
}
```

**Advantages of Typed Clients:**
- **Encapsulation**: HTTP logic is contained within repository classes
- **Testability**: Easy to mock for unit testing
- **Type Safety**: Compile-time checking of dependencies
- **Single Responsibility**: Each repository handles one external service

### HTTP Client Lifetime Management

The application configures custom handler lifetimes to optimize connection pooling and DNS resolution:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(3)); // 3 min request lifecycle
```

**Lifetime Considerations:**

| Setting | Default | Custom | Rationale |
|---------|---------|--------|-----------|
| Handler Lifetime | 2 minutes | 3 minutes | Balances DNS refresh with connection reuse |
| Client Lifetime | Transient | Transient | New instance per request, shared handler |
| Handler Pool Size | Unlimited | Unlimited | Managed by framework based on load |

**Important Notes:**
- Handlers are pooled and reused across multiple `HttpClient` instances
- DNS changes are respected after the handler lifetime expires
- Shorter lifetimes increase DNS lookup frequency but ensure fresher DNS resolution
- Longer lifetimes improve performance but may cache stale DNS entries

## HTTP Client Decorator

The decorator pattern is implemented to provide cross-cutting concerns such as base address configuration, default headers, and request/response interception.

### Decorator Pattern Implementation

**Interface Definition:**

```csharp
using System.Net.Http;

namespace BlackSlope.Api.Common.Services
{
    public interface IHttpClientDecorator
    {
        void Configure(HttpClient client);
    }
}
```

**Concrete Implementation:**

```csharp
using System;
using System.Net.Http;
using BlackSlope.Api.Common.Configuration;

namespace BlackSlope.Api.Common.Services
{
    public class HttpClientDecorator : IHttpClientDecorator
    {
        private readonly HostConfig _config;

        public HttpClientDecorator(HostConfig config)
        {
            _config = config;
        }

        public void Configure(HttpClient client)
        {
            client.BaseAddress = new Uri(_config.BaseUrl);
        }
    }
}
```

**Registration and Usage:**

```csharp
// Register decorator in DI container
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();

// Apply decorator to named client
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

### Extensibility

The decorator pattern can be extended to support additional configuration scenarios:

**Authentication Headers (Example Extension):**
```csharp
public void Configure(HttpClient client)
{
    client.BaseAddress = new Uri(_config.BaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiToken}");
    client.DefaultRequestHeaders.Add("X-API-Version", "1.0");
}
```

**Custom Timeouts (Example Extension):**
```csharp
public void Configure(HttpClient client)
{
    client.BaseAddress = new Uri(_config.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}
```

**Note:** The current implementation only sets the `BaseAddress`. Additional configuration such as headers, timeouts, or logging can be added as needed.

For more advanced request/response interception, consider implementing a `DelegatingHandler`. See [Dependency Injection](/architecture/dependency_injection.md) for handler registration patterns.

## Polly Integration

The application uses **Polly** (version 7.2.2) for implementing resilience and transient-fault-handling patterns. Polly provides retry policies, circuit breakers, timeouts, and bulkhead isolation.

### Retry Policies

Retry policies automatically retry failed HTTP requests based on configurable conditions and strategies.

**Exponential Backoff Implementation:**

```csharp
using System;
using BlackSlope.Repositories.FakeApi;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FakeApiRepositoryWithPollyServiceCollectionExtensions
    {
        public static IServiceCollection AddFakeApiRepository(
            this IServiceCollection services)
        {
            services.TryAddScoped<IFakeApiRepository, FakeApiRepository>();

            services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(3)) // 3 min request lifecycle
                .AddPolicyHandler(_ => // Retry x3 w/ Exponential Backoff
                    HttpPolicyExtensions.HandleTransientHttpError()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(2, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            return services;
        }
    }
}
```

**Retry Policy Breakdown:**

| Component | Configuration | Behavior |
|-----------|--------------|----------|
| Transient Errors | `HandleTransientHttpError()` | Handles 5xx and 408 status codes |
| Custom Conditions | `OrResult(msg => msg.StatusCode == NotFound)` | Also retries on 404 responses |
| Retry Count | `2` | Maximum of 2 retry attempts (3 total requests) |
| Backoff Strategy | `Math.Pow(2, retryAttempt)` | Exponential: 2s, 4s |

**Transient HTTP Errors Handled:**
- **5xx Server Errors**: Internal server errors, service unavailable, gateway timeout
- **408 Request Timeout**: Request took too long to complete
- **Network Failures**: Connection refused, DNS resolution failures, socket exceptions

### Circuit Breaker Patterns

While not currently implemented in the codebase, circuit breaker patterns are essential for preventing cascading failures. Here's a recommended implementation:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, duration) =>
            {
                // Log circuit breaker opened
            },
            onReset: () =>
            {
                // Log circuit breaker reset
            }));
```

**Circuit Breaker States:**
- **Closed**: Normal operation, requests flow through
- **Open**: Threshold exceeded, requests fail immediately
- **Half-Open**: Testing if service recovered, limited requests allowed

For comprehensive resilience patterns, see [Resilience Features](/features/resilience.md).

### Timeout Policies

Timeout policies ensure requests don't hang indefinitely:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));
```

**Timeout Strategy Recommendations:**

| Scenario | Timeout | Rationale |
|----------|---------|-----------|
| Fast APIs | 5-10 seconds | Quick response expected |
| Data Processing | 30-60 seconds | Complex operations allowed |
| File Downloads | 5+ minutes | Large payloads require time |
| Health Checks | 2-5 seconds | Fast failure detection |

### Exponential Backoff

Exponential backoff prevents overwhelming a recovering service by progressively increasing wait times between retries.

**Mathematical Progression:**

```
Attempt 1: Immediate (0s delay)
Attempt 2: 2^1 = 2 seconds delay
Attempt 3: 2^2 = 4 seconds delay
Total time: 6 seconds + request time
```

**Configuration Options:**

```csharp
// Simple exponential backoff
.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))

// Exponential backoff with jitter (recommended for distributed systems)
.WaitAndRetryAsync(3, retryAttempt => 
    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)))

// Capped exponential backoff
.WaitAndRetryAsync(5, retryAttempt => 
    TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt), 30)))
```

**Jitter Benefits:**
- Prevents "thundering herd" problem
- Distributes retry attempts over time
- Reduces load spikes on recovering services

## FakeAPI Repository Example

The `FakeApiRepository` demonstrates best practices for external API integration with resilience patterns.

### External API Integration

**Repository Interface:**

```csharp
public interface IFakeApiRepository
{
    Task<dynamic> GetExponentialBackoff();
}
```

**Repository Implementation:**

```csharp
public class FakeApiRepository : IFakeApiRepository
{
    private readonly HttpClient _httpClient;

    public FakeApiRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<dynamic> GetExponentialBackoff()
    {
        // HttpClient is pre-configured with Polly policies
        return await _httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos");
    }
}
```

**Key Design Decisions:**

1. **Dependency Injection**: `HttpClient` is injected, not instantiated
2. **Async/Await**: All HTTP operations are asynchronous
3. **No Try-Catch**: Polly policies handle transient failures
4. **Absolute URLs**: Full URL specified (no base address in this example)

### Resilience Patterns in Practice

The `FakeApiRepository` registration demonstrates a complete resilience strategy:

```csharp
public static IServiceCollection AddFakeApiRepository(this IServiceCollection services)
{
    // Register repository with scoped lifetime
    services.TryAddScoped<IFakeApiRepository, FakeApiRepository>();

    // Configure typed HttpClient with resilience policies
    services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(3)) // 3 min request lifecycle
        .AddPolicyHandler(_ => // Retry x3 w/ Exponential Backoff
            HttpPolicyExtensions.HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(2, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

    return services;
}
```

**Resilience Strategy Breakdown:**

| Layer | Implementation | Purpose |
|-------|----------------|---------|
| Connection Pooling | `SetHandlerLifetime(3 min)` | Efficient socket reuse |
| Transient Fault Handling | `HandleTransientHttpError()` | Automatic retry on 5xx/408 |
| Custom Error Handling | `OrResult(404)` | Retry on specific status codes |
| Backoff Strategy | Exponential (2s, 4s) | Progressive delay between retries |

**Failure Scenario Example:**

```
Request 1: 500 Internal Server Error → Wait 2 seconds
Request 2: 503 Service Unavailable → Wait 4 seconds  
Request 3: 200 OK → Success
Total time: ~6 seconds + request times
```

### Configuration and Registration

**Service Registration in Startup.cs:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... other services ...
    
    // Register FakeApiRepository with Polly policies
    services.AddFakeApiRepository();
    
    // ... other services ...
}
```

**Extension Method Pattern Benefits:**

- **Encapsulation**: All repository configuration in one place
- **Reusability**: Easy to apply same pattern to other repositories
- **Testability**: Can be mocked or replaced in test environments
- **Maintainability**: Changes isolated to extension method

**Configuration Best Practices:**

1. **Use Extension Methods**: Keep `Startup.cs` clean and focused
2. **TryAdd Methods**: Prevent duplicate registrations (`TryAddScoped`)
3. **Appropriate Lifetimes**: Scoped for repositories, Transient for decorators
4. **Policy Composition**: Chain multiple policies for comprehensive resilience
5. **Configuration-Driven**: Extract retry counts, timeouts to `appsettings.json`

**Advanced Configuration Example:**

```csharp
public static IServiceCollection AddFakeApiRepository(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var retryConfig = configuration.GetSection("FakeApi:Retry");
    var retryCount = retryConfig.GetValue<int>("Count", 2);
    var baseDelay = retryConfig.GetValue<int>("BaseDelaySeconds", 2);

    services.TryAddScoped<IFakeApiRepository, FakeApiRepository>();

    services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(3))
        .AddPolicyHandler(_ => 
            HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(baseDelay, retryAttempt))));

    return services;
}
```

**Corresponding appsettings.json:**

```json
{
  "FakeApi": {
    "Retry": {
      "Count": 3,
      "BaseDelaySeconds": 2
    }
  }
}
```

For more information on service configuration patterns, see [Service Configuration](/configuration/service_configuration.md).

## Common Pitfalls and Gotchas

### HttpClient Disposal

**❌ Anti-Pattern:**
```csharp
// DO NOT DO THIS - causes socket exhaustion
using (var client = new HttpClient())
{
    var response = await client.GetAsync("https://api.example.com");
}
```

**✅ Correct Pattern:**
```csharp
// Use IHttpClientFactory - handles lifecycle automatically
public class MyRepository
{
    private readonly HttpClient _httpClient;
    
    public MyRepository(HttpClient httpClient)
    {
        _httpClient = httpClient; // Injected by factory
    }
}
```

### Base Address Configuration

**⚠️ Warning:** When using `BaseAddress`, relative URLs must not start with `/`:

```csharp
// Correct
client.BaseAddress = new Uri("https://api.example.com/");
await client.GetAsync("todos"); // → https://api.example.com/todos

// Incorrect - leading slash replaces entire path
await client.GetAsync("/todos"); // → https://api.example.com/todos (works but inconsistent)
```

### Polly Policy Ordering

Policy order matters when chaining multiple policies:

```csharp
// Correct order: Timeout → Retry → Circuit Breaker
services.AddHttpClient<IMyService, MyService>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(10))  // Innermost
    .AddPolicyHandler(GetRetryPolicy())                               // Middle
    .AddPolicyHandler(GetCircuitBreakerPolicy());                     // Outermost
```

**Rationale:** Timeout should be innermost to apply per-attempt, not across all retries.

### Async/Await Best Practices

```csharp
// ❌ Avoid - blocks thread
public dynamic GetData()
{
    return _httpClient.GetAsync("url").Result; // Deadlock risk
}

// ✅ Correct - fully async
public async Task<dynamic> GetDataAsync()
{
    return await _httpClient.GetAsync("url");
}
```

## Related Documentation

- [Dependency Injection Architecture](/architecture/dependency_injection.md) - DI patterns and service lifetimes
- [Resilience Features](/features/resilience.md) - Comprehensive resilience strategies
- [Service Configuration](/configuration/service_configuration.md) - Configuration management patterns