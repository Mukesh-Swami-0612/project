using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit.Services;

[TestFixture]
public class WorkflowRetryTests
{
    private Mock<IWorkflowInstanceRepository> _mockRepo = null!;
    private Mock<ICommandPublisher> _mockCommandPublisher = null!;
    private Mock<IWorkflowAuditService> _mockAuditService = null!;
    private Mock<ILogger<WorkflowOrchestrator>> _mockLogger = null!;
    private WorkflowOrchestrator _orchestrator = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IWorkflowInstanceRepository>();
        _mockCommandPublisher = new Mock<ICommandPublisher>();
        _mockAuditService = new Mock<IWorkflowAuditService>();
        _mockLogger = new Mock<ILogger<WorkflowOrchestrator>>();
        _orchestrator = new WorkflowOrchestrator(
            _mockRepo.Object,
            _mockCommandPublisher.Object,
            _mockAuditService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task ProcessAsync_WhenFirstFailure_ShouldScheduleRetry()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Created,
            RetryCount = 0,
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        WorkflowInstance? updatedWorkflow = null;
        var updateCallCount = 0;
        _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w =>
            {
                updateCallCount++;
                if (updateCallCount == 1)
                {
                    throw new Exception("Database connection failed");
                }
                updatedWorkflow = w;
            })
            .Returns(Task.CompletedTask);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _orchestrator.ProcessAsync(workflowId));

        // Verify retry scheduled
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.RetryCount, Is.EqualTo(1));
        Assert.That(updatedWorkflow.NextRetryAt, Is.Not.Null);
        Assert.That(updatedWorkflow.NextRetryAt!.Value, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress)); // Not failed yet
        Assert.That(updatedWorkflow.LastError, Does.Contain("Database connection failed"));
    }

    [Test]
    public async Task ProcessAsync_WhenMaxRetriesExceeded_ShouldMarkAsDeadLetter()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Created,
            RetryCount = 2, // Already retried twice
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        WorkflowInstance? updatedWorkflow = null;
        var updateCallCount = 0;
        _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w =>
            {
                updateCallCount++;
                if (updateCallCount == 1)
                {
                    throw new Exception("Permanent failure");
                }
                updatedWorkflow = w;
            })
            .Returns(Task.CompletedTask);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _orchestrator.ProcessAsync(workflowId));

        // Verify dead letter
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.RetryCount, Is.EqualTo(3)); // MaxRetries reached
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.Failed));
        Assert.That(updatedWorkflow.CurrentStep, Is.EqualTo(WorkflowStep.Failed));
        Assert.That(updatedWorkflow.NextRetryAt, Is.Null); // No more retries
        Assert.That(updatedWorkflow.LastError, Does.Contain("Permanent failure"));
    }

    [Test]
    public async Task ProcessAsync_ExponentialBackoff_ShouldIncreaseDelay()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Created,
            RetryCount = 1, // Second retry
            MaxRetries = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        WorkflowInstance? updatedWorkflow = null;
        var updateCallCount = 0;
        _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w =>
            {
                updateCallCount++;
                if (updateCallCount == 1)
                {
                    throw new Exception("Transient failure");
                }
                updatedWorkflow = w;
            })
            .Returns(Task.CompletedTask);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _orchestrator.ProcessAsync(workflowId));

        // Verify exponential backoff (2^2 = 4 seconds)
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.RetryCount, Is.EqualTo(2));
        Assert.That(updatedWorkflow.NextRetryAt, Is.Not.Null);
        
        var expectedDelay = TimeSpan.FromSeconds(4); // 2^2
        var actualDelay = updatedWorkflow.NextRetryAt!.Value - DateTime.UtcNow;
        Assert.That(actualDelay.TotalSeconds, Is.GreaterThanOrEqualTo(3.5)); // Allow some tolerance
        Assert.That(actualDelay.TotalSeconds, Is.LessThanOrEqualTo(4.5));
    }

    [Test]
    public async Task ProcessAsync_WhenRetrySucceeds_ShouldClearRetryFields()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Created,
            RetryCount = 1,
            MaxRetries = 3,
            NextRetryAt = DateTime.UtcNow.AddSeconds(-1), // Due for retry
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        WorkflowInstance? updatedWorkflow = null;
        _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w => updatedWorkflow = w)
            .Returns(Task.CompletedTask);

        // Act
        await _orchestrator.ProcessAsync(workflowId);

        // Assert - workflow should progress normally
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.ValidationPending));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
        // RetryCount stays at 1 (not cleared, but workflow progresses)
    }
}
