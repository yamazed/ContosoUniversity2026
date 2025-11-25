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

            new ContosoUniversityStack(app, "ContosoUniversityStack", new ContosoUniversityStackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = account,
                    Region = region
                },
                EnvironmentName = app.Node.TryGetContext("environmentName")?.ToString() ?? "dev",
                DatabaseMinCapacity = double.Parse(app.Node.TryGetContext("databaseMinCapacity")?.ToString() ?? "0.5"),
                DatabaseMaxCapacity = double.Parse(app.Node.TryGetContext("databaseMaxCapacity")?.ToString() ?? "2.0")
            });

            app.Synth();
        }
    }
}
