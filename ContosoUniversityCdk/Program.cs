using Amazon.CDK;

namespace ContosoUniversityCdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // Get environment configuration from context or environment variables
            var account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT");
            var region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION");

            // Use simple stack (single EC2 instance, no load balancer)
            new ContosoUniversityStackSimple(app, "ContosoUniversityStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = account,
                    Region = region
                }
            });

            // Test stack (NotificationAPI only)
            new ContosoUniversityStackTest(app, "ContosoUniversityTestStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = account,
                    Region = region
                }
            });

            app.Synth();
        }
    }
}
