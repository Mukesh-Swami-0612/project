-- =============================================
-- Notification System - Structured Logging Table
-- =============================================
-- This table stores structured logs from Serilog
-- Provides readable, queryable logs with correlation tracking
-- =============================================

CREATE TABLE NotificationLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Log Level (Information, Warning, Error, etc.)
    Level NVARCHAR(50) NOT NULL,
    
    -- Log Message
    Message NVARCHAR(MAX) NOT NULL,
    
    -- Correlation ID for tracing across services
    CorrelationId NVARCHAR(100) NULL,
    
    -- Username (from JWT or System)
    Username NVARCHAR(100) NULL,
    
    -- Notification ID (if applicable)
    NotificationId UNIQUEIDENTIFIER NULL,
    
    -- Machine/Environment Info
    MachineName NVARCHAR(100) NULL,
    
    -- Thread ID
    ThreadId INT NULL,
    
    -- Exception details (if error)
    Exception NVARCHAR(MAX) NULL,
    
    -- Additional properties (JSON)
    Properties NVARCHAR(MAX) NULL,
    
    -- Timestamp
    LoggedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Index for common queries
    INDEX IX_NotificationLogs_Level (Level),
    INDEX IX_NotificationLogs_CorrelationId (CorrelationId),
    INDEX IX_NotificationLogs_LoggedAt (LoggedAt DESC),
    INDEX IX_NotificationLogs_NotificationId (NotificationId)
);

GO

-- Sample query to view recent logs
-- SELECT TOP 100 
--     Username,
--     Level,
--     Message,
--     CorrelationId,
--     LoggedAt
-- FROM NotificationLogs
-- ORDER BY LoggedAt DESC;
