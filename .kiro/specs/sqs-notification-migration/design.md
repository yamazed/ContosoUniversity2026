# Design Document

## Overview

This design document describes the migration of the Contoso University notification system from an in-memory queue to AWS Simple Queue Service (SQS). The design maintains the existing NotificationService interface while replacing the underlying queue implementation with AWS SQS. The solution uses the AWS SDK for .NET to send and receive messages, with configuration stored in appsettings.json.

## Architecture

### High-Level Architecture

```
Controllers (BaseController)
    ↓
NotificationService
    ↓
AWS SDK for .NET (AWSSDK.SQS)
    ↓
AWS SQS Queue (contoso-queue)
```

### Component Interaction Flow

**Sending Notifications:**
1. Controller calls `SendEntityNotification()` on BaseController
2. BaseController calls `SendNotification()` on NotificationService
3. NotificationService serializes Notification object to JSON
4. NotificationService uses AmazonSQSClient to send message to SQS
5. AWS SQS stores the message in the queue

**Receiving Notifications:**
1. NotificationsController calls `ReceiveNotification()` on NotificationService
2. NotificationService uses AmazonSQSClient to receive message from SQS
3. NotificationService deserializes JSON to Notification object
4. NotificationService deletes the message from SQS
5. NotificationService returns Notification object to controller

## Components and Interfaces

### NotificationService

**Purpose:** Manages sending and receiving notifications via AWS SQS

**Dependencies:**
- `Amazon.SQS` (AmazonSQSClient)
- `Microsoft.Extensions.Configuration` (IConfiguration)
- `Newtonsoft.Json` (JSON serialization)

**Key Properties:**
- `_sqsClient`: AmazonSQSClient instance for SQS operations
- `_queueUrl`: String containing the SQS queue URL
- `_queueName`: String for logging/debugging purposes (retained for compatibility)

**Key Methods:**

```csharp
public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
public Notification ReceiveNotification()
public void Dispose()
```

**Implementation Details:**

1. **Constructor:**
   - Read AWS region from configuration (`AWS:Region`)
   - Read queue URL from configuration (`AWS:SQS:QueueUrl`)
   - Create AmazonSQSClient with RegionEndpoint
   - Store queue URL for operations

2. **SendNotification:**
   - Create Notification object with provided parameters
   - Serialize Notification to JSON using JsonConvert
   - Create SendMessageRequest with queue URL and JSON body
   - Call SendMessageAsync on SQS client
   - Use async/await pattern with .GetAwaiter().GetResult() for synchronous interface

3. **ReceiveNotification:**
   - Create ReceiveMessageRequest with queue URL
   - Set MaxNumberOfMessages to 1
   - Set WaitTimeSeconds to 0 (short polling for compatibility)
   - Call ReceiveMessageAsync on SQS client
   - If messages received, deserialize first message body to Notification
   - Delete message from queue using DeleteMessageAsync
   - Return Notification object or null if no messages

4. **Dispose:**
   - Dispose of AmazonSQSClient instance

### Configuration Structure

**appsettings.json additions:**

```json
{
  "AWS": {
    "Region": "eu-west-1",
    "SQS": {
      "QueueUrl": "https://sqs.eu-west-1.amazonaws.com/969522832499/contoso-queue"
    }
  },
  "NotificationQueuePath": "ContosoUniversityNotifications"
}
```

Note: `NotificationQueuePath` is retained for backward compatibility but not used by SQS implementation.

### NuGet Package Dependencies

**Required packages:**
- `AWSSDK.SQS` (latest stable version compatible with .NET 8.0)
- `AWSSDK.Core` (dependency of AWSSDK.SQS, will be installed automatically)

**Existing packages to retain:**
- `Newtonsoft.Json` (already in project for JSON serialization)

## Data Models

### Notification Model

No changes required to the existing Notification model. The model already contains all necessary properties:

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

### SQS Message Format

Messages sent to SQS will be JSON-serialized Notification objects:

```json
{
  "Id": 0,
  "EntityType": "Student",
  "EntityId": "123",
  "Operation": "CREATE",
  "Message": "New Student 'John Doe' has been created",
  "CreatedAt": "2025-11-06T10:30:00",
  "CreatedBy": "System",
  "IsRead": false,
  "ReadAt": null
}
```

## Error Handling

Per requirements, extensive error handling is not needed. Basic error handling approach:

1. **SendNotification:** Wrap SQS operations in try-catch, log to Debug output, don't throw exceptions (maintains existing behavior)
2. **ReceiveNotification:** Wrap SQS operations in try-catch, return null on error (maintains existing behavior)
3. **Configuration errors:** Allow exceptions to propagate during service initialization if configuration is missing

This minimal approach ensures the main application operations are not disrupted by notification failures.

## Testing Strategy

Per requirements, unit tests are not needed. Testing approach:

1. **Manual testing:** Use the existing Notifications dashboard to verify notifications are sent and received
2. **AWS Console verification:** Check SQS queue in AWS Console to confirm messages are being sent
3. **Integration testing:** Perform CRUD operations on entities and verify notifications appear in the UI

### Testing Checklist

- [ ] Create a student and verify notification appears
- [ ] Update a course and verify notification appears
- [ ] Delete an instructor and verify notification appears
- [ ] Check AWS SQS Console to confirm messages are processed
- [ ] Verify no messages remain in queue after retrieval

## AWS Credentials

The implementation will use the AWS SDK's default credential provider chain, which checks credentials in this order:

1. Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)
2. AWS credentials file (~/.aws/credentials)
3. IAM role for EC2 instances (if deployed on EC2)
4. ECS task role (if deployed on ECS)

For local development, developers should configure credentials using AWS CLI or environment variables.

## Migration Considerations

### Backward Compatibility

- Method signatures remain unchanged
- Configuration key `NotificationQueuePath` retained but unused
- Existing controllers and notification flow unchanged

### Deployment Notes

1. Ensure AWS credentials are configured in the deployment environment
2. Update appsettings.json with correct AWS region and queue URL
3. Verify IAM permissions for SQS operations (SendMessage, ReceiveMessage, DeleteMessage)
4. No database migrations required (Notification model unchanged)

### Rollback Plan

If issues occur, revert to previous in-memory implementation by:
1. Restoring previous NotificationService.cs file
2. Removing AWS configuration from appsettings.json
3. Uninstalling AWSSDK.SQS package (optional)
