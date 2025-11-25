# EC2 Migration Summary

## What Changed

Your CDK project has been migrated from **ECS/Fargate containers** to **EC2 instances** to eliminate the Docker requirement.

## Key Changes

### 1. New Compute Construct

- **Old**: `ComputeConstruct.cs` (ECS/Fargate with Docker)
- **New**: `ComputeConstructEC2.cs` (EC2 Auto Scaling Groups)

### 2. Deployment Method

**Before (ECS/Fargate)**:
- Required Docker Desktop installed locally
- Built Docker images during `cdk deploy`
- Pushed images to Amazon ECR
- Deployed containers to ECS Fargate

**After (EC2)**:
- No Docker required!
- Publish .NET apps with `dotnet publish`
- Upload ZIP files to S3
- EC2 instances download and run apps directly

### 3. Application Runtime

**Before**:
- Applications ran in Docker containers
- Managed by ECS/Fargate

**After**:
- Applications run directly on Amazon Linux 2023
- .NET 8 runtime installed on instances
- Managed by systemd services

## New Files Created

### CDK Infrastructure
- `Constructs/ComputeConstructEC2.cs` - EC2-based compute infrastructure

### Deployment Scripts
- `deploy-ec2.sh` - Complete deployment script
- `scripts/publish-apps.sh` - Publish .NET apps to S3
- `scripts/update-instances.sh` - Update running instances

### Documentation
- `EC2_DEPLOYMENT.md` - Complete EC2 deployment guide
- `EC2_MIGRATION_SUMMARY.md` - This file

## How to Deploy

### Quick Start

```bash
cd ContosoUniversityCdk
./deploy-ec2.sh
```

### Manual Steps

```bash
# 1. Bootstrap CDK
cdk bootstrap

# 2. Build React UI
cd ../contoso-university-ui && npm install && npm run build && cd ../ContosoUniversityCdk

# 3. Publish .NET apps
./scripts/publish-apps.sh

# 4. Deploy infrastructure
cdk deploy
```

## Architecture Comparison

### Before (ECS/Fargate)
```
ALB → ECS Service → Fargate Tasks → Docker Containers
```

### After (EC2)
```
ALB → Target Group → Auto Scaling Group → EC2 Instances → .NET Apps
```

## Cost Comparison

### ECS/Fargate (Before)
- Fargate vCPU: ~$0.04048/hour
- Fargate Memory: ~$0.004445/GB/hour
- **Total**: ~$30-40/month for 2 services

### EC2 (After)
- t3.small (Contoso API): ~$15/month
- t3.micro (Notification API): ~$7.50/month
- **Total**: ~$22.50/month

**Savings**: ~$10-17/month (30-40% cheaper)

## Benefits of EC2 Approach

✅ **No Docker Required** - Deploy from any machine  
✅ **Lower Cost** - EC2 is cheaper than Fargate  
✅ **Simpler Deployment** - Just publish and upload  
✅ **Direct Access** - SSH via Session Manager  
✅ **Familiar Tools** - systemd, journalctl, standard Linux  

## Trade-offs

⚠️ **Slower Scaling** - 2-3 minutes vs 30-60 seconds  
⚠️ **Manual Updates** - Need to run update script  
⚠️ **OS Management** - Need to patch Amazon Linux  

## Instance Details

### Contoso API Instances
- **Type**: t3.small (2 vCPU, 2 GB RAM)
- **Min/Max**: 1-3 instances
- **Location**: Private subnets
- **App Path**: `/opt/contoso-api/`
- **Service**: `contoso-api.service`
- **Port**: 80

### Notification API Instances
- **Type**: t3.micro (2 vCPU, 1 GB RAM)
- **Min/Max**: 1-2 instances
- **Location**: Private subnets
- **App Path**: `/opt/notification-api/`
- **Service**: `notification-api.service`
- **Port**: 80

## Application Updates

### Update Process

1. **Publish new version**:
   ```bash
   ./scripts/publish-apps.sh
   ```

2. **Update running instances**:
   ```bash
   ./scripts/update-instances.sh
   ```

The update script uses AWS Systems Manager to:
- Download new application from S3
- Extract files
- Restart systemd service
- Verify service is running

### Zero-Downtime Updates

For production, use rolling updates:

```bash
# Update one instance at a time
aws autoscaling start-instance-refresh \
  --auto-scaling-group-name <asg-name> \
  --preferences MinHealthyPercentage=50
```

## Monitoring

### CloudWatch Logs

```bash
# View Contoso API logs
aws logs tail /ec2/contoso-api --follow

# View Notification API logs
aws logs tail /ec2/notification-api --follow
```

### SSH Access

```bash
# List instances
aws ec2 describe-instances \
  --filters "Name=tag:aws:autoscaling:groupName,Values=*contoso*" \
  --query "Reservations[*].Instances[*].[InstanceId,State.Name]" \
  --output table

# Connect via Session Manager
aws ssm start-session --target <instance-id>

# Check service status
sudo systemctl status contoso-api
sudo journalctl -u contoso-api -f
```

## Troubleshooting

### Application Not Starting

```bash
# Connect to instance
aws ssm start-session --target <instance-id>

# Check user data execution
sudo cat /var/log/cloud-init-output.log

# Check if .NET is installed
dotnet --version

# Check service status
sudo systemctl status contoso-api
sudo journalctl -u contoso-api -n 50
```

### Health Check Failures

```bash
# Check target health
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn>

# Common issues:
# - App not listening on port 80
# - Security group blocking traffic
# - App crashed during startup
```

## Rollback

If you need to go back to ECS/Fargate:

1. The old `ComputeConstruct.cs` file still exists
2. Update `ContosoUniversityStack.cs` to use `ComputeConstruct` instead of `ComputeConstructEC2`
3. Install Docker Desktop
4. Run `cdk deploy`

## Next Steps

1. **Deploy**: Run `./deploy-ec2.sh`
2. **Test**: Access application via CloudFront URL
3. **Monitor**: Check CloudWatch logs
4. **Optimize**: Adjust instance types/counts as needed
5. **Automate**: Set up CI/CD pipeline

## Questions?

See the full documentation:
- [EC2_DEPLOYMENT.md](./EC2_DEPLOYMENT.md) - Complete deployment guide
- [DEPLOYMENT.md](./DEPLOYMENT.md) - General deployment info
- [README.md](./README.md) - Project overview
