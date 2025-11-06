# Requirements Document

## Introduction

This document outlines the requirements for migrating the Contoso University notification system from an in-memory queue implementation to AWS Simple Queue Service (SQS). The migration will maintain the existing notification functionality while leveraging AWS SQS for reliable, scalable message delivery. The implementation focuses on base functionality without extensive error handling or unit tests.

## Glossary

- **NotificationService**: The service class responsible for sending and receiving notification messages
- **SQS**: Amazon Simple Queue Service, a fully managed message queuing service
- **AWS SDK**: Amazon Web Services Software Development Kit for .NET
- **Notification**: A message object containing information about entity operations (create, update, delete)
- **Queue URL**: The unique identifier for the AWS SQS queue (https://sqs.eu-west-1.amazonaws.com/969522832499/contoso-queue)
- **EntityOperation**: An enumeration representing the type of operation performed (CREATE, UPDATE, DELETE)

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want notifications to be sent to AWS SQS instead of an in-memory queue, so that notifications are reliably delivered and persisted outside the application process.

#### Acceptance Criteria

1. WHEN the NotificationService sends a notification, THE NotificationService SHALL transmit the message to the AWS SQS queue at URL "https://sqs.eu-west-1.amazonaws.com/969522832499/contoso-queue"
2. THE NotificationService SHALL serialize the Notification object to JSON format before sending to SQS
3. THE NotificationService SHALL use the AWS SDK for .NET to interact with SQS
4. THE NotificationService SHALL remove the in-memory ConcurrentQueue implementation
5. THE NotificationService SHALL maintain the existing method signatures for SendNotification

### Requirement 2

**User Story:** As a system administrator, I want to receive notifications from AWS SQS, so that I can view entity operation notifications in the admin dashboard.

#### Acceptance Criteria

1. WHEN the NotificationService receives a notification request, THE NotificationService SHALL retrieve messages from the AWS SQS queue
2. THE NotificationService SHALL deserialize JSON messages from SQS into Notification objects
3. THE NotificationService SHALL delete messages from the SQS queue after successful retrieval
4. THE NotificationService SHALL return null when no messages are available in the queue
5. THE NotificationService SHALL maintain the existing method signature for ReceiveNotification

### Requirement 3

**User Story:** As a developer, I want the AWS SDK dependencies installed in the project, so that the application can communicate with AWS SQS.

#### Acceptance Criteria

1. THE ContosoUniversity project SHALL include the AWSSDK.SQS NuGet package
2. THE ContosoUniversity project SHALL include the AWSSDK.Core NuGet package
3. THE project file SHALL reference the AWS SDK packages with compatible versions for .NET 8.0

### Requirement 4

**User Story:** As a system operator, I want AWS credentials and queue configuration stored in application settings, so that the SQS connection can be configured without code changes.

#### Acceptance Criteria

1. THE appsettings.json file SHALL contain an AWS section with Region configuration
2. THE appsettings.json file SHALL contain the SQS queue URL in the configuration
3. THE NotificationService SHALL read AWS configuration from IConfiguration
4. THE NotificationService SHALL read the queue URL from IConfiguration
5. WHERE AWS credentials are not in configuration, THE NotificationService SHALL use the default AWS credential provider chain

### Requirement 5

**User Story:** As a developer, I want the NotificationService to initialize AWS SQS client properly, so that the service can send and receive messages reliably.

#### Acceptance Criteria

1. WHEN the NotificationService is instantiated, THE NotificationService SHALL create an AmazonSQSClient instance
2. THE NotificationService SHALL configure the SQS client with the region from configuration
3. THE NotificationService SHALL store the queue URL for use in send and receive operations
4. THE NotificationService SHALL implement IDisposable to properly clean up the SQS client
5. THE NotificationService SHALL dispose of the SQS client when the service is disposed
