# Script to deploy the CDK stack to AWS
# This builds the UI, synthesizes the CDK stack, and deploys all resources

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Deploying Contoso University to AWS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Check if AWS credentials are configured
try {
    aws sts get-caller-identity | Out-Null
} catch {
    Write-Host "ERROR: AWS credentials not configured or invalid" -ForegroundColor Red
    Write-Host "Please configure AWS CLI credentials before deploying" -ForegroundColor Yellow
    exit 1
}

# Get AWS account and region info
$AWS_ACCOUNT = (aws sts get-caller-identity --query Account --output text)
$AWS_REGION = if ($env:AWS_REGION) { $env:AWS_REGION } else { "us-east-1" }

Write-Host "Deploying to AWS Account: $AWS_ACCOUNT" -ForegroundColor Cyan
Write-Host "Region: $AWS_REGION" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build React UI
Write-Host "Step 1/3: Building React UI..." -ForegroundColor Yellow
$scriptDir = Split-Path -Parent $PSCommandPath
& "$scriptDir\build-ui.ps1"
Write-Host ""

# Step 2: Navigate to CDK directory
Push-Location "$scriptDir\.."

# Restore .NET dependencies
Write-Host "Step 2/3: Restoring .NET dependencies..." -ForegroundColor Yellow
dotnet restore
Write-Host ""

# Step 3: Deploy CDK stack
Write-Host "Step 3/3: Deploying CDK stack..." -ForegroundColor Yellow
Write-Host "This may take 10-15 minutes..." -ForegroundColor Gray
cdk deploy ContosoUniversityStack --require-approval never

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Stack outputs:" -ForegroundColor Cyan
try {
    cdk deploy ContosoUniversityStack --outputs-file outputs.json --require-approval never
    if (Test-Path outputs.json) {
        Get-Content outputs.json | ConvertFrom-Json | ConvertTo-Json -Depth 10
    }
} catch {
    # Ignore errors
}

Pop-Location
