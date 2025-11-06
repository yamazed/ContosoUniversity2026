# Implementation Plan

- [x] 1. Add AWS SDK NuGet packages to the project
  - Install AWSSDK.SQS package to ContosoUniversity.csproj
  - Verify package installation and compatibility with .NET 8.0
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 2. Update application configuration for AWS SQS
  - Add AWS section to appsettings.json with Region set to "eu-west-1"
  - Add SQS QueueUrl configuration with value "https://sqs.eu-west-1.amazonaws.com/969522832499/contoso-queue"
  - Retain existing NotificationQueuePath for backward compatibility
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 3. Refactor NotificationService to use AWS SQS
  - [x] 3.1 Update class properties and constructor
    - Remove ConcurrentQueue field
    - Add AmazonSQSClient field
    - Add queue URL field
    - Update constructor to read AWS configuration and initialize SQS client
    - _Requirements: 5.1, 5.2, 5.3, 4.3, 4.4, 4.5_
  
  - [x] 3.2 Implement SendNotification with SQS
    - Serialize Notification object to JSON using JsonConvert
    - Create SendMessageRequest with queue URL and JSON body
    - Call SendMessageAsync on SQS client
    - Wrap in try-catch with debug logging
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_
  
  - [x] 3.3 Implement ReceiveNotification with SQS
    - Create ReceiveMessageRequest with queue URL
    - Call ReceiveMessageAsync on SQS client
    - Deserialize JSON message body to Notification object
    - Delete message from queue after successful retrieval
    - Return null when no messages available
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  
  - [x] 3.4 Update Dispose method
    - Dispose of AmazonSQSClient instance
    - _Requirements: 5.4, 5.5_

- [x] 4. Verify integration with existing controllers
  - Confirm BaseController still works with updated NotificationService
  - Ensure NotificationsController can receive notifications from SQS
  - _Requirements: 1.5, 2.5_
