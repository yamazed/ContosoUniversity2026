# Implementation Plan

- [x] 1. Set up CDK project structure
  - Create C# CDK project with appropriate dependencies
  - Configure CDK context and environment settings
  - Set up project structure with separate construct files
  - _Requirements: 1.1, 1.2, 1.5_

- [x] 2. Configure EC2-based deployment
  - [x] 2.1 Create deployment scripts for ContosoUniversity API
    - Publish .NET application to S3
    - Configure user data script for EC2 instances
    - Set up systemd service for application
    - _Requirements: 3.1_
  
  - [x] 2.2 Create deployment scripts for NotificationAPI
    - Publish .NET application to S3
    - Configure user data script for EC2 instances
    - Set up systemd service for application
    - _Requirements: 3.1_

- [x] 3. Implement networking infrastructure
  - [x] 3.1 Create NetworkingConstruct class
    - Define VPC with public and private subnets across 2 AZs
    - Configure NAT Gateways for private subnet internet access
    - _Requirements: 5.1_
  
  - [x] 3.2 Configure security groups
    - Create ALB security group (allow 80/443 inbound)
    - Create API security group (allow traffic from ALB)
    - Create database security group (allow 5432 from API only)
    - Configure appropriate ingress and egress rules
    - _Requirements: 5.2, 5.5_

- [x] 4. Implement database infrastructure
  - [x] 4.1 Create DatabaseConstruct class
    - Provision Aurora Serverless v2 PostgreSQL cluster
    - Configure capacity settings (0.5-2 ACU)
    - Place database in private subnets
    - Enable automatic backups with 7-day retention
    - _Requirements: 4.1, 4.2, 4.5_
  
  - [x] 4.2 Configure database credentials management
    - Create Secrets Manager secret for database credentials
    - Generate connection string with proper format
    - Configure secret rotation policy
    - _Requirements: 4.3, 4.4_

- [x] 5. Implement messaging infrastructure
  - [x] 5.1 Create MessagingConstruct class
    - Create SQS standard queue for notifications
    - Configure message retention period (4 days)
    - Set visibility timeout (30 seconds)
    - Enable encryption with AWS managed keys
    - _Requirements: 6.1, 6.2_

- [x] 6. Implement compute infrastructure
  - [x] 6.1 Create ComputeConstruct class and ECS cluster
    - Create ECS cluster for Fargate services
    - Configure cluster settings and logging
    - _Requirements: 3.4_
  
  - [x] 6.2 Configure Docker image assets
    - Create DockerImageAsset for ContosoUniversity API
    - Create DockerImageAsset for NotificationAPI
    - Configure CDK to build images in cloud and push to ECR
    - _Requirements: 3.2, 3.3_
  
  - [x] 6.3 Create ContosoUniversity API Fargate service
    - Define task definition (512 CPU, 1024 MB memory)
    - Configure container with port 80
    - Set environment variables (connection string, NotificationAPI URL)
    - Configure secrets from Secrets Manager
    - Set desired count to 1
    - _Requirements: 3.4, 3.5, 9.1, 9.4_
  
  - [x] 6.4 Create NotificationAPI Fargate service
    - Define task definition (256 CPU, 512 MB memory)
    - Configure container with port 80
    - Set environment variables (AWS region, SQS queue URL)
    - Configure IAM task role with SQS permissions
    - Set desired count to 1
    - _Requirements: 3.4, 3.5, 6.3, 6.4, 6.5, 9.1_
  
  - [x] 6.5 Configure CloudWatch logging
    - Create log groups for each service
    - Set 7-day retention period
    - Configure awslogs driver for containers
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 7. Implement load balancer and routing
  - [x] 7.1 Create Application Load Balancer
    - Configure internet-facing ALB in public subnets
    - Set up HTTP listener (port 80)
    - Configure ALB security group
    - _Requirements: 5.3_
  
  - [x] 7.2 Configure target groups
    - Create target group for ContosoUniversity API
    - Create target group for NotificationAPI
    - Configure health checks for each service
    - Set deregistration delay and thresholds
    - _Requirements: 5.4, 7.5_
  
  - [x] 7.3 Configure path-based routing
    - Add listener rule for /api/notifications/* → NotificationAPI (priority 1)
    - Add default rule for /* → ContosoUniversity API (priority 2)
    - Configure forwarding actions
    - _Requirements: 7.1, 7.2, 7.4_

- [x] 8. Implement frontend infrastructure
  - [x] 8.1 Create FrontendConstruct class
    - Create S3 bucket for static website hosting
    - Configure bucket policies for CloudFront access
    - Set up Origin Access Identity
    - _Requirements: 2.2_
  
  - [x] 8.2 Configure CloudFront distribution
    - Create distribution with S3 origin
    - Configure default root object (index.html)
    - Set up error response for SPA routing (404 → /index.html)
    - Configure caching behavior
    - Enable HTTPS with default CloudFront certificate
    - _Requirements: 2.3, 2.4, 2.5_
  
  - [x] 8.3 Configure S3 deployment
    - Use BucketDeployment construct to upload built React assets
    - Configure CloudFront cache invalidation on deployment
    - Set up deployment to run after React build
    - _Requirements: 2.1, 2.2, 10.4_

- [x] 9. Configure environment variables and CORS
  - [x] 9.1 Update ContosoUniversity API environment configuration
    - Inject database connection string from Secrets Manager
    - Inject NotificationAPI base URL (ALB DNS)
    - Set ASPNETCORE_ENVIRONMENT to Production
    - Configure CORS to allow CloudFront origin
    - _Requirements: 9.1, 9.3, 9.4_
  
  - [x] 9.2 Update NotificationAPI environment configuration
    - Inject AWS region
    - Inject SQS queue URL
    - Set ASPNETCORE_ENVIRONMENT to Production
    - _Requirements: 9.1, 6.4, 6.5_

- [x] 10. Implement main CDK stack
  - [x] 10.1 Create ContosoUniversityStack class
    - Instantiate all construct classes in correct order
    - Wire dependencies between constructs
    - Configure stack properties and tags
    - _Requirements: 1.2, 9.5_
  
  - [x] 10.2 Create CDK application entry point
    - Create Program.cs with CDK app initialization
    - Instantiate main stack with environment configuration
    - Configure AWS account and region
    - _Requirements: 1.1, 1.5_
  
  - [x] 10.3 Add stack outputs
    - Output ALB DNS name
    - Output CloudFront distribution URL
    - Output database endpoint
    - Output SQS queue URL
    - _Requirements: 10.5_

- [x] 11. Create deployment scripts and documentation
  - [x] 11.1 Create deployment scripts
    - Write script to build React UI
    - Write script to synthesize CDK stack
    - Write script to deploy CDK stack
    - Write script to destroy stack
    - _Requirements: 10.2, 10.3_
  
  - [x] 11.2 Create deployment documentation
    - Document prerequisites (AWS CLI, .NET SDK, Node.js)
    - Document deployment steps
    - Document environment configuration
    - Document troubleshooting common issues
    - _Requirements: 10.1, 10.2_

- [x] 12. Validate and test deployment
  - [x] 12.1 Synthesize CloudFormation template
    - Run cdk synth to generate CloudFormation
    - Validate template structure
    - Review generated resources
    - _Requirements: 1.3, 1.4_
  
  - [x] 12.2 Perform test deployment
    - Deploy stack to AWS account
    - Verify all resources are created correctly
    - Test frontend access via CloudFront
    - Test API access via ALB
    - Verify database connectivity
    - Test SQS message flow
    - _Requirements: 10.2, 10.5_
