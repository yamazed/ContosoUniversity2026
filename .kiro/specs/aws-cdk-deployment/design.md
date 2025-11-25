# Design Document

## Overview

This design document describes the AWS infrastructure architecture for deploying the Contoso University application using AWS CDK with C#. The solution deploys a three-tier web application consisting of a React frontend, two ASP.NET Core backend APIs, and a PostgreSQL database. The infrastructure leverages AWS managed services to provide a scalable, secure, and cost-effective deployment without requiring Docker.

The deployment uses EC2 Auto Scaling Groups for the API services, Aurora Serverless v2 for the database, S3 and CloudFront for static frontend hosting, and SQS for asynchronous notification processing. All infrastructure is defined as code using C# CDK, enabling version control and reproducible deployments.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Internet Users                           │
└────────────┬────────────────────────────────┬───────────────────┘
             │                                │
             │ HTTPS                          │ HTTPS
             ▼                                ▼
    ┌────────────────┐              ┌─────────────────┐
    │   CloudFront   │              │  Application    │
    │      CDN       │              │ Load Balancer   │
    └────────┬───────┘              └────────┬────────┘
             │                               │
             │                               │ Path-based routing
             ▼                               │
    ┌────────────────┐              ┌───────┴────────┬──────────────┐
    │   S3 Bucket    │              │                │              │
    │  (React UI)    │              ▼                ▼              │
    └────────────────┘     ┌─────────────┐  ┌──────────────┐       │
                           │ Contoso API │  │Notification  │       │
                           │  (EC2 ASG)  │  │ API (EC2 ASG)│       │
                           └──────┬──────┘  └──────┬───────┘       │
                                  │                │               │
                                  │                │               │
                                  ▼                ▼               │
                           ┌──────────────┐  ┌──────────┐         │
                           │   Aurora     │  │   SQS    │         │
                           │ Serverless   │  │  Queue   │         │
                           │ PostgreSQL   │  └──────────┘         │
                           └──────────────┘                       │
                                  ▲                                │
                                  │                                │
                                  └────────────────────────────────┘
                                    Secrets Manager (DB Creds)
```

### Network Architecture

The infrastructure uses a VPC with the following structure:

- **VPC**: Single VPC spanning multiple availability zones
- **Public Subnets**: Host the Application Load Balancer and NAT Gateways
- **Private Subnets**: Host ECS Fargate tasks and Aurora database
- **Availability Zones**: Resources distributed across 2 AZs for high availability

### Component Interactions

1. **Frontend → Backend**: Users access React UI via CloudFront, which makes API calls to the ALB
2. **ALB → APIs**: ALB routes requests based on path patterns to appropriate ECS services
3. **ContosoUniversity API → Database**: Connects to Aurora using credentials from Secrets Manager
4. **ContosoUniversity API → NotificationAPI**: Makes HTTP calls via ALB internal endpoint
5. **NotificationAPI → SQS**: Sends and receives notification messages asynchronously

## Components and Interfaces

### 1. CDK Application Structure

**ContosoUniversityCdkApp**
- Entry point for the CDK application
- Instantiates the main stack
- Configures environment and account settings

**ContosoUniversityStack**
- Main CDK stack containing all infrastructure resources
- Organized into logical sections: networking, database, compute, storage, messaging

### 2. Networking Components

**VPC Configuration**
```csharp
public class NetworkingConstruct : Construct
{
    public IVpc Vpc { get; }
    public ISecurityGroup AlbSecurityGroup { get; }
    public ISecurityGroup ApiSecurityGroup { get; }
    public ISecurityGroup DatabaseSecurityGroup { get; }
}
```

- Creates VPC with public and private subnets across 2 AZs
- Configures security groups for ALB, ECS tasks, and database
- Sets up NAT Gateways for private subnet internet access

**Security Group Rules**:
- ALB SG: Allows inbound 80/443 from internet, outbound to API SG
- API SG: Allows inbound from ALB SG, outbound to database SG and internet
- Database SG: Allows inbound 5432 from API SG only

### 3. Database Components

**Aurora Serverless v2 Cluster**
```csharp
public class DatabaseConstruct : Construct
{
    public IDatabaseCluster Cluster { get; }
    public ISecret DatabaseSecret { get; }
    public string ConnectionStringSecretArn { get; }
}
```

- Engine: Aurora PostgreSQL compatible
- Capacity: 0.5 - 2 ACU (configurable)
- Backup retention: 7 days
- Credentials stored in Secrets Manager
- Deployed in private subnets
- Automatic minor version upgrades enabled

**Connection String Format**:
```
Host={cluster-endpoint};Database=contoso;Username={username};Password={password}
```

### 4. Compute Components

**ECS Cluster**
```csharp
public class ComputeConstruct : Construct
{
    public ICluster EcsCluster { get; }
    public IApplicationLoadBalancer LoadBalancer { get; }
    public FargateService ContosoApiService { get; }
    public FargateService NotificationApiService { get; }
}
```

**ContosoUniversity API Service**:
- Task Definition:
  - CPU: 512 (0.5 vCPU)
  - Memory: 1024 MB
  - Container Port: 80
- Environment Variables:
  - `ConnectionStrings__DefaultConnection`: From Secrets Manager
  - `NotificationAPI__BaseUrl`: ALB DNS with path prefix
  - `ASPNETCORE_ENVIRONMENT`: Production
- Desired Count: 1
- Health Check: GET /api/students (path: /, interval: 30s)

**NotificationAPI Service**:
- Task Definition:
  - CPU: 256 (0.25 vCPU)
  - Memory: 512 MB
  - Container Port: 80
- Environment Variables:
  - `AWS__Region`: Stack region
  - `AWS__SQS__QueueUrl`: SQS queue URL
  - `ASPNETCORE_ENVIRONMENT`: Production
- Desired Count: 1
- Health Check: GET /api/notifications (interval: 30s)
- IAM Permissions: SQS SendMessage, ReceiveMessage, DeleteMessage

**EC2 Auto Scaling Groups**:
```csharp
// ContosoUniversity API Auto Scaling Group
var contosoApiAsg = new AutoScalingGroup(this, "ContosoApiAsg", new AutoScalingGroupProps
{
    Vpc = vpc,
    InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.SMALL),
    MachineImage = MachineImage.LatestAmazonLinux2023(),
    MinCapacity = 1,
    MaxCapacity = 3,
    DesiredCapacity = 1
});

// NotificationAPI Auto Scaling Group
var notificationApiAsg = new AutoScalingGroup(this, "NotificationApiAsg", new AutoScalingGroupProps
{
    Vpc = vpc,
    InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO),
    MachineImage = MachineImage.LatestAmazonLinux2023(),
    MinCapacity = 1,
    MaxCapacity = 2,
    DesiredCapacity = 1
});
```

### 5. Load Balancer Configuration

**Application Load Balancer**:
- Scheme: Internet-facing
- Subnets: Public subnets
- Listeners:
  - HTTP (80): Redirects to HTTPS
  - HTTPS (443): Routes to target groups based on path

**Path-Based Routing**:
- `/api/notifications/*` → NotificationAPI target group (priority 1)
- `/*` → ContosoUniversity API target group (priority 2, default)

**Target Groups**:
- Protocol: HTTP
- Port: 80
- Health check interval: 30 seconds
- Healthy threshold: 2
- Unhealthy threshold: 3
- Timeout: 5 seconds
- Deregistration delay: 30 seconds

### 6. Messaging Components

**SQS Queue**
```csharp
public class MessagingConstruct : Construct
{
    public IQueue NotificationQueue { get; }
}
```

- Queue Type: Standard
- Message Retention: 4 days (345,600 seconds)
- Visibility Timeout: 30 seconds
- Receive Message Wait Time: 0 seconds (short polling)
- Encryption: AWS managed keys (SSE-SQS)

### 7. Storage and CDN Components

**S3 Bucket for Frontend**
```csharp
public class FrontendConstruct : Construct
{
    public IBucket WebsiteBucket { get; }
    public IDistribution CloudFrontDistribution { get; }
}
```

- Bucket Configuration:
  - Public read access via CloudFront OAI
  - Versioning: Disabled
  - Auto delete objects on stack deletion (for dev)
  
**CloudFront Distribution**:
- Origin: S3 bucket with Origin Access Identity
- Default Root Object: index.html
- Error Responses: 404 → /index.html (for SPA routing)
- Price Class: PriceClass_100 (North America and Europe)
- Certificate: ACM certificate (optional, requires domain)
- Caching: Optimized for static content

**S3 Deployment**:
```csharp
new BucketDeployment(this, "DeployWebsite", new BucketDeploymentProps
{
    Sources = new[] { Source.Asset("../contoso-university-ui/dist") },
    DestinationBucket = websiteBucket,
    Distribution = distribution,
    DistributionPaths = new[] { "/*" }
});
```

### 8. Logging Components

**CloudWatch Log Groups**:
- `/ec2/contoso-api`: ContosoUniversity API logs
- `/ec2/notification-api`: NotificationAPI logs
- Retention: 7 days
- CloudWatch Agent: Configured on all EC2 instances

## Data Models

### CDK Stack Configuration

```csharp
public class ContosoUniversityStackProps : StackProps
{
    public string EnvironmentName { get; set; } = "dev";
    public double DatabaseMinCapacity { get; set; } = 0.5;
    public double DatabaseMaxCapacity { get; set; } = 2.0;
    public string DomainName { get; set; } // Optional for custom domain
    public string CertificateArn { get; set; } // Optional for HTTPS
}
```

### Environment Variables Schema

**ContosoUniversity API**:
```json
{
  "ConnectionStrings__DefaultConnection": "Host=...;Database=contoso;Username=...;Password=...",
  "NotificationAPI__BaseUrl": "http://internal-alb-xxx.region.elb.amazonaws.com",
  "ASPNETCORE_ENVIRONMENT": "Production",
  "AllowedHosts": "*"
}
```

**NotificationAPI**:
```json
{
  "AWS__Region": "us-east-1",
  "AWS__SQS__QueueUrl": "https://sqs.us-east-1.amazonaws.com/account/queue-name",
  "ASPNETCORE_ENVIRONMENT": "Production"
}
```

### Application Deployment Structure

**ContosoUniversity API Deployment**:
```bash
# Publish .NET application
dotnet publish -c Release -o ./publish

# Package for S3
zip -r contoso-api.zip ./publish/*

# Upload to S3
aws s3 cp contoso-api.zip s3://contoso-deployment-{account}/

# EC2 User Data downloads and deploys
aws s3 cp s3://contoso-deployment-{account}/contoso-api.zip .
unzip contoso-api.zip
dotnet ContosoUniversity.dll
```

**NotificationAPI Deployment**:
```bash
# Publish .NET application
dotnet publish -c Release -o ./publish

# Package for S3
zip -r notification-api.zip ./publish/*

# Upload to S3
aws s3 cp notification-api.zip s3://contoso-deployment-{account}/

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "NotificationAPI.dll"]
```

## Correctness Properties

