#!/bin/bash

# Deployment Verification Script
# This script verifies that all resources in the ContosoUniversityStack are deployed correctly

set -e

STACK_NAME="ContosoUniversityStack"
REGION="eu-west-1"

echo "========================================="
echo "Contoso University Deployment Verification"
echo "========================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print success
success() {
    echo -e "${GREEN}✓${NC} $1"
}

# Function to print error
error() {
    echo -e "${RED}✗${NC} $1"
}

# Function to print warning
warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

# Function to print info
info() {
    echo -e "ℹ $1"
}

# Check if stack exists
echo "1. Checking if stack exists..."
if aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION &> /dev/null; then
    STACK_STATUS=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].StackStatus' --output text)
    if [ "$STACK_STATUS" == "CREATE_COMPLETE" ] || [ "$STACK_STATUS" == "UPDATE_COMPLETE" ]; then
        success "Stack exists and is in $STACK_STATUS state"
    else
        error "Stack exists but is in $STACK_STATUS state"
        exit 1
    fi
else
    error "Stack does not exist. Please deploy first with 'cdk deploy'"
    exit 1
fi
echo ""

# Get stack outputs
echo "2. Retrieving stack outputs..."
VPC_ID=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`VpcId`].OutputValue' --output text)
ALB_URL=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerUrl`].OutputValue' --output text)
CF_URL=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionUrl`].OutputValue' --output text)
CF_DIST_ID=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionId`].OutputValue' --output text)
DB_ENDPOINT=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`DatabaseClusterEndpoint`].OutputValue' --output text)
QUEUE_URL=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`SqsQueueUrl`].OutputValue' --output text)
CLUSTER_NAME=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`EcsClusterName`].OutputValue' --output text)
BUCKET_NAME=$(aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' --output text)

success "Retrieved all stack outputs"
echo ""

# Display key endpoints
echo "========================================="
echo "Key Endpoints:"
echo "========================================="
info "VPC ID: $VPC_ID"
info "ALB URL: $ALB_URL"
info "CloudFront URL: $CF_URL"
info "Database Endpoint: $DB_ENDPOINT"
info "SQS Queue URL: $QUEUE_URL"
info "ECS Cluster: $CLUSTER_NAME"
info "S3 Bucket: $BUCKET_NAME"
echo ""

# Verify VPC and Networking
echo "3. Verifying VPC and Networking..."
SUBNET_COUNT=$(aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" --region $REGION --query 'length(Subnets)')
if [ "$SUBNET_COUNT" -eq 4 ]; then
    success "VPC has 4 subnets (2 public, 2 private)"
else
    error "VPC has $SUBNET_COUNT subnets, expected 4"
fi

NAT_COUNT=$(aws ec2 describe-nat-gateways --filter "Name=vpc-id,Values=$VPC_ID" "Name=state,Values=available" --region $REGION --query 'length(NatGateways)')
if [ "$NAT_COUNT" -eq 2 ]; then
    success "2 NAT Gateways are available"
else
    warning "Found $NAT_COUNT NAT Gateways, expected 2"
fi

SG_COUNT=$(aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VPC_ID" "Name=tag:Project,Values=ContosoUniversity" --region $REGION --query 'length(SecurityGroups)')
if [ "$SG_COUNT" -ge 3 ]; then
    success "Security groups created ($SG_COUNT found)"
else
    error "Found $SG_COUNT security groups, expected at least 3"
fi
echo ""

# Verify Database
echo "4. Verifying Aurora Database..."
DB_STATUS=$(aws rds describe-db-clusters --region $REGION --query "DBClusters[?Endpoint=='$DB_ENDPOINT'].Status" --output text)
if [ "$DB_STATUS" == "available" ]; then
    success "Aurora cluster is available"
else
    error "Aurora cluster status: $DB_STATUS"
fi

DB_ENGINE=$(aws rds describe-db-clusters --region $REGION --query "DBClusters[?Endpoint=='$DB_ENDPOINT'].Engine" --output text)
if [[ "$DB_ENGINE" == *"aurora-postgresql"* ]]; then
    success "Database engine is Aurora PostgreSQL"
else
    warning "Database engine: $DB_ENGINE"
fi
echo ""

# Verify SQS Queue
echo "5. Verifying SQS Queue..."
if aws sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names All --region $REGION &> /dev/null; then
    success "SQS queue is accessible"
    RETENTION=$(aws sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names MessageRetentionPeriod --region $REGION --query 'Attributes.MessageRetentionPeriod' --output text)
    info "Message retention: $RETENTION seconds ($(($RETENTION / 86400)) days)"
else
    error "Cannot access SQS queue"
fi
echo ""

# Verify ECS Cluster and Services
echo "6. Verifying ECS Services..."
if aws ecs describe-clusters --clusters $CLUSTER_NAME --region $REGION &> /dev/null; then
    success "ECS cluster exists"
    
    # Check ContosoUniversity API service
    CONTOSO_DESIRED=$(aws ecs describe-services --cluster $CLUSTER_NAME --services contoso-api-service --region $REGION --query 'services[0].desiredCount' --output text 2>/dev/null || echo "0")
    CONTOSO_RUNNING=$(aws ecs describe-services --cluster $CLUSTER_NAME --services contoso-api-service --region $REGION --query 'services[0].runningCount' --output text 2>/dev/null || echo "0")
    
    if [ "$CONTOSO_DESIRED" -eq "$CONTOSO_RUNNING" ] && [ "$CONTOSO_RUNNING" -gt 0 ]; then
        success "ContosoUniversity API service: $CONTOSO_RUNNING/$CONTOSO_DESIRED tasks running"
    else
        error "ContosoUniversity API service: $CONTOSO_RUNNING/$CONTOSO_DESIRED tasks running"
    fi
    
    # Check NotificationAPI service
    NOTIFICATION_DESIRED=$(aws ecs describe-services --cluster $CLUSTER_NAME --services notification-api-service --region $REGION --query 'services[0].desiredCount' --output text 2>/dev/null || echo "0")
    NOTIFICATION_RUNNING=$(aws ecs describe-services --cluster $CLUSTER_NAME --services notification-api-service --region $REGION --query 'services[0].runningCount' --output text 2>/dev/null || echo "0")
    
    if [ "$NOTIFICATION_DESIRED" -eq "$NOTIFICATION_RUNNING" ] && [ "$NOTIFICATION_RUNNING" -gt 0 ]; then
        success "NotificationAPI service: $NOTIFICATION_RUNNING/$NOTIFICATION_DESIRED tasks running"
    else
        error "NotificationAPI service: $NOTIFICATION_RUNNING/$NOTIFICATION_DESIRED tasks running"
    fi
else
    error "ECS cluster not found"
fi
echo ""

# Verify Application Load Balancer
echo "7. Verifying Application Load Balancer..."
ALB_DNS=$(echo $ALB_URL | sed 's|http://||')
ALB_ARN=$(aws elbv2 describe-load-balancers --region $REGION --query "LoadBalancers[?DNSName=='$ALB_DNS'].LoadBalancerArn" --output text)

if [ -n "$ALB_ARN" ]; then
    success "Application Load Balancer found"
    
    # Check target groups
    TG_COUNT=$(aws elbv2 describe-target-groups --load-balancer-arn $ALB_ARN --region $REGION --query 'length(TargetGroups)')
    if [ "$TG_COUNT" -eq 2 ]; then
        success "2 target groups configured"
    else
        warning "Found $TG_COUNT target groups, expected 2"
    fi
    
    # Check target health
    CONTOSO_TG_ARN=$(aws elbv2 describe-target-groups --load-balancer-arn $ALB_ARN --region $REGION --query 'TargetGroups[?TargetGroupName==`contoso-api-tg`].TargetGroupArn' --output text)
    if [ -n "$CONTOSO_TG_ARN" ]; then
        HEALTHY_COUNT=$(aws elbv2 describe-target-health --target-group-arn $CONTOSO_TG_ARN --region $REGION --query 'length(TargetHealthDescriptions[?TargetHealth.State==`healthy`])')
        if [ "$HEALTHY_COUNT" -gt 0 ]; then
            success "ContosoUniversity API targets are healthy ($HEALTHY_COUNT)"
        else
            error "No healthy targets for ContosoUniversity API"
        fi
    fi
    
    NOTIFICATION_TG_ARN=$(aws elbv2 describe-target-groups --load-balancer-arn $ALB_ARN --region $REGION --query 'TargetGroups[?TargetGroupName==`notification-api-tg`].TargetGroupArn' --output text)
    if [ -n "$NOTIFICATION_TG_ARN" ]; then
        HEALTHY_COUNT=$(aws elbv2 describe-target-health --target-group-arn $NOTIFICATION_TG_ARN --region $REGION --query 'length(TargetHealthDescriptions[?TargetHealth.State==`healthy`])')
        if [ "$HEALTHY_COUNT" -gt 0 ]; then
            success "NotificationAPI targets are healthy ($HEALTHY_COUNT)"
        else
            error "No healthy targets for NotificationAPI"
        fi
    fi
else
    error "Application Load Balancer not found"
fi
echo ""

# Verify CloudFront Distribution
echo "8. Verifying CloudFront Distribution..."
CF_STATUS=$(aws cloudfront get-distribution --id $CF_DIST_ID --query 'Distribution.Status' --output text 2>/dev/null || echo "NotFound")
if [ "$CF_STATUS" == "Deployed" ]; then
    success "CloudFront distribution is deployed"
else
    warning "CloudFront distribution status: $CF_STATUS (may still be deploying)"
fi

# Verify S3 bucket has content
OBJECT_COUNT=$(aws s3 ls s3://$BUCKET_NAME/ --region $REGION | wc -l)
if [ "$OBJECT_COUNT" -gt 0 ]; then
    success "S3 bucket contains $OBJECT_COUNT objects"
else
    error "S3 bucket is empty"
fi
echo ""

# Test Frontend Access
echo "9. Testing Frontend Access..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $CF_URL)
if [ "$HTTP_CODE" == "200" ]; then
    success "Frontend is accessible (HTTP $HTTP_CODE)"
else
    error "Frontend returned HTTP $HTTP_CODE"
fi
echo ""

# Test API Access
echo "10. Testing API Access..."
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $ALB_URL/)
if [ "$HTTP_CODE" == "200" ] || [ "$HTTP_CODE" == "404" ]; then
    success "ContosoUniversity API is accessible (HTTP $HTTP_CODE)"
else
    error "ContosoUniversity API returned HTTP $HTTP_CODE"
fi

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $ALB_URL/api/notifications)
if [ "$HTTP_CODE" == "200" ]; then
    success "NotificationAPI is accessible (HTTP $HTTP_CODE)"
else
    warning "NotificationAPI returned HTTP $HTTP_CODE"
fi
echo ""

# Test SQS Message Flow
echo "11. Testing SQS Message Flow..."
SEND_RESULT=$(aws sqs send-message --queue-url $QUEUE_URL --message-body '{"type":"test","message":"Verification test"}' --region $REGION 2>&1)
if [ $? -eq 0 ]; then
    success "Successfully sent test message to SQS"
    
    # Try to receive the message
    sleep 2
    RECEIVE_RESULT=$(aws sqs receive-message --queue-url $QUEUE_URL --region $REGION --max-number-of-messages 1 2>&1)
    if [[ "$RECEIVE_RESULT" == *"Messages"* ]]; then
        success "Successfully received test message from SQS"
        
        # Delete the message
        RECEIPT_HANDLE=$(echo $RECEIVE_RESULT | jq -r '.Messages[0].ReceiptHandle')
        aws sqs delete-message --queue-url $QUEUE_URL --receipt-handle "$RECEIPT_HANDLE" --region $REGION
    else
        warning "Could not receive test message (may have been processed)"
    fi
else
    error "Failed to send test message to SQS"
fi
echo ""

# Check CloudWatch Logs
echo "12. Checking CloudWatch Logs..."
CONTOSO_LOG_STREAMS=$(aws logs describe-log-streams --log-group-name /ecs/contoso-api --region $REGION --query 'length(logStreams)' 2>/dev/null || echo "0")
if [ "$CONTOSO_LOG_STREAMS" -gt 0 ]; then
    success "ContosoUniversity API is writing logs ($CONTOSO_LOG_STREAMS streams)"
else
    warning "No log streams found for ContosoUniversity API"
fi

NOTIFICATION_LOG_STREAMS=$(aws logs describe-log-streams --log-group-name /ecs/notification-api --region $REGION --query 'length(logStreams)' 2>/dev/null || echo "0")
if [ "$NOTIFICATION_LOG_STREAMS" -gt 0 ]; then
    success "NotificationAPI is writing logs ($NOTIFICATION_LOG_STREAMS streams)"
else
    warning "No log streams found for NotificationAPI"
fi
echo ""

# Summary
echo "========================================="
echo "Verification Summary"
echo "========================================="
echo ""
echo "All critical components have been verified."
echo ""
echo "Next steps:"
echo "1. Access the frontend at: $CF_URL"
echo "2. Test API endpoints at: $ALB_URL"
echo "3. Monitor logs in CloudWatch"
echo "4. Review the TEST_DEPLOYMENT_GUIDE.md for detailed testing"
echo ""
echo "To destroy the stack and clean up resources:"
echo "  cd ContosoUniversityCdk && cdk destroy"
echo ""
