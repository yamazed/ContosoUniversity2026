using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace ContosoUniversityCdk.Constructs
{
    public class ComputeConstructSimple : Construct
    {
        public IInstance ContosoApiInstance { get; }
        public string PublicIp { get; }

        public ComputeConstructSimple(
            Construct scope,
            string id,
            IVpc vpc,
            ISecurityGroup apiSecurityGroup,
            ISecret databaseSecret,
            string databaseEndpoint,
            IQueue notificationQueue,
            string cloudFrontUrl = "") : base(scope, id)
        {
            // Create IAM role for EC2 instance
            var ec2Role = new Role(this, "ContosoEc2Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"),
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy")
                }
            });

            // Grant permissions
            databaseSecret.GrantRead(ec2Role);
            notificationQueue.GrantSendMessages(ec2Role);
            notificationQueue.GrantConsumeMessages(ec2Role);

            // Grant S3 read access to deployment bucket
            var deploymentBucketName = $"contoso-deployment-{Stack.Of(this).Account}";
            ec2Role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "s3:GetObject", "s3:ListBucket" },
                Resources = new[]
                {
                    $"arn:aws:s3:::{deploymentBucketName}",
                    $"arn:aws:s3:::{deploymentBucketName}/*"
                }
            }));

            // Get the latest Amazon Linux 2023 AMI
            var ami = MachineImage.LatestAmazonLinux2023(new AmazonLinux2023ImageSsmParameterProps
            {
                CpuType = AmazonLinuxCpuType.X86_64
            });

            // User data script
            var userData = UserData.ForLinux();
            userData.AddCommands(
                "#!/bin/bash",
                "set -e",
                "exec > >(tee /var/log/user-data.log|logger -t user-data -s 2>/dev/console) 2>&1",
                "",
                "echo 'Starting user data script...'",
                "",
                "# Wait for package manager",
                "while sudo fuser /var/lib/rpm/.rpm.lock >/dev/null 2>&1; do",
                "  echo 'Waiting for package manager...'",
                "  sleep 5",
                "done",
                "",
                "# Update system",
                "dnf update -y || true",
                "dnf clean all",
                "",
                "# Install .NET 8 Runtime",
                "echo 'Installing .NET 8...'",
                "dnf install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0",
                "",
                "# Install nginx and jq",
                "echo 'Installing nginx and jq...'",
                "dnf install -y nginx jq",
                "",
                "# Create application directories",
                "mkdir -p /opt/contoso-api",
                "mkdir -p /opt/notification-api",
                "mkdir -p /var/www/html",
                "",
                "# Download applications from S3",
                "echo 'Downloading applications...'",
                $"aws s3 cp s3://{deploymentBucketName}/contoso-api.zip /opt/contoso-api/ --no-verify-ssl || echo 'Contoso API not yet uploaded'",
                $"aws s3 cp s3://{deploymentBucketName}/notification-api.zip /opt/notification-api/ --no-verify-ssl || echo 'Notification API not yet uploaded'",
                $"aws s3 cp s3://{deploymentBucketName}/react-ui.zip /var/www/html/ --no-verify-ssl || echo 'React UI not yet uploaded'",
                "",
                "# Extract applications",
                "cd /opt/contoso-api && [ -f contoso-api.zip ] && unzip -o contoso-api.zip && rm contoso-api.zip",
                "cd /opt/notification-api && [ -f notification-api.zip ] && unzip -o notification-api.zip && rm notification-api.zip",
                "cd /var/www/html && [ -f react-ui.zip ] && unzip -o react-ui.zip && rm react-ui.zip",
                "",
                "# Get database credentials",
                "echo 'Getting database credentials...'",
                $"DB_SECRET=$(aws secretsmanager get-secret-value --secret-id {databaseSecret.SecretArn} --region {Stack.Of(this).Region} --query SecretString --output text --no-verify-ssl)",
                "DB_USERNAME=$(echo \"$DB_SECRET\" | jq -r .username)",
                "DB_PASSWORD=$(echo \"$DB_SECRET\" | jq -r .password)",
                "",
                "# Get instance public IP",
                "INSTANCE_IP=$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4)",
                "",
                "# Create Contoso API systemd service",
                "echo 'Creating Contoso API service...'",
                "cat > /etc/systemd/system/contoso-api.service <<EOF",
                "[Unit]",
                "Description=Contoso University API",
                "After=network.target",
                "",
                "[Service]",
                "Type=simple",
                "WorkingDirectory=/opt/contoso-api",
                "ExecStart=/usr/bin/dotnet /opt/contoso-api/ContosoUniversity.dll",
                "Restart=always",
                "RestartSec=10",
                "User=root",
                "Environment=ASPNETCORE_ENVIRONMENT=Production",
                "Environment=ASPNETCORE_URLS=http://0.0.0.0:5000",
                $"Environment=DB_HOST={databaseEndpoint}",
                "Environment=DB_NAME=contoso",
                "Environment=DB_USERNAME=$DB_USERNAME",
                "Environment=DB_PASSWORD=$DB_PASSWORD",
                "Environment=AllowedHosts=*",
                "Environment=NotificationAPI__BaseUrl=http://$INSTANCE_IP:8080",
                "",
                "[Install]",
                "WantedBy=multi-user.target",
                "EOF",
                "",
                "# Create Notification API systemd service",
                "echo 'Creating Notification API service...'",
                "cat > /etc/systemd/system/notification-api.service <<EOF",
                "[Unit]",
                "Description=Notification API",
                "After=network.target",
                "",
                "[Service]",
                "Type=simple",
                "WorkingDirectory=/opt/notification-api",
                "ExecStart=/usr/bin/dotnet /opt/notification-api/NotificationAPI.dll",
                "Restart=always",
                "RestartSec=10",
                "User=root",
                "Environment=ASPNETCORE_ENVIRONMENT=Production",
                "Environment=ASPNETCORE_URLS=http://0.0.0.0:8080",
                $"Environment=DB_HOST={databaseEndpoint}",
                "Environment=DB_NAME=contoso",
                "Environment=DB_USERNAME=$DB_USERNAME",
                "Environment=DB_PASSWORD=$DB_PASSWORD",
                $"Environment=AWS_REGION={Stack.Of(this).Region}",
                $"Environment=SQS_QUEUE_URL={notificationQueue.QueueUrl}",
                "",
                "[Install]",
                "WantedBy=multi-user.target",
                "EOF",
                "",
                "# Configure nginx",
                "echo 'Configuring nginx...'",
                "cat > /etc/nginx/conf.d/contoso.conf <<'NGINXEOF'",
                "server {",
                "    listen 80;",
                "    server_name _;",
                "    root /var/www/html;",
                "    index index.html;",
                "",
                "    # Serve React app",
                "    location / {",
                "        try_files $uri $uri/ /index.html;",
                "    }",
                "",
                "    # Proxy API requests to Contoso API",
                "    location /api/ {",
                "        proxy_pass http://localhost:5000/api/;",
                "        proxy_http_version 1.1;",
                "        proxy_set_header Upgrade $http_upgrade;",
                "        proxy_set_header Connection keep-alive;",
                "        proxy_set_header Host $host;",
                "        proxy_cache_bypass $http_upgrade;",
                "        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;",
                "        proxy_set_header X-Forwarded-Proto $scheme;",
                "    }",
                "}",
                "NGINXEOF",
                "",
                "# Enable and start services",
                "echo 'Starting services...'",
                "systemctl daemon-reload",
                "systemctl enable contoso-api notification-api nginx",
                "systemctl start contoso-api notification-api nginx",
                "",
                "# Wait and check status",
                "sleep 10",
                "systemctl status contoso-api --no-pager || true",
                "systemctl status notification-api --no-pager || true",
                "systemctl status nginx --no-pager || true",
                "",
                "echo 'User data script completed!'"
            );

            // Create single EC2 instance in public subnet
            ContosoApiInstance = new Instance_(this, "ContosoInstance", new InstanceProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.SMALL),
                MachineImage = ami,
                SecurityGroup = apiSecurityGroup,
                Role = ec2Role,
                UserData = userData,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                },
                AssociatePublicIpAddress = true
            });

            // Output the public IP
            PublicIp = ContosoApiInstance.InstancePublicIp;

            new CfnOutput(this, "InstancePublicIp", new CfnOutputProps
            {
                Value = PublicIp,
                Description = "Public IP of the EC2 instance"
            });

            new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Value = $"http://{PublicIp}",
                Description = "Contoso University API URL"
            });

            new CfnOutput(this, "NotificationApiUrl", new CfnOutputProps
            {
                Value = $"http://{PublicIp}:8080",
                Description = "Notification API URL"
            });
        }
    }
}
