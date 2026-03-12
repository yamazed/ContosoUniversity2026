# Publish .NET applications for EC2 deployment (PowerShell)

$ErrorActionPreference = "Stop"

# Suppress Python SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Publishing .NET Applications" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get AWS account (temporarily allow errors for AWS CLI warnings)
$ErrorActionPreference = "Continue"
$ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>$null).Trim()
$REGION = (aws configure get region --no-verify-ssl 2>$null).Trim()
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

$BUCKET = "contoso-deployment-$ACCOUNT"

if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

Write-Host "Account: $ACCOUNT" -ForegroundColor Cyan
Write-Host "Region: $REGION" -ForegroundColor Cyan
Write-Host "Bucket: $BUCKET" -ForegroundColor Cyan
Write-Host ""

# Create S3 bucket if it doesn't exist
Write-Host "Checking S3 bucket..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
try {
    $null = aws s3 ls "s3://$BUCKET" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
    Write-Host "✅ Bucket exists" -ForegroundColor Green
} catch {
    Write-Host "Creating S3 bucket: $BUCKET" -ForegroundColor Yellow
    if ($REGION -eq "us-east-1") {
        $null = aws s3 mb "s3://$BUCKET" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
    } else {
        $null = aws s3 mb "s3://$BUCKET" --region $REGION --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
    }
    Write-Host "✅ Bucket created" -ForegroundColor Green
}
$ErrorActionPreference = "Stop"
Write-Host ""

# Publish ContosoUniversity API
Write-Host "Publishing ContosoUniversity API..." -ForegroundColor Yellow
$contosoPath = Join-Path $PSScriptRoot "..\..\ContosoUniversity"
if (-not (Test-Path $contosoPath)) {
    Write-Host "❌ Error: ContosoUniversity project not found at $contosoPath" -ForegroundColor Red
    exit 1
}

Push-Location $contosoPath
dotnet publish -c Release -o .\publish --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Failed to publish ContosoUniversity API" -ForegroundColor Red
    Pop-Location
    exit 1
}

Push-Location publish
Compress-Archive -Path * -DestinationPath ..\contoso-api.zip -Force
Pop-Location
Write-Host "✅ ContosoUniversity API published" -ForegroundColor Green
Write-Host ""

# Upload to S3
Write-Host "Uploading ContosoUniversity API to S3..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$null = aws s3 cp contoso-api.zip "s3://$BUCKET/contoso-api.zip" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
$ErrorActionPreference = "Stop"
Remove-Item contoso-api.zip
Remove-Item -Recurse -Force publish
Write-Host "✅ Uploaded to S3" -ForegroundColor Green
Pop-Location
Write-Host ""

# Publish NotificationAPI
Write-Host "Publishing NotificationAPI..." -ForegroundColor Yellow
$notificationPath = Join-Path $PSScriptRoot "..\..\NotificationAPI"
if (-not (Test-Path $notificationPath)) {
    Write-Host "❌ Error: NotificationAPI project not found at $notificationPath" -ForegroundColor Red
    exit 1
}

Push-Location $notificationPath
dotnet publish -c Release -o .\publish --self-contained false
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Failed to publish NotificationAPI" -ForegroundColor Red
    Pop-Location
    exit 1
}

Push-Location publish
Compress-Archive -Path * -DestinationPath ..\notification-api.zip -Force
Pop-Location
Write-Host "✅ NotificationAPI published" -ForegroundColor Green
Write-Host ""

# Upload to S3
Write-Host "Uploading NotificationAPI to S3..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$null = aws s3 cp notification-api.zip "s3://$BUCKET/notification-api.zip" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
$ErrorActionPreference = "Stop"
Remove-Item notification-api.zip
Remove-Item -Recurse -Force publish
Write-Host "✅ Uploaded to S3" -ForegroundColor Green
Pop-Location
Write-Host ""

Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ Applications Published Successfully" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Applications are available at:" -ForegroundColor Cyan
Write-Host "  s3://$BUCKET/contoso-api.zip"
Write-Host "  s3://$BUCKET/notification-api.zip"
Write-Host ""
