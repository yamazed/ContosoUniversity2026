# EC2 Deployment Guide

This guide explains how to deploy Contoso University to EC2 instances **without Docker**.

## Overview

The application runs directly on Amazon Linux 2023 EC2 instances with .NET 8 runtime. No containers or Docker required!

## Architecture

```
Internet
   ↓
CloudFront (React UI)
   ↓
Application Load Balancer
   ↓
┌─────────────────────────────────────┐
│  Auto Scaling Group (Contoso API)  │
│  - t3.small instances               │
│  - Min: 1, Max: 3                   │
│  - .NET 8 runtime                   │
│  - systemd service                  │
└─────────────────────────────────────┘
   ↓
┌─────────────────────────────────────┐
│ Auto Scaling Group (Notification)   │
│  - t3.micro instances               │
│  - Min: 1, Max: 2                   │
│  - .NET 8 runtime                   │
│  - systemd service                  │
└─────────────────────────────────────┘
   ↓
Aurora Serverless PostgreSQL
```

## Prerequisites

- AWS CLI configured
- AWS CDK CLI installed (`npm install -g aws-cdk`)
- .NET 8.0 SDK
- Node.js (for React UI)

## Quick Deployment

```bash
cd ContosoUniversityCdk
./deploy-ec2.sh
```

## Manual Deployment Steps

### Step 1: Bootstrap CDK

```bash
export CDK_DEFAULT_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
export CDK_DEFAULT_REGION=$(aws configure get region)
cdk bootstrap aws://$CDK_DEFAULT_ACCOUNT/$CDK_DEFAULT_REGION
```

### Step 2: Build React UI

```bash
cd ../contoso-university-ui
npm install
npm run build
cd ../ContosoUniversityCdk
```

### Step 3: Publish .NET Applications

```bash
./scripts/publish-apps.sh
```

This script:
- Creates S3 bucket for deployments
- Publishes ContosoUniversity API
- Publishes NotificationAPI
- Uploads ZIP files to S3

### Step 4: Deploy Infrastructure

```bash
cdk deploy
```

This creates:
- VPC with public/private subnets
- Aurora Serverless v2 PostgreSQL
- Application Load Balancer
- EC2 Auto Scaling Groups
- S3 + CloudFront for React UI
- SQS queue for notifications

### Step 5: Wait for Initialization

EC2 instances take 5-10 minutes to:
- Install .NET 8 runtime
- Download applications from S3
- Configure systemd services
- Start applications

## How It Works

### Application Deployment

1. **Build**: .NET apps are published with `dotnet publish`
2. **Package**: Published files are zipped
3. **Upload**: ZIP files uploaded to S3
4. **Download**: EC2 instances download from S3 on startup
5. **Run**: systemd services manage the applications

### User Data Script

Each EC2 instance runs a user data script that:
- Installs .NET 8 runtime
- Downloads application from S3
- Creates systemd service
- Configures CloudWatch logging
- Starts the application

### Application Updates

To update running applications:

```bash
# Publish new versions
./scripts/publish-apps.sh

# Update running instances
./scripts/update-instances.sh
```

The update script uses AWS Systems Manager (SSM) to:
- Download new application versions
- Restart systemd services
- Verify services are running

## Monitoring

### CloudWatch Logs

View application logs:

```bash
# Contoso API logs
aws logs tail /ec2/contoso-api --follow

# Notification API logs
aws logs tail /ec2/notification-api --follow
```

### SSH Access

Connect to instances using Session Manager (no SSH keys needed):

```bash
# List instances
aws ec2 describe-instances \
  --filters "Name=tag:aws:autoscaling:groupName,Values=*contoso*" \
  --query "Reservations[*].Instances[*].[InstanceId,State.Name,Tags[?Key=='Name'].Value|[0]]" \
  --output table

# Connect to instance
aws ssm start-session --target <instance-id>

# Check service status
sudo systemctl status contoso-api
sudo systemctl status notification-api

# View logs
sudo journalctl -u contoso-api -f
sudo journalctl -u notification-api -f
```

## Scaling

### Manual Scaling

Update desired capacity:

```bash
aws autoscaling set-desired-capacity \
  --auto-scaling-group-name <asg-name> \
  --desired-capacity 2
```

### Auto Scaling Policies

Add CPU-based scaling:

```bash
aws autoscaling put-scaling-policy \
  --auto-scaling-group-name <asg-name> \
  --policy-name scale-up \
  --scaling-adjustment 1 \
  --adjustment-type ChangeInCapacity
```

Or modify the CDK code to add scaling policies.

## Cost Optimization

### Instance Sizing

Current configuration:
- **Contoso API**: t3.small ($0.0208/hour = ~$15/month per instance)
- **Notification API**: t3.micro ($0.0104/hour = ~$7.50/month per instance)

For lower costs, use t3.micro for both:

```csharp
InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO)
```

### Scheduled Scaling

Scale down during off-hours:

```bash
# Scale to 0 at night (dev environments)
aws autoscaling put-scheduled-action \
  --auto-scaling-group-name <asg-name> \
  --scheduled-action-name scale-down-night \
  --recurrence "0 22 * * *" \
  --desired-capacity 0

# Scale up in morning
aws autoscaling put-scheduled-action \
  --auto-scaling-group-name <asg-name> \
  --scheduled-action-name scale-up-morning \
  --recurrence "0 8 * * *" \
  --desired-capacity 1
```

## Troubleshooting

### Application Not Starting

Check user data execution:

```bash
# Connect to instance
aws ssm start-session --target <instance-id>

# Check cloud-init logs
sudo cat /var/log/cloud-init-output.log

# Check if .NET is installed
dotnet --version

# Check if application files exist
ls -la /opt/contoso-api/
ls -la /opt/notification-api/

# Check service status
sudo systemctl status contoso-api
sudo systemctl status notification-api
```

### Database Connection Issues

Verify security groups allow traffic:

```bash
# Check security group rules
aws ec2 describe-security-groups \
  --filters "Name=tag:Name,Values=*api*" \
  --query "SecurityGroups[*].[GroupId,GroupName,IpPermissions]"
```

### Health Check Failures

Check ALB target health:

```bash
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn>
```

Common issues:
- Application not listening on port 80
- Health check path incorrect
- Security group blocking ALB traffic

### Application Not in S3

If instances can't download applications:

```bash
# Verify S3 bucket exists
aws s3 ls s3://contoso-deployment-$ACCOUNT/

# Re-publish applications
./scripts/publish-apps.sh
```

## Comparison: EC2 vs ECS/Fargate

| Feature | EC2 (This Deployment) | ECS/Fargate |
|---------|----------------------|-------------|
| **Docker Required** | ❌ No | ✅ Yes |
| **Setup Complexity** | Low | Medium |
| **Cost** | ~$22/month (2 instances) | ~$30-40/month |
| **Scaling Speed** | 2-3 minutes | 30-60 seconds |
| **Management** | systemd services | Container orchestration |
| **Updates** | SSM commands | New task definitions |
| **Best For** | Simple deployments | Microservices |

## Security

### IAM Roles

EC2 instances have IAM roles with permissions for:
- Reading database secrets from Secrets Manager
- Sending/receiving SQS messages
- Downloading from S3 deployment bucket
- CloudWatch logging
- SSM Session Manager

### Network Security

- Instances run in private subnets (no direct internet access)
- Only ALB can reach instances on port 80
- Database only accessible from API security group
- All traffic encrypted in transit

### Secrets Management

Database credentials stored in AWS Secrets Manager and injected as environment variables.

## Cleanup

Remove all resources:

```bash
cdk destroy
```

This deletes:
- EC2 instances and Auto Scaling Groups
- Load Balancer
- Database (with snapshot)
- VPC and networking
- S3 buckets (if empty)
- CloudFront distribution

Note: S3 deployment bucket must be manually deleted if not empty.

## Next Steps

1. **Custom Domain**: Add Route 53 and ACM certificate
2. **HTTPS**: Configure ALB with SSL certificate
3. **Monitoring**: Add CloudWatch alarms
4. **Backups**: Configure automated database backups
5. **CI/CD**: Set up GitHub Actions or CodePipeline

## Additional Resources

- [AWS EC2 User Guide](https://docs.aws.amazon.com/ec2/)
- [AWS Auto Scaling](https://docs.aws.amazon.com/autoscaling/)
- [AWS Systems Manager](https://docs.aws.amazon.com/systems-manager/)
- [.NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
