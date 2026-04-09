using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;
using Ecom.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Auth.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuthDbContext _context;

    public AuditLogRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<(List<AuditLogDto> logs, int total)> GetLogsAsync(AuditLogFilterDto filter)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Filters
        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(x => x.Email.Contains(filter.Email));

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(x => x.Action == filter.Action);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromDate);

        // FIX: Include full day for ToDate
        if (filter.ToDate.HasValue)
        {
            var toDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        // Total count
        var total = await query.CountAsync();

        // LIMIT: Max page size to prevent DB overload
        var pageSize = filter.PageSize > 50 ? 50 : filter.PageSize;

        // Dynamic sorting with case-insensitive order
        var sortOrder = filter.SortOrder?.ToLower() == "asc" ? "asc" : "desc";

        query = filter.SortBy?.ToLower() switch
        {
            "email" => sortOrder == "asc"
                ? query.OrderBy(x => x.Email)
                : query.OrderByDescending(x => x.Email),

            "action" => sortOrder == "asc"
                ? query.OrderBy(x => x.Action)
                : query.OrderByDescending(x => x.Action),

            "status" => sortOrder == "asc"
                ? query.OrderBy(x => x.Status)
                : query.OrderByDescending(x => x.Status),

            "createdat" or _ => sortOrder == "asc"
                ? query.OrderBy(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt)
        };

        // Pagination (after sorting)
        var logs = await query
            .Skip((filter.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                Email = x.Email,
                Action = x.Action,
                Status = x.Status,
                IpAddress = x.IpAddress,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return (logs, total);
    }
}
