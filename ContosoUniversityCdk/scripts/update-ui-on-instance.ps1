# Update React UI on running EC2 instance

$ErrorActionPreference = "Stop"

# Suppress SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Update React UI on EC2 Instance" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Get instance ID
Write-Host "Getting instance ID..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$INSTANCE_ID = (aws ec2 describe-instances --filters "Name=tag:Name,Values=ContosoUniversityStack/Compute/ContosoInstance" "Name=instance-state-name,Values=running" --query "Reservations[0].Instances[0].InstanceId" --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }).Trim()
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($INSTANCE_ID) -or $INSTANCE_ID -eq "None") {
    Write-Host "❌ No running instance found" -ForegroundColor Red
    exit 1
}

Write-Host "Instance ID: $INSTANCE_ID" -ForegroundColor Cyan
Write-Host ""

# Get AWS account
$ErrorActionPreference = "Continue"
$ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }).Trim()
$ErrorActionPreference = "Stop"
$BUCKET = "contoso-deployment-$ACCOUNT"

# Send command to update UI
Write-Host "Sending update command to instance..." -ForegroundColor Yellow

$ErrorActionPreference = "Continue"
$COMMAND_ID = (aws ssm send-command `
    --instance-ids $INSTANCE_ID `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["cd /var/www/html","rm -rf *","aws s3 cp s3://'$BUCKET'/react-ui.zip . --no-verify-ssl","unzip -o react-ui.zip","rm react-ui.zip","systemctl restart nginx","echo UI updated successfully"]' `
    --output text `
    --query "Command.CommandId" `
    --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" })
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($COMMAND_ID)) {
    Write-Host "❌ Failed to send command" -ForegroundColor Red
    exit 1
}

Write-Host "Command ID: $COMMAND_ID" -ForegroundColor Cyan
Write-Host "Waiting for command to complete..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "Command output:" -ForegroundColor Cyan
$ErrorActionPreference = "Continue"
aws ssm get-command-invocation --command-id $COMMAND_ID --instance-id $INSTANCE_ID --query "StandardOutputContent" --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ UI Updated on Instance!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Get public IP
$ErrorActionPreference = "Continue"
$PUBLIC_IP = (aws cloudformation describe-stacks --stack-name ContosoUniversityStack --query "Stacks[0].Outputs[?contains(OutputKey,'InstancePublicIp')].OutputValue" --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }).Trim()
$ErrorActionPreference = "Stop"

Write-Host "Access your application at:" -ForegroundColor Cyan
Write-Host "  http://$PUBLIC_IP" -ForegroundColor Green
