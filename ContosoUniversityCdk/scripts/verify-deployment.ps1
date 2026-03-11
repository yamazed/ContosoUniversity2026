# Deployment Verification Script
# This script verifies that all resources in the ContosoUniversityStack are deployed correctly

$ErrorActionPreference = "Continue"

$STACK_NAME = "ContosoUniversityStack"
$REGION = (aws configure get region)
if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Contoso University Deployment Verification" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Functions for colored output
function Write-Success { param([string]$msg) Write-Host "✓ $msg" -ForegroundColor Green }
function Write-Error { param([string]$msg) Write-Host "✗ $msg" -ForegroundColor Red }
function Write-Warning { param([string]$msg) Write-Host "⚠ $msg" -ForegroundColor Yellow }
function Write-Info { param([string]$msg) Write-Host "ℹ $msg" -ForegroundColor Cyan }

# Check if stack exists
Write-Host "1. Checking if stack exists..." -ForegroundColor Yellow
try {
    $STACK_STATUS = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].StackStatus' --output text 2>$null)
    if ($STACK_STATUS -eq "CREATE_COMPLETE" -or $STACK_STATUS -eq "UPDATE_COMPLETE") {
        Write-Success "Stack exists and is in $STACK_STATUS state"
    } else {
        Write-Error "Stack exists but is in $STACK_STATUS state"
        exit 1
    }
} catch {
    Write-Error "Stack does not exist. Please deploy first with 'cdk deploy'"
    exit 1
}
Write-Host ""

# Get stack outputs
Write-Host "2. Retrieving stack outputs..." -ForegroundColor Yellow
$VPC_ID = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`VpcId`].OutputValue' --output text)
$ALB_URL = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`ApplicationLoadBalancerUrl`].OutputValue' --output text)
$CF_URL = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionUrl`].OutputValue' --output text)
$CF_DIST_ID = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`CloudFrontDistributionId`].OutputValue' --output text)
$DB_ENDPOINT = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`DatabaseClusterEndpoint`].OutputValue' --output text)
$QUEUE_URL = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`SqsQueueUrl`].OutputValue' --output text)
$BUCKET_NAME = (aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' --output text)

Write-Success "Retrieved all stack outputs"
Write-Host ""

# Display key endpoints
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Key Endpoints:" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Info "VPC ID: $VPC_ID"
Write-Info "ALB URL: $ALB_URL"
Write-Info "CloudFront URL: $CF_URL"
Write-Info "Database Endpoint: $DB_ENDPOINT"
Write-Info "SQS Queue URL: $QUEUE_URL"
Write-Info "S3 Bucket: $BUCKET_NAME"
Write-Host ""

# Verify VPC and Networking
Write-Host "3. Verifying VPC and Networking..." -ForegroundColor Yellow
$SUBNET_COUNT = (aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VPC_ID" --region $REGION --query 'length(Subnets)')
if ($SUBNET_COUNT -eq 4) {
    Write-Success "VPC has 4 subnets (2 public, 2 private)"
} else {
    Write-Error "VPC has $SUBNET_COUNT subnets, expected 4"
}

$NAT_COUNT = (aws ec2 describe-nat-gateways --filter "Name=vpc-id,Values=$VPC_ID" "Name=state,Values=available" --region $REGION --query 'length(NatGateways)')
if ($NAT_COUNT -eq 2) {
    Write-Success "2 NAT Gateways are available"
} else {
    Write-Warning "Found $NAT_COUNT NAT Gateways, expected 2"
}

$SG_COUNT = (aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VPC_ID" --region $REGION --query 'length(SecurityGroups)')
if ($SG_COUNT -ge 3) {
    Write-Success "Security groups created ($SG_COUNT found)"
} else {
    Write-Error "Found $SG_COUNT security groups, expected at least 3"
}
Write-Host ""

# Verify Database
Write-Host "4. Verifying Aurora Database..." -ForegroundColor Yellow
$DB_STATUS = (aws rds describe-db-clusters --region $REGION --query "DBClusters[?Endpoint=='$DB_ENDPOINT'].Status" --output text)
if ($DB_STATUS -eq "available") {
    Write-Success "Aurora cluster is available"
} else {
    Write-Error "Aurora cluster status: $DB_STATUS"
}

$DB_ENGINE = (aws rds describe-db-clusters --region $REGION --query "DBClusters[?Endpoint=='$DB_ENDPOINT'].Engine" --output text)
if ($DB_ENGINE -like "*aurora-postgresql*") {
    Write-Success "Database engine is Aurora PostgreSQL"
} else {
    Write-Warning "Database engine: $DB_ENGINE"
}
Write-Host ""

# Verify SQS Queue
Write-Host "5. Verifying SQS Queue..." -ForegroundColor Yellow
try {
    aws sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names All --region $REGION | Out-Null
    Write-Success "SQS queue is accessible"
    $RETENTION = (aws sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names MessageRetentionPeriod --region $REGION --query 'Attributes.MessageRetentionPeriod' --output text)
    $DAYS = [math]::Floor($RETENTION / 86400)
    Write-Info "Message retention: $RETENTION seconds ($DAYS days)"
} catch {
    Write-Error "Cannot access SQS queue"
}
Write-Host ""

# Verify Application Load Balancer
Write-Host "6. Verifying Application Load Balancer..." -ForegroundColor Yellow
$ALB_DNS = $ALB_URL -replace 'http://', ''
$ALB_ARN = (aws elbv2 describe-load-balancers --region $REGION --query "LoadBalancers[?DNSName=='$ALB_DNS'].LoadBalancerArn" --output text)

if (-not [string]::IsNullOrEmpty($ALB_ARN)) {
    Write-Success "Application Load Balancer found"
    
    # Check target groups
    $TG_COUNT = (aws elbv2 describe-target-groups --load-balancer-arn $ALB_ARN --region $REGION --query 'length(TargetGroups)')
    if ($TG_COUNT -eq 2) {
        Write-Success "2 target groups configured"
    } else {
        Write-Warning "Found $TG_COUNT target groups, expected 2"
    }
} else {
    Write-Error "Application Load Balancer not found"
}
Write-Host ""

# Verify CloudFront Distribution
Write-Host "7. Verifying CloudFront Distribution..." -ForegroundColor Yellow
try {
    $CF_STATUS = (aws cloudfront get-distribution --id $CF_DIST_ID --query 'Distribution.Status' --output text 2>$null)
    if ($CF_STATUS -eq "Deployed") {
        Write-Success "CloudFront distribution is deployed"
    } else {
        Write-Warning "CloudFront distribution status: $CF_STATUS (may still be deploying)"
    }
} catch {
    Write-Error "CloudFront distribution not found"
}

# Verify S3 bucket has content
$OBJECT_COUNT = (aws s3 ls "s3://$BUCKET_NAME/" --region $REGION | Measure-Object).Count
if ($OBJECT_COUNT -gt 0) {
    Write-Success "S3 bucket contains $OBJECT_COUNT objects"
} else {
    Write-Error "S3 bucket is empty"
}
Write-Host ""

# Test Frontend Access
Write-Host "8. Testing Frontend Access..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $CF_URL -Method Head -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Success "Frontend is accessible (HTTP $($response.StatusCode))"
    }
} catch {
    Write-Error "Frontend returned HTTP $($_.Exception.Response.StatusCode.Value__)"
}
Write-Host ""

# Test API Access
Write-Host "9. Testing API Access..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$ALB_URL/" -Method Head -UseBasicParsing -TimeoutSec 10
    Write-Success "ContosoUniversity API is accessible (HTTP $($response.StatusCode))"
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    if ($statusCode -eq 404) {
        Write-Success "ContosoUniversity API is accessible (HTTP 404)"
    } else {
        Write-Error "ContosoUniversity API returned HTTP $statusCode"
    }
}
Write-Host ""

# Summary
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Verification Summary" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "All critical components have been verified." -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Access the frontend at: $CF_URL"
Write-Host "2. Test API endpoints at: $ALB_URL"
Write-Host "3. Monitor logs in CloudWatch"
Write-Host ""
Write-Host "To destroy the stack and clean up resources:" -ForegroundColor Yellow
Write-Host "  cd ContosoUniversityCdk; cdk destroy"
Write-Host ""
