# Deployment Options

This document explains the different ways to deploy the Contoso University CDK stack.

## Option 1: Deploy Without Docker (Recommended) ⭐

**Best for**: Developers without Docker installed, CI/CD pipelines, cloud-native workflows

### Quick Start

```bash
./deploy-without-docker.sh
```

### How It Works

- Docker images are built in **AWS CodeBuild** (cloud-based)
- No local Docker installation required
- Slightly longer build time (10-15 minutes first time)
- Small additional cost (~$0.03-$0.05 per deployment)

### Documentation

- [QUICK_START_NO_DOCKER.md](./QUICK_START_NO_DOCKER.md) - Quick reference
- [NO_DOCKER_DEPLOYMENT.md](./NO_DOCKER_DEPLOYMENT.md) - Detailed guide

### Pros

✅ No Docker Desktop required  
✅ No local disk space used for images  
✅ Consistent build environment  
✅ Works on any machine with AWS CLI  
✅ Ideal for CI/CD pipelines  

### Cons

⚠️ Longer build time (cloud vs local)  
⚠️ Small cost for CodeBuild usage  
⚠️ Requires internet connection  

---

## Option 2: Deploy With Local Docker

**Best for**: Developers with Docker installed, faster iteration cycles

### Quick Start

```bash
./scripts/deploy.sh
```

### How It Works

- Docker images are built **locally** on your machine
- Requires Docker Desktop installed
- Faster build time (1-2 minutes)
- No additional costs

### Prerequisites

- Docker Desktop installed and running
- Sufficient disk space for images (~2-3 GB)

### Pros

✅ Faster build time  
✅ No additional costs  
✅ Works offline (after first deployment)  
✅ Better for rapid development  

### Cons

⚠️ Requires Docker Desktop  
⚠️ Uses local disk space  
⚠️ Platform-specific (Mac/Windows/Linux)  

---

## Option 3: Pre-built Images (Advanced)

**Best for**: Production deployments, multi-stage pipelines, custom build processes

### How It Works

1. Build images in your CI/CD pipeline (GitHub Actions, GitLab CI, etc.)
2. Push images to Amazon ECR
3. Modify CDK to reference ECR image URIs instead of building from source

### Example Workflow

```yaml
# .github/workflows/deploy.yml
- name: Build and push Docker image
  run: |
    docker build -t $ECR_REGISTRY/contoso-api:$TAG .
    docker push $ECR_REGISTRY/contoso-api:$TAG
```

Then modify `ComputeConstruct.cs`:

```csharp
// Instead of:
var contosoImage = ContainerImage.FromAsset("../ContosoUniversity");

// Use:
var contosoImage = ContainerImage.FromEcrRepository(
    Repository.FromRepositoryName(this, "ContosoRepo", "contoso-api"),
    "latest"
);
```

### Pros

✅ Full control over build process  
✅ Can use custom build tools  
✅ Separate build and deploy stages  
✅ Better for production  

### Cons

⚠️ More complex setup  
⚠️ Requires CI/CD infrastructure  
⚠️ Manual image management  

---

## Comparison Table

| Feature | No Docker (CodeBuild) | Local Docker | Pre-built Images |
|---------|----------------------|--------------|------------------|
| **Setup Complexity** | Low | Medium | High |
| **Build Time** | 10-15 min | 1-2 min | Varies |
| **Cost** | ~$0.03-$0.05 | Free | Varies |
| **Docker Required** | ❌ No | ✅ Yes | ✅ Yes (CI/CD) |
| **Disk Space** | None | 2-3 GB | None (local) |
| **Best For** | Quick start, no Docker | Development | Production |

---

## Recommended Approach by Use Case

### 🎓 Learning / Evaluation
→ **Use Option 1** (No Docker)  
Fastest way to get started without installing Docker.

### 💻 Active Development
→ **Use Option 2** (Local Docker)  
Faster iteration cycles for development.

### 🏢 Production Deployment
→ **Use Option 3** (Pre-built Images)  
Better control and separation of concerns.

### 🤖 CI/CD Pipeline
→ **Use Option 1** (No Docker) or **Option 3** (Pre-built)  
Depends on your pipeline architecture.

---

## Switching Between Options

You can switch between options at any time:

### From Local Docker → No Docker
Just stop using Docker and run:
```bash
./deploy-without-docker.sh
```

### From No Docker → Local Docker
Install Docker Desktop and run:
```bash
./scripts/deploy.sh
```

### To Pre-built Images
Modify `ComputeConstruct.cs` to reference ECR repositories instead of building from assets.

---

## Cost Breakdown

### Option 1: No Docker (CodeBuild)
- **Per deployment**: ~$0.03-$0.05
- **Monthly** (10 deployments): ~$0.30-$0.50
- **Annual** (120 deployments): ~$3.60-$6.00

### Option 2: Local Docker
- **Per deployment**: $0.00
- **Monthly**: $0.00
- **Annual**: $0.00

### Option 3: Pre-built Images
- **Depends on CI/CD platform**
- GitHub Actions: Free for public repos, $0.008/min for private
- GitLab CI: Free tier available
- AWS CodePipeline: ~$1/pipeline/month

---

## Troubleshooting

### "Docker is required" error
→ You're using Option 2 but Docker isn't installed. Switch to Option 1.

### "Build takes too long"
→ First build always takes longer. Subsequent builds are faster due to caching.

### "Access Denied" during CodeBuild
→ Check IAM permissions. You need CodeBuild and ECR access.

### "Image not found" in ECS
→ Check ECR repositories. Images should be pushed successfully.

---

## Next Steps

1. Choose your deployment option
2. Follow the corresponding guide
3. Deploy your application
4. Monitor CloudWatch logs
5. Access your application via CloudFront URL

For detailed instructions, see:
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Complete deployment guide
- [NO_DOCKER_DEPLOYMENT.md](./NO_DOCKER_DEPLOYMENT.md) - No Docker guide
- [QUICK_START_NO_DOCKER.md](./QUICK_START_NO_DOCKER.md) - Quick reference
