# Design Document

## Overview

This design document describes the extraction of the NotificationService from the Contoso University monolithic application into a separate .NET 8 WebAPI microservice. The architecture follows a microservices pattern where the NotificationAPI handles all AWS SQS operations, while the ContosoUniversity application communicates with it via HTTP REST APIs. The NotificationService class in ContosoUniversity will be refactored to act as an HTTP client wrapper, maintaining the same interface for backward compatibility.

## Architecture

### High-Level Architecture

```
ContosoUniversity Application
    ↓
NotificationService (HTTP Client)
    ↓ (HTTP REST API)
NotificationAPI (WebAPI)
    ↓
NotificationService (SQS Operations)
    ↓
AWS SQS Queue
```

### Component Interaction Flow

**Sending Notifications:**
1. BaseController calls `SendNotification()` on NotificationService (in ContosoUniversity)
2. NotificationService makes HTTP POST to NotificationAPI `/api/notifications`
3. NotificationAPI controller receives request and calls its NotificationService
4. NotificationAPI NotificationService sends message to AWS SQS
5. Response returns through the chain

**Receiving Notifications:**
1. NotificationsController calls `ReceiveNotification()` on NotificationService (in ContosoUniversity)
2. NotificationService makes HTTP GET to NotificationAPI `/api/notifications`
3. NotificationAPI controller receives request and calls its NotificationService
4. NotificationAPI NotificationService retrieves message from AWS SQS
5. Notification object returns as JSON through the chain

## Components and Interfaces

### NotificationAPI Project Structure

```
NotificationAPI/
├── Controllers/
│   └── NotificationsController.cs
├── Services/
│   └── NotificationService.cs
├── Models/
│   └── Notification.cs
│   └── SendNotificationRequest.cs
├── Program.cs
├── appsettings.json
└── NotificationAPI.csproj
```

### NotificationAPI.Controllers.NotificationsController

**Purpose:** Exposes REST endpoints for notification operations

**Dependencies:**
- NotificationService (injected)

**Endpoints:**

```csharp
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    [HttpPost]
    public IActionResult SendNotification([FromBody] SendNotificationRequest request)
    {
        // Validate request
        // Call _notificationService.SendNotification()
        // Return 200 OK or 500 on error
    }

    [HttpGet]
    public IActionResult ReceiveNotification()
    {
        // Call _notificationService.ReceiveNotification()
        // Return 200 with notification or 204 if none available
        // Return 500 on error
    }
}
```

### NotificationAPI.Services.NotificationService

**Purpose:** Manages AWS SQS operations (moved from ContosoUniversity)

**Implementation:** This is the existing NotificationService with AWS SQS logic, moved to the NotificationAPI project with minimal changes.

**Key Methods:**
- `SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)`
- `SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)`
- `ReceiveNotification()` → Returns Notification or null
- `Dispose()`

### NotificationAPI.Models.SendNotificationRequest

**Purpose:** DTO for POST /api/notifications endpoint

```csharp
public class SendNotificationRequest
{
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string EntityDisplayName { get; set; }
    public string Operation { get; set; } // "CREATE", "UPDATE", "DELETE"
    public string UserName { get; set; }
}
```

### ContosoUniversity.Services.NotificationService (Refactored)

**Purpose:** HTTP client wrapper that maintains the same interface for backward compatibility

**Dependencies:**
- `HttpClient` (injected via IHttpClientFactory)
- `IConfiguration` (for reading NotificationAPI BaseUrl)

**Key Properties:**
- `_httpClient`: HttpClient instance
- `_baseUrl`: NotificationAPI base URL from configuration
- `_queueName`: Retained for compatibility (not used)

**Key Methods:**

```csharp
public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
{
    SendNotification(entityType, entityId, null, operation, userName);
}

public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
{
    // Create SendNotificationRequest object
    // Serialize to JSON
    // POST to {_baseUrl}/api/notifications
    // Use async/await with .GetAwaiter().GetResult() for sync interface
    // Wrap in try-catch, log errors, don't throw
}

public Notification ReceiveNotification()
{
    // GET from {_baseUrl}/api/notifications
    // If 200, deserialize and return Notification
    // If 204, return null
    // If error, log and return null
    // Use async/await with .GetAwaiter().GetResult() for sync interface
}

public void Dispose()
{
    // HttpClient is managed by IHttpClientFactory, no disposal needed
}
```

## Data Models

### Notification Model

The Notification model will exist in both projects with identical structure:

```csharp
public class Notification
{
    public int Id { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string Operation { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
```

### SendNotificationRequest Model

New DTO for the POST endpoint:

```csharp
public class SendNotificationRequest
{
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string EntityDisplayName { get; set; }
    public string Operation { get; set; }
    public string UserName { get; set; }
}
```

## Configuration

### NotificationAPI appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AWS": {
    "Region": "eu-west-1",
    "SQS": {
      "QueueUrl": "https://sqs.eu-west-1.amazonaws.com/969522832499/contoso-queue"
    }
  }
}
```

### NotificationAPI Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register NotificationService
builder.Services.AddSingleton<NotificationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowContosoUniversity", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowContosoUniversity");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### NotificationAPI launchSettings.json

Configure to run on port 5001:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### ContosoUniversity appsettings.json Updates

Add NotificationAPI configuration and remove AWS configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "NotificationAPI": {
    "BaseUrl": "http://localhost:5001"
  }
}
```

Remove the AWS section entirely from ContosoUniversity.

### ContosoUniversity Startup.cs / Program.cs Updates

```csharp
// Register HttpClient for NotificationService
builder.Services.AddHttpClient();

// Register NotificationService (now as HTTP client wrapper)
builder.Services.AddScoped<NotificationService>();
```

## Error Handling

### NotificationAPI Error Handling

1. **Controller Level:**
   - Wrap service calls in try-catch
   - Return 500 Internal Server Error with error message on exceptions
   - Return 204 No Content when no notifications available (not an error)

2. **Service Level:**
   - Existing SQS error handling remains (try-catch with logging)

### ContosoUniversity NotificationService Error Handling

1. **HTTP Communication Errors:**
   - Wrap all HTTP calls in try-catch
   - Log errors to Debug output
   - Return gracefully (don't throw exceptions)
   - For SendNotification: return void (fail silently)
   - For ReceiveNotification: return null

2. **Timeout Handling:**
   - Configure HttpClient with reasonable timeout (30 seconds)
   - Treat timeouts as errors, log and return gracefully

## Testing Strategy

### Manual Testing Approach

1. **NotificationAPI Testing:**
   - Start NotificationAPI on port 5001
   - Use Swagger UI to test POST /api/notifications
   - Use Swagger UI to test GET /api/notifications
   - Verify AWS SQS Console shows messages

2. **Integration Testing:**
   - Start NotificationAPI on port 5001
   - Start ContosoUniversity application
   - Perform CRUD operations on entities
   - Verify notifications appear in the Notifications dashboard
   - Check that notifications are sent through the API

3. **Error Scenario Testing:**
   - Stop NotificationAPI while ContosoUniversity is running
   - Perform entity operations
   - Verify ContosoUniversity continues to work (notifications fail gracefully)
   - Restart NotificationAPI and verify notifications work again

### Testing Checklist

- [ ] NotificationAPI runs on port 5001
- [ ] Swagger UI is accessible at http://localhost:5001/swagger
- [ ] POST /api/notifications successfully sends to SQS
- [ ] GET /api/notifications successfully retrieves from SQS
- [ ] ContosoUniversity can send notifications through API
- [ ] ContosoUniversity can receive notifications through API
- [ ] ContosoUniversity handles NotificationAPI being unavailable gracefully
- [ ] No code changes required in BaseController or NotificationsController

## Deployment Considerations

### Local Development

1. Run NotificationAPI: `dotnet run --project NotificationAPI`
2. Run ContosoUniversity: `dotnet run --project ContosoUniversity`
3. Ensure AWS credentials are configured for NotificationAPI

### Production Deployment

1. **NotificationAPI:**
   - Deploy as separate service (container, VM, or App Service)
   - Configure AWS credentials via environment variables or IAM roles
   - Ensure SQS permissions (SendMessage, ReceiveMessage, DeleteMessage)
   - Update appsettings.json with production SQS queue URL

2. **ContosoUniversity:**
   - Update appsettings.json with production NotificationAPI BaseUrl
   - Remove AWS SDK dependencies from ContosoUniversity.csproj
   - Ensure network connectivity to NotificationAPI

### Scaling Considerations

- NotificationAPI can be scaled independently
- Multiple instances of NotificationAPI can share the same SQS queue
- SQS handles message deduplication and ordering
- Consider adding health check endpoints for monitoring

## Migration Path

### Step-by-Step Migration

1. Create NotificationAPI project with SQS functionality
2. Test NotificationAPI independently using Swagger
3. Refactor ContosoUniversity NotificationService to use HTTP
4. Update ContosoUniversity configuration
5. Test integration between both services
6. Remove AWS SDK dependencies from ContosoUniversity
7. Deploy both services

### Rollback Plan

If issues occur:
1. Revert ContosoUniversity NotificationService to SQS implementation
2. Re-add AWS SDK dependencies to ContosoUniversity
3. Restore AWS configuration in ContosoUniversity appsettings.json
4. Stop NotificationAPI service

## Security Considerations

### API Security

- For production, add authentication (API keys, JWT tokens, or mutual TLS)
- Implement rate limiting to prevent abuse
- Use HTTPS for all communication
- Validate all input in NotificationAPI controllers

### CORS Configuration

- In production, restrict CORS to specific origins
- Replace `AllowAnyOrigin()` with specific ContosoUniversity URL

### AWS Credentials

- Use IAM roles instead of access keys when possible
- Never commit credentials to source control
- Use AWS Secrets Manager or environment variables for credentials
