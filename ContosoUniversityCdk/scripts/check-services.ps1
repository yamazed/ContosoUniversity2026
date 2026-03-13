# Check service status on EC2 instance

$ErrorActionPreference = "Stop"

# Suppress SSL warnings
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

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

Write-Host "Checking service status..." -ForegroundColor Yellow
$ErrorActionPreference = "Continue"
$COMMAND_ID = (aws ssm send-command --instance-ids $INSTANCE_ID --document-name "AWS-RunShellScript" --parameters 'commands=["echo === Contoso API Status ===","systemctl status contoso-api --no-pager","echo","echo === Notification API Status ===","systemctl status notification-api --no-pager","echo","echo === Recent Contoso API Logs ===","journalctl -u contoso-api -n 30 --no-pager","echo","echo === Recent Notification API Logs ===","journalctl -u notification-api -n 30 --no-pager"]' --output text --query "Command.CommandId" --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }).Trim()
$ErrorActionPreference = "Stop"

Write-Host "Command ID: $COMMAND_ID" -ForegroundColor Cyan
Write-Host "Waiting for command to complete..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Command output:" -ForegroundColor Cyan
$ErrorActionPreference = "Continue"
aws ssm get-command-invocation --command-id $COMMAND_ID --instance-id $INSTANCE_ID --query "StandardOutputContent" --output text --no-verify-ssl 2>$null | Where-Object { $_ -notmatch "InsecureRequestWarning" }
$ErrorActionPreference = "Stop"
