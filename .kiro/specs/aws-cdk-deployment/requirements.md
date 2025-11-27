# Requirements Document

## Introduction

This document specifies the requirements for deploying the Contoso University application to AWS using AWS CDK (Cloud Development Kit) with C#. The application consists of three components: a React frontend, a main ASP.NET Core API backend, and a separate Notification API service. The deployment must provision all necessary AWS infrastructure including compute, database, networking, and monitoring resources to run the application in a production-ready environment.

## Glossary

- **CDK Stack**: A unit of deployment in AWS CDK that defines a collection of AWS resources
- **EC2 (Elastic Compute Cloud)**: AWS virtual server instances for running applications
- **Auto Scaling Group**: AWS service that automatically adjusts the number of EC2 instances based on demand
- **RDS (Relational Database Service)**: AWS managed database service
- **ALB (Application Load Balancer)**: AWS load balancer that distributes incoming application traffic
- **VPC (Virtual Private Cloud)**: Isolated network environment in AWS
- **CloudFront**: AWS content delivery network (CDN) service
- **S3 (Simple Storage Service)**: AWS object storage service
- **User Data**: Initialization scripts that run when EC2 instances launch
- **ContosoUniversity API**: The main ASP.NET Core backend API that handles student, course, instructor, and department management
- **NotificationAPI**: The separate ASP.NET Core service that handles notification operations
- **React UI**: The frontend application built with React, TypeScript, and Vite

## Requirements

### Requirement 1

**User Story:** As a DevOps engineer, I want to define all AWS infrastructure as code using C# CDK, so that I can version control, review, and consistently deploy the infrastructure.

#### Acceptance Criteria

1. WHEN the CDK project is initialized THEN the system SHALL create a C# CDK project structure with appropriate dependencies
2. WHEN infrastructure is defined THEN the system SHALL use C# CDK constructs to declare all AWS resources
3. WHEN the CDK code is compiled THEN the system SHALL produce valid CloudFormation templates
4. WHEN the CDK stack is synthesized THEN the system SHALL validate all resource configurations before deployment
5. WHERE multiple environments are needed THEN the system SHALL support environment-specific configuration through CDK context or parameters

### Requirement 2

**User Story:** As a DevOps engineer, I want to deploy the React UI as a static website, so that users can access the frontend application with low latency and high availability.

#### Acceptance Criteria

1. WHEN the React application is built THEN the system SHALL compile the TypeScript code and bundle assets for production
2. WHEN deploying the frontend THEN the system SHALL upload the built assets to an S3 bucket configured for static website hosting
3. WHEN users access the frontend THEN the system SHALL serve content through CloudFront CDN for global distribution
4. WHEN CloudFront is configured THEN the system SHALL enable HTTPS using AWS Certificate Manager
5. WHEN routing is configured THEN the system SHALL handle client-side routing by redirecting 404 errors to index.html

### Requirement 3

**User Story:** As a DevOps engineer, I want to deploy the ASP.NET Core APIs on EC2 instances, so that they run in a managed environment without requiring Docker.

#### Acceptance Criteria

1. WHEN .NET applications are published THEN the system SHALL publish both ContosoUniversity API and NotificationAPI as self-contained deployments to S3
2. WHEN deploying the CDK stack THEN the system SHALL create EC2 Auto Scaling Groups for both APIs with appropriate launch templates
3. WHEN EC2 instances launch THEN the system SHALL use User Data scripts to install .NET runtime, download applications from S3, and start the services
4. WHEN configuring EC2 instances THEN the system SHALL use appropriate instance types (t3.small for Contoso API, t3.micro for Notification API)
5. WHEN configuring Auto Scaling THEN the system SHALL define appropriate min/max capacity (min: 1, max: 3) and scaling policies based on CPU utilization

### Requirement 4

**User Story:** As a DevOps engineer, I want to provision a PostgreSQL database, so that the ContosoUniversity API can persist and retrieve data.

#### Acceptance Criteria

1. WHEN the database is provisioned THEN the system SHALL create an Amazon Aurora Serverless v2 PostgreSQL cluster
2. WHEN configuring the database THEN the system SHALL place it in private subnets for security
3. WHEN setting database credentials THEN the system SHALL store them in AWS Secrets Manager
4. WHEN the API connects to the database THEN the system SHALL retrieve connection strings from Secrets Manager
5. WHEN configuring Aurora Serverless THEN the system SHALL set appropriate minimum and maximum ACU (Aurora Capacity Units) values

### Requirement 5

**User Story:** As a DevOps engineer, I want to configure networking and security, so that the application components can communicate securely while being protected from unauthorized access.

#### Acceptance Criteria

1. WHEN creating the network THEN the system SHALL provision a VPC with public and private subnets across multiple availability zones
2. WHEN configuring security groups THEN the system SHALL allow only necessary traffic between components
3. WHEN exposing APIs THEN the system SHALL place an Application Load Balancer in public subnets
4. WHEN routing traffic THEN the system SHALL configure ALB target groups for each API service
5. WHEN securing the database THEN the system SHALL ensure RDS is only accessible from the API security group

### Requirement 6

**User Story:** As a DevOps engineer, I want to provision an SQS queue for notifications, so that the NotificationAPI can send and receive notification messages asynchronously.

#### Acceptance Criteria

1. WHEN the infrastructure is deployed THEN the system SHALL create an Amazon SQS standard queue for notifications
2. WHEN configuring the queue THEN the system SHALL set appropriate message retention period and visibility timeout
3. WHEN configuring IAM permissions THEN the system SHALL grant the NotificationAPI EC2 instance role permissions to send and receive messages from the queue
4. WHEN configuring environment variables THEN the system SHALL inject the SQS queue URL into the NotificationAPI EC2 instance via User Data
5. WHEN configuring the AWS region THEN the system SHALL inject the AWS region into the NotificationAPI EC2 instance via User Data

### Requirement 7

**User Story:** As a DevOps engineer, I want to configure inter-service communication, so that the ContosoUniversity API can communicate with the NotificationAPI.

#### Acceptance Criteria

1. WHEN services are deployed THEN the system SHALL configure both APIs behind the same Application Load Balancer with different path-based routing rules
2. WHEN the ContosoUniversity API needs to call NotificationAPI THEN the system SHALL use the ALB DNS name with the appropriate path prefix
3. WHEN configuring environment variables THEN the system SHALL inject the NotificationAPI base URL into the ContosoUniversity API EC2 instance via User Data
4. WHEN configuring ALB listeners THEN the system SHALL route requests to /api/notifications/* to the NotificationAPI target group
5. WHEN configuring health checks THEN the system SHALL define appropriate health check endpoints (/health) for each service's target group

### Requirement 8

**User Story:** As a DevOps engineer, I want to configure logging, so that I can observe application behavior and troubleshoot issues.

#### Acceptance Criteria

1. WHEN EC2 instances run THEN the system SHALL install and configure CloudWatch agent to send application logs to Amazon CloudWatch Logs
2. WHEN creating log groups THEN the system SHALL organize logs by service (/aws/ec2/contoso-api and /aws/ec2/notification-api) with appropriate retention periods
3. WHEN configuring log retention THEN the system SHALL set a default retention period of 7 days for cost optimization
4. WHEN configuring IAM permissions THEN the system SHALL grant EC2 instance roles permissions to write logs to CloudWatch

### Requirement 9

**User Story:** As a DevOps engineer, I want to manage application configuration, so that the services can connect to dependencies and operate correctly.

#### Acceptance Criteria

1. WHEN deploying services THEN the system SHALL use environment variables configured in User Data scripts to configure application behavior
2. WHEN managing secrets THEN the system SHALL use AWS Secrets Manager for database credentials and grant EC2 instance roles permissions to read them
3. WHEN configuring CORS THEN the system SHALL set allowed origins based on the deployed CloudFront distribution URL
4. WHEN configuring the ContosoUniversity API THEN the system SHALL inject database connection string and NotificationAPI URL as environment variables via User Data
5. WHEN tagging resources THEN the system SHALL apply consistent tags to all AWS resources for organization

### Requirement 10

**User Story:** As a developer, I want deployment tooling, so that I can deploy the application to AWS efficiently.

#### Acceptance Criteria

1. WHEN deploying .NET applications THEN the system SHALL provide scripts to publish applications as self-contained deployments and upload to S3
2. WHEN deploying infrastructure THEN the system SHALL use CDK deployment commands (synth, deploy, destroy) to provision all AWS resources
3. WHEN EC2 instances launch THEN the system SHALL automatically download and run the latest application versions from S3
4. WHEN deploying the frontend THEN the system SHALL use CDK S3 deployment construct to upload built assets and invalidate CloudFront cache
5. WHEN deployment completes THEN the system SHALL output important endpoints including ALB URL, CloudFront distribution URL, and RDS endpoint
