# Contoso University CDK Infrastructure

This project contains the AWS CDK infrastructure code for deploying the Contoso University application to AWS.

## Architecture

The infrastructure deploys:
- **Frontend**: React UI hosted on S3 with CloudFront CDN
- **Backend APIs**: Two ASP.NET Core APIs running on EC2 Auto Scaling Groups
  - ContosoUniversity API (main application) - t3.small instances
  - NotificationAPI (notification service) - t3.micro instances
- **Database**: Aurora Serverless v2 PostgreSQL
- **Messaging**: SQS queue for asynchronous notifications
- **Networking**: VPC with public/private subnets, ALB, security groups

**No Docker Required**: Applications run directly on Amazon Linux 2023 with .NET 8 runtime.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [AWS CLI](https://aws.amazon.com/cli/) configured with credentials
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html): `npm install -g aws-cdk`
- [Node.js](https://nodejs.org/) (for building the React UI)

**Note**: Docker is NOT required! This stack deploys to EC2 instances instead of containers.

## Project Structure

```
ContosoUniversityCdk/
├── Program.cs                          # CDK app entry point
├── ContosoUniversityStack.cs           # Main stack definition
├── Constructs/
│   ├── NetworkingConstruct.cs          # VPC, subnets, security groups
│   ├── DatabaseConstruct.cs            # Aurora Serverless PostgreSQL
│   ├── MessagingConstruct.cs           # SQS queue
│   ├── ComputeConstruct.cs             # ECS cluster, Fargate services
│   └── FrontendConstruct.cs            # S3 bucket, CloudFront
├── cdk.json                            # CDK configuration
└── ContosoUniversityCdk.csproj         # C# project file
```

## Configuration

Configuration can be provided through CDK context in `cdk.json` or via command-line:

```json
{
  "context": {
    "environmentName": "dev",
    "databaseMinCapacity": 0.5,
    "databaseMaxCapacity": 2.0,
    "domainName": "example.com",
    "certificateArn": "arn:aws:acm:..."
  }
}
```

Or via command line:
```bash
cdk deploy -c environmentName=prod -c databaseMinCapacity=1.0
```

## Environment Variables

Set these environment variables before deployment:

```bash
export CDK_DEFAULT_ACCOUNT=123456789012
export CDK_DEFAULT_REGION=us-east-1
```

Or use AWS CLI configuration:
```bash
export CDK_DEFAULT_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
export CDK_DEFAULT_REGION=$(aws configure get region)
```

## Quick Start

### 🚀 One-Command Deployment (No Docker Required!)

```bash
cd ContosoUniversityCdk
./deploy-ec2.sh
```

This script will:
1. Verify prerequisites
2. Bootstrap CDK if needed
3. Build React UI
4. Publish .NET applications to S3
5. Deploy all infrastructure (EC2, ALB, Aurora, CloudFront, etc.)

### 📚 Documentation

- **[EC2_DEPLOYMENT.md](./EC2_DEPLOYMENT.md)** - EC2 deployment guide
- **[REACT_UI_DEPLOYMENT.md](./REACT_UI_DEPLOYMENT.md)** - React UI deployment and configuration
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Full deployment documentation
- **[TEST_DEPLOYMENT_GUIDE.md](./TEST_DEPLOYMENT_GUIDE.md)** - Testing guide

### Automated Deployment Scripts

Use the provided scripts for easy deployment:

```bash
# Build React UI
./scripts/build-ui.sh

# Synthesize CloudFormation template
./scripts/synth.sh

# Deploy to AWS (builds UI and deploys everything)
./scripts/deploy.sh

# Destroy all resources
./scripts/destroy.sh
```

### Manual Deployment Commands

#### First-time Setup

Bootstrap your AWS environment (only needed once per account/region):
```bash
cdk bootstrap
```

#### Build the React UI

Before deploying, build the React frontend:
```bash
cd ../contoso-university-ui
npm install
npm run build
cd ../ContosoUniversityCdk
```

#### Synthesize CloudFormation Template

Generate the CloudFormation template:
```bash
cdk synth
```

#### Deploy Infrastructure

Deploy the stack to AWS:
```bash
cdk deploy
```

The deployment will:
1. Build Docker images for both APIs in the cloud
2. Push images to ECR
3. Create all infrastructure resources
4. Deploy the React UI to S3 and CloudFront

#### View Stack Outputs

After deployment, important endpoints will be displayed:
- ALB DNS name (for API access)
- CloudFront distribution URL (for frontend access)
- Database endpoint
- SQS queue URL

#### Destroy Infrastructure

Remove all resources:
```bash
cdk destroy
```

## Useful CDK Commands

- `cdk ls` - List all stacks in the app
- `cdk synth` - Synthesize CloudFormation template
- `cdk diff` - Compare deployed stack with current state
- `cdk deploy` - Deploy stack to AWS
- `cdk destroy` - Remove stack from AWS
- `cdk doctor` - Check CDK environment

## Development

### Restore Dependencies

```bash
dotnet restore
```

### Build Project

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

## Troubleshooting

For comprehensive troubleshooting guidance, see [DEPLOYMENT.md](./DEPLOYMENT.md#troubleshooting).

Common issues:
- AWS credentials not configured
- CDK not bootstrapped
- Docker build failures
- Database connection issues
- ECS tasks not starting

## Security Considerations

- Database credentials are stored in AWS Secrets Manager
- Database is deployed in private subnets
- Security groups follow principle of least privilege
- All resources are tagged for tracking and cost allocation

## Cost Optimization

- Aurora Serverless v2 scales down to 0.5 ACU when idle
- CloudWatch logs have 7-day retention
- Consider using Reserved Instances for production workloads
- Monitor costs using AWS Cost Explorer

## License

This infrastructure code is part of the Contoso University project.
