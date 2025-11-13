# Resilience with Polly

## Polly Overview

Polly is a .NET resilience and transient-fault-handling library that enables developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner. In the BlackSlope.Api application, Polly is integrated with the `HttpClient` infrastructure to provide robust handling of transient failures when making outbound HTTP requests.

### Transient Fault Handling

Transient faults are temporary errors that often resolve themselves if the operation is retried. Common examples include:

- **Network connectivity issues**: Brief network interruptions or DNS resolution failures
- **Service unavailability**: Temporary overload or restart of downstream services
- **Timeout errors**: Requests that exceed time limits due to temporary resource constraints
- **HTTP 5xx errors**: Server-side errors that may be resolved on retry
- **HTTP 429 (Too Many Requests)**: Rate limiting that may clear after a brief delay

The BlackSlope.Api application uses Polly to automatically handle these scenarios without requiring manual retry logic in business code.

### Resilience Strategies

Polly provides several resilience strategies that can be combined to create sophisticated fault-handling behaviors:

| Strategy | Purpose | Use Case |
|----------|---------|----------|
| **Retry** | Automatically retry failed operations | Handle transient network failures |
| **Circuit Breaker** | Prevent cascading failures by stopping requests to failing services | Protect system resources when downstream service is down |
| **Timeout** | Limit operation duration | Prevent indefinite waits and resource exhaustion |
| **Fallback** | Provide alternative response when operation fails | Return cached data or default values |
| **Bulkhead Isolation** | Limit concurrent operations | Prevent resource exhaustion from too many parallel requests |

### Policy Composition

Polly policies can be composed together using the `Wrap` method or by chaining multiple `AddPolicyHandler` calls. The BlackSlope.Api application demonstrates policy composition in the `FakeApiRepository` configuration, where multiple policies work together to provide comprehensive resilience.

**Policy Execution Order**: When policies are chained, they execute in the order they are added, with each policy wrapping the next. For example:
```
Retry → Circuit Breaker → Timeout → HTTP Request
```

## Retry Policies

Retry policies automatically re-execute failed operations according to configured rules. The BlackSlope.Api application implements retry policies with exponential backoff for HTTP client operations.

### Simple Retry

A basic retry policy attempts the operation a fixed number of times with no delay between attempts:

```csharp
// Simple retry - 3 attempts with no delay
.AddPolicyHandler(
    HttpPolicyExtensions.HandleTransientHttpError()
        .RetryAsync(3)
)
```

**Note**: Simple retries without delays can overwhelm already-stressed services and should generally be avoided in production scenarios.

### Exponential Backoff

The recommended approach is exponential backoff, which increases the delay between retry attempts exponentially. This gives failing services time to recover and reduces the load on downstream systems.

The `FakeApiRepository` demonstrates this pattern:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(3)) // 3 min request lifecycle
    .AddPolicyHandler(_ => // Retry x3 w/ Exponential Backoff
        HttpPolicyExtensions.HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(2, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

**Breakdown of the Configuration**:

1. **`HandleTransientHttpError()`**: Handles HTTP 5xx errors and `HttpRequestException` (network failures)
2. **`OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)`**: Additionally treats HTTP 404 responses as retriable errors
3. **`WaitAndRetryAsync(2, ...)`**: Performs 2 retry attempts (3 total attempts including the initial request)
4. **`TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))`**: Calculates exponential backoff delays:
   - 1st retry: 2^1 = 2 seconds
   - 2nd retry: 2^2 = 4 seconds

### Retry Configuration Best Practices

**Determining Retry Count**:
- **Low retry counts (1-3)**: Suitable for most scenarios; prevents excessive delays
- **Higher retry counts (4-6)**: Use only when operations are idempotent and delays are acceptable
- **Consider total time**: With exponential backoff, delays grow quickly (2s, 4s, 8s, 16s, etc.)

**Handling Specific Status Codes**:
```csharp
.OrResult(msg => 
    msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
    msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
    msg.StatusCode == (System.Net.HttpStatusCode)429) // Too Many Requests
```

**Adding Jitter**: To prevent "retry storms" where multiple clients retry simultaneously, add randomization:
```csharp
.WaitAndRetryAsync(3, retryAttempt => 
    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) 
    + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)))
```

## Circuit Breaker

The Circuit Breaker pattern prevents an application from repeatedly attempting operations that are likely to fail, allowing the system to recover and preventing cascading failures.

### Circuit Breaker Pattern

The circuit breaker operates in three states:

```
┌─────────┐  Failure threshold exceeded  ┌──────┐
│ Closed  │─────────────────────────────>│ Open │
│(Normal) │                               │(Fast│
└─────────┘                               │Fail)│
     ^                                    └──────┘
     │                                       │
     │                                       │ Timeout expires
     │                                       v
     │                                    ┌──────────┐
     │  Success threshold met             │Half-Open │
     └────────────────────────────────────│(Testing) │
                                          └──────────┘
```

**State Descriptions**:

| State | Behavior | Transition Condition |
|-------|----------|---------------------|
| **Closed** | Requests pass through normally; failures are counted | After N consecutive failures or X% failure rate, transition to Open |
| **Open** | All requests fail immediately without attempting the operation | After timeout period, transition to Half-Open |
| **Half-Open** | Limited number of test requests are allowed through | If test requests succeed, transition to Closed; if they fail, return to Open |

### State Transitions

**Example Circuit Breaker Configuration**:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(
        HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,  // Open after 5 consecutive failures
                durationOfBreak: TimeSpan.FromSeconds(30) // Stay open for 30 seconds
            )
    );
```

**State Transition Example**:
1. **Closed State**: Service is healthy, requests succeed
2. **Failure Accumulation**: 5 consecutive requests fail (network timeout, 500 errors, etc.)
3. **Open State**: Circuit opens, all requests fail immediately with `BrokenCircuitException`
4. **Recovery Period**: After 30 seconds, circuit transitions to Half-Open
5. **Test Request**: Next request is allowed through to test service health
6. **Success**: If test succeeds, circuit closes and normal operation resumes
7. **Failure**: If test fails, circuit reopens for another 30 seconds

### Configuration

**Advanced Circuit Breaker with Retry**:

```csharp
// Define circuit breaker policy
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
        {
            // Log circuit breaker opening
            Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds}s");
        },
        onReset: () =>
        {
            // Log circuit breaker closing
            Console.WriteLine("Circuit breaker reset");
        },
        onHalfOpen: () =>
        {
            // Log circuit breaker testing
            Console.WriteLine("Circuit breaker half-open, testing service");
        }
    );

// Define retry policy
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Combine policies: Retry wraps Circuit Breaker
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
```

**Important Considerations**:

- **Policy Order Matters**: Place retry policy before circuit breaker to retry before breaking the circuit
- **Shared vs. Instance Policies**: Circuit breaker state is shared across all requests using the same policy instance
- **Monitoring**: Implement logging in `onBreak`, `onReset`, and `onHalfOpen` callbacks for observability
- **Failure Threshold**: Set `handledEventsAllowedBeforeBreaking` based on expected failure rates and service SLAs

## Timeout Policies

Timeout policies prevent operations from running indefinitely, protecting system resources and providing predictable response times.

### Optimistic Timeouts

Optimistic timeouts allow the operation to complete but cancel the `CancellationToken` after the timeout period. The operation may continue in the background but the caller receives a timeout exception.

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(10),
        timeoutStrategy: TimeoutStrategy.Optimistic
    ));
```

**Characteristics**:
- **Cooperative Cancellation**: Relies on the operation respecting the `CancellationToken`
- **Lower Overhead**: Doesn't create additional threads
- **Best For**: Operations that properly handle cancellation tokens

**Gotcha**: If the underlying operation doesn't respect cancellation tokens, it will continue consuming resources even after timeout.

### Pessimistic Timeouts

Pessimistic timeouts forcefully terminate the operation by abandoning the task after the timeout period, regardless of whether it respects cancellation.

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(10),
        timeoutStrategy: TimeoutStrategy.Pessimistic
    ));
```

**Characteristics**:
- **Forceful Termination**: Abandons the task after timeout
- **Higher Overhead**: May create additional threads for monitoring
- **Best For**: Operations that may not respect cancellation tokens

**Warning**: Abandoned tasks continue to consume resources until they complete or fail. Use pessimistic timeouts sparingly.

### Timeout Configuration Best Practices

**Combining Timeout with Retry**:

```csharp
// Per-request timeout
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(10));

// Retry with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutRejectedException>() // Handle timeout exceptions
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Overall timeout for all retries
var overallTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(30));

// Apply policies: Overall Timeout → Retry → Per-Request Timeout
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(overallTimeoutPolicy)
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(timeoutPolicy);
```

**Timeout Values**:
- **Per-Request Timeout**: Should be slightly longer than expected response time (e.g., 10s for 5s average)
- **Overall Timeout**: Should account for all retries and delays (e.g., 30s for 3 retries with exponential backoff)
- **Handler Lifetime**: Set via `SetHandlerLifetime()` to control connection pooling (default: 2 minutes)

## Testing Resilience

The BlackSlope.Api application provides a dedicated endpoint for testing Polly resilience policies in action.

### Exponential Backoff Endpoint

The `MoviesController` includes a test endpoint that demonstrates the exponential backoff retry policy:

```csharp
/// <summary>
/// Invoke Http Test with Polly Exponential Backoff
/// </summary>
/// <response code="200">Success.</response>
/// <response code="400">Bad Request</response>
/// <response code="401">Unauthorized</response>
/// <response code="500">Internal Server Error</response>
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpGet]
[Route("api/v1/movies/httpExponentialBackoffTest")]
public async Task<ActionResult> GetExponentialBackoff()
{
    await _movieService.GetExponentialBackoff();

    // 200 response
    return HandleSuccessResponse(null);
}
```

**Testing the Endpoint**:

```bash
# Test exponential backoff behavior
curl -X GET "https://localhost:51385/api/v1/movies/httpExponentialBackoffTest" \
     -H "accept: */*"
```

### Simulating Failures

To effectively test resilience policies, you need to simulate various failure scenarios:

**1. Network Failures**:
```csharp
// In your test repository, throw HttpRequestException
public async Task<string> GetDataAsync()
{
    throw new HttpRequestException("Simulated network failure");
}
```

**2. HTTP Error Responses**:
```csharp
// Return specific status codes
return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
{
    Content = new StringContent("Service temporarily unavailable")
};
```

**3. Timeouts**:
```csharp
// Simulate slow response
await Task.Delay(TimeSpan.FromSeconds(15));
return new HttpResponseMessage(HttpStatusCode.OK);
```

**4. Intermittent Failures**:
```csharp
private int _requestCount = 0;

public async Task<HttpResponseMessage> GetDataAsync()
{
    _requestCount++;
    
    // Fail first 2 requests, succeed on 3rd
    if (_requestCount < 3)
    {
        throw new HttpRequestException("Simulated failure");
    }
    
    return new HttpResponseMessage(HttpStatusCode.OK);
}
```

### Verifying Policies

**Logging Retry Attempts**:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler((services, request) =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = services.GetService<ILogger<FakeApiRepository>>();
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms due to {Exception}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()
                    );
                }
            )
    );
```

**Integration Test Example**:

```csharp
[Fact]
public async Task GetExponentialBackoff_WithTransientFailures_RetriesAndSucceeds()
{
    // Arrange
    var mockHandler = new MockHttpMessageHandler();
    mockHandler.SetupSequence()
        .ReturnsResponse(HttpStatusCode.ServiceUnavailable) // 1st attempt fails
        .ReturnsResponse(HttpStatusCode.ServiceUnavailable) // 2nd attempt fails
        .ReturnsResponse(HttpStatusCode.OK);                // 3rd attempt succeeds
    
    var httpClient = new HttpClient(mockHandler);
    var repository = new FakeApiRepository(httpClient);
    
    // Act
    var result = await repository.GetDataAsync();
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(3, mockHandler.RequestCount); // Verify 3 attempts were made
}
```

**Monitoring in Production**:

For production environments, integrate with Application Insights or similar monitoring tools:

```csharp
.AddPolicyHandler((services, request) =>
{
    var telemetryClient = services.GetService<TelemetryClient>();
    
    return HttpPolicyExtensions.HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                telemetryClient?.TrackEvent("HttpRetry", new Dictionary<string, string>
                {
                    ["RetryCount"] = retryCount.ToString(),
                    ["Delay"] = timespan.TotalMilliseconds.ToString(),
                    ["Endpoint"] = request.RequestUri?.ToString(),
                    ["StatusCode"] = outcome.Result?.StatusCode.ToString()
                });
            }
        );
});
```

## Integration with HttpClientFactory

The BlackSlope.Api application leverages ASP.NET Core's `HttpClientFactory` for managing `HttpClient` instances with Polly policies. This integration is configured in `Startup.cs`:

```csharp
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

**Benefits of HttpClientFactory Integration**:

1. **Automatic Lifetime Management**: Prevents socket exhaustion by properly managing `HttpClient` lifetimes
2. **Policy Reuse**: Polly policies are applied consistently across all requests
3. **Dependency Injection**: Typed clients can be injected with policies pre-configured
4. **Handler Pipeline**: Allows composition of multiple delegating handlers

**Handler Lifetime Configuration**:

```csharp
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(3)); // Recycle handlers every 3 minutes
```

The default handler lifetime is 2 minutes. Adjust based on:
- **DNS changes**: Shorter lifetimes ensure DNS changes are picked up
- **Connection pooling**: Longer lifetimes improve performance by reusing connections
- **Memory usage**: Shorter lifetimes reduce memory footprint

## Related Documentation

For additional information on related topics, see:

- [HTTP Client Configuration](/features/http_client.md) - Detailed HttpClient setup and configuration
- [Integration Tests](/testing/integration_tests.md) - Testing strategies for resilience policies
- [Production Best Practices](/deployment/production_best_practices.md) - Deployment considerations for resilient applications

## Summary

Polly provides comprehensive resilience capabilities for the BlackSlope.Api application:

- **Retry policies** with exponential backoff handle transient failures gracefully
- **Circuit breakers** prevent cascading failures and allow systems to recover
- **Timeout policies** protect resources and provide predictable response times
- **Policy composition** enables sophisticated fault-handling strategies
- **HttpClientFactory integration** ensures consistent policy application across all HTTP requests

By implementing these patterns, the BlackSlope.Api application maintains high availability and gracefully handles the inevitable failures that occur in distributed systems.