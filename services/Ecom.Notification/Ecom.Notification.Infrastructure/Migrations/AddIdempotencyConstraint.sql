-- Migration: Add idempotency constraint to prevent duplicate notifications
-- Run this against ECom_NotificationDB

-- Add unique constraint on CorrelationId + Type
-- This ensures the same event cannot create duplicate notifications
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_Notification_Idempotency' 
    AND object_id = OBJECT_ID(N'[dbo].[Notifications]')
)
BEGIN
    -- First, ensure CorrelationId column exists and is not null for constraint
    -- Update any NULL CorrelationIds to a unique value
    UPDATE [dbo].[Notifications]
    SET CorrelationId = CAST(NEWID() AS NVARCHAR(100))
    WHERE CorrelationId IS NULL;

    -- Create unique constraint
    CREATE UNIQUE INDEX UQ_Notification_Idempotency 
    ON [dbo].[Notifications]([CorrelationId], [Type])
    WHERE CorrelationId IS NOT NULL;

    PRINT 'Idempotency constraint created successfully';
END
ELSE
BEGIN
    PRINT 'Idempotency constraint already exists';
END
GO

PRINT 'Migration completed: Idempotency constraint';
