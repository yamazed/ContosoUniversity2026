# Script to destroy the CDK stack and all AWS resources
# WARNING: This will delete all resources including data in the database

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Red
Write-Host "WARNING: Destroying Contoso University Stack" -ForegroundColor Red
Write-Host "==========================================" -ForegroundColor Red
Write-Host ""
Write-Host "This will delete ALL resources including:" -ForegroundColor Yellow
Write-Host "  - EC2 Instances and Auto Scaling Groups" -ForegroundColor Yellow
Write-Host "  - Aurora Database (and all data)" -ForegroundColor Yellow
Write-Host "  - S3 Bucket (and all files)" -ForegroundColor Yellow
Write-Host "  - CloudFront Distribution" -ForegroundColor Yellow
Write-Host "  - Load Balancer" -ForegroundColor Yellow
Write-Host "  - VPC and Networking" -ForegroundColor Yellow
Write-Host "  - SQS Queue" -ForegroundColor Yellow
Write-Host ""

# Prompt for confirmation
$confirmation = Read-Host "Are you sure you want to destroy the stack? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Destruction cancelled" -ForegroundColor Green
    exit 0
}

# Check if AWS credentials are configured
try {
    aws sts get-caller-identity | Out-Null
} catch {
    Write-Host "ERROR: AWS credentials not configured or invalid" -ForegroundColor Red
    exit 1
}

# Navigate to CDK directory
$scriptDir = Split-Path -Parent $PSCommandPath
Push-Location "$scriptDir\.."

Write-Host "Destroying CDK stack..." -ForegroundColor Yellow
cdk destroy --force

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Stack destroyed successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Pop-Location
