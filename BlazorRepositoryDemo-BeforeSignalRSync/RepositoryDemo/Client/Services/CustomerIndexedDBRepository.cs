public class CustomerIndexedDBRepository : IndexedDBRepository<Customer>
{
    public CustomerIndexedDBRepository(IBlazorDbFactory dbFactory)
        : base("RepositoryDemo", "Id", true, dbFactory)
    {
    }
}