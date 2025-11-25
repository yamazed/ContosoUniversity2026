using Amazon.CDK.AWS.EC2;
using Constructs;

namespace ContosoUniversityCdk.Constructs
{
    public class NetworkingConstruct : Construct
    {
        public IVpc Vpc { get; }
        public ISecurityGroup AlbSecurityGroup { get; }
        public ISecurityGroup ApiSecurityGroup { get; }
        public ISecurityGroup DatabaseSecurityGroup { get; }

        public NetworkingConstruct(Construct scope, string id) : base(scope, id)
        {
            // Create VPC with public and private subnets across 2 AZs
            // NAT Gateways are automatically configured for private subnet internet access
            // Note: Using 1 NAT Gateway to reduce costs (~$32/month savings)
            // For production, consider using 2 NAT Gateways for high availability
            Vpc = new Vpc(this, "ContosoVpc", new VpcProps
            {
                MaxAzs = 2,
                NatGateways = 1, // Reduced to 1 for cost savings (use 2 for HA in production)
                SubnetConfiguration = new[]
                {
                    new SubnetConfiguration
                    {
                        Name = "Public",
                        SubnetType = SubnetType.PUBLIC,
                        CidrMask = 24
                    },
                    new SubnetConfiguration
                    {
                        Name = "Private",
                        SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                        CidrMask = 24
                    }
                }
            });

            // Create security group for Application Load Balancer
            // Allows inbound HTTP (80) and HTTPS (443) from internet
            AlbSecurityGroup = new SecurityGroup(this, "AlbSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for Application Load Balancer",
                AllowAllOutbound = true
            });

            AlbSecurityGroup.AddIngressRule(
                Peer.AnyIpv4(),
                Port.Tcp(80),
                "Allow HTTP traffic from internet"
            );

            AlbSecurityGroup.AddIngressRule(
                Peer.AnyIpv4(),
                Port.Tcp(443),
                "Allow HTTPS traffic from internet"
            );

            // Create security group for API services (ECS tasks)
            // Allows inbound traffic from ALB only
            ApiSecurityGroup = new SecurityGroup(this, "ApiSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for API services (ECS Fargate tasks)",
                AllowAllOutbound = true
            });

            ApiSecurityGroup.AddIngressRule(
                AlbSecurityGroup,
                Port.Tcp(80),
                "Allow traffic from ALB"
            );

            // Create security group for database
            // Allows inbound PostgreSQL (5432) from API security group only
            DatabaseSecurityGroup = new SecurityGroup(this, "DatabaseSecurityGroup", new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "Security group for Aurora PostgreSQL database",
                AllowAllOutbound = false
            });

            DatabaseSecurityGroup.AddIngressRule(
                ApiSecurityGroup,
                Port.Tcp(5432),
                "Allow PostgreSQL traffic from API services"
            );
        }
    }
}
