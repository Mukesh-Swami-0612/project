using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Reporting.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly ReportingDbContext _context;

    public AuditRepository(ReportingDbContext context) => _context = context;

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, int entityId) =>
        await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<AuditLog>> GetAllAsync(DateTime? from, DateTime? to)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to.Value);
        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, int>> GetActionCountsAsync()
    {
        // ✅ Use database aggregation
        return await _context.AuditLogs
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action ?? "Unknown", x => x.Count);
    }

    public async Task<IEnumerable<AuditLog>> GetFilteredAsync(DateTime? from, DateTime? to, string? reportType, int limit)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Apply date filters
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        // ✅ Apply report type filter
        if (!string.IsNullOrWhiteSpace(reportType))
        {
            query = reportType.ToLower() switch
            {
                "catalog-quality" => query.Where(a => a.EntityName == "Product"),
                "low-stock" => query.Where(a => a.Action == "StockChanged"),
                "price-changes" => query.Where(a => a.Action == "PriceChanged"),
                "all-audit" => query,
                _ => throw new InvalidOperationException($"Unknown report type: {reportType}. Valid types: catalog-quality, low-stock, price-changes, all-audit")
            };
        }

        // ✅ Apply limit and order
        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
