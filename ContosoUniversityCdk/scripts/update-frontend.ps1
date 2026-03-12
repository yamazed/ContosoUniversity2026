# Update React frontend with correct API URL
$ErrorActionPreference = "Continue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Updating React Frontend" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get instance public IP
Write-Host "Getting instance public IP..." -ForegroundColor Yellow
$outputs = aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs" --output json --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" } | ConvertFrom-Json

$instanceIp = ($outputs | Where-Object { $_.OutputKey -eq "ComputeInstancePublicIp939CCFF7" }).OutputValue
$cloudfrontId = ($outputs | Where-Object { $_.OutputKey -eq "CloudFrontDistributionId" }).OutputValue
$bucketName = ($outputs | Where-Object { $_.OutputKey -eq "FrontendBucketName" }).OutputValue

if ([string]::IsNullOrEmpty($instanceIp)) {
    Write-Host "❌ Error: Could not get instance IP" -ForegroundColor Red
    exit 1
}

Write-Host "Instance IP: $instanceIp" -ForegroundColor Cyan
Write-Host "CloudFront ID: $cloudfrontId" -ForegroundColor Cyan
Write-Host "S3 Bucket: $bucketName" -ForegroundColor Cyan
Write-Host ""

# Update React .env.production
Write-Host "Updating .env.production..." -ForegroundColor Yellow
Push-Location ..\contoso-university-ui

"VITE_API_BASE_URL=http://$instanceIp/api" | Out-File -FilePath .env.production -Encoding utf8 -NoNewline
Write-Host "✅ Updated .env.production" -ForegroundColor Green

# Rebuild React app
Write-Host "Rebuilding React app..." -ForegroundColor Yellow
npm run build --loglevel=error
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: React build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Write-Host "✅ React app rebuilt" -ForegroundColor Green

# Upload to S3
Write-Host "Uploading to S3..." -ForegroundColor Yellow
$null = aws s3 sync dist/ "s3://$bucketName/" --delete --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
Write-Host "✅ Uploaded to S3" -ForegroundColor Green

# Invalidate CloudFront cache
Write-Host "Invalidating CloudFront cache..." -ForegroundColor Yellow
$null = aws cloudfront create-invalidation --distribution-id $cloudfrontId --paths "/*" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
Write-Host "✅ CloudFront cache invalidated" -ForegroundColor Green

Pop-Location

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ Frontend Updated Successfully" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Wait 2-3 minutes for CloudFront to update, then access:" -ForegroundColor Cyan
Write-Host "  https://d3hepc6y3wkw3d.cloudfront.net" -ForegroundColor Yellow
Write-Host ""
Write-Host "Or test directly at:" -ForegroundColor Cyan
Write-Host "  http://$instanceIp" -ForegroundColor Yellow
