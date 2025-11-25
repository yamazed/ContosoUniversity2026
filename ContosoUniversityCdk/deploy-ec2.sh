#!/bin/bash

# Complete deployment script for EC2-based infrastructure

set -e

echo "=========================================="
echo "Contoso University EC2 Deployment"
echo "=========================================="
echo ""

# Check prerequisites
if ! command -v aws &> /dev/null; then
    echo "❌ Error: AWS CLI is not installed"
    exit 1
fi

if ! command -v cdk &> /dev/null; then
    echo "❌ Error: AWS CDK CLI is not installed"
    echo "Install with: npm install -g aws-cdk"
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed"
    exit 1
fi

# Check AWS credentials
if ! aws sts get-caller-identity &> /dev/null; then
    echo "❌ Error: AWS CLI is not configured"
    echo "Run: aws configure"
    exit 1
fi

echo "✅ Prerequisites verified"
echo ""

# Get AWS account and region
export CDK_DEFAULT_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
export CDK_DEFAULT_REGION=$(aws configure get region)

if [ -z "$CDK_DEFAULT_REGION" ]; then
    export CDK_DEFAULT_REGION="us-east-1"
fi

echo "Account: $CDK_DEFAULT_ACCOUNT"
echo "Region: $CDK_DEFAULT_REGION"
echo ""

# Step 1: Bootstrap CDK if needed
echo "Step 1: Checking CDK bootstrap..."
if ! aws cloudformation describe-stacks --stack-name CDKToolkit --region $CDK_DEFAULT_REGION &> /dev/null; then
    echo "Bootstrapping CDK..."
    cdk bootstrap aws://$CDK_DEFAULT_ACCOUNT/$CDK_DEFAULT_REGION
    echo "✅ CDK bootstrapped"
else
    echo "✅ CDK already bootstrapped"
fi
echo ""

# Step 2: Deploy infrastructure first to get ALB URL
echo "Step 2: Deploying CDK stack (without React UI)..."
echo "This will create the infrastructure and give us the ALB URL"
echo ""

# Deploy without React UI first (will fail on BucketDeployment but that's ok)
cdk deploy --require-approval never 2>&1 | tee /tmp/cdk-deploy.log || true

# Extract ALB URL from outputs
ALB_URL=$(aws cloudformation describe-stacks \
    --stack-name ContosoUniversityStack \
    --region $CDK_DEFAULT_REGION \
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" \
    --output text 2>/dev/null || echo "")

if [ -z "$ALB_URL" ]; then
    echo "⚠️  Could not get ALB URL from stack outputs"
    echo "Using placeholder URL - you'll need to update React app later"
    ALB_URL="http://placeholder-alb.example.com"
fi

echo "ALB URL: $ALB_URL"
echo ""

# Step 3: Build React UI with correct API URL
echo "Step 3: Building React UI with ALB endpoint..."
if [ -d "../contoso-university-ui" ]; then
    cd ../contoso-university-ui
    
    # Update .env.production with ALB URL
    echo "VITE_API_BASE_URL=${ALB_URL}/api" > .env.production
    echo "✅ Updated .env.production with ALB URL"
    
    if [ ! -d "node_modules" ]; then
        npm install
    fi
    npm run build
    cd ../ContosoUniversityCdk
    echo "✅ React UI built with correct API endpoint"
else
    echo "⚠️  React UI not found, skipping"
fi
echo ""

# Step 3: Publish .NET applications
echo "Step 3: Publishing .NET applications..."
./scripts/publish-apps.sh
echo ""

# Step 4: Deploy CDK stack again with React UI
echo "Step 4: Deploying CDK stack with React UI..."
echo "This will upload the React app to S3 and invalidate CloudFront cache"
echo ""

cdk deploy --require-approval never

echo ""
echo "=========================================="
echo "✅ Deployment Complete!"
echo "=========================================="
echo ""

# Get outputs
echo "Stack Outputs:"
echo "=============="
aws cloudformation describe-stacks \
    --stack-name ContosoUniversityStack \
    --region $CDK_DEFAULT_REGION \
    --query "Stacks[0].Outputs[*].[OutputKey,OutputValue]" \
    --output table

echo ""
echo "Next Steps:"
echo "1. Wait 5-10 minutes for instances to initialize"
echo "2. Access your application at the CloudFront URL"
echo "3. Check logs: aws logs tail /ec2/contoso-api --follow"
echo ""
echo "To update applications:"
echo "  ./scripts/publish-apps.sh"
echo "  ./scripts/update-instances.sh"
echo ""
echo "To destroy everything:"
echo "  cdk destroy"
