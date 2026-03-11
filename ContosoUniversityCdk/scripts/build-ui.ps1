# Script to build the React UI for deployment
# This script builds the React application with the correct API endpoint

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Building React UI" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Get AWS region
$REGION = (aws configure get region 2>$null)
if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

# Get ALB URL from CloudFormation stack
Write-Host "Getting ALB URL from CloudFormation..." -ForegroundColor Yellow
try {
    $ALB_URL = (aws cloudformation describe-stacks `
        --stack-name ContosoUniversityStack `
        --region $REGION `
        --query "Stacks[0].Outputs[?OutputKey=='ApplicationLoadBalancerUrl'].OutputValue" `
        --output text 2>$null)
} catch {
    $ALB_URL = ""
}

if ([string]::IsNullOrEmpty($ALB_URL)) {
    Write-Host "⚠️  Warning: Could not get ALB URL from stack" -ForegroundColor Yellow
    Write-Host "Using placeholder URL. Deploy infrastructure first with: cdk deploy" -ForegroundColor Yellow
    $ALB_URL = "http://placeholder-alb.example.com"
}

Write-Host "API Endpoint: $ALB_URL/api" -ForegroundColor Cyan
Write-Host ""

# Navigate to the React UI directory
$scriptDir = Split-Path -Parent $PSCommandPath
Push-Location "$scriptDir\..\..\contoso-university-ui"

# Update .env.production with ALB URL
"VITE_API_BASE_URL=$ALB_URL/api" | Out-File -FilePath .env.production -Encoding utf8
Write-Host "✅ Updated .env.production" -ForegroundColor Green
Write-Host ""

# Check if node_modules exists, if not run npm install
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing dependencies..." -ForegroundColor Yellow
    npm install
}

# Build the React application
Write-Host "Building production bundle..." -ForegroundColor Yellow
npm run build

Write-Host "==========================================" -ForegroundColor Green
Write-Host "React UI build completed successfully!" -ForegroundColor Green
Write-Host "Build output location: $(Get-Location)\dist" -ForegroundColor Cyan
Write-Host "API endpoint: $ALB_URL/api" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Green

Pop-Location
