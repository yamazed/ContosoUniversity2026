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

- [x] 6. Implement compute infrastructure (EC2 Auto Scaling Groups)
  - [x] 6.1 Create ComputeConstructEC2 class
    - Create IAM role for EC2 instances with necessary permissions
    - Configure permissions for Secrets Manager, SQS, S3, and CloudWatch
    - _Requirements: 3.4, 8.4, 9.2_
  
  - [x] 6.2 Create ContosoUniversity API Auto Scaling Group
    - Configure t3.small instances with Amazon Linux 2023
    - Define user data script to install .NET 8 runtime
    - Configure systemd service for application
    - Download application from S3 deployment bucket
    - Set min/max/desired capacity (1/3/1)
    - Place instances in private subnets
    - _Requirements: 3.1, 3.4, 3.5, 9.1, 9.4_
  
  - [x] 6.3 Create NotificationAPI Auto Scaling Group
    - Configure t3.micro instances with Amazon Linux 2023
    - Define user data script to install .NET 8 runtime
    - Configure systemd service for application
    - Download application from S3 deployment bucket
    - Set min/max/desired capacity (1/2/1)
    - Place instances in private subnets
    - _Requirements: 3.1, 3.4, 3.5, 6.3, 6.4, 6.5, 9.1_
  
  - [x] 6.4 Configure CloudWatch logging
    - Install CloudWatch agent on EC2 instances
    - Create log groups for each service (/ec2/contoso-api, /ec2/notification-api)
    - Set 7-day retention period
    - Configure log streaming from systemd journals
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 7. Implement load balancer and routing
  - [x] 7.1 Create Application Load Balancer
    - Configure internet-facing ALB in public subnets
    - Set up HTTP listener (port 80)
    - Configure ALB security group
    - _Requirements: 5.3_
  
  - [x] 7.2 Configure target groups
    - Create target group for ContosoUniversity API (instance type)
    - Create target group for NotificationAPI (instance type)
    - Configure health checks for each service
    - Set deregistration delay and thresholds
    - Attach Auto Scaling Groups to target groups
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
    - Inject database credentials from Secrets Manager via user data
    - Inject NotificationAPI base URL (ALB DNS)
    - Set ASPNETCORE_ENVIRONMENT to Production
    - Configure CORS to allow CloudFront origin
    - _Requirements: 9.1, 9.3, 9.4_
  
  - [x] 9.2 Update NotificationAPI environment configuration
    - Inject AWS region via user data
    - Inject SQS queue URL via user data
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
    - Output ALB DNS name and URL
    - Output CloudFront distribution URL and ID
    - Output database endpoint and secret ARN
    - Output SQS queue URL and ARN
    - Output S3 bucket name and Auto Scaling Group names
    - Output VPC ID
    - _Requirements: 10.5_

- [x] 11. Create deployment scripts and documentation
  - [x] 11.1 Create deployment scripts
    - Write script to build React UI (build-ui.sh)
    - Write script to publish .NET apps to S3 (publish-apps.sh)
    - Write script to synthesize CDK stack (synth.sh)
    - Write script to deploy CDK stack (deploy.sh)
    - Write script to destroy stack (destroy.sh)
    - Write script to update running instances (update-instances.sh)
    - Write script to check EC2 status (check-ec2-status.sh)
    - Write script to verify deployment (verify-deployment.sh)
    - _Requirements: 10.2, 10.3_

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
    - Verify EC2 instances are healthy and applications are running
    - _Requirements: 10.2, 10.5_


