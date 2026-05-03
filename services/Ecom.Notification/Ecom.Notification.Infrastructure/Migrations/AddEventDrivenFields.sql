-- Migration: Add event-driven notification fields
-- Run this against ECom_NotificationDB

-- Add Type column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'Type')
BEGIN
    ALTER TABLE [dbo].[Notifications]
    ADD [Type] NVARCHAR(50) NOT NULL DEFAULT 'MANUAL';
END
GO

-- Add CorrelationId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'CorrelationId')
BEGIN
    ALTER TABLE [dbo].[Notifications]
    ADD [CorrelationId] NVARCHAR(100) NULL;
END
GO

-- Add Status column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[Notifications]
    ADD [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending';
END
GO

-- Create index on Status for faster queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_Status' AND object_id = OBJECT_ID(N'[dbo].[Notifications]'))
BEGIN
    CREATE INDEX IX_Notifications_Status ON [dbo].[Notifications]([Status]);
END
GO

-- Create index on CorrelationId for tracing
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_CorrelationId' AND object_id = OBJECT_ID(N'[dbo].[Notifications]'))
BEGIN
    CREATE INDEX IX_Notifications_CorrelationId ON [dbo].[Notifications]([CorrelationId]);
END
GO

PRINT 'Migration completed: Event-driven notification fields added';
