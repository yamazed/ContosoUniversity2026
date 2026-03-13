# Notification System Fix Bugfix Design

## Overview

The notification system is failing to persist and display notifications due to an incorrect SQS queue name configuration. The NotificationAPI is configured to connect to "contoso-queue" (which does not exist) instead of the actual queue "contoso-notifications" that was provisioned by the CDK infrastructure. This causes SQS permission errors when attempting to read from or write to the queue, preventing notifications from being stored in the database and displayed to users. The fix requires updating a single configuration value in appsettings.json to point to the correct queue URL.

## Glossary

- **Bug_Condition (C)**: The condition that triggers the bug - when the NotificationAPI attempts to interact with SQS using the incorrect queue name "contoso-queue"
- **Property (P)**: The desired behavior - notifications should be successfully persisted to the database and retrievable via the API
- **Preservation**: Existing HTTP endpoint behavior, request/response handling, and database connectivity that must remain unchanged by the fix
- **NotificationService**: The service class in `NotificationAPI/Services/NotificationService.cs` that handles sending and receiving notifications via AWS SQS
- **_queueUrl**: The configuration value read from appsettings.json that specifies the SQS queue endpoint
- **ReceiveNotification()**: The method in NotificationService that polls SQS for messages and deserializes them into Notification objects
- **SendNotification()**: The method in NotificationService that serializes notifications and sends them to SQS

## Bug Details

### Bug Condition

The bug manifests when the NotificationAPI attempts to interact with AWS SQS (either sending or receiving messages). The NotificationService reads the queue URL from configuration and uses it for all SQS operations, but the configured URL points to a non-existent queue, causing permission errors.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type SQSOperation (SendMessage or ReceiveMessage)
  OUTPUT: boolean
  
  RETURN input.queueUrl == "https://sqs.us-east-1.amazonaws.com/981461568039/contoso-queue"
         AND actualProvisionedQueue == "https://sqs.us-east-1.amazonaws.com/981461568039/contoso-notifications"
         AND input.operation IN [SendMessage, ReceiveMessage]
END FUNCTION
```

### Examples

- **POST /api/notifications with valid notification data**: Expected to persist notification to database and return 200 OK, but actually fails with SQS permission error and returns 500 Internal Server Error
- **GET /api/notifications when notifications exist in queue**: Expected to return 200 OK with notification data, but actually returns 204 No Content because ReceiveNotification() fails to read from the incorrect queue
- **User creates a new student in Contoso API**: Expected to see notification on notifications page, but actually no notification appears because NotificationAPI cannot persist it
- **Edge case - Multiple rapid notification requests**: Expected to queue all notifications successfully, but actually all fail due to queue name mismatch

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- HTTP POST requests to /api/notifications must continue to accept the same request body format (SendNotificationRequest)
- HTTP GET requests to /api/notifications must continue to return the same response format (Notification object or 204 No Content)
- The NotificationAPI must continue to listen on port 8080 and accept requests from the Contoso API
- Database connectivity and schema must remain unchanged
- Request validation logic (checking for required fields) must remain unchanged
- Logging behavior and log message formats must remain unchanged
- CORS policy allowing requests from ContosoUniversity must remain unchanged

**Scope:**
All inputs that do NOT involve SQS queue operations should be completely unaffected by this fix. This includes:
- HTTP request routing and controller method invocation
- Request body validation and error responses
- Logging and debugging output
- Service registration and dependency injection
- AWS credential loading and region configuration

## Hypothesized Root Cause

Based on the bug description and code analysis, the root cause is:

1. **Configuration Mismatch**: The appsettings.json file contains the incorrect queue URL
   - Configured: `https://sqs.us-east-1.amazonaws.com/981461568039/contoso-queue`
   - Actual queue: `https://sqs.us-east-1.amazonaws.com/981461568039/contoso-notifications`
   - The queue name "contoso-queue" does not exist in the AWS account

2. **Hardcoded Queue Name**: The configuration was likely set during development or migration and never updated to match the actual provisioned infrastructure

3. **CDK Infrastructure Mismatch**: The CDK code in `ContosoUniversityCdk/Constructs/MessagingConstruct.cs` provisions a queue named "contoso-notifications", but the NotificationAPI configuration was not updated to reflect this

4. **No Runtime Validation**: The NotificationService constructor does not validate that the queue exists, so the misconfiguration is only discovered when SQS operations are attempted, resulting in permission errors

## Correctness Properties

Property 1: Bug Condition - SQS Operations Use Correct Queue

_For any_ SQS operation (SendMessage or ReceiveMessage) performed by the NotificationService, the fixed configuration SHALL use the queue URL "https://sqs.us-east-1.amazonaws.com/981461568039/contoso-notifications", allowing successful interaction with the actual provisioned SQS queue and enabling notifications to be persisted and retrieved.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

Property 2: Preservation - Non-SQS Functionality Unchanged

_For any_ HTTP request processing, validation logic, logging operation, or service initialization that does NOT directly involve SQS queue URL usage, the fixed code SHALL produce exactly the same behavior as the original code, preserving all existing API endpoint behavior, request validation, error handling, and logging functionality.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File**: `NotificationAPI/appsettings.json`

**Configuration Section**: `AWS.SQS.QueueUrl`

**Specific Changes**:
1. **Update Queue URL**: Change the QueueUrl value from the incorrect queue name to the correct one
   - Current: `"QueueUrl": "https://sqs.us-east-1.amazonaws.com/981461568039/contoso-queue"`
   - Fixed: `"QueueUrl": "https://sqs.us-east-1.amazonaws.com/981461568039/contoso-notifications"`

2. **Verify Region Configuration**: Ensure the Region remains "us-east-1" (no change needed, but verify for consistency)

3. **No Code Changes Required**: The NotificationService.cs code correctly reads from configuration and uses the _queueUrl field for all operations, so no code modifications are necessary

4. **Deployment Consideration**: After updating the configuration, the NotificationAPI service must be restarted to load the new configuration value

5. **Verification**: After deployment, test both SendNotification and ReceiveNotification operations to confirm successful SQS interaction

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code by attempting SQS operations and observing permission errors, then verify the fix works correctly by confirming successful SQS operations and that all non-SQS functionality remains unchanged.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the bug BEFORE implementing the fix. Confirm that the incorrect queue URL causes SQS permission errors. If we observe different errors, we will need to re-hypothesize.

**Test Plan**: Write tests that simulate POST and GET requests to the NotificationAPI endpoints and observe the SQS errors in logs. Run these tests on the UNFIXED code to confirm the queue name mismatch is the root cause.

**Test Cases**:
1. **Send Notification Test**: POST to /api/notifications with valid data (will fail on unfixed code with SQS permission error for "contoso-queue")
2. **Receive Notification Test**: GET /api/notifications when queue should have messages (will fail on unfixed code, returning 204 because ReceiveNotification catches the SQS error)
3. **Log Analysis Test**: Examine application logs for AmazonSQSException with ErrorCode indicating permission denied for "contoso-queue" ARN (will show errors on unfixed code)
4. **Multiple Notifications Test**: Send multiple notifications rapidly (will all fail on unfixed code due to queue mismatch)

**Expected Counterexamples**:
- AmazonSQSException with error code related to queue access or permissions
- Log entries showing "AWS SQS error sending notification" or "AWS SQS error receiving notification"
- Possible causes: incorrect queue URL, queue does not exist, IAM permissions issue (but queue name mismatch is most likely)

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds (SQS operations), the fixed configuration produces the expected behavior (successful queue interaction).

**Pseudocode:**
```
FOR ALL operation WHERE isBugCondition(operation) DO
  result := performSQSOperation_fixed(operation)
  ASSERT result.success == true
  ASSERT result.error == null
  ASSERT (operation == SendMessage IMPLIES messageInQueue)
  ASSERT (operation == ReceiveMessage IMPLIES notificationReturned OR noMessagesAvailable)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold (non-SQS operations), the fixed configuration produces the same result as the original configuration.

**Pseudocode:**
```
FOR ALL request WHERE NOT isBugCondition(request) DO
  ASSERT handleRequest_original(request) = handleRequest_fixed(request)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain (various request formats, validation scenarios)
- It catches edge cases that manual unit tests might miss (malformed requests, missing fields, boundary values)
- It provides strong guarantees that behavior is unchanged for all non-SQS functionality (request validation, error responses, logging)

**Test Plan**: Observe behavior on UNFIXED code first for request validation, error handling, and logging, then write property-based tests capturing that behavior to ensure it remains unchanged after the fix.

**Test Cases**:
1. **Request Validation Preservation**: Verify that invalid requests (missing EntityType, invalid Operation) continue to return 400 Bad Request with the same error messages
2. **Logging Preservation**: Verify that log messages for request processing, validation errors, and service initialization remain unchanged
3. **CORS Preservation**: Verify that CORS headers and policy continue to work correctly for cross-origin requests
4. **Service Initialization Preservation**: Verify that NotificationService constructor behavior (credential loading, region configuration) remains unchanged

### Unit Tests

- Test POST /api/notifications with valid notification data returns 200 OK after fix
- Test GET /api/notifications successfully retrieves notifications from correct queue after fix
- Test POST /api/notifications with missing EntityType returns 400 Bad Request (preservation)
- Test POST /api/notifications with invalid Operation returns 400 Bad Request (preservation)
- Test that NotificationService initializes with correct queue URL after fix
- Test that SQS SendMessageAsync is called with correct queue URL after fix
- Test that SQS ReceiveMessageAsync is called with correct queue URL after fix

### Property-Based Tests

- Generate random valid SendNotificationRequest objects and verify all are successfully sent to SQS after fix
- Generate random invalid request bodies (missing fields, wrong types) and verify validation behavior is preserved
- Generate random notification data and verify messages can be sent and received successfully after fix
- Test that error handling and logging behavior is consistent across many scenarios (preservation)

### Integration Tests

- Test full flow: Contoso API sends notification → NotificationAPI persists to SQS → GET retrieves notification
- Test that notifications appear in the React UI after creating/updating/deleting entities
- Test that multiple rapid notifications are all successfully queued and retrievable
- Test that the system recovers gracefully if SQS is temporarily unavailable (error handling preservation)
