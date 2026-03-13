# Test deployment - NotificationAPI only

$ErrorActionPreference = "Stop"

# Suppress Python SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Test Deployment - NotificationAPI Only" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Publish NotificationAPI
Write-Host "Step 1/2: Publishing NotificationAPI..." -ForegroundColor Yellow
$notificationPath = Join-Path $PSScriptRoot "..\..\NotificationAPI"

Push-Location $notificationPath
dotnet publish -c Release -o .\publish --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to publish NotificationAPI" -ForegroundColor Red
    Pop-Location
    exit 1
}

Push-Location publish
Compress-Archive -Path * -DestinationPath ..\notification-api.zip -Force
Pop-Location

# Upload to S3
$ErrorActionPreference = "Continue"
$ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }).Trim()
$ErrorActionPreference = "Stop"
$BUCKET = "contoso-deployment-$ACCOUNT"

Write-Host "Uploading to S3..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$null = aws s3 cp notification-api.zip "s3://$BUCKET/notification-api.zip" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
$ErrorActionPreference = "Stop"
Remove-Item notification-api.zip
Remove-Item -Recurse -Force publish
Pop-Location

Write-Host "✅ Published" -ForegroundColor Green
Write-Host ""

# Step 2: Deploy test stack
Write-Host "Step 2/2: Deploying test stack..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\.."

# Use test stack name
$env:CDK_STACK_NAME = "ContosoUniversityTestStack"

cdk deploy ContosoUniversityTestStack --require-approval never

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ Test Deployment Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Check the outputs above for the Swagger URL" -ForegroundColor Cyan

Pop-Location

