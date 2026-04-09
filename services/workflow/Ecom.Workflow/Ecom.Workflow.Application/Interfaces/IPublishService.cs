using Ecom.Workflow.Application.DTOs;

namespace Ecom.Workflow.Application.Interfaces;

public interface IPublishService
{
    Task<PublishChecklistDto> GetChecklistAsync(int productId);
    Task PublishAsync(int productId, int publishedBy);
    Task ArchiveAsync(int productId, int archivedBy);
}
