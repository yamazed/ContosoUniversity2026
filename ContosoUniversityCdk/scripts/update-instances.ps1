# Update running EC2 instances with new application versions

$ErrorActionPreference = "Continue"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Updating EC2 Instances" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$REGION = (aws configure get region --no-verify-ssl 2>$null).Trim()
if ([string]::IsNullOrEmpty($REGION)) {
    $REGION = "us-east-1"
}

# Get Auto Scaling Group names from CloudFormation outputs
$STACK_NAME = "ContosoUniversityStack"

Write-Host "Getting Auto Scaling Group names..." -ForegroundColor Yellow
$CONTOSO_ASG = (aws cloudformation describe-stacks `
    --stack-name $STACK_NAME `
    --region $REGION `
    --query "Stacks[0].Outputs[?OutputKey=='ContosoApiAsgName'].OutputValue" `
    --output text `
    --no-verify-ssl 2>$null).Trim()

$NOTIFICATION_ASG = (aws cloudformation describe-stacks `
    --stack-name $STACK_NAME `
    --region $REGION `
    --query "Stacks[0].Outputs[?OutputKey=='NotificationApiAsgName'].OutputValue" `
    --output text `
    --no-verify-ssl 2>$null).Trim()

if ([string]::IsNullOrEmpty($CONTOSO_ASG) -or [string]::IsNullOrEmpty($NOTIFICATION_ASG)) {
    Write-Host "❌ Error: Could not find Auto Scaling Groups" -ForegroundColor Red
    Write-Host "Make sure the stack is deployed: cdk deploy" -ForegroundColor Yellow
    exit 1
}

Write-Host "Contoso API ASG: $CONTOSO_ASG" -ForegroundColor Cyan
Write-Host "Notification API ASG: $NOTIFICATION_ASG" -ForegroundColor Cyan
Write-Host ""

# Function to update instances in an ASG
function Update-AsgInstances {
    param(
        [string]$ASG_NAME,
        [string]$APP_NAME,
        [string]$SERVICE_NAME
    )
    
    Write-Host "Updating $APP_NAME instances..." -ForegroundColor Yellow
    
    # Get instance IDs
    $INSTANCE_IDS = (aws autoscaling describe-auto-scaling-groups `
        --auto-scaling-group-names $ASG_NAME `
        --region $REGION `
        --query "AutoScalingGroups[0].Instances[?LifecycleState=='InService'].InstanceId" `
        --output text `
        --no-verify-ssl 2>$null).Trim()
    
    if ([string]::IsNullOrEmpty($INSTANCE_IDS)) {
        Write-Host "⚠️  No running instances found in $ASG_NAME" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Found instances: $INSTANCE_IDS" -ForegroundColor Cyan
    
    # Send command to each instance via SSM
    foreach ($INSTANCE_ID in $INSTANCE_IDS -split '\s+') {
        if ([string]::IsNullOrWhiteSpace($INSTANCE_ID)) {
            continue
        }
        
        Write-Host "  Updating instance $INSTANCE_ID..." -ForegroundColor Gray
        
        # Create command script
        $commandScript = @"
cd /opt/$APP_NAME
aws s3 cp s3://contoso-deployment-`$(aws sts get-caller-identity --query Account --output text --no-verify-ssl)/$APP_NAME.zip . --no-verify-ssl
unzip -o $APP_NAME.zip
rm $APP_NAME.zip
systemctl restart $SERVICE_NAME
sleep 5
systemctl status $SERVICE_NAME --no-pager
"@
        
        # Write to temp file
        $tempFile = [System.IO.Path]::GetTempFileName()
        $commandScript | Out-File -FilePath $tempFile -Encoding utf8 -NoNewline
        
        try {
            $COMMAND_ID = (aws ssm send-command `
                --instance-ids $INSTANCE_ID `
                --document-name "AWS-RunShellScript" `
                --parameters "commands=[`"cd /opt/$APP_NAME`",`"aws s3 cp s3://contoso-deployment-`$(aws sts get-caller-identity --query Account --output text --no-verify-ssl)/$APP_NAME.zip . --no-verify-ssl`",`"unzip -o $APP_NAME.zip`",`"rm $APP_NAME.zip`",`"systemctl restart $SERVICE_NAME`",`"sleep 5`",`"systemctl status $SERVICE_NAME --no-pager`"]" `
                --region $REGION `
                --query "Command.CommandId" `
                --output text `
                --no-verify-ssl 2>$null)
            
            if ([string]::IsNullOrWhiteSpace($COMMAND_ID)) {
                Write-Host "  ⚠️  Failed to send command (SSM might not be ready)" -ForegroundColor Yellow
                continue
            }
            
            $COMMAND_ID = $COMMAND_ID.Trim()
            Write-Host "  Command sent: $COMMAND_ID" -ForegroundColor Gray
            
            # Wait for command to complete
            Start-Sleep -Seconds 10
            
            try {
                $STATUS = (aws ssm get-command-invocation `
                    --command-id $COMMAND_ID `
                    --instance-id $INSTANCE_ID `
                    --region $REGION `
                    --query "Status" `
                    --output text `
                    --no-verify-ssl 2>$null)
                
                if (-not [string]::IsNullOrWhiteSpace($STATUS)) {
                    $STATUS = $STATUS.Trim()
                    Write-Host "  Status: $STATUS" -ForegroundColor Gray
                } else {
                    Write-Host "  Status: Pending" -ForegroundColor Gray
                }
            } catch {
                Write-Host "  Status: Pending" -ForegroundColor Gray
            }
        } catch {
            Write-Host "  ⚠️  Error sending command: $_" -ForegroundColor Yellow
        } finally {
            Remove-Item -Path $tempFile -ErrorAction SilentlyContinue
        }
    }
    
    Write-Host "✅ $APP_NAME instances updated" -ForegroundColor Green
    Write-Host ""
}

# Update both ASGs
Update-AsgInstances -ASG_NAME $CONTOSO_ASG -APP_NAME "contoso-api" -SERVICE_NAME "contoso-api"
Update-AsgInstances -ASG_NAME $NOTIFICATION_ASG -APP_NAME "notification-api" -SERVICE_NAME "notification-api"

Write-Host "==========================================" -ForegroundColor Green
Write-Host "✅ Update Complete" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Check application logs:" -ForegroundColor Cyan
Write-Host "  aws logs tail /aws/ec2/contoso-api --follow"
Write-Host "  aws logs tail /aws/ec2/notification-api --follow"
Write-Host ""
Write-Host "Or SSH to instances using Session Manager:" -ForegroundColor Cyan
Write-Host "  aws ssm start-session --target <instance-id>"
