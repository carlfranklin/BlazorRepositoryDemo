public class EFRepository<TEntity, TDataContext> : IRepository<TEntity>
  where TEntity : class
  where TDataContext : DbContext
{
    protected readonly TDataContext context;
    internal DbSet<TEntity> dbSet;

    public EFRepository(TDataContext dataContext)
    {
        context = dataContext;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        dbSet = context.Set<TEntity>();
    }
    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Task.FromResult(dbSet);
    }

    public async Task<TEntity> GetByIdAsync(object Id)
    {
        return await dbSet.FindAsync(Id);
    }

    public async Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Filter)
    {
        var allitems = (await GetAllAsync()).ToList();
        return await Task.FromResult(Filter.GetFilteredList(allitems));
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        await dbSet.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entityToUpdate)
    {
        var dbSet = context.Set<TEntity>();
        dbSet.Attach(entityToUpdate);
        context.Entry(entityToUpdate).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return entityToUpdate;
    }

    public async Task<bool> DeleteAsync(TEntity entityToDelete)
    {
        if (context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
        return await context.SaveChangesAsync() >= 1;
    }

    public async Task<bool> DeleteByIdAsync(object id)
    {
        TEntity entityToDelete = await dbSet.FindAsync(id);
        return await DeleteAsync(entityToDelete);
    }

    public async Task DeleteAllAsync()
    {
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Customer");
    }

    
}