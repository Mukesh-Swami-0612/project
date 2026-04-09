using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit;

[TestFixture]
public class InventoryServiceTests
{
    private Mock<IOutboxRepository> _outboxMock = null!;
    private InventoryService _service = null!;

    [SetUp]
    public void Setup()
    {
        _outboxMock = new Mock<IOutboxRepository>();
        _outboxMock.Setup(o => o.AddAsync(It.IsAny<OutboxEvent>())).Returns(Task.CompletedTask);
        _service = new InventoryService(_outboxMock.Object);
    }

    [Test]
    public async Task SaveInventoryAsync_ValidQuantity_ReturnsDto()
    {
        var dto = new InventoryDto { ProductVariantId = 1, Quantity = 50 };
        var result = await _service.SaveInventoryAsync(1, dto);
        Assert.That(result.Quantity, Is.EqualTo(50));
    }

    [Test]
    public void SaveInventoryAsync_NegativeQuantity_ThrowsInvalidOperation()
    {
        var dto = new InventoryDto { ProductVariantId = 1, Quantity = -5 };
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveInventoryAsync(1, dto));
    }

    [Test]
    public async Task SaveInventoryAsync_ValidData_PublishesOutboxEvent()
    {
        var dto = new InventoryDto { ProductVariantId = 1, Quantity = 10 };
        await _service.SaveInventoryAsync(1, dto);
        _outboxMock.Verify(o => o.AddAsync(It.Is<OutboxEvent>(e => e.EventType == "inventory.updated")), Times.Once);
    }
}
