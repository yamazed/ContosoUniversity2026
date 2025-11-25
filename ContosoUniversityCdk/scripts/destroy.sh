#!/bin/bash

# Script to destroy the CDK stack and all AWS resources
# WARNING: This will delete all resources including data in the database

set -e

echo "=========================================="
echo "WARNING: Destroying Contoso University Stack"
echo "=========================================="
echo ""
echo "This will delete ALL resources including:"
echo "  - ECS Services and Tasks"
echo "  - Aurora Database (and all data)"
echo "  - S3 Bucket (and all files)"
echo "  - CloudFront Distribution"
echo "  - Load Balancer"
echo "  - VPC and Networking"
echo "  - SQS Queue"
echo ""

# Prompt for confirmation
read -p "Are you sure you want to destroy the stack? (yes/no): " -r
echo
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo "Destruction cancelled"
    exit 0
fi

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &> /dev/null; then
    echo "ERROR: AWS credentials not configured or invalid"
    exit 1
fi

# Navigate to CDK directory
cd "$(dirname "$0")/.."

echo "Destroying CDK stack..."
cdk destroy --force

echo ""
echo "=========================================="
echo "Stack destroyed successfully!"
echo "=========================================="
