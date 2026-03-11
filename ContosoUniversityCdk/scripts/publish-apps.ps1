# Publish .NET applications for EC2 deployment (PowerShell)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Publishing .NET Applications" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get AWS account
$accountOutput = aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>&1
$ACCOUNT = ($accountOutput | Where-Object { $_ -match '^\d{12}$' }) -join ''

$regionOutput = aws configure get region --no-verify-ssl 2>&1
$REGION = ($regionOutput | Where-Object { $_ -match '^[a-z]+-[a-z]+-\d+$' }) -join ''

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
try {
    aws s3 ls "s3://$BUCKET" 2>$null | Out-Null
    Write-Host "✅ Bucket exists" -ForegroundColor Green
} catch {
    Write-Host "Creating S3 bucket: $BUCKET" -ForegroundColor Yellow
    if ($REGION -eq "us-east-1") {
        aws s3 mb "s3://$BUCKET" 2>$null
    } else {
        aws s3 mb "s3://$BUCKET" --region $REGION 2>$null
    }
    Write-Host "✅ Bucket created" -ForegroundColor Green
}
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
aws s3 cp contoso-api.zip "s3://$BUCKET/contoso-api.zip" 2>$null
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
aws s3 cp notification-api.zip "s3://$BUCKET/notification-api.zip" 2>$null
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
