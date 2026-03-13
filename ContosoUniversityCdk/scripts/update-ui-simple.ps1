# Update React UI for simple stack deployment

$ErrorActionPreference = "Stop"

# Suppress SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Update React UI (Simple Stack)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get EC2 public IP
Write-Host "Getting EC2 public IP..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$PUBLIC_IP = (aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs[?contains(OutputKey,'InstancePublicIp')].OutputValue" --output text --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" })
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($PUBLIC_IP) -or $PUBLIC_IP -eq "None") {
    Write-Host "❌ Error: Could not get EC2 public IP from stack outputs" -ForegroundColor Red
    Write-Host "Available outputs:" -ForegroundColor Yellow
    aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs[].OutputKey" --output text --no-verify-ssl 2>$null
    exit 1
}

Write-Host "EC2 Public IP: $PUBLIC_IP" -ForegroundColor Cyan
Write-Host "API URL: http://$PUBLIC_IP/api" -ForegroundColor Cyan
Write-Host ""

# Build React UI
Write-Host "Building React UI..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\..\..\contoso-university-ui"

# Update .env.production
"VITE_API_BASE_URL=http://$PUBLIC_IP/api" | Out-File -FilePath .env.production -Encoding utf8
Write-Host "✅ Updated .env.production" -ForegroundColor Green

# Build
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location
Write-Host "✅ Build complete" -ForegroundColor Green
Write-Host ""

# Deploy to S3
Write-Host "Deploying to S3 and CloudFront..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\.."
cdk deploy ContosoUniversityStack --require-approval never
Pop-Location

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ UI Updated!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Get CloudFront URL
$ErrorActionPreference = "Continue"
$CLOUDFRONT_URL = (aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionUrl'].OutputValue" --output text --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" })
$ErrorActionPreference = "Stop"

Write-Host "Access your application at:" -ForegroundColor Cyan
Write-Host "  $CLOUDFRONT_URL" -ForegroundColor Green
Write-Host ""
Write-Host "Note: CloudFront cache invalidation may take 1-2 minutes" -ForegroundColor Yellow
