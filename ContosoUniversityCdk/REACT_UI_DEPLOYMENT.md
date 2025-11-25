# React UI Deployment Guide

## Overview

The React UI needs to be configured with the correct API endpoint (ALB URL) before deployment. This guide explains how the deployment process works and how to update the UI.

## How It Works

### API Endpoint Configuration

The React app uses environment variables to configure the API endpoint:

- **Development**: `.env.development` → `http://localhost:5000/api`
- **Production**: `.env.production` → ALB URL from CloudFormation

### Deployment Flow

```
1. Deploy Infrastructure (CDK) → Get ALB URL
2. Update .env.production with ALB URL
3. Build React App with correct endpoint
4. Deploy to S3 + CloudFront
```

## Deployment Methods

### Method 1: Complete Deployment (Recommended)

Use the automated deployment script that handles everything:

```bash
cd ContosoUniversityCdk
./deploy-ec2.sh
```

This script:
1. Deploys infrastructure first
2. Gets the ALB URL from CloudFormation
3. Updates `.env.production` with the ALB URL
4. Builds React app with correct endpoint
5. Deploys React app to S3/CloudFront

### Method 2: Update UI Only

If infrastructure is already deployed and you just need to update the React UI:

```bash
cd ContosoUniversityCdk
./scripts/deploy-ui.sh
```

This script:
1. Gets ALB URL from existing stack
2. Updates `.env.production`
3. Builds React app
4. Deploys to S3/CloudFront

### Method 3: Manual Deployment

For more control over the process:

```bash
# 1. Deploy infrastructure
cd ContosoUniversityCdk
cdk deploy

# 2. Get ALB URL
ALB_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" \
  --output text)

echo "ALB URL: $ALB_URL"

# 3. Update React environment
cd ../contoso-university-ui
echo "VITE_API_BASE_URL=${ALB_URL}/api" > .env.production

# 4. Build React app
npm install
npm run build

# 5. Deploy (triggers CDK to upload to S3)
cd ../ContosoUniversityCdk
cdk deploy
```

## Verifying the Configuration

### Check Current API Endpoint

After deployment, verify the React app is using the correct endpoint:

```bash
# Get CloudFront URL
CLOUDFRONT_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionUrl'].OutputValue" \
  --output text)

echo "React UI: $CLOUDFRONT_URL"

# Open in browser and check Network tab
# API calls should go to your ALB URL, not api.contoso-university.com
```

### Check .env.production

```bash
cd contoso-university-ui
cat .env.production
```

Should show:
```
VITE_API_BASE_URL=http://contoso-alb-123456789.us-east-1.elb.amazonaws.com/api
```

## Troubleshooting

### Issue: React app still using old API endpoint

**Cause**: CloudFront cache not invalidated or browser cache

**Solution**:
```bash
# 1. Invalidate CloudFront cache
DIST_ID=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" \
  --output text)

aws cloudfront create-invalidation \
  --distribution-id $DIST_ID \
  --paths "/*"

# 2. Clear browser cache or use incognito mode
```

### Issue: API calls failing with CORS errors

**Cause**: ALB not configured to allow CloudFront origin

**Solution**: The CDK stack automatically configures CORS. Check that:
1. CloudFront URL is passed to ComputeConstruct
2. API is setting correct CORS headers

```bash
# Test API directly
ALB_URL=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" \
  --output text)

curl -I "${ALB_URL}/api/students"
```

### Issue: Build fails with "ALB URL not found"

**Cause**: Infrastructure not deployed yet

**Solution**:
```bash
# Deploy infrastructure first
cd ContosoUniversityCdk
cdk deploy

# Then build UI
./scripts/build-ui.sh
```

## Environment Variables

### React App (.env.production)

```bash
VITE_API_BASE_URL=http://your-alb-url.elb.amazonaws.com/api
```

This is automatically set by the deployment scripts.

### CDK Deployment

The CDK stack passes the CloudFront URL to the API for CORS configuration:

```csharp
var cloudFrontUrl = $"https://{frontend.CloudFrontDistribution.DistributionDomainName}";
var compute = new ComputeConstructEC2(
    // ...
    cloudFrontUrl  // Used for CORS configuration
);
```

## Updating After Changes

### Update React Code Only

```bash
cd ContosoUniversityCdk
./scripts/deploy-ui.sh
```

### Update API Code Only

```bash
cd ContosoUniversityCdk
./scripts/publish-apps.sh
./scripts/update-instances.sh
```

### Update Both

```bash
# Update API
cd ContosoUniversityCdk
./scripts/publish-apps.sh
./scripts/update-instances.sh

# Update UI
./scripts/deploy-ui.sh
```

## CloudFront Cache

### Cache Invalidation

CloudFront caches content for performance. After deploying UI changes:

```bash
# Automatic (done by CDK BucketDeployment)
cdk deploy  # Automatically invalidates /*

# Manual
DIST_ID=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" \
  --output text)

aws cloudfront create-invalidation \
  --distribution-id $DIST_ID \
  --paths "/*"
```

### Cache Behavior

- **Static assets** (JS, CSS, images): Cached for 1 year
- **index.html**: Should be invalidated on each deployment
- **API calls**: Not cached (go directly to ALB)

## Custom Domain (Optional)

To use a custom domain like `app.contoso-university.com`:

1. **Create Route 53 hosted zone**
2. **Request ACM certificate** for your domain
3. **Update FrontendConstruct**:

```csharp
CloudFrontDistribution = new Distribution(this, "WebsiteDistribution", new DistributionProps
{
    // ... existing config ...
    DomainNames = new[] { "app.contoso-university.com" },
    Certificate = Certificate.FromCertificateArn(this, "Cert", "arn:aws:acm:...")
});
```

4. **Create Route 53 alias record** pointing to CloudFront

## Monitoring

### Check Deployment Status

```bash
# CloudFormation stack status
aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].StackStatus"

# CloudFront distribution status
DIST_ID=$(aws cloudformation describe-stacks \
  --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" \
  --output text)

aws cloudfront get-distribution --id $DIST_ID \
  --query "Distribution.Status"
```

### View Logs

```bash
# CloudFront access logs (if enabled)
aws s3 ls s3://your-logs-bucket/cloudfront/

# API logs
aws logs tail /ec2/contoso-api --follow
```

## Best Practices

1. **Always deploy infrastructure before UI** - UI needs ALB URL
2. **Use deployment scripts** - They handle the correct order
3. **Invalidate CloudFront cache** - After UI updates
4. **Test in incognito mode** - Avoid browser cache issues
5. **Monitor API calls** - Check Network tab in browser DevTools
6. **Use HTTPS in production** - Configure custom domain with ACM certificate

## Quick Reference

```bash
# Complete deployment
./deploy-ec2.sh

# Update UI only
./scripts/deploy-ui.sh

# Update API only
./scripts/publish-apps.sh && ./scripts/update-instances.sh

# Build UI manually
./scripts/build-ui.sh

# Invalidate CloudFront
aws cloudfront create-invalidation --distribution-id $DIST_ID --paths "/*"

# Get stack outputs
aws cloudformation describe-stacks --stack-name ContosoUniversityStack \
  --query "Stacks[0].Outputs[*].[OutputKey,OutputValue]" --output table
```
