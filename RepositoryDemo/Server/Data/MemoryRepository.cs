public class MemoryRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private List<TEntity> Data;
    private PropertyInfo IdProperty = null;
    private string IdPropertyName = "";
    public MemoryRepository(string idPropertyName)
    {
        IdPropertyName = idPropertyName;
        Data = new List<TEntity>();
        IdProperty = typeof(TEntity).GetProperty(idPropertyName);
    }

    public async Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Filter)
    {
        var allitems = (await GetAllAsync()).ToList();
        return await Task.FromResult(Filter.GetFilteredList(allitems));
    }
    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Task.FromResult(Data); 
    }

    public async Task<TEntity> GetByIdAsync(object Id)
    {
        if (IdProperty == null) return default(TEntity);
        TEntity entity = null;
        if (IdProperty.PropertyType.IsValueType)
        {
            entity = (from x in Data
                      where IdProperty.GetValue(x).ToString() == Id.ToString()
                      select x).FirstOrDefault();
        }
        else
        {
            entity = (from x in Data
                      where IdProperty.GetValue(x) == Id
                      select x).FirstOrDefault();
        }
        return await Task.FromResult(entity);
    }

    public async Task<TEntity> InsertAsync(TEntity Entity)
    {
        if (Entity == null) 
            return await Task.FromResult(default(TEntity));
        
        try
        {
            lock (Data)
            {
                Data.Add(Entity);
            }
            return await Task.FromResult(Entity);
        }
        catch { }
        return await Task.FromResult(default(TEntity));
    }

    public async Task<TEntity> UpdateAsync(TEntity EntityToUpdate)
    {
        if (EntityToUpdate == null)
            return await Task.FromResult(default(TEntity));
        if (IdProperty == null)
            return await Task.FromResult(default(TEntity));
        try
        {
            var id = IdProperty.GetValue(EntityToUpdate);
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                lock (Data)
                {
                    var index = Data.IndexOf(entity);
                    Data[index] = EntityToUpdate;
                }
                return await Task.FromResult(EntityToUpdate);
            }
            else
                return await Task.FromResult(default(TEntity));
        }
        catch { }
        return await Task.FromResult(default(TEntity));
    }

    public async Task<bool> DeleteAsync(TEntity EntityToDelete)
    {
        if (EntityToDelete == null)
            return await Task.FromResult(false);

        try
        {
            if (Data.Contains(EntityToDelete))
            {
                lock (Data)
                {
                    Data.Remove(EntityToDelete);
                }
                return await Task.FromResult(true);
            }
        }
        catch { }
        return await Task.FromResult(false);
    }

    public async Task<bool> DeleteByIdAsync(object Id)
    {
        try
        {
            var EntityToDelete = await GetByIdAsync(Id);
            return await DeleteAsync(EntityToDelete);
        }
        catch { }
        return false;
    }

    public async Task DeleteAllAsync()
    {
        await Task.Delay(0);
        lock (Data)
        {
            Data.Clear();
        }
    }
}