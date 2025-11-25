# Test Deployment Guide

## Prerequisites Verification

### ✅ Prerequisites Met
- AWS CLI installed: v2.16.1
- AWS credentials configured: Account 969522832499, User: api-user
- CDK bootstrap completed: CDKToolkit stack exists in eu-west-1
- React UI built: dist folder exists with assets
- .NET SDK available for building Docker images

## Deployment Steps

### Step 1: Review Deployment Plan

Before deploying, review what will be created:

```bash
cd ContosoUniversityCdk
cdk diff
```

This will show you all the resources that will be created, modified, or deleted.

### Step 2: Deploy the Stack

Deploy the complete infrastructure:

```bash
cdk deploy --require-approval never
```

Or with approval prompts for IAM changes:

```bash
cdk deploy
```

**Expected Duration**: 15-25 minutes
- VPC and networking: ~2 minutes
- Aurora Serverless v2: ~5-8 minutes
- ECS services and ALB: ~3-5 minutes
- CloudFront distribution: ~5-10 minutes
- Docker image builds: ~3-5 minutes

### Step 3: Monitor Deployment

The deployment will show progress for each resource. Key milestones:
1. VPC and subnets created
2. Security groups configured
3. Aurora cluster provisioning (longest step)
4. Docker images built and pushed to ECR
5. ECS services starting
6. CloudFront distribution deploying
7. Stack outputs displayed

### Step 4: Capture Stack Outputs

After deployment completes, save the stack outputs:

```bash
aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs' \
  --output table
```

Key outputs to note:
- **ApplicationLoadBalancerUrl**: API endpoint
- **CloudFrontDistributionUrl**: Frontend URL
- **DatabaseClusterEndpoint**: Database endpoint
- **SqsQueueUrl**: Notification queue URL

## Verification Steps

### 1. Verify VPC and Networking

```bash
# Get VPC ID from stack outputs
VPC_ID=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`VpcId`].OutputValue' \
  --output text)

# Verify VPC exists
aws ec2 describe-vpcs --vpc-ids $VPC_ID --region eu-west-1

# Verify subnets (should be 4: 2 public, 2 private)
aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" --region eu-west-1 \
  --query 'Subnets[*].[SubnetId,AvailabilityZone,MapPublicIpOnLaunch,CidrBlock]' \
  --output table

# Verify NAT Gateways (should be 2)
aws ec2 describe-nat-gateways --filter "Name=vpc-id,Values=$VPC_ID" --region eu-west-1 \
  --query 'NatGateways[*].[NatGatewayId,State,SubnetId]' \
  --output table

# Verify security groups (should be 3: ALB, API, Database)
aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VPC_ID" --region eu-west-1 \
  --query 'SecurityGroups[*].[GroupId,GroupName,Description]' \
  --output table
```

### 2. Verify Database

```bash
# Get database endpoint
DB_ENDPOINT=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`DatabaseClusterEndpoint`].OutputValue' \
  --output text)

echo "Database Endpoint: $DB_ENDPOINT"

# Verify Aurora cluster
aws rds describe-db-clusters \
  --region eu-west-1 \
  --query 'DBClusters[?Endpoint==`'$DB_ENDPOINT'`].[DBClusterIdentifier,Status,Engine,EngineVersion,ServerlessV2ScalingConfiguration]' \
  --output table

# Verify database secret exists
SECRET_ARN=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`DatabaseSecretArn`].OutputValue' \
  --output text)

aws secretsmanager describe-secret --secret-id $SECRET_ARN --region eu-west-1
```

### 3. Verify SQS Queue

```bash
# Get queue URL
QUEUE_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`SqsQueueUrl`].OutputValue' \
  --output text)

echo "Queue URL: $QUEUE_URL"

# Verify queue attributes
aws sqs get-queue-attributes \
  --queue-url $QUEUE_URL \
  --attribute-names All \
  --region eu-west-1 \
  --query 'Attributes.{MessageRetentionPeriod:MessageRetentionPeriod,VisibilityTimeout:VisibilityTimeout,QueueArn:QueueArn}' \
  --output table
```

### 4. Verify ECS Services

```bash
# Get cluster name
CLUSTER_NAME=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`EcsClusterName`].OutputValue' \
  --output text)

echo "ECS Cluster: $CLUSTER_NAME"

# List services
aws ecs list-services --cluster $CLUSTER_NAME --region eu-west-1

# Describe ContosoUniversity API service
aws ecs describe-services \
  --cluster $CLUSTER_NAME \
  --services contoso-api-service \
  --region eu-west-1 \
  --query 'services[0].{ServiceName:serviceName,Status:status,DesiredCount:desiredCount,RunningCount:runningCount,TaskDefinition:taskDefinition}' \
  --output table

# Describe NotificationAPI service
aws ecs describe-services \
  --cluster $CLUSTER_NAME \
  --services notification-api-service \
  --region eu-west-1 \
  --query 'services[0].{ServiceName:serviceName,Status:status,DesiredCount:desiredCount,RunningCount:runningCount,TaskDefinition:taskDefinition}' \
  --output table

# Check task health
aws ecs list-tasks --cluster $CLUSTER_NAME --region eu-west-1
```

### 5. Verify Application Load Balancer

```bash
# Get ALB URL
ALB_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerUrl`].OutputValue' \
  --output text)

echo "ALB URL: $ALB_URL"

# Get ALB ARN
ALB_ARN=$(aws elbv2 describe-load-balancers \
  --region eu-west-1 \
  --query 'LoadBalancers[?contains(DNSName, `contoso-alb`)].LoadBalancerArn' \
  --output text)

# Verify target groups
aws elbv2 describe-target-groups \
  --load-balancer-arn $ALB_ARN \
  --region eu-west-1 \
  --query 'TargetGroups[*].[TargetGroupName,HealthCheckPath,HealthCheckIntervalSeconds,HealthyThresholdCount]' \
  --output table

# Check target health for ContosoUniversity API
CONTOSO_TG_ARN=$(aws elbv2 describe-target-groups \
  --load-balancer-arn $ALB_ARN \
  --region eu-west-1 \
  --query 'TargetGroups[?TargetGroupName==`contoso-api-tg`].TargetGroupArn' \
  --output text)

aws elbv2 describe-target-health \
  --target-group-arn $CONTOSO_TG_ARN \
  --region eu-west-1 \
  --query 'TargetHealthDescriptions[*].[Target.Id,TargetHealth.State,TargetHealth.Reason]' \
  --output table

# Check target health for NotificationAPI
NOTIFICATION_TG_ARN=$(aws elbv2 describe-target-groups \
  --load-balancer-arn $ALB_ARN \
  --region eu-west-1 \
  --query 'TargetGroups[?TargetGroupName==`notification-api-tg`].TargetGroupArn' \
  --output text)

aws elbv2 describe-target-health \
  --target-group-arn $NOTIFICATION_TG_ARN \
  --region eu-west-1 \
  --query 'TargetHealthDescriptions[*].[Target.Id,TargetHealth.State,TargetHealth.Reason]' \
  --output table
```

### 6. Verify CloudFront Distribution

```bash
# Get CloudFront URL
CF_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionUrl`].OutputValue' \
  --output text)

echo "CloudFront URL: $CF_URL"

# Get distribution ID
CF_DIST_ID=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionId`].OutputValue' \
  --output text)

# Verify distribution status
aws cloudfront get-distribution \
  --id $CF_DIST_ID \
  --query 'Distribution.{Id:Id,Status:Status,DomainName:DomainName,Enabled:DistributionConfig.Enabled}' \
  --output table

# Verify S3 bucket
BUCKET_NAME=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text)

aws s3 ls s3://$BUCKET_NAME/ --region eu-west-1
```

### 7. Test Frontend Access

```bash
# Get CloudFront URL
CF_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionUrl`].OutputValue' \
  --output text)

# Test frontend (should return 200 and HTML content)
curl -I $CF_URL

# Test that index.html is served
curl -s $CF_URL | head -20
```

### 8. Test API Access

```bash
# Get ALB URL
ALB_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerUrl`].OutputValue' \
  --output text)

# Test ContosoUniversity API health (root path)
echo "Testing ContosoUniversity API..."
curl -I $ALB_URL/

# Test ContosoUniversity API - Students endpoint
echo "Testing Students API..."
curl -s $ALB_URL/api/students | jq '.' | head -20

# Test NotificationAPI - Notifications endpoint
echo "Testing NotificationAPI..."
curl -s $ALB_URL/api/notifications | jq '.' | head -20
```

### 9. Verify Database Connectivity

To test database connectivity, you would need to:
1. Connect to one of the ECS tasks via ECS Exec (requires additional configuration)
2. Or create a bastion host in the VPC
3. Or use AWS Systems Manager Session Manager

For now, verify that the ECS tasks are running and healthy, which indicates successful database connectivity.

### 10. Test SQS Message Flow

```bash
# Get queue URL
QUEUE_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --region eu-west-1 \
  --query 'Stacks[0].Outputs[?OutputKey==`SqsQueueUrl`].OutputValue' \
  --output text)

# Send a test message
aws sqs send-message \
  --queue-url $QUEUE_URL \
  --message-body '{"type":"test","message":"Test notification"}' \
  --region eu-west-1

# Receive the message (should return the test message)
aws sqs receive-message \
  --queue-url $QUEUE_URL \
  --region eu-west-1 \
  --max-number-of-messages 1

# Check queue metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/SQS \
  --metric-name NumberOfMessagesSent \
  --dimensions Name=QueueName,Value=ContosoUniversityStack-MessagingNotificationQueue \
  --start-time $(date -u -v-1H +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region eu-west-1
```

### 11. Check CloudWatch Logs

```bash
# View ContosoUniversity API logs
aws logs tail /ecs/contoso-api --follow --region eu-west-1

# View NotificationAPI logs
aws logs tail /ecs/notification-api --follow --region eu-west-1

# Get recent log events
aws logs filter-log-events \
  --log-group-name /ecs/contoso-api \
  --start-time $(date -u -v-10M +%s)000 \
  --region eu-west-1 \
  --limit 20
```

## Verification Checklist

After running the verification steps, confirm:

- [ ] VPC created with 2 public and 2 private subnets across 2 AZs
- [ ] 2 NAT Gateways provisioned (one per AZ)
- [ ] 3 Security groups created (ALB, API, Database)
- [ ] Aurora Serverless v2 cluster is available
- [ ] Database secret exists in Secrets Manager
- [ ] SQS queue created with correct attributes
- [ ] ECS cluster created with Container Insights enabled
- [ ] 2 ECS services running (contoso-api-service, notification-api-service)
- [ ] Both services have desired count = running count = 1
- [ ] Application Load Balancer is active
- [ ] 2 target groups created with correct health checks
- [ ] All targets are healthy in both target groups
- [ ] CloudFront distribution is deployed and enabled
- [ ] S3 bucket contains frontend assets
- [ ] Frontend accessible via CloudFront URL (returns HTML)
- [ ] ContosoUniversity API accessible via ALB (returns data)
- [ ] NotificationAPI accessible via ALB at /api/notifications/*
- [ ] SQS messages can be sent and received
- [ ] CloudWatch logs are being written for both services

## Troubleshooting

### ECS Tasks Not Starting

Check task logs:
```bash
aws ecs describe-tasks \
  --cluster $CLUSTER_NAME \
  --tasks $(aws ecs list-tasks --cluster $CLUSTER_NAME --region eu-west-1 --query 'taskArns[0]' --output text) \
  --region eu-west-1
```

### Target Health Checks Failing

Check target health details:
```bash
aws elbv2 describe-target-health \
  --target-group-arn $CONTOSO_TG_ARN \
  --region eu-west-1
```

Common issues:
- Security group rules not allowing traffic from ALB to ECS tasks
- Health check path returning non-200 status
- Container not listening on the correct port

### Database Connection Issues

Check:
- Database security group allows traffic from API security group on port 5432
- Database secret contains correct credentials
- ECS task role has permission to read the secret
- Database is in "available" state

### CloudFront Not Serving Content

- Wait 10-15 minutes for distribution to fully deploy
- Check distribution status: should be "Deployed"
- Verify S3 bucket has content
- Check Origin Access Identity permissions

## Cleanup

To destroy all resources and avoid charges:

```bash
cd ContosoUniversityCdk
cdk destroy
```

This will delete all resources created by the stack. Confirm when prompted.

**Note**: Some resources may take several minutes to delete, especially:
- CloudFront distribution (10-15 minutes)
- Aurora cluster (5-10 minutes)
- NAT Gateways (2-3 minutes)

## Cost Estimation

Approximate monthly costs (us-east-1 pricing):
- Aurora Serverless v2 (0.5-2 ACU): $43-$172/month
- ECS Fargate (2 tasks): $30-$40/month
- Application Load Balancer: $16-$20/month
- NAT Gateways (2): $65/month
- CloudFront: $1-$10/month (depends on traffic)
- S3: $1-$5/month
- CloudWatch Logs: $1-$5/month
- SQS: $0-$1/month (first 1M requests free)

**Total estimated cost**: $157-$318/month

To minimize costs:
- Use smaller Aurora capacity (0.5 ACU minimum)
- Reduce NAT Gateways to 1 (less HA)
- Use shorter log retention periods
- Delete the stack when not in use
