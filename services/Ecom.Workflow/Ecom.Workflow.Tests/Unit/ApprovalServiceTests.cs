using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit;

[TestFixture]
public class ApprovalServiceTests
{
    private Mock<IOutboxRepository> _outboxMock = null!;
    private ApprovalService _service = null!;

    [SetUp]
    public void Setup()
    {
        _outboxMock = new Mock<IOutboxRepository>();
        _outboxMock.Setup(o => o.AddAsync(It.IsAny<OutboxEvent>())).Returns(Task.CompletedTask);
        _service = new ApprovalService(_outboxMock.Object);
    }

    [Test]
    public async Task SubmitForReviewAsync_PublishesSubmittedEvent()
    {
        await _service.SubmitForReviewAsync(1, 2);
        _outboxMock.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == "product.submitted")), Times.Once);
    }

    [Test]
    public async Task ApproveAsync_PublishesApprovedEvent()
    {
        var dto = new ApprovalDto { ProductId = 1, ActionType = "Approve", ApprovedBy = 2 };
        await _service.ApproveAsync(dto);
        _outboxMock.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == "product.approved")), Times.Once);
    }

    [Test]
    public void RejectAsync_MissingComments_ThrowsInvalidOperation()
    {
        var dto = new ApprovalDto { ProductId = 1, ActionType = "Reject", ApprovedBy = 2, Comments = null };
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.RejectAsync(dto));
    }

    [Test]
    public async Task RejectAsync_WithComments_PublishesRejectedEvent()
    {
        var dto = new ApprovalDto { ProductId = 1, ActionType = "Reject", ApprovedBy = 2, Comments = "Missing images" };
        await _service.RejectAsync(dto);
        _outboxMock.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == "product.rejected")), Times.Once);
    }
}
