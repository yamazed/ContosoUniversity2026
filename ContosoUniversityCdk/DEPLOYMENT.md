# Contoso University AWS Deployment Guide

This guide provides step-by-step instructions for deploying the Contoso University application to AWS using AWS CDK.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Architecture Overview](#architecture-overview)
- [Deployment Steps](#deployment-steps)
- [Environment Configuration](#environment-configuration)
- [Post-Deployment](#post-deployment)
- [Troubleshooting](#troubleshooting)
- [Cleanup](#cleanup)

## Prerequisites

Before deploying, ensure you have the following installed and configured:

### Required Software

1. **AWS CLI** (version 2.x or later)
   - Installation: https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html
   - Verify: `aws --version`

2. **.NET SDK** (version 8.0 or later)
   - Installation: https://dotnet.microsoft.com/download
   - Verify: `dotnet --version`

3. **Node.js** (version 18.x or later)
   - Installation: https://nodejs.org/
   - Verify: `node --version` and `npm --version`

4. **AWS CDK CLI** (version 2.x)
   - Installation: `npm install -g aws-cdk`
   - Verify: `cdk --version`

**Note**: Docker is NOT required for deployment. The application runs directly on EC2 instances with .NET 8 runtime.

### AWS Account Setup

1. **AWS Account**: You need an active AWS account with appropriate permissions

2. **AWS Credentials**: Configure AWS CLI with your credentials
   ```bash
   aws configure
   ```
   You'll need:
   - AWS Access Key ID
   - AWS Secret Access Key
   - Default region (e.g., `us-east-1`)
   - Default output format (e.g., `json`)

3. **IAM Permissions**: Your AWS user/role needs permissions to create:
   - VPC and networking resources
   - ECS clusters and services
   - RDS Aurora clusters
   - S3 buckets
   - CloudFront distributions
   - Application Load Balancers
   - SQS queues
   - CloudWatch log groups
   - IAM roles and policies
   - Secrets Manager secrets

4. **CDK Bootstrap**: Bootstrap your AWS account for CDK (one-time setup per account/region)
   ```bash
   cdk bootstrap aws://ACCOUNT-NUMBER/REGION
   ```
   Example:
   ```bash
   cdk bootstrap aws://123456789012/us-east-1
   ```

## Architecture Overview

The deployment creates the following AWS resources:

- **Frontend**: React UI hosted on S3 + CloudFront CDN
- **Backend APIs**: Two ASP.NET Core services on EC2 Auto Scaling Groups
  - ContosoUniversity API (main application) - t3.small instances
  - NotificationAPI (notification service) - t3.micro instances
- **Database**: Aurora Serverless v2 PostgreSQL cluster
- **Networking**: VPC with public/private subnets across 2 AZs
- **Load Balancer**: Application Load Balancer for API routing
- **Messaging**: SQS queue for asynchronous notifications
- **Logging**: CloudWatch log groups for all services

## Deployment Steps

### Step 1: Clone and Prepare the Repository

```bash
# Navigate to the project root
cd /path/to/contoso-university

# Ensure all dependencies are available
cd ContosoUniversityCdk
dotnet restore
```

### Step 2: Build the React UI

```bash
# From the ContosoUniversityCdk directory
./scripts/build-ui.sh
```

This script will:
- Navigate to the React UI directory
- Install npm dependencies (if needed)
- Build the production bundle

### Step 3: Synthesize the CDK Stack (Optional)

To preview the CloudFormation template without deploying:

```bash
./scripts/synth.sh
```

This generates the CloudFormation template in `cdk.out/` directory.

### Step 4: Deploy to AWS

```bash
./scripts/deploy.sh
```

This script will:
1. Build the React UI
2. Restore .NET dependencies
3. Deploy the CDK stack to AWS

**Expected Duration**: 10-15 minutes

The deployment process will:
- Publish .NET applications to S3
- Create all infrastructure resources
- Deploy EC2 instances with Auto Scaling
- Configure the application on EC2 instances

### Step 5: Verify Deployment

After deployment completes, you'll see stack outputs including:
- ALB DNS name (for API access)
- CloudFront distribution URL (for frontend access)
- Database endpoint
- SQS queue URL

## Environment Configuration

### CDK Context Variables

You can customize the deployment by modifying `cdk.json` or passing context variables:

```json
{
  "context": {
    "environmentName": "dev",
    "databaseMinCapacity": 0.5,
    "databaseMaxCapacity": 2.0
  }
}
```

Or pass via command line:

```bash
cdk deploy -c environmentName=prod -c databaseMinCapacity=1
```

### Environment-Specific Settings

#### Development Environment
- Database: 0.5-1 ACU
- ECS Tasks: Minimal CPU/memory
- Log retention: 7 days

#### Production Environment
- Database: 1-4 ACU (adjust based on load)
- ECS Tasks: Increased CPU/memory
- Log retention: 30+ days
- Enable CloudFront custom domain
- Enable ALB HTTPS with ACM certificate

### Database Initialization

After first deployment, you need to initialize the database:

1. Connect to the database using the connection string from Secrets Manager
2. Run Entity Framework migrations:
   ```bash
   # From ContosoUniversity directory
   dotnet ef database update
   ```

Alternatively, the application can run migrations on startup if configured.

## Post-Deployment

### Accessing the Application

1. **Frontend**: Use the CloudFront distribution URL from stack outputs
   ```
   https://d1234567890abc.cloudfront.net
   ```

2. **API**: Use the ALB DNS name
   ```
   http://contoso-alb-1234567890.us-east-1.elb.amazonaws.com
   ```

### Monitoring

1. **CloudWatch Logs**: View application logs
   - Log group: `/ecs/contoso-api`
   - Log group: `/ecs/notification-api`

2. **ECS Console**: Monitor service health and task status

3. **RDS Console**: Monitor database performance and connections

### Updating the Application

To deploy updates:

1. Make code changes
2. Run deployment script again:
   ```bash
   ./scripts/deploy.sh
   ```

CDK will detect changes and update only affected resources.

## Troubleshooting

### Common Issues

#### 1. AWS Credentials Not Configured

**Error**: `Unable to locate credentials`

**Solution**:
```bash
aws configure
# Enter your AWS credentials
```

#### 2. CDK Not Bootstrapped

**Error**: `This stack uses assets, so the toolkit stack must be deployed`

**Solution**:
```bash
cdk bootstrap aws://ACCOUNT-NUMBER/REGION
```

#### 3. React Build Fails

**Error**: `npm run build failed`

**Solution**:
```bash
cd contoso-university-ui
rm -rf node_modules package-lock.json
npm install
npm run build
```

#### 5. Database Connection Issues

**Error**: `Could not connect to database`

**Solution**:
- Verify security group rules allow ECS tasks to access RDS
- Check that connection string in Secrets Manager is correct
- Ensure database is in "available" state in RDS console

#### 6. EC2 Instances Not Starting

**Error**: `Instance failed health checks`

**Solution**:
- Check CloudWatch logs for application errors
- Verify environment variables are set correctly
- Ensure .NET applications were published to S3
- Check IAM instance role has necessary permissions
- Verify security groups allow traffic

#### 7. CloudFront Not Serving Content

**Error**: `403 Forbidden` or `404 Not Found`

**Solution**:
- Verify S3 bucket has files in it
- Check CloudFront origin access identity permissions
- Wait for CloudFront distribution to fully deploy (can take 15-20 minutes)
- Invalidate CloudFront cache: `aws cloudfront create-invalidation --distribution-id ID --paths "/*"`

#### 8. Insufficient Permissions

**Error**: `User is not authorized to perform: [action]`

**Solution**:
- Ensure your IAM user/role has necessary permissions
- Required permissions include: EC2, Auto Scaling, RDS, S3, CloudFront, IAM, CloudWatch, Secrets Manager

### Viewing Logs

```bash
# View EC2 application logs
aws logs tail /ec2/contoso-api --follow

# View specific log stream
aws logs get-log-events --log-group-name /ec2/contoso-api --log-stream-name [instance-id]
```

### Debugging CDK Issues

```bash
# Synthesize with verbose output
cdk synth --verbose

# View CDK diff before deploying
cdk diff

# Deploy with verbose logging
cdk deploy --verbose
```

## Cleanup

To delete all AWS resources and avoid ongoing charges:

```bash
./scripts/destroy.sh
```

**Warning**: This will permanently delete:
- All data in the database
- All files in S3 buckets
- All logs in CloudWatch
- All infrastructure resources

The script will prompt for confirmation before proceeding.

### Manual Cleanup (if script fails)

If the destroy script fails, you can manually delete resources:

1. Delete the CloudFormation stack from AWS Console
2. Empty and delete S3 buckets manually if needed
3. Delete any remaining resources in the VPC

## Cost Estimation

Approximate monthly costs (us-east-1 region):

- **Aurora Serverless v2**: $40-80 (0.5-2 ACU)
- **EC2 Instances**: $15-30 (t3.small + t3.micro running 24/7)
- **Application Load Balancer**: $20-25
- **CloudFront**: $1-10 (depends on traffic)
- **S3**: $1-5 (depends on storage)
- **SQS**: $0-1 (first 1M requests free)
- **CloudWatch Logs**: $1-5
- **NAT Gateway**: $30-45

**Total**: ~$110-200/month for development environment

To reduce costs:
- Use smaller Aurora capacity (0.5 ACU minimum)
- Use smaller EC2 instance types or reduce instance count
- Delete resources when not in use
- Use AWS Free Tier where applicable (t3.micro eligible)

## Additional Resources

- [AWS CDK Documentation](https://docs.aws.amazon.com/cdk/)
- [AWS EC2 Documentation](https://docs.aws.amazon.com/ec2/)
- [AWS Auto Scaling Documentation](https://docs.aws.amazon.com/autoscaling/)
- [Aurora Serverless Documentation](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/aurora-serverless-v2.html)
- [CloudFront Documentation](https://docs.aws.amazon.com/cloudfront/)

## Support

For issues specific to this deployment:
1. Check the troubleshooting section above
2. Review CloudWatch logs for error messages
3. Verify all prerequisites are met
4. Ensure AWS credentials have sufficient permissions
