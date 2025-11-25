using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;

namespace ContosoUniversityCdk.Constructs
{
    public class DatabaseConstruct : Construct
    {
        public IDatabaseCluster Cluster { get; }
        public ISecret DatabaseSecret { get; }
        public string ConnectionStringSecretArn { get; }

        public DatabaseConstruct(
            Construct scope,
            string id,
            IVpc vpc,
            ISecurityGroup securityGroup,
            double minCapacity,
            double maxCapacity) : base(scope, id)
        {
            // Create Secrets Manager secret for database credentials
            // This will auto-generate username and password
            DatabaseSecret = new Secret(this, "DbSecret", new SecretProps
            {
                Description = "Aurora PostgreSQL credentials",
                GenerateSecretString = new SecretStringGenerator
                {
                    SecretStringTemplate = "{\"username\":\"contosoAdmin\"}",
                    GenerateStringKey = "password",
                    ExcludePunctuation = true, // Only letters and numbers, no special characters
                    PasswordLength = 32
                }
            });

            // Create Aurora Serverless v2 PostgreSQL cluster
            Cluster = new DatabaseCluster(this, "AuroraCluster", new DatabaseClusterProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_17_4
                }),
                Credentials = Credentials.FromSecret(DatabaseSecret),
                DefaultDatabaseName = "contoso",
                Writer = ClusterInstance.ServerlessV2("Writer", new ServerlessV2ClusterInstanceProps
                {
                    PubliclyAccessible = false
                }),
                ServerlessV2MinCapacity = minCapacity,
                ServerlessV2MaxCapacity = maxCapacity,
                Vpc = vpc,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                SecurityGroups = new[] { securityGroup },
                Backup = new BackupProps
                {
                    Retention = Duration.Days(7)
                },
                RemovalPolicy = RemovalPolicy.SNAPSHOT // Create snapshot on deletion for safety
            });

            // Configure automatic secret rotation (30 days)
            // This uses AWS managed rotation for RDS single-user credentials
            DatabaseSecret.AddRotationSchedule("Rotation", new RotationScheduleOptions
            {
                AutomaticallyAfter = Duration.Days(30),
                HostedRotation = HostedRotation.PostgreSqlSingleUser()
            });

            // Store the database secret ARN for use by ECS tasks
            // The application will construct the connection string at runtime
            // using the cluster endpoint and credentials from the secret
            ConnectionStringSecretArn = DatabaseSecret.SecretArn;

            // Output the cluster endpoint for reference
            new CfnOutput(this, "DatabaseEndpoint", new CfnOutputProps
            {
                Value = Cluster.ClusterEndpoint.Hostname,
                Description = "Aurora PostgreSQL cluster endpoint"
            });

            new CfnOutput(this, "DatabaseSecretArn", new CfnOutputProps
            {
                Value = DatabaseSecret.SecretArn,
                Description = "ARN of the database credentials secret"
            });
        }
    }
}
