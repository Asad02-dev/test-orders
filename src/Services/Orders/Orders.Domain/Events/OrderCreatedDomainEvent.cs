using SharedKernel.Domain;

namespace Orders.Domain.Events;

public record OrderCreatedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    decimal TotalAmount
) : DomainEvent;
