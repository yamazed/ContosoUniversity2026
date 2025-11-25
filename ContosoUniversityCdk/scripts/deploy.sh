#!/bin/bash

# Script to deploy the CDK stack to AWS
# This builds the UI, synthesizes the CDK stack, and deploys all resources

set -e

echo "=========================================="
echo "Deploying Contoso University to AWS"
echo "=========================================="

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &> /dev/null; then
    echo "ERROR: AWS credentials not configured or invalid"
    echo "Please configure AWS CLI credentials before deploying"
    exit 1
fi

# Get AWS account and region info
AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
AWS_REGION=${AWS_REGION:-us-east-1}

echo "Deploying to AWS Account: $AWS_ACCOUNT"
echo "Region: $AWS_REGION"
echo ""

# Step 1: Build React UI
echo "Step 1/3: Building React UI..."
bash "$(dirname "$0")/build-ui.sh"
echo ""

# Step 2: Navigate to CDK directory
cd "$(dirname "$0")/.."

# Restore .NET dependencies
echo "Step 2/3: Restoring .NET dependencies..."
dotnet restore
echo ""

# Step 3: Deploy CDK stack
echo "Step 3/3: Deploying CDK stack..."
echo "This may take 10-15 minutes..."
cdk deploy --require-approval never

echo ""
echo "=========================================="
echo "Deployment completed successfully!"
echo "=========================================="
echo ""
echo "Stack outputs:"
cdk deploy --outputs-file outputs.json --require-approval never || true
if [ -f outputs.json ]; then
    cat outputs.json
fi
