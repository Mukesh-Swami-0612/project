using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Ecom.Shared.Infrastructure.Testing;

/// <summary>
/// 🔥 TESTING: Utility to test CORS configuration across all services
/// </summary>
public class CorsTestHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CorsTestHelper> _logger;

    public CorsTestHelper(HttpClient httpClient, ILogger<CorsTestHelper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Test CORS preflight request
    /// </summary>
    public async Task<CorsTestResult> TestPreflightAsync(string serviceUrl, string origin = "http://localhost:4200")
    {
        var result = new CorsTestResult { ServiceUrl = serviceUrl, Origin = origin };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Options, serviceUrl);
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "Content-Type,Authorization");

            var response = await _httpClient.SendAsync(request);
            
            result.StatusCode = (int)response.StatusCode;
            result.IsSuccessful = response.IsSuccessStatusCode;

            // Check CORS headers
            result.AccessControlAllowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
            result.AccessControlAllowMethods = response.Headers.GetValues("Access-Control-Allow-Methods").FirstOrDefault();
            result.AccessControlAllowHeaders = response.Headers.GetValues("Access-Control-Allow-Headers").FirstOrDefault();
            result.AccessControlAllowCredentials = response.Headers.GetValues("Access-Control-Allow-Credentials").FirstOrDefault();

            _logger.LogInformation("CORS preflight test for {ServiceUrl}: {Status}", serviceUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            _logger.LogError(ex, "CORS preflight test failed for {ServiceUrl}", serviceUrl);
        }

        return result;
    }

    /// <summary>
    /// Test actual CORS request
    /// </summary>
    public async Task<CorsTestResult> TestActualRequestAsync(string serviceUrl, string origin = "http://localhost:4200")
    {
        var result = new CorsTestResult { ServiceUrl = serviceUrl, Origin = origin };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{serviceUrl}/health");
            request.Headers.Add("Origin", origin);

            var response = await _httpClient.SendAsync(request);
            
            result.StatusCode = (int)response.StatusCode;
            result.IsSuccessful = response.IsSuccessStatusCode;

            // Check CORS headers in actual response
            if (response.Headers.Contains("Access-Control-Allow-Origin"))
            {
                result.AccessControlAllowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
            }

            _logger.LogInformation("CORS actual request test for {ServiceUrl}: {Status}", serviceUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            _logger.LogError(ex, "CORS actual request test failed for {ServiceUrl}", serviceUrl);
        }

        return result;
    }

    /// <summary>
    /// Test CORS across all services
    /// </summary>
    public async Task<List<CorsTestResult>> TestAllServicesAsync()
    {
        var services = new[]
        {
            "https://localhost:7001", // Auth
            "https://localhost:7002", // Catalog
            "https://localhost:7003", // Workflow
            "https://localhost:7004", // Reporting
            "https://localhost:7005", // Notification
            "https://localhost:7000"  // Gateway
        };

        var results = new List<CorsTestResult>();

        foreach (var service in services)
        {
            _logger.LogInformation("Testing CORS for service: {Service}", service);

            // Test preflight
            var preflightResult = await TestPreflightAsync(service);
            preflightResult.TestType = "Preflight";
            results.Add(preflightResult);

            // Test actual request
            var actualResult = await TestActualRequestAsync(service);
            actualResult.TestType = "Actual";
            results.Add(actualResult);
        }

        return results;
    }

    /// <summary>
    /// Generate CORS test report
    /// </summary>
    public string GenerateReport(List<CorsTestResult> results)
    {
        var report = new StringBuilder();
        report.AppendLine("🔥 CORS Configuration Test Report");
        report.AppendLine("=====================================");
        report.AppendLine();

        var groupedResults = results.GroupBy(r => r.ServiceUrl);

        foreach (var group in groupedResults)
        {
            report.AppendLine($"Service: {group.Key}");
            report.AppendLine("─────────────────────────────────────");

            foreach (var result in group)
            {
                var status = result.IsSuccessful ? "✅ PASS" : "❌ FAIL";
                report.AppendLine($"{result.TestType}: {status} (HTTP {result.StatusCode})");

                if (!string.IsNullOrEmpty(result.AccessControlAllowOrigin))
                    report.AppendLine($"  Allow-Origin: {result.AccessControlAllowOrigin}");
                if (!string.IsNullOrEmpty(result.AccessControlAllowMethods))
                    report.AppendLine($"  Allow-Methods: {result.AccessControlAllowMethods}");
                if (!string.IsNullOrEmpty(result.AccessControlAllowHeaders))
                    report.AppendLine($"  Allow-Headers: {result.AccessControlAllowHeaders}");
                if (!string.IsNullOrEmpty(result.AccessControlAllowCredentials))
                    report.AppendLine($"  Allow-Credentials: {result.AccessControlAllowCredentials}");
                if (!string.IsNullOrEmpty(result.Error))
                    report.AppendLine($"  Error: {result.Error}");
            }

            report.AppendLine();
        }

        // Summary
        var totalTests = results.Count;
        var passedTests = results.Count(r => r.IsSuccessful);
        var failedTests = totalTests - passedTests;

        report.AppendLine("Summary:");
        report.AppendLine($"Total Tests: {totalTests}");
        report.AppendLine($"Passed: {passedTests} ✅");
        report.AppendLine($"Failed: {failedTests} ❌");
        report.AppendLine($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");

        return report.ToString();
    }
}

public class CorsTestResult
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public string? AccessControlAllowOrigin { get; set; }
    public string? AccessControlAllowMethods { get; set; }
    public string? AccessControlAllowHeaders { get; set; }
    public string? AccessControlAllowCredentials { get; set; }
    public string? Error { get; set; }
}