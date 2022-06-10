using RepositoryDemo.Client;

public class LocalTransaction<TEntity>
{
    public TEntity Entity { get; set; }
    public LocalTransactionTypes Action { get; set; }
    public string ActionName { get; set; }
    public object Id { get; set; }
}