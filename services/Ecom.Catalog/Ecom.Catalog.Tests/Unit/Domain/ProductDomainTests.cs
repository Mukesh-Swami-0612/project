using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Enums;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit.Domain;

[TestFixture]
public class ProductDomainTests
{
    [Test]
    public void GetLifecycleStatus_Should_Return_Correct_Enum()
    {
        // Arrange
        var product = new Product { StatusId = 1 };

        // Act
        var status = product.GetLifecycleStatus();

        // Assert
        Assert.That(status, Is.EqualTo(ProductLifecycleStatus.Draft));
    }

    [Test]
    public void IsEditable_Should_Return_True_When_Draft()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Draft };

        // Act
        var isEditable = product.IsEditable();

        // Assert
        Assert.That(isEditable, Is.True);
    }

    [Test]
    public void IsEditable_Should_Return_True_When_InEnrichment()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.InEnrichment };

        // Act
        var isEditable = product.IsEditable();

        // Assert
        Assert.That(isEditable, Is.True);
    }

    [Test]
    public void IsEditable_Should_Return_True_When_Rejected()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Rejected };

        // Act
        var isEditable = product.IsEditable();

        // Assert
        Assert.That(isEditable, Is.True);
    }

    [Test]
    public void IsEditable_Should_Return_False_When_Published()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Published };

        // Act
        var isEditable = product.IsEditable();

        // Assert
        Assert.That(isEditable, Is.False);
    }

    [Test]
    public void IsEditable_Should_Return_False_When_Approved()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Approved };

        // Act
        var isEditable = product.IsEditable();

        // Assert
        Assert.That(isEditable, Is.False);
    }

    [Test]
    public void IsDeletable_Should_Return_True_When_Draft()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Draft };

        // Act
        var isDeletable = product.IsDeletable();

        // Assert
        Assert.That(isDeletable, Is.True);
    }

    [Test]
    public void IsDeletable_Should_Return_True_When_Rejected()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Rejected };

        // Act
        var isDeletable = product.IsDeletable();

        // Assert
        Assert.That(isDeletable, Is.True);
    }

    [Test]
    public void IsDeletable_Should_Return_True_When_Archived()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Archived };

        // Act
        var isDeletable = product.IsDeletable();

        // Assert
        Assert.That(isDeletable, Is.True);
    }

    [Test]
    public void IsDeletable_Should_Return_False_When_Published()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Published };

        // Act
        var isDeletable = product.IsDeletable();

        // Assert
        Assert.That(isDeletable, Is.False);
    }

    [Test]
    public void IsPublished_Should_Return_True_When_Published()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Published };

        // Act
        var isPublished = product.IsPublished();

        // Assert
        Assert.That(isPublished, Is.True);
    }

    [Test]
    public void IsPublished_Should_Return_False_When_Not_Published()
    {
        // Arrange
        var product = new Product { StatusId = (int)ProductLifecycleStatus.Draft };

        // Act
        var isPublished = product.IsPublished();

        // Assert
        Assert.That(isPublished, Is.False);
    }

    [Test]
    public void HasMinimumData_Should_Return_True_When_All_Required_Fields_Present()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        var hasMinimumData = product.HasMinimumData();

        // Assert
        Assert.That(hasMinimumData, Is.True);
    }

    [Test]
    public void HasMinimumData_Should_Return_False_When_Name_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "",
            SKU = "TEST-001",
            CategoryId = 1
        };

        // Act
        var hasMinimumData = product.HasMinimumData();

        // Assert
        Assert.That(hasMinimumData, Is.False);
    }

    [Test]
    public void HasMinimumData_Should_Return_False_When_SKU_Is_Empty()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "",
            CategoryId = 1
        };

        // Act
        var hasMinimumData = product.HasMinimumData();

        // Assert
        Assert.That(hasMinimumData, Is.False);
    }

    [Test]
    public void HasMinimumData_Should_Return_False_When_CategoryId_Is_Zero()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            SKU = "TEST-001",
            CategoryId = 0
        };

        // Act
        var hasMinimumData = product.HasMinimumData();

        // Assert
        Assert.That(hasMinimumData, Is.False);
    }

    [Test]
    public void Product_Should_Default_To_Draft_Status()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        Assert.That(product.StatusId, Is.EqualTo(1));
        Assert.That(product.GetLifecycleStatus(), Is.EqualTo(ProductLifecycleStatus.Draft));
    }

    [Test]
    public void Product_Should_Default_IsDeleted_To_False()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        Assert.That(product.IsDeleted, Is.False);
    }

    [Test]
    public void Product_Should_Set_CreatedAt_To_UtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var product = new Product();

        // Assert
        var afterCreation = DateTime.UtcNow.AddSeconds(1);
        Assert.That(product.CreatedAt, Is.GreaterThan(beforeCreation));
        Assert.That(product.CreatedAt, Is.LessThan(afterCreation));
    }
}
