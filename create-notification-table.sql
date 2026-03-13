-- Create Notification table if it doesn't exist
CREATE TABLE IF NOT EXISTS "Notification" (
    "Id" SERIAL PRIMARY KEY,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Operation" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "CreatedBy" TEXT NOT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReadAt" TIMESTAMP WITHOUT TIME ZONE NULL
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_Notification_CreatedAt" ON "Notification" ("CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_Notification_IsRead" ON "Notification" ("IsRead");

-- Verify table was created
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Notification';
