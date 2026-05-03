namespace Ecom.Notification.Application.Utilities;

/// <summary>
/// Utility class for sanitizing sensitive data in logs
/// Prevents leaking passwords, tokens, and PII
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Masks email address for logging
    /// Example: john.doe@example.com → jo***@example.com
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) 
            return "unknown";

        var parts = email.Split('@');
        if (parts.Length != 2) 
            return "invalid-email";

        var name = parts[0];
        var domain = parts[1];

        var maskedName = name.Length <= 2
            ? "**"
            : name.Substring(0, 2) + "***";

        return $"{maskedName}@{domain}";
    }

    /// <summary>
    /// Masks token/password for logging
    /// Example: abc123def456 → abc12*****
    /// </summary>
    public static string MaskToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) 
            return "*****";

        return token.Length > 10
            ? token.Substring(0, 5) + "*****"
            : "*****";
    }

    /// <summary>
    /// Masks credit card number for logging
    /// Example: 1234567890123456 → ************3456
    /// </summary>
    public static string MaskCreditCard(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) 
            return "****";

        if (cardNumber.Length < 4)
            return "****";

        return "************" + cardNumber.Substring(cardNumber.Length - 4);
    }

    /// <summary>
    /// Masks phone number for logging
    /// Example: +1234567890 → +123****890
    /// </summary>
    public static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber)) 
            return "****";

        if (phoneNumber.Length < 6)
            return "****";

        var start = phoneNumber.Substring(0, 3);
        var end = phoneNumber.Substring(phoneNumber.Length - 3);

        return $"{start}****{end}";
    }

    /// <summary>
    /// Truncates long text for logging (e.g., email body)
    /// Example: Long text... → Long text... [truncated]
    /// </summary>
    public static string TruncateText(string? text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text)) 
            return string.Empty;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "... [truncated]";
    }

    /// <summary>
    /// Sanitizes a full message body - removes sensitive patterns
    /// Use this for email bodies that might contain sensitive data
    /// </summary>
    public static string SanitizeMessageBody(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        // Don't log full body - just metadata
        return $"[Message body length: {body.Length} characters]";
    }
}
