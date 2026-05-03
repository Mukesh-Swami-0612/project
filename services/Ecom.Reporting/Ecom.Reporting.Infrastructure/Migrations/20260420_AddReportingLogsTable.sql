-- Reporting Service Logs Table
CREATE TABLE ReportingLogs (
    Id INT IDENTITY PRIMARY KEY,
    Level NVARCHAR(50),
    Message NVARCHAR(MAX),
    CorrelationId NVARCHAR(100),
    Username NVARCHAR(100),
    Endpoint NVARCHAR(200),
    LoggedAt DATETIME2,
    Exception NVARCHAR(MAX)
);

-- Indexes for efficient querying
CREATE INDEX IX_ReportingLogs_LoggedAt ON ReportingLogs(LoggedAt);
CREATE INDEX IX_ReportingLogs_Level ON ReportingLogs(Level);
CREATE INDEX IX_ReportingLogs_CorrelationId ON ReportingLogs(CorrelationId);
CREATE INDEX IX_ReportingLogs_Username ON ReportingLogs(Username);
