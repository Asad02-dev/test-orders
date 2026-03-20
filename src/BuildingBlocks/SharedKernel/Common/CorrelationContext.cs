namespace SharedKernel.Common;

public class CorrelationContext
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? TraceId { get; set; }
}
