# Script to update the Contoso API on the running EC2 instance
# This rebuilds the API, uploads to S3, and restarts the service

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Updating Contoso API on EC2" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Disable SSL verification
$env:AWS_CA_BUNDLE = ""
$env:NODE_TLS_REJECT_UNAUTHORIZED = "0"

# Get AWS account
$AWS_ACCOUNT = (aws sts get-caller-identity --query Account --output text --no-verify-ssl)
$DEPLOYMENT_BUCKET = "contoso-deployment-$AWS_ACCOUNT"

Write-Host "Deployment bucket: $DEPLOYMENT_BUCKET" -ForegroundColor Cyan
Write-Host ""

# Get root directory
$ROOT_DIR = "$PSScriptRoot\..\.."

# Step 1: Build and publish Contoso API
Write-Host "Step 1/4: Building Contoso API..." -ForegroundColor Yellow
Push-Location "$ROOT_DIR\ContosoUniversity"
dotnet publish -c Release -o "$ROOT_DIR\publish\ContosoUniversity"
Pop-Location
Write-Host ""

# Step 2: Create zip file
Write-Host "Step 2/4: Creating zip file..." -ForegroundColor Yellow
if (Test-Path "$ROOT_DIR\publish\contoso-api.zip") {
    Remove-Item "$ROOT_DIR\publish\contoso-api.zip" -Force
}
Compress-Archive -Path "$ROOT_DIR\publish\ContosoUniversity\*" -DestinationPath "$ROOT_DIR\publish\contoso-api.zip" -Force
Write-Host ""

# Step 3: Upload to S3
Write-Host "Step 3/4: Uploading to S3..." -ForegroundColor Yellow
aws s3 cp "$ROOT_DIR\publish\contoso-api.zip" s3://$DEPLOYMENT_BUCKET/contoso-api.zip --no-verify-ssl
Write-Host ""

# Step 4: Restart service on EC2
Write-Host "Step 4/4: Restarting API on EC2..." -ForegroundColor Yellow
$INSTANCE_ID = (aws ec2 describe-instances --filters "Name=tag:Name,Values=ContosoUniversityStack/Compute/ContosoInstance" "Name=instance-state-name,Values=running" --query "Reservations[0].Instances[0].InstanceId" --output text --no-verify-ssl)

if ($INSTANCE_ID -eq "None" -or [string]::IsNullOrEmpty($INSTANCE_ID)) {
    Write-Host "ERROR: Could not find running EC2 instance" -ForegroundColor Red
    exit 1
}

Write-Host "Instance ID: $INSTANCE_ID" -ForegroundColor Gray

# Create a temporary JSON file for the SSM command
$commandJson = @{
    commands = @(
        "cd /opt/contoso-api",
        "aws s3 cp s3://$DEPLOYMENT_BUCKET/contoso-api.zip . --no-verify-ssl",
        "unzip -o contoso-api.zip",
        "rm contoso-api.zip",
        "systemctl restart contoso-api",
        "sleep 5",
        "systemctl status contoso-api --no-pager"
    )
} | ConvertTo-Json

$tempFile = [System.IO.Path]::GetTempFileName()
$commandJson | Out-File -FilePath $tempFile -Encoding utf8

# Send commands to EC2 via SSM
$COMMAND_ID = (aws ssm send-command `
    --instance-ids $INSTANCE_ID `
    --document-name "AWS-RunShellScript" `
    --parameters file://$tempFile `
    --query "Command.CommandId" `
    --output text)

Remove-Item $tempFile

Write-Host "Command ID: $COMMAND_ID" -ForegroundColor Gray
Write-Host "Waiting for command to complete..." -ForegroundColor Gray

# Wait for command to complete
Start-Sleep -Seconds 15

# Get command output
$OUTPUT = (aws ssm get-command-invocation `
    --command-id $COMMAND_ID `
    --instance-id $INSTANCE_ID `
    --query "StandardOutputContent" `
    --output text)

Write-Host ""
Write-Host "Command output:" -ForegroundColor Cyan
Write-Host $OUTPUT

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "API update completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
