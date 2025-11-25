#!/bin/bash

# Script to build the React UI for deployment
# This script builds the React application with the correct API endpoint

set -e

echo "=========================================="
echo "Building React UI"
echo "=========================================="

# Get AWS region
REGION=$(aws configure get region 2>/dev/null || echo "us-east-1")

# Get ALB URL from CloudFormation stack
echo "Getting ALB URL from CloudFormation..."
ALB_URL=$(aws cloudformation describe-stacks \
    --stack-name ContosoUniversityStack \
    --region $REGION \
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" \
    --output text 2>/dev/null || echo "")

if [ -z "$ALB_URL" ]; then
    echo "⚠️  Warning: Could not get ALB URL from stack"
    echo "Using placeholder URL. Deploy infrastructure first with: cdk deploy"
    ALB_URL="http://placeholder-alb.example.com"
fi

echo "API Endpoint: ${ALB_URL}/api"
echo ""

# Navigate to the React UI directory
cd "$(dirname "$0")/../../contoso-university-ui"

# Update .env.production with ALB URL
echo "VITE_API_BASE_URL=${ALB_URL}/api" > .env.production
echo "✅ Updated .env.production"
echo ""

# Check if node_modules exists, if not run npm install
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# Build the React application
echo "Building production bundle..."
npm run build

echo "=========================================="
echo "React UI build completed successfully!"
echo "Build output location: $(pwd)/dist"
echo "API endpoint: ${ALB_URL}/api"
echo "=========================================="
