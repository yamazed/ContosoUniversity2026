# Quick update - rebuild and redeploy NotificationAPI only
$ErrorActionPreference = "Continue"

Write-Host "Quick Update - NotificationAPI Only" -ForegroundColor Cyan
Write-Host ""

# Get account
$ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }).Trim()
$BUCKET = "contoso-deployment-$ACCOUNT"

# Publish NotificationAPI
Write-Host "Publishing NotificationAPI..." -ForegroundColor Yellow
Push-Location ..\NotificationAPI
dotnet publish -c Release -o .\publish --self-contained false
Push-Location publish
Compress-Archive -Path * -DestinationPath ..\notification-api.zip -Force
Pop-Location
Write-Host "✅ Published" -ForegroundColor Green

# Upload to S3
Write-Host "Uploading to S3..." -ForegroundColor Yellow
$null = aws s3 cp notification-api.zip "s3://$BUCKET/notification-api.zip" --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }
Remove-Item notification-api.zip
Remove-Item -Recurse -Force publish
Write-Host "✅ Uploaded" -ForegroundColor Green
Pop-Location

# Get instance ID
$instanceId = (aws ec2 describe-instances --filters "Name=tag:aws:cloudformation:stack-name,Values=ContosoUniversityStack" "Name=instance-state-name,Values=running" --query "Reservations[0].Instances[0].InstanceId" --output text --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }).Trim()

Write-Host "Updating instance $instanceId..." -ForegroundColor Yellow
$cmdId = (aws ssm send-command --instance-ids $instanceId --document-name "AWS-RunShellScript" --parameters 'commands=["cd /opt/notification-api","aws s3 cp s3://contoso-deployment-'$ACCOUNT'/notification-api.zip . --no-verify-ssl","unzip -o notification-api.zip","rm notification-api.zip","systemctl restart notification-api","sleep 5","systemctl status notification-api --no-pager"]' --region us-east-1 --query "Command.CommandId" --output text --no-verify-ssl 2>&1 | Where-Object { $_ -notmatch "InsecureRequestWarning" -and $_ -notmatch "urllib3" }).Trim()

Write-Host "Command sent: $cmdId" -ForegroundColor Gray
Start-Sleep -Seconds 10

Write-Host "✅ Update complete!" -ForegroundColor Green
Write-Host "Test at: http://50.19.77.253:8080/api/notifications" -ForegroundColor Cyan
