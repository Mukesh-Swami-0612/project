namespace Ecom.Notification.Application.Constants;

/// <summary>
/// Centralized email subject constants for consistent messaging
/// Single source of truth for all notification subjects
/// </summary>
public static class EmailSubjects
{
    // Auth & User Management
    public const string Welcome = "Welcome to Ecom Platform 🎉";
    public const string LoginAlert = "🔒 Security Alert: New Login Detected";
    
    // Catalog & Product Management
    public const string ProductApproved = "✅ Your Product Has Been Approved";
    public const string ProductRejected = "❌ Your Product Was Rejected";
    public const string ProductPublished = "🚀 Your Product Is Now Live";
    
    // Workflow & System
    public const string WorkflowFailed = "⚠️ Workflow Execution Failed";
}
