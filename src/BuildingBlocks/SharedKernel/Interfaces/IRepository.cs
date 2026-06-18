using SharedKernel.Domain;

namespace SharedKernel.Interfaces;

public interface IRepository<TEntity, TId> where TEntity : AggregateRoot<TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
