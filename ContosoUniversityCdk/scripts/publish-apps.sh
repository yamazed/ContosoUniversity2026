#!/bin/bash

# Publish .NET applications for EC2 deployment

set -e

echo "=========================================="
echo "Publishing .NET Applications"
echo "=========================================="
echo ""

# Get AWS account
ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
REGION=$(aws configure get region)
BUCKET="contoso-deployment-$ACCOUNT"

if [ -z "$REGION" ]; then
    REGION="us-east-1"
fi

echo "Account: $ACCOUNT"
echo "Region: $REGION"
echo "Bucket: $BUCKET"
echo ""

# Create S3 bucket if it doesn't exist
echo "Checking S3 bucket..."
if ! aws s3 ls "s3://$BUCKET" 2>/dev/null; then
    echo "Creating S3 bucket: $BUCKET"
    if [ "$REGION" = "us-east-1" ]; then
        aws s3 mb "s3://$BUCKET"
    else
        aws s3 mb "s3://$BUCKET" --region "$REGION"
    fi
    echo "✅ Bucket created"
else
    echo "✅ Bucket exists"
fi
echo ""

# Publish ContosoUniversity API
echo "Publishing ContosoUniversity API..."
cd ../ContosoUniversity
dotnet publish -c Release -o ./publish --self-contained false
cd publish
zip -r ../contoso-api.zip .
cd ..
echo "✅ ContosoUniversity API published"
echo ""

# Upload to S3
echo "Uploading ContosoUniversity API to S3..."
aws s3 cp contoso-api.zip "s3://$BUCKET/contoso-api.zip"
rm contoso-api.zip
rm -rf publish
echo "✅ Uploaded to S3"
echo ""

# Publish NotificationAPI
echo "Publishing NotificationAPI..."
cd ../NotificationAPI
dotnet publish -c Release -o ./publish --self-contained false
cd publish
zip -r ../notification-api.zip .
cd ..
echo "✅ NotificationAPI published"
echo ""

# Upload to S3
echo "Uploading NotificationAPI to S3..."
aws s3 cp notification-api.zip "s3://$BUCKET/notification-api.zip"
rm notification-api.zip
rm -rf publish
echo "✅ Uploaded to S3"
echo ""

echo "=========================================="
echo "✅ Applications Published Successfully"
echo "=========================================="
echo ""
echo "Applications are available at:"
echo "  s3://$BUCKET/contoso-api.zip"
echo "  s3://$BUCKET/notification-api.zip"
echo ""
echo "Next steps:"
echo "1. Deploy CDK stack: cd ../ContosoUniversityCdk && cdk deploy"
echo "2. Or update running instances: ./update-instances.sh"
