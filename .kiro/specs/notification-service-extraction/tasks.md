# Implementation Plan

- [x] 1. Create NotificationAPI WebAPI project structure
  - Create new .NET 8 WebAPI project named "NotificationAPI" at the same level as ContosoUniversity
  - Configure project to run on port 5001 in launchSettings.json
  - Add Swagger/OpenAPI support for API testing
  - _Requirements: 1.1, 1.2, 5.5_

- [x] 2. Set up NotificationAPI models and dependencies
  - [x] 2.1 Copy Notification model to NotificationAPI/Models
    - Copy Notification.cs from ContosoUniversity to NotificationAPI/Models
    - Ensure all properties are preserved
    - _Requirements: 1.4_
  
  - [x] 2.2 Create SendNotificationRequest DTO
    - Create SendNotificationRequest.cs in NotificationAPI/Models
    - Add properties: EntityType, EntityId, EntityDisplayName, Operation, UserName
    - _Requirements: 2.2_
  
  - [x] 2.3 Add AWS SDK NuGet packages
    - Install AWSSDK.SQS package to NotificationAPI project
    - Install AWSSDK.Core package to NotificationAPI project
    - Install Newtonsoft.Json for JSON serialization
    - _Requirements: 1.5_

- [x] 3. Implement NotificationAPI service layer
  - [x] 3.1 Copy NotificationService to NotificationAPI
    - Copy NotificationService.cs from ContosoUniversity/Services to NotificationAPI/Services
    - Ensure AWS SQS functionality is preserved
    - Verify Dispose method is included
    - _Requirements: 1.3, 1.5_
  
  - [x] 3.2 Configure NotificationAPI appsettings.json
    - Add AWS section with Region set to "eu-west-1"
    - Add SQS QueueUrl configuration
    - Configure logging settings
    - _Requirements: 7.1, 7.2_

- [x] 4. Implement NotificationAPI REST endpoints
  - [x] 4.1 Create NotificationsController
    - Create NotificationsController.cs in NotificationAPI/Controllers
    - Add ApiController and Route attributes
    - Inject NotificationService via constructor
    - _Requirements: 2.1, 3.1_
  
  - [x] 4.2 Implement POST /api/notifications endpoint
    - Create POST action method accepting SendNotificationRequest
    - Validate request body
    - Call NotificationService.SendNotification with request data
    - Return 200 OK on success, 500 on error with error message
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  
  - [x] 4.3 Implement GET /api/notifications endpoint
    - Create GET action method
    - Call NotificationService.ReceiveNotification
    - Return 200 OK with Notification JSON when available
    - Return 204 No Content when no notifications available
    - Return 500 on error with error message
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 5. Configure NotificationAPI dependency injection and middleware
  - Register NotificationService as singleton in Program.cs
  - Configure CORS to allow requests from ContosoUniversity
  - Add Swagger middleware for development environment
  - Configure controllers and routing
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 6. Refactor ContosoUniversity NotificationService to use HTTP client
  - [x] 6.1 Update ContosoUniversity appsettings.json
    - Add NotificationAPI section with BaseUrl set to "http://localhost:5001"
    - Remove AWS section entirely
    - Preserve all other existing configuration
    - _Requirements: 5.1, 7.3, 7.4, 7.5_
  
  - [x] 6.2 Register HttpClient in ContosoUniversity
    - Add HttpClient registration in Program.cs or Startup.cs
    - Ensure IHttpClientFactory is available
    - _Requirements: 4.1_
  
  - [x] 6.3 Refactor NotificationService to use HttpClient
    - Replace AWS SQS client with HttpClient
    - Update constructor to inject IHttpClientFactory and IConfiguration
    - Read NotificationAPI BaseUrl from configuration with default "http://localhost:5001"
    - Remove all AWS SDK and SQS-related code
    - _Requirements: 4.1, 4.2, 4.5, 5.2, 5.3, 5.4_
  
  - [x] 6.4 Implement SendNotification methods with HTTP POST
    - Create SendNotificationRequest object from parameters
    - Serialize to JSON
    - POST to {BaseUrl}/api/notifications
    - Handle errors gracefully (log and don't throw)
    - Maintain synchronous interface using .GetAwaiter().GetResult()
    - _Requirements: 4.2, 4.3, 8.3, 8.4_
  
  - [x] 6.5 Implement ReceiveNotification method with HTTP GET
    - GET from {BaseUrl}/api/notifications
    - Deserialize JSON response to Notification object on 200 status
    - Return null on 204 No Content status
    - Handle errors gracefully (log and return null)
    - Maintain synchronous interface using .GetAwaiter().GetResult()
    - _Requirements: 4.2, 4.4, 8.3, 8.4_
  
  - [x] 6.6 Update Dispose method
    - Remove AWS SQS client disposal
    - HttpClient is managed by IHttpClientFactory, no disposal needed
    - _Requirements: 4.1_

- [x] 7. Remove AWS dependencies from ContosoUniversity
  - Remove AWSSDK.SQS NuGet package from ContosoUniversity.csproj
  - Remove AWSSDK.Core NuGet package from ContosoUniversity.csproj
  - Verify project builds successfully without AWS SDK
  - _Requirements: 4.5_

- [x] 8. Verify integration and backward compatibility
  - Verify BaseController continues to work without code changes
  - Verify NotificationsController continues to work without code changes
  - Confirm no changes needed to any controllers or other consumers
  - Test that notifications flow through the new architecture
  - _Requirements: 8.1, 8.2, 8.5_
