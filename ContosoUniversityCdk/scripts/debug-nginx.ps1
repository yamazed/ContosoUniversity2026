# Debug nginx setup

$ErrorActionPreference = "Continue"
$env:PYTHONWARNINGS = "ignore:Unverified HTTPS request"

$INSTANCE_ID = "i-0a01b722787ee7938"

Write-Host "Checking nginx setup..." -ForegroundColor Yellow

$COMMAND_ID = (aws ssm send-command `
    --instance-ids $INSTANCE_ID `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["echo === Files in /var/www/html ===","ls -la /var/www/html/","echo","echo === Nginx config ===","cat /etc/nginx/conf.d/contoso.conf","echo","echo === Nginx error log ===","tail -20 /var/log/nginx/error.log","echo","echo === Nginx status ===","systemctl status nginx --no-pager"]' `
    --output text `
    --query "Command.CommandId" `
    --no-verify-ssl 2>$null | Where-Object { $_ -and $_ -notmatch "InsecureRequestWarning" })

Write-Host "Command ID: $COMMAND_ID"
Write-Host "Waiting..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host ""
aws ssm get-command-invocation --command-id $COMMAND_ID --instance-id $INSTANCE_ID --query "StandardOutputContent" --output text --no-verify-ssl 2>$null
