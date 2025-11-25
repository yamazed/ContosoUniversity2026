# Quick Start: Deploy Without Docker

## ⚠️ Important Update

**CDK v2 requires Docker to be installed locally** to build container images using `ContainerImage.FromAsset()`.

While CDK has cloud-based building capabilities, they are not automatically used during `cdk deploy` without local Docker.

## Your Options

### Option 1: Install Docker Desktop (Recommended)

**Easiest solution**: Install Docker Desktop and deploy normally.

```bash
# Install Docker Desktop from:
# https://www.docker.com/products/docker-desktop/

# Then deploy:
cd ContosoUniversityCdk
cdk deploy
```

### Option 2: Modify CDK Stack to Use Pre-built Images

Requires code changes to reference ECR images instead of building from source.

See [DEPLOYMENT_OPTIONS.md](./DEPLOYMENT_OPTIONS.md) for details.

### Option 3: Use CI/CD Pipeline

Deploy from GitHub Actions, GitLab CI, or AWS CodePipeline where Docker is available.

## Why Docker is Required

CDK's `ContainerImage.FromAsset()` builds Docker images locally during synthesis.
While CDK supports cloud-based building, it's not automatically enabled for `cdk deploy`.

## How Long Does It Take?

- **First deployment**: 15-20 minutes (includes building Docker images in the cloud)
- **Subsequent deployments**: 5-10 minutes (only rebuilds if code changed)

## Where Are Images Built?

Docker images are built in **AWS CodeBuild**, not on your local machine. This means:

- ✅ No Docker Desktop required
- ✅ No local disk space used
- ✅ Consistent build environment
- ⚠️ Slightly longer build time (cloud vs local)
- 💰 Small cost (~$0.03-$0.05 per deployment)

## After Deployment

Check the outputs for:
- **CloudFront URL**: Your React UI
- **ALB URL**: Your API endpoints
- **Database Endpoint**: PostgreSQL connection

## Troubleshooting

### "AWS CLI not configured"
```bash
aws configure
```

### "CDK not bootstrapped"
```bash
cdk bootstrap
```

### "No default region"
```bash
export CDK_DEFAULT_REGION=us-east-1
```

## More Details

See [NO_DOCKER_DEPLOYMENT.md](./NO_DOCKER_DEPLOYMENT.md) for comprehensive documentation.

## Manual Commands

If you prefer manual control:

```bash
# Set environment
export CDK_DEFAULT_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
export CDK_DEFAULT_REGION=$(aws configure get region)

# Bootstrap (first time only)
cdk bootstrap

# Build UI
cd ../contoso-university-ui && npm install && npm run build && cd ../ContosoUniversityCdk

# Deploy
cdk deploy
```

## Cost Estimate

**Monthly cost** (for dev environment with minimal usage):
- Aurora Serverless v2: ~$40-60/month
- ECS Fargate: ~$30-40/month
- ALB: ~$20/month
- CloudFront: ~$1-5/month
- Other services: ~$5-10/month

**Total**: ~$100-135/month

**Per deployment cost**: ~$0.03-$0.05 (CodeBuild charges)

## Cleanup

To remove everything:

```bash
cdk destroy
```

This deletes all resources and stops all charges.
