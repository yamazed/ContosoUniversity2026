#!/bin/bash

# Deploy/update React UI with correct ALB endpoint

set -e

echo "=========================================="
echo "Deploy React UI"
echo "=========================================="
echo ""

# Get AWS region
REGION=$(aws configure get region)
if [ -z "$REGION" ]; then
    REGION="us-east-1"
fi

# Get ALB URL from CloudFormation stack
echo "Getting ALB URL from CloudFormation..."
ALB_URL=$(aws cloudformation describe-stacks \
    --stack-name ContosoUniversityStack \
    --region $REGION \
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" \
    --output text)

if [ -z "$ALB_URL" ]; then
    echo "❌ Error: Could not get ALB URL from stack"
    echo "Make sure the CDK stack is deployed: cdk deploy"
    exit 1
fi

echo "ALB URL: $ALB_URL"
echo ""

# Check if React UI directory exists
if [ ! -d "../contoso-university-ui" ]; then
    echo "❌ Error: React UI directory not found"
    echo "Expected: ../contoso-university-ui"
    exit 1
fi

# Build React UI with correct API endpoint
echo "Building React UI..."
cd ../contoso-university-ui

# Update .env.production with ALB URL
echo "VITE_API_BASE_URL=${ALB_URL}/api" > .env.production
echo "✅ Updated .env.production:"
cat .env.production
echo ""

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "Installing npm dependencies..."
    npm install
fi

# Build
echo "Building production bundle..."
npm run build

cd ../ContosoUniversityCdk
echo "✅ React UI built successfully"
echo ""

# Deploy to S3 and invalidate CloudFront
echo "Deploying to S3 and CloudFront..."
cdk deploy --require-approval never

echo ""
echo "=========================================="
echo "✅ React UI Deployed!"
echo "=========================================="
echo ""

# Get CloudFront URL
CLOUDFRONT_URL=$(aws cloudformation describe-stacks \
    --stack-name ContosoUniversityStack \
    --region $REGION \
    --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionUrl'].OutputValue" \
    --output text)

echo "Access your application at:"
echo "  $CLOUDFRONT_URL"
echo ""
echo "API endpoint configured:"
echo "  ${ALB_URL}/api"
echo ""
echo "Note: CloudFront cache invalidation may take 1-2 minutes"
