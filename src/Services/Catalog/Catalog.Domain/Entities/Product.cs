using SharedKernel.Domain;

namespace Catalog.Domain.Entities;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string Sku { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string ImageUrl { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { } // EF Core

    public static Product Create(
        string name,
        string description,
        decimal price,
        string sku,
        string category,
        string imageUrl = "",
        string currency = "USD")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Price must be non-negative.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            Sku = sku,
            Category = category,
            ImageUrl = imageUrl,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
        };
        return product;
    }

    public void UpdateDetails(string name, string description, decimal price, string category, string imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

        Name = name;
        Description = description;
        Price = price;
        Category = category;
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
