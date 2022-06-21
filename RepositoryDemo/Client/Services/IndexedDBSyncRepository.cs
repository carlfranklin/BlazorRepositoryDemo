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
    HttpClient _httpClient;

    protected HubConnection hubConnection;
    IndexedDbManager manager;
    string storeName = "";
    string keyStoreName = "";
    Type entityType;
    PropertyInfo primaryKey;
    public bool IsOnline { get; set; } = true;

    public delegate void OnlineStatusEventHandler(object sender,
        OnlineStatusEventArgs e);
    public event OnlineStatusEventHandler OnlineStatusChanged;

    public delegate void DataChangedEventHandler(object sender,
        DataChangedEventArgs e);
    public event DataChangedEventHandler DataChanged;

    public IndexedDBSyncRepository(string dbName,
        string primaryKeyName,
        bool autoGenerateKey,
        IBlazorDbFactory dbFactory,
        APIRepository<TEntity> apiRepository,
        IJSRuntime jsRuntime,
        HttpClient httpClient)
    {
        _dbName = dbName;
        _dbFactory = dbFactory;
        _apiRepository = apiRepository;
        _jsRuntime = jsRuntime;
        _primaryKeyName = primaryKeyName;
        _autoGenerateKey = autoGenerateKey;
        _httpClient = httpClient;

        entityType = typeof(TEntity);
        storeName = entityType.Name;
        keyStoreName = $"{storeName}{Globals.KeysSuffix}";
        primaryKey = entityType.GetProperty(primaryKeyName);

        _jsRuntime.InvokeVoidAsync("connectivity.initialize",
            DotNetObjectReference.Create(this));

        hubConnection = new HubConnectionBuilder()
           .WithUrl($"{_httpClient.BaseAddress}DataSyncHub")
           .Build();

        Task.Run(async () => await AsyncConstructor());

    }

    async Task AsyncConstructor()
    {
        hubConnection.On<string, string, string>("ReceiveSyncRecord", async (Table, Action, Id) =>
        {
            // SignalR may not be the BEST way to send and receive messages.
            // If this were a production system, I would use a cloud-based queue or messaging system,
            // but SignalR makes for a good simple demonstration of how to keep client side data in sync.

            await EnsureManager();

            // only interested in our table
            if (Table == storeName)
            {
                if (Action == "insert")
                {
                    // an item was inserted
                    // fetch it
                    var item = await _apiRepository.GetByIdAsync(Id);
                    if (item != null)
                    {
                        // add to the local database
                        var localItem = await InsertOfflineAsync(item);
                    }
                }
                else if (Action == "update")
                {
                    // an item was updated
                    // update the item in the local database
                    var item = await _apiRepository.GetByIdAsync(Id);
                    if (item != null)
                    {
                        var localItem = await UpdateKeyToLocal(item);
                        await UpdateOfflineAsync(localItem);
                    }
                }
                else if (Action == "delete")
                {
                    // an item was deleted
                    // delete the item in the local database
                    var localId = await GetLocalId(Id);
                    await DeleteByIdOfflineAsync(localId);
                }
                else if (Action == "delete-all")
                {
                    // clear local database
                    await DeleteAllOfflineAsync();
                }

                // raise DataChanged event
                var args = new DataChangedEventArgs(Table, Action, Id);
                DataChanged?.Invoke(this, args);
            }
        });

        if (IsOnline)
        {
            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {

            }
        }
    }

    public string LocalStoreName
    {
        get { return $"{storeName}{Globals.LocalTransactionsSuffix}"; }
    }

    [JSInvokable("ConnectivityChanged")]
    public async void OnConnectivityChanged(bool isOnline)
    {
        IsOnline = isOnline;

        if (!isOnline)
        {
            OnlineStatusChanged?.Invoke(this,
                new OnlineStatusEventArgs { IsOnline = false });
        }
        else
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
        {
            await _apiRepository.DeleteAllAsync();
            await hubConnection.InvokeAsync("SyncRecord", storeName, "delete-all", "");
        }

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
            var onlineId = primaryKey.GetValue(EntityToDelete);
            deleted = await _apiRepository.DeleteAsync(EntityToDelete);
            var localEntity = await UpdateKeyToLocal(EntityToDelete);
            await DeleteOfflineAsync(localEntity);
            await hubConnection.InvokeAsync("SyncRecord", storeName, "delete", onlineId.ToString());
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
        return await DeleteByIdOfflineAsync(Id);
    }

    public async Task<bool> DeleteByIdAsync(object Id)
    {
        bool deleted = false;

        if (IsOnline)
        {
            var localId = await GetLocalId(Id);
            await DeleteByIdOfflineAsync(localId);
            deleted = await _apiRepository.DeleteByIdAsync(Id);
            await hubConnection.InvokeAsync("SyncRecord", storeName, "delete", Id.ToString());
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
            var result = await manager.DeleteRecordAsync(storeName, Id);
            if (result.Failed) return false;

            if (IsOnline)
            {
                // delete key map only if we're online.
                var keys = await GetKeys();
                if (keys.Count > 0)
                {
                    var key = (from x in keys
                               where x.LocalId.ToString() == Id.ToString()
                               select x).FirstOrDefault();
                    if (key != null)
                        await manager.DeleteRecordAsync(keyStoreName, key.Id);
                }
            }

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
                        var localId = primaryKey.GetValue(localData[i]);
                        var key = new OnlineOfflineKey()
                        {
                            Id = Convert.ToInt32(localId),
                            OnlineId = primaryKey.GetValue(allData[i]),
                            LocalId = localId,
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
            var Id = primaryKey.GetValue(returnValue);
            await InsertOfflineAsync(returnValue);
            await hubConnection.InvokeAsync("SyncRecord", storeName, "insert", Id.ToString());
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
            var onlineId = primaryKey.GetValue(Entity);

            var record = new StoreRecord<TEntity>()
            {
                StoreName = storeName,
                Record = Entity
            };
            var result = await manager.AddRecordAsync<TEntity>(record);
            var allItems = await GetAllOfflineAsync();
            var last = allItems.Last();
            var localId = primaryKey.GetValue(last);

            // record in the keys database
            var key = new OnlineOfflineKey()
            {
                Id = Convert.ToInt32(localId),
                OnlineId = onlineId,
                LocalId = localId
            };
            var storeRecord = new StoreRecord<OnlineOfflineKey>
            {
                DbName = _dbName,
                StoreName = keyStoreName,
                Record = key
            };
            await manager.AddRecordAsync(storeRecord);

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
            returnValue = await _apiRepository.UpdateAsync(EntityToUpdate);
            var Id = primaryKey.GetValue(returnValue);
            returnValue = await UpdateKeyToLocal(returnValue);
            await UpdateOfflineAsync(returnValue);
            await hubConnection.InvokeAsync("SyncRecord", storeName, "update", onlineId.ToString());
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

    private async Task<object> GetLocalId(object OnlineId)
    {
        var keys = await GetKeys();
        var item = (from x in keys
                    where x.OnlineId.ToString() == OnlineId.ToString()
                    select x).FirstOrDefault();
        var localId = item.LocalId;
        localId = JsonConvert.DeserializeObject<object>(localId.ToString());
        return localId;
    }

    private async Task<object> GetOnlineId(object LocalId)
    {
        var keys = await GetKeys();
        var item = (from x in keys
                    where x.LocalId.ToString() == LocalId.ToString()
                    select x).FirstOrDefault();
        var onlineId = item.OnlineId;
        onlineId = JsonConvert.DeserializeObject<object>(onlineId.ToString());
        return onlineId;
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

    private async Task<TEntity> UpdateKeyToLocal(TEntity Entity)
    {
        var OnlineId = primaryKey.GetValue(Entity);
        OnlineId = JsonConvert.DeserializeObject<object>(OnlineId.ToString());

        var keys = await GetKeys();
        if (keys == null) return null;

        var item = (from x in keys
                    where x.OnlineId.ToString() == OnlineId.ToString()
                    select x).FirstOrDefault();

        if (item == null) return null;

        var key = item.LocalId;

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
                            var localId = primaryKey.GetValue(localTransaction.Entity);
                            var onlineId = primaryKey.GetValue(insertedEntity);
                            var key = new OnlineOfflineKey()
                            {
                                Id = Convert.ToInt32(localId),
                                OnlineId = onlineId,
                                LocalId = localId
                            };
                            await manager.AddRecordAsync<OnlineOfflineKey>
                                (new StoreRecord<OnlineOfflineKey>
                                {
                                    StoreName = keyStoreName,
                                    Record = key
                                });

                            // send a sync message 
                            await hubConnection.InvokeAsync("SyncRecord", storeName, "insert", onlineId.ToString());

                            break;

                        case LocalTransactionTypes.Update:
                            localTransaction.Entity = await UpdateKeyFromLocal
                                (localTransaction.Entity);
                            await _apiRepository.UpdateAsync(localTransaction.Entity);
                            onlineId = primaryKey.GetValue(localTransaction.Entity);
                            // send a sync message 
                            await hubConnection.InvokeAsync("SyncRecord", storeName, "update", onlineId.ToString());

                            break;

                        case LocalTransactionTypes.Delete:
                            localTransaction.Entity = await UpdateKeyFromLocal
                                (localTransaction.Entity);
                            onlineId = primaryKey.GetValue(localTransaction.Entity);
                            await _apiRepository.DeleteAsync(localTransaction.Entity);
                            // send a sync message 
                            await hubConnection.InvokeAsync("SyncRecord", storeName, "delete", onlineId.ToString());
                            break;

                        case LocalTransactionTypes.DeleteAll:
                            await _apiRepository.DeleteAllAsync();
                            // send a sync message 
                            await hubConnection.InvokeAsync("SyncRecord", storeName, "delete-all", "");
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

            // TODO: Get all new records since last online
            // Get last record id
            // ask for new records since that id was recorded in the database
            // may require a time stamp field in the data record (invasive!)

            return true;
        }
    }

    private async Task DeleteAllTransactionsAsync()
    {
        await EnsureManager();
        await manager.ClearTableAsync(LocalStoreName);
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