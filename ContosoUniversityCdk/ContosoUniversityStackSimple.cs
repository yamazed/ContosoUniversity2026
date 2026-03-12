using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using ContosoUniversityCdk.Constructs;
using Constructs;

namespace ContosoUniversityCdk
{
    public class ContosoUniversityStackSimple : Stack
    {
        internal ContosoUniversityStackSimple(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // Networking
            var networking = new NetworkingConstruct(this, "Networking");

            // Security Groups
            var apiSecurityGroup = new SecurityGroup(this, "ApiSecurityGroup", new SecurityGroupProps
            {
                Vpc = networking.Vpc,
                Description = "Security group for API instances",
                AllowAllOutbound = true
            });

            // Allow HTTP from anywhere (for demo purposes)
            apiSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow HTTP from anywhere");
            apiSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(8080), "Allow HTTP on 8080 from anywhere");

            // Database
            var database = new DatabaseConstruct(this, "Database", networking.Vpc, apiSecurityGroup, 0.5, 2.0);

            // Messaging
            var messaging = new MessagingConstruct(this, "Messaging");

            // Compute (Simple - single EC2 instance)
            var compute = new ComputeConstructSimple(
                this,
                "Compute",
                networking.Vpc,
                apiSecurityGroup,
                database.DatabaseSecret,
                database.Cluster.ClusterEndpoint.Hostname,
                messaging.NotificationQueue
            );

            // Frontend
            var frontend = new FrontendConstruct(this, "Frontend");

            // Outputs
            new CfnOutput(this, "VpcId", new CfnOutputProps
            {
                Value = networking.Vpc.VpcId,
                Description = "VPC ID"
            });

            new CfnOutput(this, "DatabaseEndpoint", new CfnOutputProps
            {
                Value = database.Cluster.ClusterEndpoint.Hostname,
                Description = "Database cluster endpoint"
            });

            new CfnOutput(this, "DatabaseSecretArn", new CfnOutputProps
            {
                Value = database.DatabaseSecret.SecretArn,
                Description = "Database credentials secret ARN"
            });

            new CfnOutput(this, "SqsQueueUrl", new CfnOutputProps
            {
                Value = messaging.NotificationQueue.QueueUrl,
                Description = "SQS Queue URL for notifications"
            });

            new CfnOutput(this, "SqsQueueArn", new CfnOutputProps
            {
                Value = messaging.NotificationQueue.QueueArn,
                Description = "SQS Queue ARN"
            });

            new CfnOutput(this, "FrontendBucketName", new CfnOutputProps
            {
                Value = frontend.WebsiteBucket.BucketName,
                Description = "S3 bucket name for frontend"
            });

            new CfnOutput(this, "CloudFrontDistributionId", new CfnOutputProps
            {
                Value = frontend.CloudFrontDistribution.DistributionId,
                Description = "CloudFront distribution ID"
            });

            new CfnOutput(this, "CloudFrontDistributionUrl", new CfnOutputProps
            {
                Value = $"https://{frontend.CloudFrontDistribution.DistributionDomainName}",
                Description = "CloudFront URL for the application"
            });

            new CfnOutput(this, "ComputeDeploymentBucket", new CfnOutputProps
            {
                Value = $"contoso-deployment-{this.Account}",
                Description = "S3 bucket for application deployments"
            });
        }
    }
}
