using AutoMapper;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Application.Mappings;
using Ecom.Catalog.Application.Services;
using Ecom.Catalog.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _repoMock = null!;
    private IMapper _mapper = null!;
    private CategoryService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repoMock = new Mock<ICategoryRepository>();
        _mapper = new MapperConfiguration(c => c.AddProfile<CatalogMappingProfile>()).CreateMapper();
        _service = new CategoryService(_repoMock.Object, _mapper);
    }

    [Test]
    public async Task GetAllAsync_ReturnsCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Clothing" }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(categories);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }
}
