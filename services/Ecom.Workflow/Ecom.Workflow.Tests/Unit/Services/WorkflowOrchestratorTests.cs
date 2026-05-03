using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit.Services;

[TestFixture]
public class WorkflowOrchestratorTests
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
    public async Task ProcessAsync_WhenStepIsCreated_ShouldMoveToValidationPending()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Created,
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

        // Assert
        _mockRepo.Verify(x => x.GetByIdAsync(workflowId), Times.Once);
        _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()), Times.Once);
        _mockCommandPublisher.Verify(
            x => x.PublishAsync("validate.product", It.IsAny<object>()),
            Times.Once);

        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.ValidationPending));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsValidationCompleted_ShouldMoveToApprovalPending()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.ValidationCompleted,
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

        // Assert
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.ApprovalPending));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
        _mockCommandPublisher.Verify(
            x => x.PublishAsync("request.approval", It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsApproved_ShouldMoveToPublishing()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Approved,
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

        // Assert
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.Publishing));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
        _mockCommandPublisher.Verify(
            x => x.PublishAsync("publish.product", It.IsAny<object>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsPublished_ShouldCompleteWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Published,
            CreatedAt = createdAt,
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

        // Assert
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.Completed));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.Completed));
        Assert.That(updatedWorkflow.CompletedAt, Is.Not.Null);
        Assert.That(updatedWorkflow.CompletedAt!.Value, Is.GreaterThan(createdAt));
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsRejected_ShouldMarkAsFailed()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.Rejected,
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

        // Assert
        Assert.That(updatedWorkflow, Is.Not.Null);
        Assert.That(updatedWorkflow!.CurrentStep, Is.EqualTo(WorkflowStep.Failed));
        Assert.That(updatedWorkflow.Status, Is.EqualTo(WorkflowStatus.Failed));
        Assert.That(updatedWorkflow.LastError, Is.EqualTo("Product was rejected during approval"));
    }

    [Test]
    public void ProcessAsync_WhenWorkflowNotFound_ShouldThrowException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync((WorkflowInstance?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _orchestrator.ProcessAsync(workflowId));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public async Task ProcessAsync_WhenRepositoryFails_ShouldScheduleRetry()
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

        WorkflowInstance? failedWorkflow = null;
        var updateCallCount = 0;
        _mockRepo.Setup(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w =>
            {
                updateCallCount++;
                if (updateCallCount == 1)
                {
                    throw new Exception("Database connection failed");
                }
                failedWorkflow = w;
            })
            .Returns(Task.CompletedTask);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _orchestrator.ProcessAsync(workflowId));

        // Verify workflow was updated with retry info (not marked as failed yet)
        _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()), Times.Exactly(2));
        Assert.That(failedWorkflow, Is.Not.Null);
        Assert.That(failedWorkflow!.Status, Is.EqualTo(WorkflowStatus.InProgress)); // Still in progress for retry
        Assert.That(failedWorkflow.RetryCount, Is.EqualTo(1));
        Assert.That(failedWorkflow.NextRetryAt, Is.Not.Null); // Retry scheduled
        Assert.That(failedWorkflow.LastError, Does.Contain("Database connection failed"));
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsCompleted_ShouldNotUpdateState()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.Completed,
            CurrentStep = WorkflowStep.Completed,
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        // Act
        await _orchestrator.ProcessAsync(workflowId);

        // Assert
        _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()), Times.Never);
    }

    [Test]
    public async Task ProcessAsync_WhenStepIsFailed_ShouldNotUpdateState()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            ProductId = 123,
            Status = WorkflowStatus.Failed,
            CurrentStep = WorkflowStep.Failed,
            LastError = "Previous error",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockRepo.Setup(x => x.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        // Act
        await _orchestrator.ProcessAsync(workflowId);

        // Assert
        _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<WorkflowInstance>()), Times.Never);
    }
}
