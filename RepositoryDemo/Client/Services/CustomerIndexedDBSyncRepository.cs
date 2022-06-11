
public class CustomerIndexedDBSyncRepository : IndexedDBSyncRepository<Customer>
{
    public CustomerIndexedDBSyncRepository(IBlazorDbFactory dbFactory,
                                            CustomerRepository customerRepository,
                                            IJSRuntime jsRuntime,
                                            HttpClient httpClient)
        : base("RepositoryDemo",
            "Id",
            true,
            dbFactory,
            customerRepository,
            jsRuntime,
            httpClient) {  }
}