using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using Ecom.Catalog.Domain.Exceptions;
using Ecom.Catalog.Domain.Services;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit.Domain;

[TestFixture]
public class ProductValidationServiceTests
{
    private ProductValidationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new ProductValidationService();
    }

    // ── CREATION VALIDATION TESTS ────────────────────────────────────────────

    [Test]
    public void ValidateForCreation_Should_Pass_With_Valid_Product()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForCreation(product));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_Name_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("name"));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_Name_Too_Long()
    {
        // Arrange
        var product = new Product
        {
            Name = new string('A', 151),
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("150 characters"));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_SKU_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("SKU"));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_SKU_Too_Long()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = new string('A', 101),
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("100 characters"));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_CategoryId_Is_Zero()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 0
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("category"));
    }

    // ── SKU FORMAT VALIDATION TESTS ──────────────────────────────────────────

    [Test]
    public void ValidateForCreation_Should_Fail_When_SKU_Contains_Spaces()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST 001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("spaces"));
    }

    [Test]
    public void ValidateForCreation_Should_Fail_When_SKU_Has_Invalid_Characters()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST@001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForCreation(product));

        Assert.That(ex!.Message, Does.Contain("letters, numbers, hyphens"));
    }

    [Test]
    public void ValidateForCreation_Should_Allow_SKU_With_Hyphens()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001-ABC",
            CategoryId = 1
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForCreation(product));
    }

    [Test]
    public void ValidateForCreation_Should_Allow_SKU_With_Underscores()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST_001_ABC",
            CategoryId = 1
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForCreation(product));
    }

    // ── UPDATE VALIDATION TESTS ──────────────────────────────────────────────

    [Test]
    public void ValidateForUpdate_Should_Pass_With_Valid_Product()
    {
        // Arrange
        var product = new Product
        {
            Name = "Updated Product",
            SKU = "TEST-002",
            CategoryId = 2
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForUpdate(product));
    }

    [Test]
    public void ValidateForUpdate_Should_Fail_When_Name_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            _service.ValidateForUpdate(product));
    }

    // ── REVIEW VALIDATION TESTS ──────────────────────────────────────────────

    [Test]
    public void ValidateForReview_Should_Pass_With_Minimum_Data()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForReview(product));
    }

    [Test]
    public void ValidateForReview_Should_Fail_Without_Minimum_Data()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            _service.ValidateForReview(product));
    }

    // ── PUBLISH VALIDATION TESTS ─────────────────────────────────────────────

    [Test]
    public void ValidateForPublish_Should_Fail_When_No_Media_Assets()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            MediaAssets = new List<MediaAsset>()
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForPublish(product));

        Assert.That(ex!.Message, Does.Contain("image"));
    }

    [Test]
    public void ValidateForPublish_Should_Fail_When_No_Primary_Image()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            MediaAssets = new List<MediaAsset>
            {
                new MediaAsset { IsPrimary = false, FileUrl = "test.jpg" }
            }
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForPublish(product));

        Assert.That(ex!.Message, Does.Contain("primary image"));
    }

    [Test]
    public void ValidateForPublish_Should_Pass_With_Primary_Image()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            MediaAssets = new List<MediaAsset>
            {
                new MediaAsset { IsPrimary = true, FileUrl = "test.jpg" }
            }
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForPublish(product));
    }

    [Test]
    public void ValidateForPublish_Should_Fail_When_Name_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1,
            MediaAssets = new List<MediaAsset>
            {
                new MediaAsset { IsPrimary = true, FileUrl = "test.jpg" }
            }
        };

        // Act & Assert
        Assert.Throws<DomainException>(() =>
            _service.ValidateForPublish(product));
    }

    // ── ARCHIVE VALIDATION TESTS ─────────────────────────────────────────────

    [Test]
    public void ValidateForArchive_Should_Fail_When_Product_Is_Draft()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.Draft,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            _service.ValidateForArchive(product));

        Assert.That(ex!.Message, Does.Contain("deleted"));
    }

    [Test]
    public void ValidateForArchive_Should_Pass_When_Product_Is_Published()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.Published,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.DoesNotThrow(() => _service.ValidateForArchive(product));
    }
}
