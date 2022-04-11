#nullable disable
namespace AvnRepository;
/// <summary>
/// Generic repository interface that uses
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<bool> DeleteAsync(TEntity EntityToDelete);
    Task<bool> DeleteByIdAsync(object Id);
    Task DeleteAllAsync(); // Be Careful!!!
    Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Filter);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> GetByIdAsync(object Id);
    Task<TEntity> InsertAsync(TEntity Entity);
    Task<TEntity> UpdateAsync(TEntity EntityToUpdate);
}