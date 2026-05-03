using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Ecom.Workflow.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit.Consumers;

[TestFixture]
public class ProductCreatedConsumerTests
{
    private Mock<IWorkflowInstanceRepository> _mockRepo;
    private Mock<IWorkflowOrchestrator> _mockOrchestrator;
    private Mock<ILogger<ProductCreatedConsumer>> _mockLogger;
    private Mock<IServiceScopeFactory> _mockScopeFactory;
    private Mock<IServiceScope> _mockScope;
    private Mock<IServiceProvider> _mockServiceProvider;
    private ProductCreatedConsumer _consumer;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IWorkflowInstanceRepository>();
        _mockOrchestrator = new Mock<IWorkflowOrchestrator>();
        _mockLogger = new Mock<ILogger<ProductCreatedConsumer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IWorkflowInstanceRepository)))
            .Returns(_mockRepo.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IWorkflowOrchestrator)))
            .Returns(_mockOrchestrator.Object);

        _consumer = new ProductCreatedConsumer(_mockScopeFactory.Object, _mockLogger.Object);
    }

    [Test]
    public async Task HandleAsync_WhenProductCreated_ShouldCreateWorkflowInstance()
    {
        // Arrange
        var productCreatedEvent = new ProductCreatedEvent
        {
            ProductId = 123,
            SKU = "TEST-SKU-001",
            CreatedBy = 1,
            OccurredAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByProductIdAsync(123))
            .ReturnsAsync((WorkflowInstance?)null);

        WorkflowInstance? capturedWorkflow = null;
        _mockRepo.Setup(x => x.AddAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w => capturedWorkflow = w)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.HandleAsync(productCreatedEvent);

        // Assert
        _mockRepo.Verify(x => x.GetByProductIdAsync(123), Times.Once);
        _mockRepo.Verify(x => x.AddAsync(It.IsAny<WorkflowInstance>()), Times.Once);

        Assert.That(capturedWorkflow, Is.Not.Null);
        Assert.That(capturedWorkflow!.ProductId, Is.EqualTo(123));
        Assert.That(capturedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
        Assert.That(capturedWorkflow.CurrentStep, Is.EqualTo(WorkflowStep.Created));
        Assert.That(capturedWorkflow.RetryCount, Is.EqualTo(0));
        Assert.That(capturedWorkflow.CorrelationId, Is.Not.Null);
        Assert.That(capturedWorkflow.CorrelationId, Is.Not.Empty);
    }

    [Test]
    public async Task HandleAsync_WhenWorkflowAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var productCreatedEvent = new ProductCreatedEvent
        {
            ProductId = 123,
            SKU = "TEST-SKU-001",
            CreatedBy = 1,
            OccurredAt = DateTime.UtcNow
        };

        var existingWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            ProductId = 123,
            Status = WorkflowStatus.InProgress,
            CurrentStep = WorkflowStep.ValidationPending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByProductIdAsync(123))
            .ReturnsAsync(existingWorkflow);

        // Act
        await _consumer.HandleAsync(productCreatedEvent);

        // Assert
        _mockRepo.Verify(x => x.GetByProductIdAsync(123), Times.Once);
        _mockRepo.Verify(x => x.AddAsync(It.IsAny<WorkflowInstance>()), Times.Never);
    }

    [Test]
    public void HandleAsync_WhenRepositoryFails_ShouldThrowException()
    {
        // Arrange
        var productCreatedEvent = new ProductCreatedEvent
        {
            ProductId = 123,
            SKU = "TEST-SKU-001",
            CreatedBy = 1,
            OccurredAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByProductIdAsync(123))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
            await _consumer.HandleAsync(productCreatedEvent));
    }

    [Test]
    public async Task HandleAsync_ShouldSetCorrectInitialState()
    {
        // Arrange
        var productCreatedEvent = new ProductCreatedEvent
        {
            ProductId = 456,
            SKU = "TEST-SKU-002",
            CreatedBy = 2,
            OccurredAt = DateTime.UtcNow
        };

        _mockRepo.Setup(x => x.GetByProductIdAsync(456))
            .ReturnsAsync((WorkflowInstance?)null);

        WorkflowInstance? capturedWorkflow = null;
        _mockRepo.Setup(x => x.AddAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(w => capturedWorkflow = w)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.HandleAsync(productCreatedEvent);

        // Assert
        Assert.That(capturedWorkflow, Is.Not.Null);
        Assert.That(capturedWorkflow!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(capturedWorkflow.ProductId, Is.EqualTo(456));
        Assert.That(capturedWorkflow.Status, Is.EqualTo(WorkflowStatus.InProgress));
        Assert.That(capturedWorkflow.CurrentStep, Is.EqualTo(WorkflowStep.Created));
        Assert.That(capturedWorkflow.RetryCount, Is.EqualTo(0));
        Assert.That(capturedWorkflow.LastError, Is.Null);
        Assert.That(capturedWorkflow.CompletedAt, Is.Null);
        Assert.That(capturedWorkflow.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        Assert.That(capturedWorkflow.UpdatedAt, Is.Not.EqualTo(default(DateTime)));
    }
}
