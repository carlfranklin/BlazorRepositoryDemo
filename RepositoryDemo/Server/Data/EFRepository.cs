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
    public async Task<IEnumerable<TEntity>> GetAll()
    {
        await Task.Delay(0);
        return dbSet;
    }

    public async Task<TEntity> GetById(object Id)
    {
        return await dbSet.FindAsync(Id);
    }

    public async Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> Filter)
    {
        var allitems = (await GetAll()).ToList();
        return await Filter.GetFilteredList(allitems);
    }

    public async Task<TEntity> Insert(TEntity entity)
    {
        await dbSet.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<TEntity> Update(TEntity entityToUpdate)
    {
        var dbSet = context.Set<TEntity>();
        dbSet.Attach(entityToUpdate);
        context.Entry(entityToUpdate).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return entityToUpdate;
    }

    public async Task<bool> Delete(TEntity entityToDelete)
    {
        if (context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
        return await context.SaveChangesAsync() >= 1;
    }

    public async Task<bool> Delete(object id)
    {
        TEntity entityToDelete = await dbSet.FindAsync(id);
        return await Delete(entityToDelete);
    }

    public async Task DeleteAll()
    {
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Customer");
    }

    
}