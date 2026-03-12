# Destroy CDK stack with SSL workaround
$ErrorActionPreference = "Continue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Destroying Contoso University Stack" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Disable SSL verification for Node.js (CDK)
$env:NODE_TLS_REJECT_UNAUTHORIZED = "0"

# Disable SSL verification for AWS SDK
$env:AWS_SDK_LOAD_CONFIG = "1"

Write-Host "⚠️  SSL certificate verification disabled for this session" -ForegroundColor Yellow
Write-Host ""

Write-Host "Starting stack destruction..." -ForegroundColor Yellow
Write-Host "This may take 10-15 minutes..." -ForegroundColor Gray
Write-Host ""

cdk destroy --force

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "✅ Stack Destroyed Successfully" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Error: Stack destruction failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "You can try manually deleting the stack from AWS Console:" -ForegroundColor Yellow
    Write-Host "https://console.aws.amazon.com/cloudformation" -ForegroundColor Cyan
}
