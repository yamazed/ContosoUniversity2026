# Requirements Document

## Introduction

This feature adds persistent storage for notifications in the NotificationAPI system. Currently, notifications are sent to AWS SQS and immediately deleted when read, resulting in no notification history. This enhancement will store notifications in a PostgreSQL database, allowing them to persist after being read, support read/unread status tracking, and maintain a complete notification history.

## Glossary

- **NotificationAPI**: The ASP.NET Core Web API service responsible for sending and receiving notifications
- **Notification_Store**: The PostgreSQL database table that persists notification records
- **SQS_Queue**: The AWS Simple Queue Service queue currently used for notification delivery
- **Database_Context**: The Entity Framework Core DbContext for database operations
- **Repository**: The data access layer component that encapsulates database operations for notifications
- **Migration**: Entity Framework Core database schema change script
- **Read_Status**: A boolean flag indicating whether a notification has been viewed by a user
- **ContosoUniversity_Database**: The shared PostgreSQL database used by both ContosoUniversity API and NotificationAPI

## Requirements

### Requirement 1: Database Schema and Context

**User Story:** As a developer, I want a database table to store notifications, so that notification data persists beyond the SQS queue lifecycle

#### Acceptance Criteria

1. THE NotificationAPI SHALL define a Notification entity with properties: Id, EntityType, EntityId, Operation, Message, CreatedAt, CreatedBy, IsRead, and ReadAt
2. THE Database_Context SHALL include a DbSet for Notification entities
3. THE NotificationAPI SHALL create an Entity Framework Core migration to add the Notification_Store table to the ContosoUniversity_Database
4. THE Notification_Store table SHALL use an auto-incrementing integer as the primary key
5. THE Notification_Store table SHALL index the CreatedAt column for efficient sorting
6. THE Notification_Store table SHALL index the IsRead column for efficient filtering

### Requirement 2: Notification Persistence on Send

**User Story:** As a system, I want to persist notifications to the database when they are sent, so that a permanent record exists

#### Acceptance Criteria

1. WHEN a notification is sent via POST /api/notifications, THE NotificationAPI SHALL save the notification to the Notification_Store
2. WHEN a notification is sent via POST /api/notifications, THE NotificationAPI SHALL send the notification to the SQS_Queue
3. IF the database save operation fails, THEN THE NotificationAPI SHALL log the error and continue sending to SQS_Queue
4. THE NotificationAPI SHALL set IsRead to false for all newly created notifications
5. THE NotificationAPI SHALL set CreatedAt to the current UTC timestamp for all newly created notifications
6. THE NotificationAPI SHALL set ReadAt to null for all newly created notifications

### Requirement 3: Retrieve Notifications from Database

**User Story:** As a user, I want to retrieve notifications from the database instead of SQS, so that I can see my notification history

#### Acceptance Criteria

1. WHEN GET /api/notifications is called, THE NotificationAPI SHALL return notifications from the Notification_Store ordered by CreatedAt descending
2. THE NotificationAPI SHALL return notifications in JSON format with all notification properties
3. IF no notifications exist in the Notification_Store, THEN THE NotificationAPI SHALL return an empty array with HTTP 200 status
4. THE NotificationAPI SHALL support pagination with optional query parameters: page and pageSize
5. WHERE pagination parameters are provided, THE NotificationAPI SHALL return the specified page of results with default pageSize of 20
6. THE NotificationAPI SHALL include pagination metadata in the response: totalCount, currentPage, pageSize, and totalPages

### Requirement 4: Mark Notifications as Read

**User Story:** As a user, I want to mark notifications as read, so that I can track which notifications I have already viewed

#### Acceptance Criteria

1. THE NotificationAPI SHALL provide a PUT /api/notifications/{id}/read endpoint
2. WHEN PUT /api/notifications/{id}/read is called, THE NotificationAPI SHALL set IsRead to true for the specified notification
3. WHEN PUT /api/notifications/{id}/read is called, THE NotificationAPI SHALL set ReadAt to the current UTC timestamp
4. IF the notification ID does not exist, THEN THE NotificationAPI SHALL return HTTP 404 status
5. WHEN the operation succeeds, THE NotificationAPI SHALL return HTTP 200 status with the updated notification

### Requirement 5: Mark Notifications as Unread

**User Story:** As a user, I want to mark notifications as unread, so that I can flag notifications that need my attention

#### Acceptance Criteria

1. THE NotificationAPI SHALL provide a PUT /api/notifications/{id}/unread endpoint
2. WHEN PUT /api/notifications/{id}/unread is called, THE NotificationAPI SHALL set IsRead to false for the specified notification
3. WHEN PUT /api/notifications/{id}/unread is called, THE NotificationAPI SHALL set ReadAt to null
4. IF the notification ID does not exist, THEN THE NotificationAPI SHALL return HTTP 404 status
5. WHEN the operation succeeds, THE NotificationAPI SHALL return HTTP 200 status with the updated notification

### Requirement 6: Filter Notifications by Read Status

**User Story:** As a user, I want to filter notifications by read/unread status, so that I can focus on notifications that need my attention

#### Acceptance Criteria

1. THE NotificationAPI SHALL support an optional query parameter 'isRead' on GET /api/notifications
2. WHEN isRead=true is provided, THE NotificationAPI SHALL return only notifications where IsRead is true
3. WHEN isRead=false is provided, THE NotificationAPI SHALL return only notifications where IsRead is false
4. WHEN isRead parameter is not provided, THE NotificationAPI SHALL return all notifications regardless of read status
5. THE NotificationAPI SHALL combine read status filtering with pagination

### Requirement 7: Repository Pattern Implementation

**User Story:** As a developer, I want a repository layer for data access, so that database operations are encapsulated and testable

#### Acceptance Criteria

1. THE NotificationAPI SHALL define an INotificationRepository interface with methods: GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, and MarkAsReadAsync
2. THE NotificationAPI SHALL implement a NotificationRepository class that uses the Database_Context
3. THE NotificationRepository SHALL handle all database operations for notifications
4. THE NotificationAPI SHALL register the repository in the dependency injection container
5. THE NotificationService SHALL use the Repository for all database operations

### Requirement 8: Database Connection Configuration

**User Story:** As a developer, I want to configure the database connection, so that the NotificationAPI can connect to the ContosoUniversity_Database

#### Acceptance Criteria

1. THE NotificationAPI SHALL read the database connection string from appsettings.json
2. THE NotificationAPI SHALL configure Entity Framework Core to use PostgreSQL
3. THE NotificationAPI SHALL register the Database_Context in the dependency injection container
4. THE NotificationAPI SHALL use the same connection string format as ContosoUniversity API
5. WHERE the connection string is missing, THE NotificationAPI SHALL throw a clear configuration error on startup

### Requirement 9: SQS Queue Backward Compatibility

**User Story:** As a system administrator, I want the SQS queue to remain functional, so that existing integrations continue to work during migration

#### Acceptance Criteria

1. THE NotificationAPI SHALL continue to send notifications to the SQS_Queue when POST /api/notifications is called
2. THE NotificationAPI SHALL maintain the existing SQS message format
3. THE NotificationAPI SHALL log both database and SQS operations for monitoring
4. IF SQS send operation fails, THE NotificationAPI SHALL log the error but not fail the HTTP request
5. THE NotificationAPI SHALL prioritize database persistence over SQS delivery

### Requirement 10: Bulk Mark as Read

**User Story:** As a user, I want to mark multiple notifications as read at once, so that I can efficiently manage my notification list

#### Acceptance Criteria

1. THE NotificationAPI SHALL provide a PUT /api/notifications/mark-read endpoint
2. THE endpoint SHALL accept an array of notification IDs in the request body
3. WHEN PUT /api/notifications/mark-read is called, THE NotificationAPI SHALL set IsRead to true for all specified notifications
4. WHEN PUT /api/notifications/mark-read is called, THE NotificationAPI SHALL set ReadAt to the current UTC timestamp for all specified notifications
5. THE NotificationAPI SHALL return a summary response indicating how many notifications were successfully updated
6. IF any notification ID does not exist, THE NotificationAPI SHALL skip it and continue processing remaining IDs
7. THE NotificationAPI SHALL perform the bulk update operation within a single database transaction

### Requirement 11: Delete Old Notifications

**User Story:** As a system administrator, I want to delete old notifications, so that the database does not grow indefinitely

#### Acceptance Criteria

1. THE NotificationAPI SHALL provide a DELETE /api/notifications/cleanup endpoint
2. THE endpoint SHALL accept an optional 'olderThanDays' query parameter with a default value of 90
3. WHEN DELETE /api/notifications/cleanup is called, THE NotificationAPI SHALL delete notifications where CreatedAt is older than the specified number of days
4. THE NotificationAPI SHALL return the count of deleted notifications
5. THE NotificationAPI SHALL log the cleanup operation with the number of deleted records
6. THE NotificationAPI SHALL only delete notifications where IsRead is true
7. THE cleanup operation SHALL be performed within a single database transaction
