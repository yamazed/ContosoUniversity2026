# Windows Deployment Guide

## Quick Start

```powershell
cd ContosoUniversityCdk
.\deploy-ec2.ps1
```

## Common Issues and Fixes

### 1. SSL Certificate Error

**Error:** `SSL: CERTIFICATE_VERIFY_FAILED`

**Fix:** The script now automatically disables SSL verification. If you still see this error, run:

```powershell
$env:AWS_CLI_SSL_VERIFY = "false"
.\deploy-ec2.ps1
```

**Better Fix (Recommended):** Install/update AWS CLI certificates:
```powershell
# Download and install the latest AWS CLI
# https://aws.amazon.com/cli/
```

### 2. Node Version Warning

**Warning:** `This software has not been tested with node v25.1.0`

**Fix:** This is just a warning and won't prevent deployment. To silence it:

```powershell
$env:JSII_SILENCE_WARNING_UNTESTED_NODE_VERSION = "1"
.\deploy-ec2.ps1
```

Or downgrade to Node 24 LTS:
```powershell
nvm install 24
nvm use 24
```

### 3. React UI Build Missing

**Error:** `Cannot find asset at C:\...\contoso-university-ui\dist`

**Fix:** The updated script now builds the React UI first. If you still see this:

```powershell
cd ..\contoso-university-ui
npm install
npm run build
cd ..\ContosoUniversityCdk
.\deploy-ec2.ps1
```

## Step-by-Step Manual Deployment

If the automated script fails, deploy manually:

### Step 1: Build React UI
```powershell
cd contoso-university-ui
npm install
npm run build
cd ..
```

### Step 2: Publish .NET Apps
```powershell
cd ContosoUniversityCdk
.\scripts\publish-apps.ps1
```

### Step 3: Deploy Infrastructure
```powershell
cdk deploy --require-approval never
```

### Step 4: Get ALB URL
```powershell
$ALB_URL = aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" `
    --output text

Write-Host "ALB URL: $ALB_URL"
```

### Step 5: Rebuild React UI with Real URL
```powershell
cd ..\contoso-university-ui
"VITE_API_BASE_URL=$ALB_URL/api" | Out-File -FilePath .env.production -Encoding utf8
npm run build
cd ..\ContosoUniversityCdk
```

### Step 6: Redeploy
```powershell
cdk deploy --require-approval never
```

## Prerequisites

Make sure you have:

1. **AWS CLI** configured with credentials
   ```powershell
   aws configure
   ```

2. **AWS CDK** installed globally
   ```powershell
   npm install -g aws-cdk
   ```

3. **.NET SDK** (version 8.0 or later)
   ```powershell
   dotnet --version
   ```

4. **Node.js** (version 20, 22, or 24 recommended)
   ```powershell
   node --version
   ```

## Verify Deployment

After deployment completes:

```powershell
.\scripts\verify-deployment.ps1
```

## Get Website URL

```powershell
aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionUrl'].OutputValue" `
    --output text
```

## Troubleshooting Commands

### Check CDK Bootstrap
```powershell
aws cloudformation describe-stacks --stack-name CDKToolkit
```

### Check Stack Status
```powershell
aws cloudformation describe-stacks --stack-name ContosoUniversityStack
```

### View Logs
```powershell
aws logs tail /aws/ec2/contoso-api --follow
```

### Check EC2 Instances
```powershell
aws ec2 describe-instances `
    --filters "Name=tag:aws:cloudformation:stack-name,Values=ContosoUniversityStack" `
    --query "Reservations[*].Instances[*].[InstanceId,State.Name,PublicIpAddress]" `
    --output table
```

## Clean Up

To destroy all resources:

```powershell
.\scripts\destroy.ps1
```

Or manually:

```powershell
cdk destroy --force
```

## Support

If you encounter issues:

1. Check the error message carefully
2. Review this guide for common fixes
3. Check AWS CloudFormation console for stack events
4. Review CloudWatch logs for application errors
