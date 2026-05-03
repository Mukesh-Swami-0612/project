using AutoMapper;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Application.Mappings;
using Ecom.Reporting.Application.Services;
using Ecom.Reporting.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Ecom.Reporting.Tests.Unit;

[TestFixture]
public class AuditServiceTests
{
    private Mock<IAuditRepository> _repoMock = null!;
    private IMapper _mapper = null!;
    private AuditService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<IAuditRepository>();
        _mapper = new MapperConfiguration(c => c.AddProfile<ReportingMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        _service = new AuditService(_repoMock.Object, _mapper);
    }

    [Test]
    public async Task GetByProductIdAsync_ReturnsMappedDtos()
    {
        _repoMock.Setup(r => r.GetByEntityAsync("Product", 1))
            .ReturnsAsync(new List<AuditLog>
            {
                new() { Id = 1, EntityName = "Product", EntityId = 1, Action = "Approved" }
            });

        var result = await _service.GetByProductIdAsync(1);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Action, Is.EqualTo("Approved"));
    }

    [Test]
    public async Task WriteAsync_CallsRepositoryAdd()
    {
        var dto = new AuditLogDto { EntityName = "Product", EntityId = 1, Action = "Published" };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);

        await _service.WriteAsync(dto);

        _repoMock.Verify(r => r.AddAsync(It.Is<AuditLog>(l => l.Action == "Published")), Times.Once);
    }
}
