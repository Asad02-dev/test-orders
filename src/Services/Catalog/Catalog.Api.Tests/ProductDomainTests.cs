using Catalog.Domain.Entities;

namespace Catalog.Api.Tests;

public class ProductDomainTests
{
    [Fact]
    public void Product_Create_WithValidData_CreatesProduct()
    {
        // Arrange & Act
        var product = Product.Create(
            name: "Test Product",
            description: "Test Description",
            price: 99.99m,
            sku: "TEST-SKU",
            category: "Electronics",
            imageUrl: "https://example.com/image.jpg",
            currency: "USD"
        );

        // Assert
        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal(99.99m, product.Price);
        Assert.Equal("TEST-SKU", product.Sku);
        Assert.True(product.IsActive);
        Assert.Equal("USD", product.Currency);
    }

    [Fact]
    public void Product_Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            Product.Create("", "Description", 10.00m, "SKU", "Category")
        );
    }

    [Fact]
    public void Product_Create_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Product.Create("Product", "Description", -10.00m, "SKU", "Category")
        );
    }

    [Fact]
    public void Product_UpdateDetails_WithValidData_UpdatesProduct()
    {
        // Arrange
        var product = Product.Create("Original", "Original Desc", 100.00m, "SKU", "Cat");
        var originalVersion = product.Version;

        // Act
        product.UpdateDetails("Updated", "Updated Desc", 150.00m, "New Cat", "new-image.jpg");

        // Assert
        Assert.Equal("Updated", product.Name);
        Assert.Equal("Updated Desc", product.Description);
        Assert.Equal(150.00m, product.Price);
        Assert.Equal("New Cat", product.Category);
        Assert.NotNull(product.UpdatedAt);
        Assert.Equal(originalVersion + 1, product.Version);
    }

    [Fact]
    public void Product_Deactivate_DeactivatesProduct()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 10.00m, "SKU", "Category");
        Assert.True(product.IsActive);
        var originalVersion = product.Version;

        // Act
        product.Deactivate();

        // Assert
        Assert.False(product.IsActive);
        Assert.NotNull(product.UpdatedAt);
        Assert.Equal(originalVersion + 1, product.Version);
    }

    [Fact]
    public void Product_Activate_ActivatesProduct()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 10.00m, "SKU", "Category");
        product.Deactivate();
        Assert.False(product.IsActive);
        var originalVersion = product.Version;

        // Act
        product.Activate();

        // Assert
        Assert.True(product.IsActive);
        Assert.NotNull(product.UpdatedAt);
        Assert.Equal(originalVersion + 1, product.Version);
    }

    [Fact]
    public void Product_UpdateDetails_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var product = Product.Create("Product", "Description", 10.00m, "SKU", "Category");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            product.UpdateDetails("Updated", "Updated Desc", -50.00m, "Category", "")
        );
    }
}
