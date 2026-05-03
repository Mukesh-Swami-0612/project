using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Interfaces;

public interface IWorkflowExportService
{
    byte[] ExportLogsToExcel(List<WorkflowAuditLog> logs, Guid workflowId);
}
