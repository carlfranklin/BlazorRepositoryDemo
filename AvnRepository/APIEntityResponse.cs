namespace AvnRepository;
public class APIEntityResponse<TEntity> where TEntity : class
{
    public bool Success { get; set; }
    public List<string> ErrorMessages { get; set; } = new List<string>();
    public TEntity Data { get; set; }
}