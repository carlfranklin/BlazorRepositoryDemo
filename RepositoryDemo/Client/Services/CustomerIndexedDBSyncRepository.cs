using Microsoft.JSInterop;
public class CustomerIndexedDBSyncRepository : IndexedDBSyncRepository<Customer>
{
    public CustomerIndexedDBSyncRepository(IBlazorDbFactory dbFactory, CustomerRepository customerRepository, IJSRuntime jsRuntime)
        : base("RepositoryDemo", "Id", true, dbFactory, customerRepository, jsRuntime)
    {
    }
}