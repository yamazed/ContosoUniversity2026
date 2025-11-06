# Requirements Document

## Introduction

This document outlines the requirements for extracting the NotificationService from the Contoso University monolithic application into a separate .NET 8 WebAPI microservice. The new service will be independently deployable and will communicate with the main application via HTTP REST APIs. The notification service will continue to use AWS SQS for message queuing while exposing HTTP endpoints for sending and receiving notifications.

## Glossary

- **NotificationAPI**: The new standalone .NET 8 WebAPI project that hosts notification endpoints
- **ContosoUniversity**: The main ASP.NET Core MVC application that will consume the NotificationAPI
- **NotificationService**: The service class that manages AWS SQS operations (will be moved to NotificationAPI)
- **HTTP Client**: The mechanism used by ContosoUniversity to communicate with NotificationAPI
- **REST API**: Representational State Transfer Application Programming Interface for HTTP communication
- **Microservice**: An independently deployable service component
- **SQS**: Amazon Simple Queue Service for message queuing
- **Notification**: A message object containing information about entity operations

## Requirements

### Requirement 1

**User Story:** As a system architect, I want the NotificationService extracted into a separate WebAPI project, so that it can be deployed and scaled independently from the main application.

#### Acceptance Criteria

1. THE system SHALL create a new .NET 8 WebAPI project named "NotificationAPI"
2. THE NotificationAPI project SHALL be located in the same directory level as ContosoUniversity
3. THE NotificationAPI project SHALL include the NotificationService class with AWS SQS functionality
4. THE NotificationAPI project SHALL include the Notification model class
5. THE NotificationAPI project SHALL include all AWS SDK dependencies required for SQS operations

### Requirement 2

**User Story:** As a developer, I want the NotificationAPI to expose REST endpoints for sending notifications, so that the main application can send notifications via HTTP.

#### Acceptance Criteria

1. THE NotificationAPI SHALL expose a POST endpoint at "/api/notifications"
2. WHEN a POST request is received at "/api/notifications", THE NotificationAPI SHALL accept a JSON request body containing entityType, entityId, entityDisplayName, operation, and userName
3. WHEN the POST endpoint is called, THE NotificationAPI SHALL invoke NotificationService to send the notification to AWS SQS
4. THE POST endpoint SHALL return HTTP 200 OK on successful notification send
5. THE POST endpoint SHALL return HTTP 500 Internal Server Error if notification send fails

### Requirement 3

**User Story:** As a developer, I want the NotificationAPI to expose REST endpoints for receiving notifications, so that the main application can retrieve notifications via HTTP.

#### Acceptance Criteria

1. THE NotificationAPI SHALL expose a GET endpoint at "/api/notifications"
2. WHEN a GET request is received at "/api/notifications", THE NotificationAPI SHALL invoke NotificationService to receive a notification from AWS SQS
3. THE GET endpoint SHALL return HTTP 200 OK with the Notification object as JSON when a notification is available
4. THE GET endpoint SHALL return HTTP 204 No Content when no notifications are available
5. THE GET endpoint SHALL return HTTP 500 Internal Server Error if notification retrieval fails

### Requirement 4

**User Story:** As a developer, I want the ContosoUniversity NotificationService to use HTTP client to communicate with NotificationAPI, so that it can send and receive notifications through the microservice without changing existing code.

#### Acceptance Criteria

1. THE ContosoUniversity NotificationService SHALL be refactored to use HttpClient instead of AWS SQS
2. THE ContosoUniversity NotificationService SHALL maintain all existing method signatures
3. THE ContosoUniversity NotificationService SHALL send notifications via HTTP POST to NotificationAPI
4. THE ContosoUniversity NotificationService SHALL receive notifications via HTTP GET from NotificationAPI
5. THE ContosoUniversity NotificationService SHALL remove all AWS SDK dependencies and SQS-related code

### Requirement 5

**User Story:** As a system operator, I want the NotificationAPI base URL configured in appsettings.json, so that the connection can be changed without code modifications.

#### Acceptance Criteria

1. THE ContosoUniversity appsettings.json SHALL contain a NotificationAPI section with BaseUrl configuration
2. THE ContosoUniversity NotificationService SHALL read the BaseUrl from IConfiguration
3. THE ContosoUniversity NotificationService SHALL construct full endpoint URLs using the configured BaseUrl
4. WHERE the BaseUrl is not configured, THE ContosoUniversity NotificationService SHALL use a default value of "http://localhost:5001"
5. THE NotificationAPI SHALL be configured to run on port 5001 by default

### Requirement 6

**User Story:** As a developer, I want the NotificationAPI to have proper dependency injection configuration, so that services are properly initialized and managed.

#### Acceptance Criteria

1. THE NotificationAPI SHALL register NotificationService in the dependency injection container
2. THE NotificationAPI SHALL configure AWS settings from appsettings.json
3. THE NotificationAPI SHALL register HttpClient services if needed
4. THE NotificationAPI SHALL configure CORS to allow requests from ContosoUniversity
5. THE NotificationAPI SHALL use scoped or singleton lifetime for NotificationService as appropriate

### Requirement 7

**User Story:** As a developer, I want both projects to have proper configuration files, so that each service can be configured independently.

#### Acceptance Criteria

1. THE NotificationAPI SHALL have its own appsettings.json with AWS configuration
2. THE NotificationAPI appsettings.json SHALL contain AWS Region and SQS QueueUrl
3. THE ContosoUniversity appsettings.json SHALL be updated to include NotificationAPI BaseUrl
4. THE ContosoUniversity appsettings.json SHALL remove AWS SQS configuration
5. THE ContosoUniversity appsettings.json SHALL retain other existing configuration sections

### Requirement 8

**User Story:** As a developer, I want existing controllers to continue working without changes, so that the microservice extraction is transparent to the rest of the application.

#### Acceptance Criteria

1. THE BaseController SHALL continue to inject and use NotificationService without code changes
2. THE NotificationsController SHALL continue to inject and use NotificationService without code changes
3. THE NotificationService SHALL handle HTTP communication errors gracefully
4. THE NotificationService SHALL not throw exceptions if notification sending fails
5. THE NotificationService SHALL maintain backward compatibility with all existing consumers
