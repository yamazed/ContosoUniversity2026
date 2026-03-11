# Deploy/update React UI with correct ALB endpoint

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Deploy React UI" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get AWS region
$REGION = (aws configure get region)
if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

# Get ALB URL from CloudFormation stack
Write-Host "Getting ALB URL from CloudFormation..." -ForegroundColor Yellow
$ALB_URL = (aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --region $REGION `
    --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" `
    --output text)

if ([string]::IsNullOrEmpty($ALB_URL)) {
    Write-Host "❌ Error: Could not get ALB URL from stack" -ForegroundColor Red
    Write-Host "Make sure the CDK stack is deployed: cdk deploy" -ForegroundColor Yellow
    exit 1
}

Write-Host "ALB URL: $ALB_URL" -ForegroundColor Cyan
Write-Host ""

# Check if React UI directory exists
if (-not (Test-Path "..\contoso-university-ui")) {
    Write-Host "❌ Error: React UI directory not found" -ForegroundColor Red
    Write-Host "Expected: ..\contoso-university-ui" -ForegroundColor Yellow
    exit 1
}

# Build React UI with correct API endpoint
Write-Host "Building React UI..." -ForegroundColor Yellow
Push-Location ..\contoso-university-ui

# Update .env.production with ALB URL
"VITE_API_BASE_URL=$ALB_URL/api" | Out-File -FilePath .env.production -Encoding utf8
Write-Host "✅ Updated .env.production:" -ForegroundColor Green
Get-Content .env.production
Write-Host ""

# Install dependencies if needed
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
    npm install
}

# Build
Write-Host "Building production bundle..." -ForegroundColor Yellow
npm run build

Pop-Location
Write-Host "✅ React UI built successfully" -ForegroundColor Green
Write-Host ""

# Deploy to S3 and invalidate CloudFront
Write-Host "Deploying to S3 and CloudFront..." -ForegroundColor Yellow
cdk deploy --require-approval never

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ React UI Deployed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Get CloudFront URL
$CLOUDFRONT_URL = (aws cloudformation describe-stacks `
    --stack-name ContosoUniversityStack `
    --region $REGION `
    --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionUrl'].OutputValue" `
    --output text)

Write-Host "Access your application at:" -ForegroundColor Cyan
Write-Host "  $CLOUDFRONT_URL"
Write-Host ""
Write-Host "API endpoint configured:" -ForegroundColor Cyan
Write-Host "  $ALB_URL/api"
Write-Host ""
Write-Host "Note: CloudFront cache invalidation may take 1-2 minutes" -ForegroundColor Yellow
