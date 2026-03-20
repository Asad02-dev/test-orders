namespace Catalog.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string Sku,
    string Category,
    string ImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Sku,
    string Category,
    string ImageUrl = "",
    string Currency = "USD"
);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    string Category,
    string ImageUrl
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
};
