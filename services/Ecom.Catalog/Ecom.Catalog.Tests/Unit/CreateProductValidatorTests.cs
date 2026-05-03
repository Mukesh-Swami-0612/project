using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Validators;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit;

[TestFixture]
public class CreateProductValidatorTests
{
    private CreateProductValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new CreateProductValidator();
    }

    [Test]
    public void Validate_ValidProduct_PassesValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyName_FailsValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Name"), Is.True);
    }

    [Test]
    public void Validate_EmptySKU_FailsValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Product",
            SKU = "",
            CategoryId = 1,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "SKU"), Is.True);
    }

    [Test]
    public void Validate_InvalidCategoryId_FailsValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 0,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "CategoryId"), Is.True);
    }

    [Test]
    public void Validate_NameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = new string('A', 151), // 151 characters
            SKU = "TEST-001",
            CategoryId = 1,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Name"), Is.True);
    }

    [Test]
    public void Validate_SKUTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Test Product",
            SKU = new string('A', 101), // 101 characters
            CategoryId = 1,
            CreatedBy = 1
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "SKU"), Is.True);
    }
}
