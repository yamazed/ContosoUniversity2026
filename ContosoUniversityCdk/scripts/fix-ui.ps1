# Fix UI on instance

$ErrorActionPreference = "Stop"
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "Fixing UI on instance..." -ForegroundColor Yellow

$INSTANCE_ID = "i-0a01b722787ee7938"
$BUCKET = "contoso-deployment-981461568039"

# Send fix command
$ErrorActionPreference = "Continue"
aws ssm send-command `
    --instance-ids $INSTANCE_ID `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["cd /var/www/html","rm -rf *","aws s3 cp s3://'$BUCKET'/react-ui.zip . --no-verify-ssl","unzip -o react-ui.zip","rm react-ui.zip","ls -la","systemctl start nginx","systemctl status nginx --no-pager"]' `
    --no-verify-ssl 2>$null

Write-Host ""
Write-Host "Command sent. Wait 10 seconds then check http://50.19.77.253" -ForegroundColor Cyan
