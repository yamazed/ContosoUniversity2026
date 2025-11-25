using Amazon.CDK;
using Constructs;

namespace ContosoUniversityCdk
{
    public class ContosoUniversityStackProps : StackProps
    {
        public string EnvironmentName { get; set; } = "dev";
        public double DatabaseMinCapacity { get; set; } = 0.5;
        public double DatabaseMaxCapacity { get; set; } = 2.0;
    }

    public class ContosoUniversityStack : Stack
    {
        internal ContosoUniversityStack(Construct scope, string id, ContosoUniversityStackProps props) : base(scope, id, props)
        {
            // Tag all resources in this stack
            Amazon.CDK.Tags.Of(this).Add("Project", "ContosoUniversity");
            Amazon.CDK.Tags.Of(this).Add("Environment", props.EnvironmentName);
            Amazon.CDK.Tags.Of(this).Add("ManagedBy", "CDK");

            // Infrastructure components will be instantiated here in subsequent tasks
            // Following the order: Networking -> Database -> Messaging -> Compute -> Frontend

            // 1. Networking infrastructure
            var networking = new Constructs.NetworkingConstruct(this, "Networking");

            // 2. Database infrastructure
            var database = new Constructs.DatabaseConstruct(
                this,
                "Database",
                networking.Vpc,
                networking.DatabaseSecurityGroup,
                props.DatabaseMinCapacity,
                props.DatabaseMaxCapacity
            );

            // 3. Messaging infrastructure
            var messaging = new Constructs.MessagingConstruct(this, "Messaging");

            // 5. Frontend infrastructure (S3, CloudFront)
            // Create frontend first to get CloudFront URL for CORS configuration
            var frontend = new Constructs.FrontendConstruct(this, "Frontend");

            // 4. Compute infrastructure (EC2, Auto Scaling, ALB)
            // Pass CloudFront URL for CORS configuration
            var cloudFrontUrl = $"https://{frontend.CloudFrontDistribution.DistributionDomainName}";
            var compute = new Constructs.ComputeConstructEC2(
                this,
                "Compute",
                networking.Vpc,
                networking.AlbSecurityGroup,
                networking.ApiSecurityGroup,
                database.DatabaseSecret,
                database.Cluster.ClusterEndpoint.Hostname,
                messaging.NotificationQueue,
                cloudFrontUrl
            );

            // Stack Outputs - Consolidated outputs for easy access to key endpoints
            // These outputs will be displayed after deployment and can be used by other stacks

            // ALB DNS name - Primary endpoint for API access
            new CfnOutput(this, "ApplicationLoadBalancerDnsName", new CfnOutputProps
            {
                Value = compute.LoadBalancer.LoadBalancerDnsName,
                Description = "Application Load Balancer DNS name for API access",
                ExportName = $"{props.EnvironmentName}-alb-dns"
            });

            // ALB URL - Full HTTP URL for API access
            new CfnOutput(this, "ApplicationLoadBalancerUrl", new CfnOutputProps
            {
                Value = $"http://{compute.LoadBalancer.LoadBalancerDnsName}",
                Description = "Application Load Balancer URL (HTTP)",
                ExportName = $"{props.EnvironmentName}-alb-url"
            });

            // CloudFront Distribution URL - Primary endpoint for frontend access
            new CfnOutput(this, "CloudFrontDistributionUrl", new CfnOutputProps
            {
                Value = $"https://{frontend.CloudFrontDistribution.DistributionDomainName}",
                Description = "CloudFront distribution URL for React UI (HTTPS)",
                ExportName = $"{props.EnvironmentName}-cloudfront-url"
            });

            // CloudFront Distribution ID - For cache invalidation
            new CfnOutput(this, "CloudFrontDistributionId", new CfnOutputProps
            {
                Value = frontend.CloudFrontDistribution.DistributionId,
                Description = "CloudFront distribution ID for cache invalidation",
                ExportName = $"{props.EnvironmentName}-cloudfront-id"
            });

            // Database Endpoint - For direct database access if needed
            new CfnOutput(this, "DatabaseClusterEndpoint", new CfnOutputProps
            {
                Value = database.Cluster.ClusterEndpoint.Hostname,
                Description = "Aurora PostgreSQL cluster endpoint",
                ExportName = $"{props.EnvironmentName}-db-endpoint"
            });

            // Database Secret ARN - For retrieving credentials
            new CfnOutput(this, "DatabaseSecretArn", new CfnOutputProps
            {
                Value = database.DatabaseSecret.SecretArn,
                Description = "ARN of the database credentials secret in Secrets Manager",
                ExportName = $"{props.EnvironmentName}-db-secret-arn"
            });

            // SQS Queue URL - For notification processing
            new CfnOutput(this, "SqsQueueUrl", new CfnOutputProps
            {
                Value = messaging.NotificationQueue.QueueUrl,
                Description = "SQS queue URL for notifications",
                ExportName = $"{props.EnvironmentName}-sqs-url"
            });

            // SQS Queue ARN - For IAM permissions
            new CfnOutput(this, "SqsQueueArn", new CfnOutputProps
            {
                Value = messaging.NotificationQueue.QueueArn,
                Description = "SQS queue ARN for notifications",
                ExportName = $"{props.EnvironmentName}-sqs-arn"
            });

            // S3 Bucket Name - For frontend asset management
            new CfnOutput(this, "FrontendBucketName", new CfnOutputProps
            {
                Value = frontend.WebsiteBucket.BucketName,
                Description = "S3 bucket name for frontend assets",
                ExportName = $"{props.EnvironmentName}-frontend-bucket"
            });

            // Auto Scaling Group Names - For service management
            new CfnOutput(this, "ContosoApiAsgName", new CfnOutputProps
            {
                Value = compute.ContosoApiAsg.AutoScalingGroupName,
                Description = "Auto Scaling Group name for Contoso API",
                ExportName = $"{props.EnvironmentName}-contoso-asg"
            });

            new CfnOutput(this, "NotificationApiAsgName", new CfnOutputProps
            {
                Value = compute.NotificationApiAsg.AutoScalingGroupName,
                Description = "Auto Scaling Group name for Notification API",
                ExportName = $"{props.EnvironmentName}-notification-asg"
            });

            // VPC ID - For network integration
            new CfnOutput(this, "VpcId", new CfnOutputProps
            {
                Value = networking.Vpc.VpcId,
                Description = "VPC ID for network integration",
                ExportName = $"{props.EnvironmentName}-vpc-id"
            });
        }
    }
}
