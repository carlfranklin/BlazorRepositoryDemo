using Microsoft.JSInterop;
using RepositoryDemo.Client;
using System.Reflection;

public class IndexedDBSyncRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    // injected
    IBlazorDbFactory _dbFactory;
    private readonly APIRepository<TEntity> _apiRepository;
    private readonly IJSRuntime _jsRuntime;
    string _dbName = "";
    string _primaryKeyName = "";
    bool _autoGenerateKey;

    IndexedDbManager manager;
    string storeName = "";
    string keyStoreName = "";
    Type entityType;
    PropertyInfo primaryKey;
    public bool IsOnline { get; set; } = true;

    public delegate void OnlineStatusEventHandler(object sender,
        OnlineStatusEventArgs e);
    public event OnlineStatusEventHandler OnlineStatusChanged;

    public IndexedDBSyncRepository(string dbName, string primaryKeyName,
        bool autoGenerateKey, IBlazorDbFactory dbFactory,
        APIRepository<TEntity> apiRepository, IJSRuntime jsRuntime)
    {
        _dbName = dbName;
        _dbFactory = dbFactory;
        _apiRepository = apiRepository;
        _jsRuntime = jsRuntime;
        _primaryKeyName = primaryKeyName;
        _autoGenerateKey = autoGenerateKey;

        entityType = typeof(TEntity);
        storeName = entityType.Name;
        keyStoreName = $"{storeName}{Globals.KeysSuffix}";
        primaryKey = entityType.GetProperty(primaryKeyName);

        _ = _jsRuntime.InvokeVoidAsync("connectivity.initialize",
            DotNetObjectReference.Create(this));
    }

    public string LocalStoreName
    {
        get { return $"{storeName}{Globals.LocalTransactionsSuffix}"; }
    }

    [JSInvokable("ConnectivityChanged")]
    public async void OnConnectivityChanged(bool isOnline)
    {
        if (IsOnline != isOnline)
        {
            IsOnline = isOnline;
            OnlineStatusChanged?.Invoke(this,
                new OnlineStatusEventArgs { IsOnline = false });
        }

        if (IsOnline)
        {
            await SyncLocalToServer();
            OnlineStatusChanged?.Invoke(this,
                new OnlineStatusEventArgs { IsOnline = true });
        }
    }

    private async Task EnsureManager()
    {
        if (manager == null)
        {
            manager = await _dbFactory.GetDbManager(_dbName);
            await manager.OpenDb();
        }
    }
    public async Task DeleteAllAsync()
    {
        if (IsOnline)
            await _apiRepository.DeleteAllAsync();

        await DeleteAllOfflineAsync();
    }

    private async Task DeleteAllOfflineAsync()
    {
        await EnsureManager();

        // clear the keys table
        await manager.ClearTableAsync(keyStoreName);

        // clear the data table
        await manager.ClearTableAsync(storeName);

        RecordDeleteAllAsync();
    }

    public async void RecordDeleteAllAsync()
    {
        if (IsOnline) return;

        var action = LocalTransactionTypes.DeleteAll;
        var record = new StoreRecord<LocalTransaction<TEntity>>()
        {
            StoreName = LocalStoreName,
            Record = new LocalTransaction<TEntity>
            {
                Entity = null,
                Action = action,
                ActionName = action.ToString()
            }
        };

        await manager.AddRecordAsync(record);
    }

    public async Task<bool> DeleteAsync(TEntity EntityToDelete)
    {
        bool deleted = false;

        if (IsOnline)
        {
            deleted = await _apiRepository.DeleteAsync(EntityToDelete);
            await DeleteOfflineAsync(EntityToDelete);
        }
        else
        {
            deleted = await DeleteOfflineAsync(EntityToDelete);
        }

        return deleted;
    }

    public async Task<bool> DeleteOfflineAsync(TEntity EntityToDelete)
    {
        await EnsureManager();
        var Id = primaryKey.GetValue(EntityToDelete);
        return await DeleteByIdAsync(Id);
    }

    public async Task<bool> DeleteByIdAsync(object Id)
    {
        bool deleted = false;

        if (IsOnline)
        {
            deleted = await _apiRepository.DeleteByIdAsync(Id);
            await DeleteByIdOfflineAsync(Id);
        }
        else
        {
            deleted = await DeleteByIdOfflineAsync(Id);
        }

        return deleted;
    }

    public async Task<bool> DeleteByIdOfflineAsync(object Id)
    {
        await EnsureManager();
        try
        {
            RecordDeleteByIdAsync(Id);
            await manager.DeleteRecordAsync(storeName, Id);
            return true;
        }
        catch (Exception ex)
        {
            // log exception
            return false;
        }
    }

    public async void RecordDeleteByIdAsync(object id)
    {
        if (IsOnline) return;
        var action = LocalTransactionTypes.Delete;

        var entity = await GetByIdAsync(id);

        var record = new StoreRecord<LocalTransaction<TEntity>>()
        {
            StoreName = LocalStoreName,
            Record = new LocalTransaction<TEntity>
            {
                Entity = entity,
                Action = action,
                ActionName = action.ToString(),
                Id = int.Parse(id.ToString())
            }
        };

        await manager.AddRecordAsync(record);
    }

    /// <summary>
    /// just to satisfy the contract
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await GetAllAsync(false);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(bool dontSync = false)
    {
        if (IsOnline)
        {
            // retrieve all the data
            var list = await _apiRepository.GetAllAsync();
            if (list != null)
            {
                var allData = list.ToList();
                if (!dontSync)
                {
                    // clear the local db
                    await DeleteAllOfflineAsync();
                    // write the values into IndexedDB
                    var result = await manager.BulkAddRecordAsync<TEntity>
                        (storeName, allData);
                    // get all the local data
                    var localList = await GetAllOfflineAsync();
                    var localData = (localList).ToList();
                    // record the primary keys
                    var keys = new List<OnlineOfflineKey>();
                    for (int i = 0; i < allData.Count(); i++)
                    {
                        var key = new OnlineOfflineKey()
                        {
                            OnlineId = primaryKey.GetValue(allData[i]),
                            LocalId = primaryKey.GetValue(localData[i]),
                        };
                        keys.Add(key);
                    };
                    // remove all the keys
                    await manager.ClearTableAsync(keyStoreName);
                    // store all of the keys
                    result = await manager.BulkAddRecordAsync<OnlineOfflineKey>
                        (keyStoreName, keys);
                }
                // return the data
                return allData;
            }
            else
                return null;
        }
        else
            return await GetAllOfflineAsync();
    }

    public async Task<IEnumerable<TEntity>> GetAllOfflineAsync()
    {
        await EnsureManager();
        var array = await manager.ToArray<TEntity>(storeName);
        if (array == null)
            return new List<TEntity>();
        else
            return array.ToList();
    }

    public async Task<IEnumerable<TEntity>> GetAsync(QueryFilter<TEntity> Filter)
    {
        // We have to load all items and use LINQ to filter them. :(
        var allitems = await GetAllAsync(true);
        return Filter.GetFilteredList(allitems);
    }

    public async Task<TEntity> GetByIdAsync(object Id)
    {
        if (IsOnline)
            return await _apiRepository.GetByIdAsync(Id);
        else
            return await GetByIdOfflineAsync(Id);
    }

    public async Task<TEntity> GetByIdOfflineAsync(object Id)
    {
        await EnsureManager();
        var items = await manager.Where<TEntity>(storeName, _primaryKeyName, Id);
        if (items.Any())
            return items.First();
        else
            return null;
    }

    public async Task<TEntity> InsertAsync(TEntity Entity)
    {
        TEntity returnValue;

        if (IsOnline)
        {
            returnValue = await _apiRepository.InsertAsync(Entity);
            await InsertOfflineAsync(Entity);
        }
        else
        {
            returnValue = await InsertOfflineAsync(Entity);
        }
        return returnValue;

    }

    public async Task<TEntity> InsertOfflineAsync(TEntity Entity)
    {
        await EnsureManager();

        try
        {
            var record = new StoreRecord<TEntity>()
            {
                StoreName = storeName,
                Record = Entity
            };
            var entity = await manager.AddRecordAsync<TEntity>(record);

            var allItems = await GetAllAsync();
            var last = allItems.Last();

            RecordInsertAsync(last);

            return last;
        }
        catch (Exception ex)
        {
            // log exception
            return null;
        }
    }

    public async void RecordInsertAsync(TEntity Entity)
    {
        if (IsOnline) return;
        try
        {
            var action = LocalTransactionTypes.Insert;

            var record = new StoreRecord<LocalTransaction<TEntity>>()
            {
                StoreName = LocalStoreName,
                Record = new LocalTransaction<TEntity>
                {
                    Entity = Entity,
                    Action = action,
                    ActionName = action.ToString()
                }
            };

            await manager.AddRecordAsync(record);
        }
        catch (Exception ex)
        {
            // log exception
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity EntityToUpdate)
    {
        TEntity returnValue;

        if (IsOnline)
        {
            await UpdateOfflineAsync(EntityToUpdate);
            returnValue = await _apiRepository.UpdateAsync(EntityToUpdate);
        }
        else
        {
            returnValue = await UpdateOfflineAsync(EntityToUpdate);
        }
        return returnValue;
    }

    public async Task<TEntity> UpdateOfflineAsync(TEntity EntityToUpdate)
    {
        await EnsureManager();
        object Id = primaryKey.GetValue(EntityToUpdate);
        try
        {
            await manager.UpdateRecord(new UpdateRecord<TEntity>()
            {
                StoreName = storeName,
                Record = EntityToUpdate,
                Key = Id
            });

            RecordUpdateAsync(EntityToUpdate);

            return EntityToUpdate;
        }
        catch (Exception ex)
        {
            // log exception
            return null;
        }
    }

    public async void RecordUpdateAsync(TEntity Entity)
    {
        if (IsOnline) return;
        try
        {
            var action = LocalTransactionTypes.Update;

            var record = new StoreRecord<LocalTransaction<TEntity>>()
            {
                StoreName = LocalStoreName,
                Record = new LocalTransaction<TEntity>
                {
                    Entity = Entity,
                    Action = action,
                    ActionName = action.ToString()
                }
            };

            await manager.AddRecordAsync(record);
        }
        catch (Exception ex)
        {
            // log exception
        }
    }

    private async Task<List<OnlineOfflineKey>> GetKeys()
    {
        await EnsureManager();
        var returnList = new List<OnlineOfflineKey>();

        var array = await manager.ToArray<OnlineOfflineKey>(keyStoreName);
        if (array == null) return null;

        foreach (var key in array)
        {
            var onlineId = key.OnlineId;
            key.OnlineId = JsonConvert.DeserializeObject<object>(onlineId.ToString());

            var localId = key.LocalId;
            key.LocalId = JsonConvert.DeserializeObject<object>(localId.ToString());

            returnList.Add(key);
        }

        return returnList;
    }

    private async Task<TEntity> UpdateKeyFromLocal(TEntity Entity)
    {
        var LocalId = primaryKey.GetValue(Entity);
        LocalId = JsonConvert.DeserializeObject<object>(LocalId.ToString());

        var keys = await GetKeys();
        if (keys == null) return null;

        var item = (from x in keys
                    where x.LocalId.ToString() == LocalId.ToString()
                    select x).FirstOrDefault();

        if (item == null) return null;

        var key = item.OnlineId;

        var typeName = key.GetType().Name;

        if (typeName == nameof(Int64))
        {
            if (primaryKey.PropertyType.Name == nameof(Int32))
                key = Convert.ToInt32(key);
        }
        else if (typeName == "string")
        {
            if (primaryKey.PropertyType.Name != "string")
                key = key.ToString();
        }

        primaryKey.SetValue(Entity, key);

        return Entity;
    }

    public async Task<bool> SyncLocalToServer()
    {
        if (!IsOnline) return false;

        await EnsureManager();

        var array = await manager.ToArray<LocalTransaction<TEntity>>(LocalStoreName);
        if (array == null || array.Count == 0)
            return true;
        else
        {
            foreach (var localTransaction in array.ToList())
            {
                try
                {
                    switch (localTransaction.Action)
                    {
                        case LocalTransactionTypes.Insert:
                            var insertedEntity = await
                                _apiRepository.InsertAsync(localTransaction.Entity);
                            // update the keys table
                            var key = new OnlineOfflineKey()
                            {
                                OnlineId = primaryKey.GetValue(insertedEntity),
                                LocalId = primaryKey.GetValue(localTransaction.Entity),
                            };
                            await manager.AddRecordAsync<OnlineOfflineKey>
                                (new StoreRecord<OnlineOfflineKey>
                                {
                                    StoreName = keyStoreName,
                                    Record = key
                                });
                            break;

                        case LocalTransactionTypes.Update:
                            localTransaction.Entity = await UpdateKeyFromLocal
                                (localTransaction.Entity);
                            await _apiRepository.UpdateAsync(localTransaction.Entity);
                            break;

                        case LocalTransactionTypes.Delete:
                            localTransaction.Entity = await UpdateKeyFromLocal
                                (localTransaction.Entity);
                            await _apiRepository.DeleteAsync(localTransaction.Entity);
                            break;

                        case LocalTransactionTypes.DeleteAll:
                            await _apiRepository.DeleteAllAsync();
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                }
            }

            await DeleteAllTransactionsAsync();
            return true;
        }
    }

    private async Task DeleteAllTransactionsAsync()
    {
        await EnsureManager();
        await manager.ClearTableAsync(LocalStoreName);
    }

    public async Task<bool> UpdateOfflineIds(TEntity onlineEntity, TEntity offlineEntity)
    {
        await EnsureManager();

        object Id = primaryKey.GetValue(offlineEntity);

        var array = await manager.ToArray<LocalTransaction<TEntity>>(LocalStoreName);
        if (array == null)
            return false;
        else
        {
            var items = array.Where(i => i.Entity != null).ToList();

            foreach (var item in items)
            {
                var updatedEntity = await UpdateOfflineAsync(item, onlineEntity);
            }
        }

        return true;
    }

    public async Task<LocalTransaction<TEntity>>
        UpdateOfflineAsync(LocalTransaction<TEntity> entityToUpdate,
        TEntity onlineEntity)
    {
        await EnsureManager();

        object Id = primaryKey.GetValue(entityToUpdate.Entity);

        entityToUpdate.Entity = onlineEntity;

        try
        {
            await manager.UpdateRecord(new UpdateRecord<LocalTransaction<TEntity>>()
            {
                StoreName = LocalStoreName,
                Record = entityToUpdate,
                Key = Id
            });

            return entityToUpdate;
        }
        catch (Exception ex)
        {
            // log exception
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("connectivity.dispose");
    }
}