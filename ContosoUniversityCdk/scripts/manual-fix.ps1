# Manual fix for EC2 instance service
$ErrorActionPreference = "Continue"

Write-Host "Fixing Contoso API service on EC2 instance..." -ForegroundColor Yellow

$instanceId = "i-01205e6d0daac5b61"
$region = "us-east-1"

# Step 1: Create the service file
Write-Host "Step 1: Creating systemd service file..." -ForegroundColor Cyan
$cmdId1 = aws ssm send-command `
    --instance-ids $instanceId `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["cat > /tmp/create-service.sh << '\''EOFSCRIPT'\''\n#!/bin/bash\nDB_SECRET=$(aws secretsmanager get-secret-value --secret-id arn:aws:secretsmanager:us-east-1:981461568039:secret:DatabaseDbSecret1098DC6E-jOpdtasXdg5U-6JFMTH --region us-east-1 --query SecretString --output text --no-verify-ssl 2>/dev/null)\nDB_USERNAME=$(echo \"$DB_SECRET\" | jq -r .username)\nDB_PASSWORD=$(echo \"$DB_SECRET\" | jq -r .password)\ncat > /etc/systemd/system/contoso-api.service << EOF\n[Unit]\nDescription=Contoso University API\nAfter=network.target\n\n[Service]\nType=simple\nWorkingDirectory=/opt/contoso-api\nExecStart=/usr/bin/dotnet /opt/contoso-api/ContosoUniversity.dll\nRestart=always\nRestartSec=10\nUser=root\nEnvironment=ASPNETCORE_ENVIRONMENT=Production\nEnvironment=ASPNETCORE_URLS=http://0.0.0.0:80\nEnvironment=DB_HOST=contosouniversitystack-databaseauroracluster35ae33-zgboy05utsxy.cluster-cglw06ya4k77.us-east-1.rds.amazonaws.com\nEnvironment=DB_NAME=contoso\nEnvironment=DB_USERNAME=$DB_USERNAME\nEnvironment=DB_PASSWORD=$DB_PASSWORD\nEnvironment=AllowedHosts=*\nEnvironment=NotificationAPI__BaseUrl=http://contoso-alb-2061956418.us-east-1.elb.amazonaws.com\n\n[Install]\nWantedBy=multi-user.target\nEOF\nEOFSCRIPT\n","chmod +x /tmp/create-service.sh","bash /tmp/create-service.sh"]' `
    --region $region `
    --query "Command.CommandId" `
    --output text `
    --no-verify-ssl 2>$null

if ($cmdId1) {
    $cmdId1 = $cmdId1.Trim()
    Write-Host "Command sent: $cmdId1" -ForegroundColor Gray
    Start-Sleep -Seconds 10
}

# Step 2: Start the service
Write-Host "Step 2: Starting the service..." -ForegroundColor Cyan
$cmdId2 = aws ssm send-command `
    --instance-ids $instanceId `
    --document-name "AWS-RunShellScript" `
    --parameters 'commands=["systemctl daemon-reload","systemctl enable contoso-api","systemctl start contoso-api","sleep 5","systemctl status contoso-api --no-pager","echo ---","curl -v http://localhost/ 2>&1 | head -20"]' `
    --region $region `
    --query "Command.CommandId" `
    --output text `
    --no-verify-ssl 2>$null

if ($cmdId2) {
    $cmdId2 = $cmdId2.Trim()
    Write-Host "Command sent: $cmdId2" -ForegroundColor Gray
    Start-Sleep -Seconds 10
    
    # Get output
    Write-Host "`nService status:" -ForegroundColor Cyan
    $output = aws ssm get-command-invocation `
        --command-id $cmdId2 `
        --instance-id $instanceId `
        --region $region `
        --no-verify-ssl 2>$null | ConvertFrom-Json
    
    Write-Host $output.StandardOutputContent
    
    if ($output.StandardErrorContent) {
        Write-Host "`nErrors:" -ForegroundColor Yellow
        Write-Host $output.StandardErrorContent
    }
}

Write-Host "`nWaiting 30 seconds for health checks..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`nChecking target health..." -ForegroundColor Cyan
$tgArn = (aws elbv2 describe-target-groups --names contoso-api-tg --query "TargetGroups[0].TargetGroupArn" --output text --no-verify-ssl 2>$null).Trim()
aws elbv2 describe-target-health --target-group-arn $tgArn --query "TargetHealthDescriptions[*].[Target.Id,TargetHealth.State]" --output table --no-verify-ssl 2>$null

Write-Host "`nTesting ALB endpoint..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://contoso-alb-2061956418.us-east-1.elb.amazonaws.com/" -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✅ ALB is responding! Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  ALB still returning error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "The service may need more time to become healthy." -ForegroundColor Gray
}
