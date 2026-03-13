using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace ContosoUniversityCdk
{
    public class ContosoUniversityStackTest : Stack
    {
        internal ContosoUniversityStackTest(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // Create VPC
            var vpc = new Vpc(this, "TestVpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 0
            });

            // Create SQS Queue
            var queue = new Queue(this, "TestQueue", new QueueProps
            {
                QueueName = "contoso-test-queue",
                VisibilityTimeout = Duration.Seconds(300)
            });

            // Security Group
            var securityGroup = new SecurityGroup(this, "TestSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,
                Description = "Security group for test instance",
                AllowAllOutbound = true
            });

            securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow HTTP");
            securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(8080), "Allow HTTP on 8080");

            // IAM Role
            var role = new Role(this, "TestEc2Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore")
                }
            });

            queue.GrantSendMessages(role);
            queue.GrantConsumeMessages(role);

            var deploymentBucketName = $"contoso-deployment-{this.Account}";
            role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "s3:GetObject", "s3:ListBucket" },
                Resources = new[]
                {
                    $"arn:aws:s3:::{deploymentBucketName}",
                    $"arn:aws:s3:::{deploymentBucketName}/*"
                }
            }));

            // User data
            var userData = UserData.ForLinux();
            userData.AddCommands(
                "#!/bin/bash",
                "set -e",
                "exec > >(tee /var/log/user-data.log|logger -t user-data -s 2>/dev/console) 2>&1",
                "",
                "echo 'Starting user data script...'",
                "",
                "# Update system",
                "dnf update -y || true",
                "",
                "# Install .NET 8",
                "echo 'Installing .NET 8...'",
                "dnf install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0",
                "",
                "# Create directory",
                "mkdir -p /opt/notification-api",
                "",
                "# Download from S3",
                "echo 'Downloading NotificationAPI...'",
                $"aws s3 cp s3://{deploymentBucketName}/notification-api.zip /opt/notification-api/ --no-verify-ssl || echo 'Not yet uploaded'",
                "",
                "# Extract",
                "cd /opt/notification-api && [ -f notification-api.zip ] && unzip -o notification-api.zip && rm notification-api.zip",
                "",
                "# Create systemd service",
                "cat > /etc/systemd/system/notification-api.service <<EOF",
                "[Unit]",
                "Description=Notification API Test",
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
                $"Environment=AWS__Region={this.Region}",
                $"Environment=AWS__SQS__QueueUrl={queue.QueueUrl}",
                "",
                "[Install]",
                "WantedBy=multi-user.target",
                "EOF",
                "",
                "# Start service",
                "systemctl daemon-reload",
                "systemctl enable notification-api",
                "systemctl start notification-api",
                "",
                "sleep 5",
                "systemctl status notification-api --no-pager || true",
                "",
                "echo 'Setup complete!'"
            );

            // EC2 Instance
            var instance = new Instance_(this, "TestInstance", new InstanceProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO),
                MachineImage = MachineImage.LatestAmazonLinux2023(),
                SecurityGroup = securityGroup,
                Role = role,
                UserData = userData,
                VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
                AssociatePublicIpAddress = true
            });

            // Outputs
            new CfnOutput(this, "InstancePublicIp", new CfnOutputProps
            {
                Value = instance.InstancePublicIp,
                Description = "Public IP of test instance"
            });

            new CfnOutput(this, "SwaggerUrl", new CfnOutputProps
            {
                Value = $"http://{instance.InstancePublicIp}:8080/swagger",
                Description = "Swagger UI URL"
            });

            new CfnOutput(this, "QueueUrl", new CfnOutputProps
            {
                Value = queue.QueueUrl,
                Description = "SQS Queue URL"
            });
        }
    }
}
