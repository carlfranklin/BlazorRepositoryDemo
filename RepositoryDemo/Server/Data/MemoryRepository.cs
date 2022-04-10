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

    public async Task<bool> Delete(TEntity EntityToDelete)
    {
        if (EntityToDelete == null) return false;


        await Task.Delay(0);
        try
        {
            if (Data.Contains(EntityToDelete))
            {
                lock (Data)
                {
                    Data.Remove(EntityToDelete);
                }
                return true;
            }
        }
        catch { }
        return false;
    }

    public async Task<bool> Delete(object Id)
    {
        try
        {
            var EntityToDelete = await GetById(Id);
            return await Delete(EntityToDelete);


        }
        catch { }
        return false;
    }

    public async Task DeleteAll()
    {
        await Task.Delay(0);
        lock (Data)
        {
            Data.Clear();
        }
    }

    public async Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> JsonExpression)
    {
        
        var allitems = (await GetAll()).ToList();
        return await JsonExpression.GetFilteredList(allitems);
    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        await Task.Delay(0);
        return Data;
    }

    public async Task<TEntity> GetById(object Id)
    {
        await Task.Delay(0);
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
        return entity;
    }


    public async Task<TEntity> Insert(TEntity Entity)
    {
        await Task.Delay(0);
        if (Entity == null) return default(TEntity);
        try
        {
            lock (Data)
            {
                Data.Add(Entity);
            }
            return Entity;
        }
        catch { }
        return default(TEntity);
    }


    public async Task<TEntity> Update(TEntity EntityToUpdate)
    {
        await Task.Delay(0);
        if (EntityToUpdate == null) return default(TEntity);
        if (IdProperty == null) return default(TEntity);
        try
        {
            var id = IdProperty.GetValue(EntityToUpdate);
            var entity = await GetById(id);
            if (entity != null)
            {
                lock (Data)
                {
                    var index = Data.IndexOf(entity);
                    Data[index] = EntityToUpdate;
                }
                return EntityToUpdate;
            }
            else
                return default(TEntity);
        }
        catch { }
        return default(TEntity);
    }
}