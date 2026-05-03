using AutoMapper;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Domain.Entities;

namespace Ecom.Reporting.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    private readonly IMapper _mapper;

    public AuditService(IAuditRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AuditLogDto>> GetByProductIdAsync(int productId)
    {
        var logs = await _repo.GetByEntityAsync("Product", productId);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAllAsync(DateTime? from, DateTime? to)
    {
        var logs = await _repo.GetAllAsync(from, to);
        return _mapper.Map<IEnumerable<AuditLogDto>>(logs);
    }

    public async Task WriteAsync(AuditLogDto dto)
    {
        var entity = _mapper.Map<AuditLog>(dto);
        await _repo.AddAsync(entity);
    }
}
