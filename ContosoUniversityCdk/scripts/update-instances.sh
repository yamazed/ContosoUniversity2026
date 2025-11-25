#!/bin/bash

# Update running EC2 instances with new application versions

set -e

echo "=========================================="
echo "Updating EC2 Instances"
echo "=========================================="
echo ""

REGION=$(aws configure get region)
if [ -z "$REGION" ]; then
    REGION="us-east-1"
fi

# Get Auto Scaling Group names from CloudFormation outputs
STACK_NAME="ContosoUniversityStack"

echo "Getting Auto Scaling Group names..."
CONTOSO_ASG=$(aws cloudformation describe-stacks \
    --stack-name $STACK_NAME \
    --region $REGION \
    --query "Stacks[0].Outputs[?OutputKey=='ContosoApiAsgName'].OutputValue" \
    --output text)

NOTIFICATION_ASG=$(aws cloudformation describe-stacks \
    --stack-name $STACK_NAME \
    --region $REGION \
    --query "Stacks[0].Outputs[?OutputKey=='NotificationApiAsgName'].OutputValue" \
    --output text)

if [ -z "$CONTOSO_ASG" ] || [ -z "$NOTIFICATION_ASG" ]; then
    echo "❌ Error: Could not find Auto Scaling Groups"
    echo "Make sure the stack is deployed: cdk deploy"
    exit 1
fi

echo "Contoso API ASG: $CONTOSO_ASG"
echo "Notification API ASG: $NOTIFICATION_ASG"
echo ""

# Function to update instances in an ASG
update_asg() {
    local ASG_NAME=$1
    local APP_NAME=$2
    local SERVICE_NAME=$3
    
    echo "Updating $APP_NAME instances..."
    
    # Get instance IDs
    INSTANCE_IDS=$(aws autoscaling describe-auto-scaling-groups \
        --auto-scaling-group-names $ASG_NAME \
        --region $REGION \
        --query "AutoScalingGroups[0].Instances[?LifecycleState=='InService'].InstanceId" \
        --output text)
    
    if [ -z "$INSTANCE_IDS" ]; then
        echo "⚠️  No running instances found in $ASG_NAME"
        return
    fi
    
    echo "Found instances: $INSTANCE_IDS"
    
    # Send command to each instance via SSM
    for INSTANCE_ID in $INSTANCE_IDS; do
        echo "  Updating instance $INSTANCE_ID..."
        
        COMMAND_ID=$(aws ssm send-command \
            --instance-ids $INSTANCE_ID \
            --document-name "AWS-RunShellScript" \
            --parameters "commands=[
                'cd /opt/$APP_NAME',
                'aws s3 cp s3://contoso-deployment-\$(aws sts get-caller-identity --query Account --output text)/$APP_NAME.zip .',
                'unzip -o $APP_NAME.zip',
                'rm $APP_NAME.zip',
                'systemctl restart $SERVICE_NAME',
                'sleep 5',
                'systemctl status $SERVICE_NAME'
            ]" \
            --region $REGION \
            --query "Command.CommandId" \
            --output text)
        
        echo "  Command sent: $COMMAND_ID"
        
        # Wait for command to complete
        sleep 5
        
        STATUS=$(aws ssm get-command-invocation \
            --command-id $COMMAND_ID \
            --instance-id $INSTANCE_ID \
            --region $REGION \
            --query "Status" \
            --output text 2>/dev/null || echo "Pending")
        
        echo "  Status: $STATUS"
    done
    
    echo "✅ $APP_NAME instances updated"
    echo ""
}

# Update both ASGs
update_asg "$CONTOSO_ASG" "contoso-api" "contoso-api"
update_asg "$NOTIFICATION_ASG" "notification-api" "notification-api"

echo "=========================================="
echo "✅ Update Complete"
echo "=========================================="
echo ""
echo "Check application logs:"
echo "  aws logs tail /ec2/contoso-api --follow"
echo "  aws logs tail /ec2/notification-api --follow"
echo ""
echo "Or SSH to instances using Session Manager:"
echo "  aws ssm start-session --target <instance-id>"
