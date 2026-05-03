using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Application.Mappings;
using Ecom.Catalog.Application.Services;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IProductRepository> _repoMock = null!;
    private Mock<IAuditService> _auditServiceMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<IOutboxRepository> _outboxRepoMock = null!;
    private IMapper _mapper = null!;
    private ProductService _service = null!;
    private OutboxService _outboxService = null!;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<IProductRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _outboxRepoMock = new Mock<IOutboxRepository>();
        _outboxService = new OutboxService(_outboxRepoMock.Object);
        _mapper = new MapperConfiguration(c => c.AddProfile<CatalogMappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        _service = new ProductService(_repoMock.Object, _mapper, _auditServiceMock.Object, _httpContextAccessorMock.Object, _outboxService);
    }

    [Test]
    public async Task CreateAsync_DuplicateSku_ThrowsInvalidOperation()
    {
        _repoMock.Setup(r => r.SkuExistsAsync("SKU-001")).ReturnsAsync(true);
        Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new CreateProductDto { Name = "Test", SKU = "SKU-001", CategoryId = 1, CreatedBy = 1 }));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);
        var result = await _service.GetByIdAsync(99);
        Assert.That(result, Is.Null);
    }
}
