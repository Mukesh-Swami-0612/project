using Asp.Versioning;
using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Workflow.API.Controllers;

/// <summary>
/// Admin API for workflow management and operations
/// Provides visibility and control over workflow instances
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/workflows")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowInstanceRepository _repo;
    private readonly IWorkflowAuditRepository _auditRepo;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly IWorkflowExportService _exportService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowInstanceRepository repo,
        IWorkflowAuditRepository auditRepo,
        IWorkflowOrchestrator orchestrator,
        IWorkflowExportService exportService,
        ILogger<WorkflowController> logger)
    {
        _repo = repo;
        _auditRepo = auditRepo;
        _orchestrator = orchestrator;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflows with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] WorkflowQueryDto query)
    {
        _logger.LogInformation(
            "WORKFLOW_QUERY | Status: {Status} | Step: {Step} | Page: {Page}",
            query.Status,
            query.Step,
            query.Page);

        var result = await _repo.QueryAsync(query);

        var response = result.Data.Select(x => new WorkflowResponseDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            Status = x.Status.ToString(),
            CurrentStep = x.CurrentStep.ToString(),
            RetryCount = x.RetryCount,
            MaxRetries = x.MaxRetries,
            NextRetryAt = x.NextRetryAt,
            LastError = x.LastError,
            CorrelationId = x.CorrelationId,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            CompletedAt = x.CompletedAt
        });

        return Ok(new
        {
            data = response,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
            hasPreviousPage = result.HasPreviousPage,
            hasNextPage = result.HasNextPage
        });
    }

    /// <summary>
    /// Get workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("WORKFLOW_GET_BY_ID | WorkflowId: {WorkflowId}", id);

        var wf = await _repo.GetByIdAsync(id);

        if (wf == null)
        {
            _logger.LogWarning("WORKFLOW_NOT_FOUND | WorkflowId: {WorkflowId}", id);
            return NotFound(new { message = "Workflow not found" });
        }

        return Ok(new WorkflowResponseDto
        {
            Id = wf.Id,
            ProductId = wf.ProductId,
            Status = wf.Status.ToString(),
            CurrentStep = wf.CurrentStep.ToString(),
            RetryCount = wf.RetryCount,
            MaxRetries = wf.MaxRetries,
            NextRetryAt = wf.NextRetryAt,
            LastError = wf.LastError,
            CorrelationId = wf.CorrelationId,
            CreatedAt = wf.CreatedAt,
            UpdatedAt = wf.UpdatedAt,
            CompletedAt = wf.CompletedAt
        });
    }

    /// <summary>
    /// Get workflow by product ID
    /// </summary>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        _logger.LogInformation("WORKFLOW_GET_BY_PRODUCT | ProductId: {ProductId}", productId);

        var wf = await _repo.GetByProductIdAsync(productId);

        if (wf == null)
        {
            _logger.LogWarning("WORKFLOW_NOT_FOUND_FOR_PRODUCT | ProductId: {ProductId}", productId);
            return NotFound(new { message = "Workflow not found for product" });
        }

        return Ok(new WorkflowResponseDto
        {
            Id = wf.Id,
            ProductId = wf.ProductId,
            Status = wf.Status.ToString(),
            CurrentStep = wf.CurrentStep.ToString(),
            RetryCount = wf.RetryCount,
            MaxRetries = wf.MaxRetries,
            NextRetryAt = wf.NextRetryAt,
            LastError = wf.LastError,
            CorrelationId = wf.CorrelationId,
            CreatedAt = wf.CreatedAt,
            UpdatedAt = wf.UpdatedAt,
            CompletedAt = wf.CompletedAt
        });
    }

    /// <summary>
    /// Manually retry a failed workflow
    /// Resets retry count and triggers orchestrator
    /// </summary>
    [HttpPost("{id}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Retry(Guid id)
    {
        _logger.LogInformation("WORKFLOW_MANUAL_RETRY_REQUESTED | WorkflowId: {WorkflowId}", id);

        var wf = await _repo.GetByIdAsync(id);

        if (wf == null)
        {
            _logger.LogWarning("WORKFLOW_NOT_FOUND | WorkflowId: {WorkflowId}", id);
            return NotFound(new { message = "Workflow not found" });
        }

        if (wf.Status != WorkflowStatus.Failed)
        {
            _logger.LogWarning(
                "WORKFLOW_RETRY_INVALID_STATE | WorkflowId: {WorkflowId} | Status: {Status}",
                id,
                wf.Status);
            return BadRequest(new { message = "Only failed workflows can be retried" });
        }

        // Reset workflow state for manual retry
        wf.Status = WorkflowStatus.InProgress;
        wf.RetryCount = 0;
        wf.NextRetryAt = null;
        wf.LastError = null;
        wf.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(wf);

        _logger.LogInformation(
            "WORKFLOW_MANUAL_RETRY_INITIATED | WorkflowId: {WorkflowId} | ProductId: {ProductId}",
            wf.Id,
            wf.ProductId);

        // Trigger orchestrator
        try
        {
            await _orchestrator.ProcessAsync(wf.Id);

            return Ok(new
            {
                message = "Workflow retry initiated successfully",
                workflowId = wf.Id,
                productId = wf.ProductId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WORKFLOW_MANUAL_RETRY_FAILED | WorkflowId: {WorkflowId} | Error: {Error}",
                wf.Id,
                ex.Message);

            return StatusCode(500, new
            {
                message = "Workflow retry failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get workflow statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStats()
    {
        _logger.LogInformation("WORKFLOW_STATS_REQUESTED");

        var inProgress = await _repo.GetByStatusAsync(WorkflowStatus.InProgress);
        var completed = await _repo.GetByStatusAsync(WorkflowStatus.Completed);
        var failed = await _repo.GetByStatusAsync(WorkflowStatus.Failed);

        return Ok(new
        {
            inProgress = inProgress.Count(),
            completed = completed.Count(),
            failed = failed.Count(),
            total = inProgress.Count() + completed.Count() + failed.Count()
        });
    }

    /// <summary>
    /// Get audit logs for a specific workflow with filtering and pagination
    /// </summary>
    [HttpGet("{id}/logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLogs(Guid id, [FromQuery] WorkflowLogQueryDto query)
    {
        _logger.LogInformation(
            "WORKFLOW_LOGS_REQUESTED | WorkflowId: {WorkflowId} | Action: {Action} | FromDate: {FromDate} | ToDate: {ToDate}",
            id,
            query.Action,
            query.FromDate,
            query.ToDate);

        // Verify workflow exists
        var workflow = await _repo.GetByIdAsync(id);
        if (workflow == null)
        {
            _logger.LogWarning("WORKFLOW_NOT_FOUND | WorkflowId: {WorkflowId}", id);
            return NotFound(new { message = "Workflow not found" });
        }

        var result = await _auditRepo.GetLogsAsync(id, query);

        return Ok(new
        {
            data = result.Data.Select(x => new
            {
                x.Username,
                x.Action,
                x.Message,
                x.LoggedAt
            }),
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
            hasPreviousPage = result.HasPreviousPage,
            hasNextPage = result.HasNextPage
        });
    }

    /// <summary>
    /// Export workflow audit logs to Excel with optional filtering
    /// </summary>
    [HttpGet("{id}/logs/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportLogs(Guid id, [FromQuery] WorkflowLogQueryDto query)
    {
        _logger.LogInformation(
            "WORKFLOW_LOGS_EXPORT_REQUESTED | WorkflowId: {WorkflowId} | Action: {Action} | FromDate: {FromDate} | ToDate: {ToDate}",
            id,
            query.Action,
            query.FromDate,
            query.ToDate);

        // Verify workflow exists
        var workflow = await _repo.GetByIdAsync(id);
        if (workflow == null)
        {
            _logger.LogWarning("WORKFLOW_NOT_FOUND | WorkflowId: {WorkflowId}", id);
            return NotFound(new { message = "Workflow not found" });
        }

        // Get all logs (no pagination for export)
        var logs = await _auditRepo.GetAllLogsAsync(id, query);

        _logger.LogInformation(
            "WORKFLOW_LOGS_EXPORT_GENERATED | WorkflowId: {WorkflowId} | RecordCount: {RecordCount}",
            id,
            logs.Count);

        var fileBytes = _exportService.ExportLogsToExcel(logs, id);

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"workflow-logs-{id}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }
}
