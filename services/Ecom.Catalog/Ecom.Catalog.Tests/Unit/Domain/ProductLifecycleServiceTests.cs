using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using Ecom.Catalog.Domain.Services;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit.Domain;

[TestFixture]
public class ProductLifecycleServiceTests
{
    private ProductLifecycleService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new ProductLifecycleService();
    }

    // ── TRANSITION VALIDATION TESTS ──────────────────────────────────────────

    [Test]
    public void Should_Not_Publish_If_Not_Approved()
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
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Published));

        Assert.That(ex!.Message, Does.Contain("Cannot transition"));
    }

    [Test]
    public void Should_Allow_Draft_To_InEnrichment()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.Draft,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.InEnrichment);

        // Assert
        Assert.That(product.StatusId, Is.EqualTo((int)ProductLifecycleStatus.InEnrichment));
    }

    [Test]
    public void Should_Allow_InEnrichment_To_ReadyForReview()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.InEnrichment,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.ReadyForReview);

        // Assert
        Assert.That(product.StatusId, Is.EqualTo((int)ProductLifecycleStatus.ReadyForReview));
    }

    [Test]
    public void Should_Allow_ReadyForReview_To_Approved()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.ReadyForReview,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.Approved);

        // Assert
        Assert.That(product.StatusId, Is.EqualTo((int)ProductLifecycleStatus.Approved));
    }

    [Test]
    public void Should_Allow_Approved_To_Published()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.Approved,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            MediaAssets = new List<MediaAsset>
            {
                new MediaAsset { IsPrimary = true, FileUrl = "test.jpg" }
            }
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.Published);

        // Assert
        Assert.That(product.StatusId, Is.EqualTo((int)ProductLifecycleStatus.Published));
    }

    [Test]
    public void Should_Allow_ReadyForReview_To_Rejected()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.ReadyForReview,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.Rejected);

        // Assert
        Assert.That(product.StatusId, Is.EqualTo((int)ProductLifecycleStatus.Rejected));
    }

    [Test]
    public void Should_Not_Allow_Draft_To_Published()
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
        Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Published));
    }

    [Test]
    public void Should_Not_Allow_Published_To_Draft()
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
        Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Draft));
    }

    [Test]
    public void Should_Throw_When_Transitioning_To_Same_Status()
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
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Draft));

        Assert.That(ex!.Message, Does.Contain("already in"));
    }

    // ── VALIDATION RULE TESTS ────────────────────────────────────────────────

    [Test]
    public void Should_Require_Name_For_ReadyForReview()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.InEnrichment,
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.ReadyForReview));

        Assert.That(ex!.Message, Does.Contain("name"));
    }

    [Test]
    public void Should_Require_SKU_For_ReadyForReview()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.InEnrichment,
            Name = "Test Product",
            SKU = "",
            CategoryId = 1
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.ReadyForReview));

        Assert.That(ex!.Message, Does.Contain("SKU"));
    }

    [Test]
    public void Should_Require_Category_For_ReadyForReview()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.InEnrichment,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 0
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.ReadyForReview));

        Assert.That(ex!.Message, Does.Contain("category"));
    }

    [Test]
    public void Should_Only_Allow_Approved_From_ReadyForReview()
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
        Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Approved));
    }

    [Test]
    public void Should_Only_Allow_Published_From_Approved()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.ReadyForReview,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Published));
    }

    [Test]
    public void Should_Only_Allow_Rejected_From_ReadyForReview()
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
        Assert.Throws<InvalidOperationException>(() =>
            _service.TransitionTo(product, ProductLifecycleStatus.Rejected));
    }

    // ── HELPER METHOD TESTS ──────────────────────────────────────────────────

    [Test]
    public void CanTransitionTo_Should_Return_True_For_Valid_Transition()
    {
        // Act
        var canTransition = _service.CanTransitionTo(
            ProductLifecycleStatus.Draft,
            ProductLifecycleStatus.InEnrichment);

        // Assert
        Assert.That(canTransition, Is.True);
    }

    [Test]
    public void CanTransitionTo_Should_Return_False_For_Invalid_Transition()
    {
        // Act
        var canTransition = _service.CanTransitionTo(
            ProductLifecycleStatus.Draft,
            ProductLifecycleStatus.Published);

        // Assert
        Assert.That(canTransition, Is.False);
    }

    [Test]
    public void GetValidNextStates_Should_Return_Correct_States_For_Draft()
    {
        // Act
        var validStates = _service.GetValidNextStates(ProductLifecycleStatus.Draft);

        // Assert
        Assert.That(validStates, Contains.Item(ProductLifecycleStatus.InEnrichment));
        Assert.That(validStates, Contains.Item(ProductLifecycleStatus.Archived));
        Assert.That(validStates.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetValidNextStates_Should_Return_Empty_For_Archived()
    {
        // Act
        var validStates = _service.GetValidNextStates(ProductLifecycleStatus.Archived);

        // Assert
        Assert.That(validStates, Is.Empty);
    }

    [Test]
    public void CanEdit_Should_Return_True_For_Draft()
    {
        // Act
        var canEdit = _service.CanEdit(ProductLifecycleStatus.Draft);

        // Assert
        Assert.That(canEdit, Is.True);
    }

    [Test]
    public void CanEdit_Should_Return_False_For_Published()
    {
        // Act
        var canEdit = _service.CanEdit(ProductLifecycleStatus.Published);

        // Assert
        Assert.That(canEdit, Is.False);
    }

    [Test]
    public void CanDelete_Should_Return_True_For_Draft()
    {
        // Act
        var canDelete = _service.CanDelete(ProductLifecycleStatus.Draft);

        // Assert
        Assert.That(canDelete, Is.True);
    }

    [Test]
    public void CanDelete_Should_Return_False_For_Published()
    {
        // Act
        var canDelete = _service.CanDelete(ProductLifecycleStatus.Published);

        // Assert
        Assert.That(canDelete, Is.False);
    }

    [Test]
    public void TransitionTo_Should_Update_UpdatedAt_Timestamp()
    {
        // Arrange
        var product = new Product
        {
            StatusId = (int)ProductLifecycleStatus.Draft,
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1,
            UpdatedAt = null
        };

        // Act
        _service.TransitionTo(product, ProductLifecycleStatus.InEnrichment);

        // Assert
        Assert.That(product.UpdatedAt, Is.Not.Null);
        Assert.That(product.UpdatedAt!.Value, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
    }
}
