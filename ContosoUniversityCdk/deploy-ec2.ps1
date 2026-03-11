# Complete deployment script for EC2-based infrastructure (PowerShell)
# Uses --no-verify-ssl flag to bypass SSL certificate issues

$ErrorActionPreference = "Continue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Contoso University EC2 Deployment" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Configure environment
$env:JSII_SILENCE_WARNING_UNTESTED_NODE_VERSION = "1"
$env:NODE_OPTIONS = "--no-warnings"

Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check AWS CLI
if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: AWS CLI is not installed" -ForegroundColor Red
    exit 1
}

# Check CDK
if (-not (Get-Command cdk -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: AWS CDK CLI is not installed" -ForegroundColor Red
    Write-Host "Install with: npm install -g aws-cdk" -ForegroundColor Yellow
    exit 1
}

# Check .NET
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: .NET SDK is not installed" -ForegroundColor Red
    exit 1
}

# Check npm
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Error: npm is not installed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Prerequisites verified" -ForegroundColor Green
Write-Host ""

# Get AWS account and region using --no-verify-ssl
Write-Host "Getting AWS account information..." -ForegroundColor Yellow

$awsOutput = aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>&1
$ACCOUNT = ($awsOutput | Where-Object { $_ -match '^\d{12}$' }) -join ''

$regionOutput = aws configure get region --no-verify-ssl 2>&1
$REGION = ($regionOutput | Where-Object { $_ -match '^[a-z]+-[a-z]+-\d+$' }) -join ''

if ([string]::IsNullOrEmpty($ACCOUNT)) {
    Write-Host "❌ Error: Could not get AWS account." -ForegroundColor Red
    Write-Host ""
    Write-Host "Please verify your AWS credentials:" -ForegroundColor Yellow
    Write-Host "  aws configure" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Test with:" -ForegroundColor Yellow
    Write-Host "  aws sts get-caller-identity --no-verify-ssl" -ForegroundColor Cyan
    exit 1
}

if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

$env:CDK_DEFAULT_ACCOUNT = $ACCOUNT
$env:CDK_DEFAULT_REGION = $REGION

Write-Host "Account: $ACCOUNT" -ForegroundColor Cyan
Write-Host "Region: $REGION" -ForegroundColor Cyan
Write-Host ""

# Step 1: Bootstrap CDK if needed
Write-Host "Step 1: Checking CDK bootstrap..." -ForegroundColor Yellow
$bootstrapped = $false
try {
    $stackOutput = aws cloudformation describe-stacks --stack-name CDKToolkit --region $REGION --no-verify-ssl 2>&1
    if ($stackOutput -match "CDKToolkit") {
        $bootstrapped = $true
    }
} catch {
    # Not bootstrapped
}

if ($bootstrapped) {
    Write-Host "✅ CDK already bootstrapped" -ForegroundColor Green
} else {
    Write-Host "Bootstrapping CDK (this may take a few minutes)..." -ForegroundColor Yellow
    cdk bootstrap "aws://$ACCOUNT/$REGION"
    Write-Host "✅ CDK bootstrapped" -ForegroundColor Green
}
Write-Host ""

# Step 2: Build React UI
Write-Host "Step 2: Building React UI..." -ForegroundColor Yellow
if (-not (Test-Path "..\contoso-university-ui")) {
    Write-Host "❌ Error: React UI directory not found at ..\contoso-university-ui" -ForegroundColor Red
    exit 1
}

Push-Location ..\contoso-university-ui

# Create placeholder .env.production
"VITE_API_BASE_URL=http://placeholder/api" | Out-File -FilePath .env.production -Encoding utf8 -NoNewline
Write-Host "✅ Created .env.production" -ForegroundColor Green

# Always run npm install to ensure dependencies are up to date
Write-Host "Installing npm dependencies (this may take a few minutes)..." -ForegroundColor Yellow
npm install --loglevel=error
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: npm install failed" -ForegroundColor Red
    Write-Host "Try manually:" -ForegroundColor Yellow
    Write-Host "  cd ..\contoso-university-ui" -ForegroundColor Cyan
    Write-Host "  Remove-Item -Recurse -Force node_modules" -ForegroundColor Cyan
    Write-Host "  npm install" -ForegroundColor Cyan
    Pop-Location
    exit 1
}

# Build
Write-Host "Building React app..." -ForegroundColor Yellow
npm run build --loglevel=error
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: React build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location
Write-Host "✅ React UI built successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Publish .NET applications
Write-Host "Step 3: Publishing .NET applications..." -ForegroundColor Yellow
$publishScript = Join-Path $PSScriptRoot "scripts\publish-apps.ps1"
if (Test-Path $publishScript) {
    & $publishScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Error: Failed to publish .NET applications" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ Error: publish-apps.ps1 not found at $publishScript" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Deploy CDK stack
Write-Host "Step 4: Deploying CDK stack..." -ForegroundColor Yellow
Write-Host "This will create all infrastructure (VPC, ALB, EC2, RDS, S3, CloudFront, etc.)" -ForegroundColor Gray
Write-Host "This may take 15-20 minutes..." -ForegroundColor Gray
Write-Host ""

cdk deploy --require-approval never
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: CDK deployment failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Infrastructure deployed!" -ForegroundColor Green
Write-Host ""

# Step 5: Get ALB URL and rebuild React UI
Write-Host "Step 5: Updating React UI with real API endpoint..." -ForegroundColor Yellow
$albOutput = aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --region $REGION `
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" `
    --output text `
    --no-verify-ssl 2>&1

$ALB_URL = ($albOutput | Where-Object { $_ -match '^http' }) -join ''

if ([string]::IsNullOrEmpty($ALB_URL)) {
    Write-Host "⚠️  Warning: Could not retrieve ALB URL" -ForegroundColor Yellow
    Write-Host "Skipping React UI update. You may need to update manually later." -ForegroundColor Yellow
} else {
    Write-Host "ALB URL: $ALB_URL" -ForegroundColor Cyan
    
    # Rebuild React UI with correct API endpoint
    Push-Location ..\contoso-university-ui
    
    "VITE_API_BASE_URL=$ALB_URL/api" | Out-File -FilePath .env.production -Encoding utf8 -NoNewline
    Write-Host "✅ Updated .env.production with real ALB URL" -ForegroundColor Green
    
    npm run build --loglevel=error
    Pop-Location
    Write-Host "✅ React UI rebuilt" -ForegroundColor Green
    Write-Host ""
    
    # Redeploy to update React UI in S3
    Write-Host "Step 6: Redeploying to update React UI in S3..." -ForegroundColor Yellow
    cdk deploy --require-approval never
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Get outputs
Write-Host "Stack Outputs:" -ForegroundColor Cyan
Write-Host "==============" -ForegroundColor Cyan
aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --region $REGION `
    --query "Stacks[0].Outputs[*].[OutputKey,OutputValue]" `
    --output table `
    --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "ERROR" -and $_ -notmatch "SSL" }

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Wait 5-10 minutes for EC2 instances to fully initialize"
Write-Host "2. Access your application at the CloudFront URL above"
Write-Host "3. Check logs: aws logs tail /aws/ec2/contoso-api --follow --no-verify-ssl"
Write-Host ""
Write-Host "To update applications:" -ForegroundColor Cyan
Write-Host "  .\scripts\publish-apps.ps1"
Write-Host "  .\scripts\update-instances.ps1"
Write-Host ""
Write-Host "To destroy everything:" -ForegroundColor Cyan
Write-Host "  .\scripts\destroy.ps1"
