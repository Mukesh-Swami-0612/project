-- Migration: Create UserLoginHistory table for suspicious login detection
-- Run this against ECom_NotificationDB

-- Create UserLoginHistory table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLoginHistory')
BEGIN
    CREATE TABLE [dbo].[UserLoginHistory] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] INT NOT NULL,
        [IpAddress] NVARCHAR(100) NOT NULL,
        [UserAgent] NVARCHAR(500) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    -- Create index on UserId and CreatedAt for faster last-login queries
    CREATE INDEX IX_UserLoginHistory_UserId_CreatedAt 
    ON [dbo].[UserLoginHistory]([UserId], [CreatedAt] DESC);

    -- Create index on CreatedAt for cleanup queries
    CREATE INDEX IX_UserLoginHistory_CreatedAt 
    ON [dbo].[UserLoginHistory]([CreatedAt]);

    PRINT 'UserLoginHistory table created successfully';
END
ELSE
BEGIN
    PRINT 'UserLoginHistory table already exists';
END
GO

PRINT 'Migration completed: UserLoginHistory table';
