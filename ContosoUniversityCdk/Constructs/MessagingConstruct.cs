using Amazon.CDK;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace ContosoUniversityCdk.Constructs
{
    public class MessagingConstruct : Construct
    {
        public IQueue NotificationQueue { get; }

        public MessagingConstruct(Construct scope, string id) : base(scope, id)
        {
            // Create SQS standard queue for notifications
            // Configure with 4-day message retention and 30-second visibility timeout
            NotificationQueue = new Queue(this, "NotificationQueue", new QueueProps
            {
                QueueName = "contoso-notifications",
                
                // Message retention period: 4 days (345,600 seconds)
                RetentionPeriod = Duration.Days(4),
                
                // Visibility timeout: 30 seconds
                // This is the time a message is hidden from other consumers after being received
                VisibilityTimeout = Duration.Seconds(30),
                
                // Enable encryption with AWS managed keys (SSE-SQS)
                Encryption = QueueEncryption.KMS_MANAGED,
                
                // Receive message wait time: 0 seconds (short polling)
                ReceiveMessageWaitTime = Duration.Seconds(0),
                
                // Dead letter queue configuration can be added later if needed
                // DeadLetterQueue = new DeadLetterQueue { ... }
            });

            // Output the queue URL for reference
            new CfnOutput(this, "NotificationQueueUrl", new CfnOutputProps
            {
                Value = NotificationQueue.QueueUrl,
                Description = "SQS queue URL for notifications"
            });

            new CfnOutput(this, "NotificationQueueArn", new CfnOutputProps
            {
                Value = NotificationQueue.QueueArn,
                Description = "SQS queue ARN for notifications"
            });
        }
    }
}
