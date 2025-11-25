using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SQS;
using Constructs;
using System.Collections.Generic;

namespace ContosoUniversityCdk.Constructs
{
    public class ComputeConstructEC2 : Construct
    {
        public IApplicationLoadBalancer LoadBalancer { get; }
        public IAutoScalingGroup ContosoApiAsg { get; }
        public IAutoScalingGroup NotificationApiAsg { get; }

        public ComputeConstructEC2(
            Construct scope,
            string id,
            IVpc vpc,
            ISecurityGroup albSecurityGroup,
            ISecurityGroup apiSecurityGroup,
            ISecret databaseSecret,
            string databaseEndpoint,
            IQueue notificationQueue,
            string cloudFrontUrl = "") : base(scope, id)
        {
            // Create Application Load Balancer in public subnets
            LoadBalancer = new ApplicationLoadBalancer(this, "ContosoAlb", new ApplicationLoadBalancerProps
            {
                Vpc = vpc,
                InternetFacing = true,
                LoadBalancerName = "contoso-alb",
                SecurityGroup = albSecurityGroup,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PUBLIC
                }
            });

            // Create IAM role for EC2 instances
            var ec2Role = new Role(this, "ContosoEc2Role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"),
                    ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy")
                }
            });

            // Grant permissions to read database secrets
            databaseSecret.GrantRead(ec2Role);

            // Grant SQS permissions for NotificationAPI
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

            // User data script for ContosoUniversity API
            var contosoUserData = UserData.ForLinux();
            contosoUserData.AddCommands(
                "#!/bin/bash",
                "set -e",
                "",
                "# Wait for any existing package manager operations to complete",
                "while sudo fuser /var/lib/rpm/.rpm.lock >/dev/null 2>&1; do",
                "  echo 'Waiting for other package managers to finish...'",
                "  sleep 5",
                "done",
                "",
                "# Update system (allow failure)",
                "dnf update -y || true",
                "",
                "# Clean DNF cache",
                "dnf clean all",
                "",
                "# Install .NET 8 Runtime with retry",
                "for i in 1 2 3; do",
                "  dnf install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0 && break || {",
                "    echo 'Installation failed, cleaning cache and retrying...'",
                "    dnf clean all",
                "    sleep 10",
                "  }",
                "done",
                "",
                "# Install CloudWatch agent",
                "wget https://s3.amazonaws.com/amazoncloudwatch-agent/amazon_linux/amd64/latest/amazon-cloudwatch-agent.rpm",
                "rpm -U ./amazon-cloudwatch-agent.rpm",
                "",
                "# Create application directory",
                "mkdir -p /opt/contoso-api",
                "cd /opt/contoso-api",
                "",
                "# Download application from S3 (will be uploaded separately)",
                $"aws s3 cp s3://contoso-deployment-{Stack.Of(this).Account}/contoso-api.zip . || echo 'Application not yet uploaded'",
                "if [ -f contoso-api.zip ]; then",
                "  unzip -o contoso-api.zip",
                "  rm contoso-api.zip",
                "fi",
                "",
                "# Get database credentials from Secrets Manager",
                $"DB_SECRET=$(aws secretsmanager get-secret-value --secret-id {databaseSecret.SecretArn} --region {Stack.Of(this).Region} --query SecretString --output text)",
                "DB_USERNAME=$(echo \"$DB_SECRET\" | jq -r .username)",
                "DB_PASSWORD=$(echo \"$DB_SECRET\" | jq -r .password)",
                "",
                "# Create systemd service with credentials",
                "cat > /etc/systemd/system/contoso-api.service <<EOF",
                "[Unit]",
                "Description=Contoso University API",
                "After=network.target",
                "",
                "[Service]",
                "Type=simple",
                "WorkingDirectory=/opt/contoso-api",
                "ExecStart=/usr/bin/dotnet /opt/contoso-api/ContosoUniversity.dll",
                "StandardOutput=journal",
                "StandardError=journal",
                "Restart=always",
                "RestartSec=10",
                "KillSignal=SIGINT",
                "SyslogIdentifier=contoso-api",
                "User=root",
                "Environment=ASPNETCORE_ENVIRONMENT=Production",
                "Environment=ASPNETCORE_URLS=http://0.0.0.0:80",
                $"Environment=DB_HOST={databaseEndpoint}",
                "Environment=DB_NAME=contoso",
                "Environment=DB_USERNAME=$DB_USERNAME",
                "Environment=DB_PASSWORD=$DB_PASSWORD",
                "Environment=AllowedHosts=*",
                $"Environment=NotificationAPI__BaseUrl=http://{LoadBalancer.LoadBalancerDnsName}",
                string.IsNullOrEmpty(cloudFrontUrl) ? "" : $"Environment=CORS__AllowedOrigins={cloudFrontUrl}",
                "",
                "[Install]",
                "WantedBy=multi-user.target",
                "EOF",
                "",
                "# Enable and start service",
                "systemctl daemon-reload",
                "systemctl enable contoso-api",
                "systemctl start contoso-api || echo 'Service will start when application is deployed'",
                "",
                "# Configure CloudWatch Logs",
                "cat > /opt/aws/amazon-cloudwatch-agent/etc/config.json << 'EOF'",
                "{",
                "  \"logs\": {",
                "    \"logs_collected\": {",
                "      \"files\": {",
                "        \"collect_list\": [",
                "          {",
                "            \"file_path\": \"/var/log/messages\",",
                "            \"log_group_name\": \"/ec2/contoso-api\",",
                "            \"log_stream_name\": \"{instance_id}\"",
                "          }",
                "        ]",
                "      }",
                "    }",
                "  }",
                "}",
                "EOF",
                "",
                "/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -a fetch-config -m ec2 -s -c file:/opt/aws/amazon-cloudwatch-agent/etc/config.json"
            );

            // User data script for NotificationAPI
            var notificationUserData = UserData.ForLinux();
            notificationUserData.AddCommands(
                "#!/bin/bash",
                "set -e",
                "",
                "# Wait for any existing package manager operations to complete",
                "while sudo fuser /var/lib/rpm/.rpm.lock >/dev/null 2>&1; do",
                "  echo 'Waiting for other package managers to finish...'",
                "  sleep 5",
                "done",
                "",
                "# Update system (allow failure)",
                "dnf update -y || true",
                "",
                "# Clean DNF cache",
                "dnf clean all",
                "",
                "# Install .NET 8 Runtime with retry",
                "for i in 1 2 3; do",
                "  dnf install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0 && break || {",
                "    echo 'Installation failed, cleaning cache and retrying...'",
                "    dnf clean all",
                "    sleep 10",
                "  }",
                "done",
                "",
                "# Install CloudWatch agent",
                "wget https://s3.amazonaws.com/amazoncloudwatch-agent/amazon_linux/amd64/latest/amazon-cloudwatch-agent.rpm",
                "rpm -U ./amazon-cloudwatch-agent.rpm",
                "",
                "# Create application directory",
                "mkdir -p /opt/notification-api",
                "cd /opt/notification-api",
                "",
                "# Download application from S3",
                $"aws s3 cp s3://contoso-deployment-{Stack.Of(this).Account}/notification-api.zip . || echo 'Application not yet uploaded'",
                "if [ -f notification-api.zip ]; then",
                "  unzip -o notification-api.zip",
                "  rm notification-api.zip",
                "fi",
                "",
                "# Create systemd service",
                "cat > /etc/systemd/system/notification-api.service << 'EOF'",
                "[Unit]",
                "Description=Notification API",
                "After=network.target",
                "",
                "[Service]",
                "Type=simple",
                "WorkingDirectory=/opt/notification-api",
                "ExecStart=/usr/bin/dotnet /opt/notification-api/NotificationAPI.dll",
                "StandardOutput=journal",
                "StandardError=journal",
                "Restart=always",
                "RestartSec=10",
                "KillSignal=SIGINT",
                "SyslogIdentifier=notification-api",
                "User=root",
                "Environment=ASPNETCORE_ENVIRONMENT=Production",
                "Environment=ASPNETCORE_URLS=http://0.0.0.0:80",
                $"Environment=AWS__Region={Stack.Of(this).Region}",
                $"Environment=AWS__SQS__QueueUrl={notificationQueue.QueueUrl}",
                "",
                "[Install]",
                "WantedBy=multi-user.target",
                "EOF",
                "",
                "# Enable and start service",
                "systemctl daemon-reload",
                "systemctl enable notification-api",
                "systemctl start notification-api || echo 'Service will start when application is deployed'",
                "",
                "# Configure CloudWatch Logs",
                "cat > /opt/aws/amazon-cloudwatch-agent/etc/config.json << 'EOF'",
                "{",
                "  \"logs\": {",
                "    \"logs_collected\": {",
                "      \"files\": {",
                "        \"collect_list\": [",
                "          {",
                "            \"file_path\": \"/var/log/messages\",",
                "            \"log_group_name\": \"/ec2/notification-api\",",
                "            \"log_stream_name\": \"{instance_id}\"",
                "          }",
                "        ]",
                "      }",
                "    }",
                "  }",
                "}",
                "EOF",
                "",
                "/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl -a fetch-config -m ec2 -s -c file:/opt/aws/amazon-cloudwatch-agent/etc/config.json"
            );

            // Create target groups first (before ASGs)
            var contosoApiTargetGroup = new ApplicationTargetGroup(this, "ContosoApiTargetGroup", new ApplicationTargetGroupProps
            {
                Vpc = vpc,
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.INSTANCE,
                TargetGroupName = "contoso-api-tg",
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/",
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    HealthyThresholdCount = 2,
                    UnhealthyThresholdCount = 3
                },
                DeregistrationDelay = Duration.Seconds(30)
            });

            var notificationApiTargetGroup = new ApplicationTargetGroup(this, "NotificationApiTargetGroup", new ApplicationTargetGroupProps
            {
                Vpc = vpc,
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                TargetType = TargetType.INSTANCE,
                TargetGroupName = "notification-api-tg",
                HealthCheck = new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
                {
                    Path = "/api/notifications",
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    HealthyThresholdCount = 2,
                    UnhealthyThresholdCount = 3
                },
                DeregistrationDelay = Duration.Seconds(30)
            });

            // Create Auto Scaling Group for ContosoUniversity API
            ContosoApiAsg = new AutoScalingGroup(this, "ContosoApiAsg", new AutoScalingGroupProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.SMALL),
                MachineImage = ami,
                MinCapacity = 1,
                MaxCapacity = 3,
                DesiredCapacity = 1,
                SecurityGroup = apiSecurityGroup,
                Role = ec2Role,
                UserData = contosoUserData,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                AssociatePublicIpAddress = false // Explicitly disable public IP assignment
            });

            // Create Auto Scaling Group for NotificationAPI
            NotificationApiAsg = new AutoScalingGroup(this, "NotificationApiAsg", new AutoScalingGroupProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO),
                MachineImage = ami,
                MinCapacity = 1,
                MaxCapacity = 2,
                DesiredCapacity = 1,
                SecurityGroup = apiSecurityGroup,
                Role = ec2Role,
                UserData = notificationUserData,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                AssociatePublicIpAddress = false // Explicitly disable public IP assignment
            });

            // Attach ASGs to target groups using L1 construct
            var cfnContosoAsg = ContosoApiAsg.Node.DefaultChild as Amazon.CDK.AWS.AutoScaling.CfnAutoScalingGroup;
            if (cfnContosoAsg != null)
            {
                cfnContosoAsg.TargetGroupArns = new[] { contosoApiTargetGroup.TargetGroupArn };
            }

            var cfnNotificationAsg = NotificationApiAsg.Node.DefaultChild as Amazon.CDK.AWS.AutoScaling.CfnAutoScalingGroup;
            if (cfnNotificationAsg != null)
            {
                cfnNotificationAsg.TargetGroupArns = new[] { notificationApiTargetGroup.TargetGroupArn };
            }

            // Create HTTP listener on port 80
            var httpListener = LoadBalancer.AddListener("HttpListener", new ApplicationListenerProps
            {
                Port = 80,
                Protocol = ApplicationProtocol.HTTP,
                DefaultAction = ListenerAction.Forward(new[] { contosoApiTargetGroup })
            });

            // Add path-based routing rules
            httpListener.AddTargetGroups("NotificationApiRule", new AddApplicationTargetGroupsProps
            {
                TargetGroups = new[] { notificationApiTargetGroup },
                Priority = 1,
                Conditions = new[]
                {
                    ListenerCondition.PathPatterns(new[] { "/api/notifications/*" })
                }
            });

            // Output the ALB DNS name
            new CfnOutput(this, "LoadBalancerDnsName", new CfnOutputProps
            {
                Value = LoadBalancer.LoadBalancerDnsName,
                Description = "Application Load Balancer DNS name"
            });

            new CfnOutput(this, "LoadBalancerUrl", new CfnOutputProps
            {
                Value = $"http://{LoadBalancer.LoadBalancerDnsName}",
                Description = "Application Load Balancer URL"
            });

            // Output deployment bucket name
            new CfnOutput(this, "DeploymentBucket", new CfnOutputProps
            {
                Value = $"contoso-deployment-{Stack.Of(this).Account}",
                Description = "S3 bucket for application deployments (create manually)"
            });
        }
    }
}
