using Ecom.Catalog.Domain.Rules;
using NUnit.Framework;

namespace Ecom.Catalog.Tests.Unit;

[TestFixture]
public class SkuValidationTests
{
    [Test]
    public void IsSatisfiedBy_UniqueSku_ReturnsTrue()
    {
        var result = SkuUniquenessRule.IsSatisfiedBy("NEW-SKU", new[] { "OLD-SKU", "OTHER-SKU" });
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsSatisfiedBy_DuplicateSku_ReturnsFalse()
    {
        var result = SkuUniquenessRule.IsSatisfiedBy("OLD-SKU", new[] { "OLD-SKU", "OTHER-SKU" });
        Assert.That(result, Is.False);
    }
}
