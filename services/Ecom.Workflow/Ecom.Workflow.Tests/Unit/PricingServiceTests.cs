using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Ecom.Workflow.Tests.Unit;

[TestFixture]
public class PricingServiceTests
{
    private Mock<IOutboxRepository> _outboxMock = null!;
    private PricingService _service = null!;

    [SetUp]
    public void Setup()
    {
        _outboxMock = new Mock<IOutboxRepository>();
        _outboxMock.Setup(o => o.AddAsync(It.IsAny<OutboxEvent>())).Returns(Task.CompletedTask);
        _service = new PricingService(_outboxMock.Object);
    }

    [Test]
    public async Task SavePricingAsync_ValidPrice_ReturnsDto()
    {
        var dto = new PricingDto { ProductVariantId = 1, MRP = 100, SalePrice = 80 };
        var result = await _service.SavePricingAsync(1, dto);
        Assert.That(result.SalePrice, Is.EqualTo(80));
    }

    [Test]
    public void SavePricingAsync_SalePriceExceedsMRP_ThrowsInvalidOperation()
    {
        var dto = new PricingDto { ProductVariantId = 1, MRP = 50, SalePrice = 100 };
        Assert.ThrowsAsync<InvalidOperationException>(() => _service.SavePricingAsync(1, dto));
    }
}
