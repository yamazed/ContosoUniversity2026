# Bugfix Requirements Document

## Introduction

The notification system in the Contoso University application is failing to store and display notifications when users create, update, or delete entities (students, courses, instructors, departments). The root cause is that the NotificationAPI is attempting to read from an incorrect SQS queue name ("contoso-queue") instead of the actual queue ("contoso-notifications"), resulting in SQS permission errors and preventing notifications from being persisted to the database.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the NotificationAPI receives a POST request with notification data THEN the system fails to persist the notification to the database due to SQS queue name mismatch

1.2 WHEN the NotificationAPI attempts to read from SQS THEN the system encounters permission errors for "contoso-queue" (arn:aws:sqs:us-east-1:981461568039:contoso-queue) which does not exist

1.3 WHEN the React UI requests GET /api/notifications THEN the system returns 204 (No Content) with no notifications available

1.4 WHEN users create, update, or delete students/courses/instructors/departments THEN the notifications are not visible on the notifications page despite the Contoso API successfully sending them

### Expected Behavior (Correct)

2.1 WHEN the NotificationAPI receives a POST request with notification data THEN the system SHALL persist the notification to the PostgreSQL database successfully

2.2 WHEN the NotificationAPI attempts to read from SQS THEN the system SHALL connect to the correct queue "contoso-notifications" (arn:aws:sqs:us-east-1:981461568039:contoso-notifications) without permission errors

2.3 WHEN the React UI requests GET /api/notifications THEN the system SHALL return 200 (OK) with the list of stored notifications

2.4 WHEN users create, update, or delete students/courses/instructors/departments THEN the notifications SHALL be visible on the notifications page showing the recent changes

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the Contoso API sends notifications via HTTP POST to the NotificationAPI THEN the system SHALL CONTINUE TO successfully send the requests and log "Notification sent successfully to NotificationAPI"

3.2 WHEN the NotificationAPI receives POST requests on port 8080 THEN the system SHALL CONTINUE TO accept and process the incoming requests

3.3 WHEN the React UI is configured to call http://50.19.77.253:8080/api/notifications THEN the system SHALL CONTINUE TO route requests to the correct NotificationAPI endpoint

3.4 WHEN both APIs access the shared PostgreSQL database THEN the system SHALL CONTINUE TO maintain database connectivity and data integrity
