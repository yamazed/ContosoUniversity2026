# Implementation Plan: Persistent Notification Storage

## Overview

This implementation plan adds PostgreSQL database persistence to the NotificationAPI system using Entity Framework Core. The implementation will maintain backward compatibility with the existing SQS-based notification delivery while adding a database layer for persistent storage, read/unread tracking, and notification history management.

## Tasks

- [x] 1. Set up database infrastructure and Entity Framework Core
  - [x] 1.1 Add Entity Framework Core NuGet packages
    - Add Microsoft.EntityFrameworkCore (8.0.x)
    - Add Npgsql.EntityFrameworkCore.PostgreSQL (8.0.x)
    - Add Microsoft.EntityFrameworkCore.Design (8.0.x) for migrations
    - _Requirements: 8.2_
  
  - [x] 1.2 Create NotificationContext DbContext class
    - Create Data/NotificationContext.cs with DbSet<Notification>
    - Configure entity model with indexes on CreatedAt and IsRead
    - Set table name to "Notification"
    - _Requirements: 1.2, 1.5, 1.6_
  
  - [x] 1.3 Configure database connection in Program.cs
    - Read connection string from appsettings.json with fallback to environment variables
    - Register NotificationContext in dependency injection container
    - Add database connection validation on startup
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_
  
  - [x] 1.4 Create and apply Entity Framework migration
    - Generate migration to create Notification table with auto-incrementing Id
    - Include indexes for CreatedAt and IsRead columns
    - Apply migration to create database schema
    - _Requirements: 1.3, 1.4, 1.5, 1.6_

- [ ]* 1.5 Write property test for auto-incrementing ID generation
  - **Property 1: Auto-incrementing ID generation**
  - **Validates: Requirements 1.4**

- [x] 2. Implement repository pattern for data access
  - [x] 2.1 Create INotificationRepository interface
    - Define methods: CreateAsync, GetByIdAsync, GetAllAsync, UpdateAsync, MarkAsReadAsync, MarkAsUnreadAsync, BulkMarkAsReadAsync, DeleteOldNotificationsAsync
    - Include pagination parameters in GetAllAsync
    - _Requirements: 7.1_
  
  - [x] 2.2 Implement NotificationRepository class
    - Implement CreateAsync to add notifications to database
    - Implement GetByIdAsync to retrieve by primary key
    - Implement GetAllAsync with filtering, pagination, and ordering
    - Implement UpdateAsync to save changes
    - Implement MarkAsReadAsync and MarkAsUnreadAsync
    - Implement BulkMarkAsReadAsync with transaction support
    - Implement DeleteOldNotificationsAsync with filtering
    - _Requirements: 7.2, 7.3_
  
  - [x] 2.3 Register repository in dependency injection
    - Add INotificationRepository and NotificationRepository to DI container in Program.cs
    - _Requirements: 7.4_

- [ ]* 2.4 Write property tests for repository operations
  - **Property 2: Notification persistence round-trip**
  - **Validates: Requirements 2.1**

- [x] 3. Enhance NotificationService to use database persistence
  - [x] 3.1 Inject INotificationRepository into NotificationService
    - Update constructor to accept INotificationRepository
    - Store repository as private field
    - _Requirements: 7.5_
  
  - [x] 3.2 Update SendNotificationAsync to persist to database
    - Create Notification entity with IsRead=false, CreatedAt=UtcNow, ReadAt=null
    - Save to database via repository (primary operation)
    - Send to SQS queue (secondary operation, best-effort)
    - Handle database errors by returning 500 (do not send to SQS if DB fails)
    - Handle SQS errors gracefully with logging (do not fail request if DB succeeded)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 9.1, 9.2, 9.3, 9.4, 9.5_
  
  - [x] 3.3 Add GetNotificationsAsync method
    - Call repository GetAllAsync with filtering and pagination parameters
    - Return notifications and pagination metadata
    - _Requirements: 3.1, 3.4, 3.5, 3.6_
  
  - [x] 3.4 Add GetNotificationByIdAsync method
    - Call repository GetByIdAsync
    - Return notification or null if not found
    - _Requirements: 4.4, 5.4_
  
  - [x] 3.5 Add MarkAsReadAsync method
    - Call repository MarkAsReadAsync to set IsRead=true and ReadAt=UtcNow
    - Return updated notification or null if not found
    - _Requirements: 4.2, 4.3_
  
  - [x] 3.6 Add MarkAsUnreadAsync method
    - Call repository MarkAsUnreadAsync to set IsRead=false and ReadAt=null
    - Return updated notification or null if not found
    - _Requirements: 5.2, 5.3_
  
  - [x] 3.7 Add BulkMarkAsReadAsync method
    - Call repository BulkMarkAsReadAsync with list of IDs
    - Return count of updated notifications
    - _Requirements: 10.3, 10.4, 10.5, 10.6, 10.7_
  
  - [x] 3.8 Add CleanupOldNotificationsAsync method
    - Call repository DeleteOldNotificationsAsync with age parameter
    - Return count of deleted notifications
    - _Requirements: 11.3, 11.4, 11.5, 11.6, 11.7_

- [ ]* 3.9 Write property tests for notification state management
  - **Property 3: Initial notification state**
  - **Validates: Requirements 2.4, 2.5, 2.6**

- [x] 4. Update NotificationsController endpoints
  - [x] 4.1 Update POST /api/notifications endpoint
    - Call enhanced SendNotificationAsync
    - Return created notification with HTTP 201 status
    - Return HTTP 500 if database operation fails
    - _Requirements: 2.1, 2.2, 2.3_
  
  - [x] 4.2 Update GET /api/notifications endpoint
    - Accept optional query parameters: isRead, page, pageSize
    - Call GetNotificationsAsync with parameters
    - Return PaginatedResponse with notifications and pagination metadata
    - Return empty array with HTTP 200 if no notifications exist
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [x] 4.3 Create PUT /api/notifications/{id}/read endpoint
    - Call MarkAsReadAsync with notification ID
    - Return HTTP 404 if notification not found
    - Return HTTP 200 with updated notification if successful
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 4.4 Create PUT /api/notifications/{id}/unread endpoint
    - Call MarkAsUnreadAsync with notification ID
    - Return HTTP 404 if notification not found
    - Return HTTP 200 with updated notification if successful
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 4.5 Create PUT /api/notifications/mark-read endpoint
    - Accept BulkMarkReadRequest with array of notification IDs
    - Call BulkMarkAsReadAsync
    - Return BulkMarkReadResponse with updated count and message
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_
  
  - [x] 4.6 Create DELETE /api/notifications/cleanup endpoint
    - Accept optional query parameter olderThanDays (default 90)
    - Call CleanupOldNotificationsAsync
    - Return CleanupResponse with deleted count and message
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7_

- [ ]* 4.7 Write property tests for notification retrieval
  - **Property 4: Descending chronological order**
  - **Property 5: Complete notification serialization**
  - **Validates: Requirements 3.1, 3.2**

- [ ]* 4.8 Write property tests for pagination
  - **Property 6: Pagination correctness**
  - **Validates: Requirements 3.4, 3.5, 3.6**

- [x] 5. Create response models for new endpoints
  - [x] 5.1 Create PaginationMetadata class
    - Define properties: TotalCount, CurrentPage, PageSize, TotalPages
    - _Requirements: 3.6_
  
  - [x] 5.2 Create PaginatedResponse<T> class
    - Define properties: Data (List<T>), Pagination (PaginationMetadata)
    - _Requirements: 3.6_
  
  - [x] 5.3 Create BulkMarkReadRequest class
    - Define property: NotificationIds (List<int>)
    - _Requirements: 10.2_
  
  - [x] 5.4 Create BulkMarkReadResponse class
    - Define properties: UpdatedCount, Message
    - _Requirements: 10.5_
  
  - [x] 5.5 Create CleanupResponse class
    - Define properties: DeletedCount, Message
    - _Requirements: 11.4_

- [x] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ]* 7. Write property tests for read/unread status management
  - [ ]* 7.1 Write property test for mark as read
    - **Property 7: Mark as read updates state**
    - **Validates: Requirements 4.2, 4.3**
  
  - [ ]* 7.2 Write property test for mark as unread
    - **Property 8: Mark as unread resets state**
    - **Validates: Requirements 5.2, 5.3**
  
  - [ ]* 7.3 Write property test for invalid ID handling
    - **Property 9: Invalid ID returns 404**
    - **Validates: Requirements 4.4, 5.4**
  
  - [ ]* 7.4 Write property test for successful update response
    - **Property 10: Successful update returns notification**
    - **Validates: Requirements 4.5, 5.5**

- [ ]* 8. Write property tests for filtering functionality
  - [ ]* 8.1 Write property test for read status filtering (read)
    - **Property 11: Read status filtering (read)**
    - **Validates: Requirements 6.2**
  
  - [ ]* 8.2 Write property test for read status filtering (unread)
    - **Property 12: Read status filtering (unread)**
    - **Validates: Requirements 6.3**
  
  - [ ]* 8.3 Write property test for no filter returns all
    - **Property 13: No filter returns all notifications**
    - **Validates: Requirements 6.4**
  
  - [ ]* 8.4 Write property test for combined filtering and pagination
    - **Property 14: Combined filtering and pagination**
    - **Validates: Requirements 6.5**

- [ ]* 9. Write property tests for bulk operations
  - [ ]* 9.1 Write property test for bulk mark as read
    - **Property 15: Bulk mark as read updates all**
    - **Validates: Requirements 10.3, 10.4**
  
  - [ ]* 9.2 Write property test for bulk update count accuracy
    - **Property 16: Bulk update count accuracy**
    - **Validates: Requirements 10.5, 10.6**

- [ ]* 10. Write property tests for cleanup operations
  - [ ]* 10.1 Write property test for cleanup deletion logic
    - **Property 17: Cleanup deletes old read notifications**
    - **Validates: Requirements 11.3, 11.6**
  
  - [ ]* 10.2 Write property test for cleanup count accuracy
    - **Property 18: Cleanup count accuracy**
    - **Validates: Requirements 11.4**

- [ ]* 11. Write unit tests for edge cases and error handling
  - [ ]* 11.1 Write unit test for empty notification list
    - Test GET /api/notifications returns empty array with HTTP 200
    - _Requirements: 3.3_
  
  - [ ]* 11.2 Write unit test for missing database configuration
    - Test application fails to start with clear error message
    - _Requirements: 8.5_
  
  - [ ]* 11.3 Write unit test for SQS failure handling
    - Test that SQS failure logs error but doesn't fail request
    - Test that database save succeeded even when SQS fails
    - _Requirements: 9.4_
  
  - [ ]* 11.4 Write unit test for database failure handling
    - Test that database failure returns HTTP 500
    - Test that SQS is not called when database fails
    - _Requirements: 2.3, 9.5_
  
  - [ ]* 11.5 Write unit test for pagination edge cases
    - Test first page, last page, beyond last page
    - Test invalid pagination parameters return HTTP 400
    - _Requirements: 3.4, 3.5_
  
  - [ ]* 11.6 Write unit test for bulk operations with empty list
    - Test bulk mark as read with empty ID list
    - Test bulk mark as read with all invalid IDs
    - _Requirements: 10.6_
  
  - [ ]* 11.7 Write unit test for cleanup with no matching notifications
    - Test cleanup returns 0 when no notifications match criteria
    - _Requirements: 11.3_
  
  - [ ]* 11.8 Write unit test for transaction rollback
    - Test that bulk operations rollback on failure
    - Test that cleanup operations rollback on failure
    - _Requirements: 10.7, 11.7_

- [ ] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The implementation uses C# with ASP.NET Core and Entity Framework Core
- Database is PostgreSQL (shared ContosoUniversity database)
- Property tests validate universal correctness properties across randomized inputs
- Unit tests validate specific examples, edge cases, and error conditions
- All database operations use the repository pattern for testability
- SQS integration is maintained for backward compatibility but database is primary
