# Publish React UI for EC2 deployment

$ErrorActionPreference = "Stop"

# Suppress SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Publishing React UI" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get AWS account
$ErrorActionPreference = "Continue"
$ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" })
$ErrorActionPreference = "Stop"

$BUCKET = "contoso-deployment-$ACCOUNT"

Write-Host "Account: $ACCOUNT" -ForegroundColor Cyan
Write-Host "Bucket: $BUCKET" -ForegroundColor Cyan
Write-Host ""

# Get EC2 public IP for API URL
Write-Host "Getting EC2 public IP..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$PUBLIC_IP = (aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs[?contains(OutputKey,'InstancePublicIp')].OutputValue" --output text --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" })
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($PUBLIC_IP)) {
    Write-Host "❌ Error: Could not get EC2 public IP" -ForegroundColor Red
    exit 1
}

Write-Host "EC2 Public IP: $PUBLIC_IP" -ForegroundColor Cyan
Write-Host ""

# Build React UI
Write-Host "Building React UI..." -ForegroundColor Yellow
$uiPath = Join-Path $PSScriptRoot "..\..\contoso-university-ui"

Push-Location $uiPath

# Update .env.production with EC2 IP
"VITE_API_BASE_URL=http://$PUBLIC_IP/api" | Out-File -FilePath .env.production -Encoding utf8
Write-Host "✅ Updated .env.production" -ForegroundColor Green

# Build
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Zip the dist folder
Push-Location dist
Compress-Archive -Path * -DestinationPath ..\react-ui.zip -Force
Pop-Location

Write-Host "✅ React UI built" -ForegroundColor Green
Write-Host ""

# Upload to S3
Write-Host "Uploading React UI to S3..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$null = aws s3 cp react-ui.zip "s3://$BUCKET/react-ui.zip" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
$ErrorActionPreference = "Stop"
Remove-Item react-ui.zip

Pop-Location

Write-Host "✅ Uploaded to S3" -ForegroundColor Green
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ React UI Published!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "React UI available at: s3://$BUCKET/react-ui.zip" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: Update the EC2 instance to download and serve the UI" -ForegroundColor Yellow
