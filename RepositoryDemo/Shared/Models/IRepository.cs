using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
#nullable disable
/// <summary>
/// Generic repository interface that uses
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<bool> Delete(TEntity EntityToDelete);
    Task<bool> Delete(object Id);
    Task DeleteAll(); // Be Careful!!!
    Task<IEnumerable<TEntity>> Get(QueryFilter<TEntity> Filter);
    Task<IEnumerable<TEntity>> GetAll();
    Task<TEntity> GetById(object Id);
    Task<TEntity> Insert(TEntity Entity);
    Task<TEntity> Update(TEntity EntityToUpdate);
}

/// <summary>
/// A serializable filter. An alternative to trying to serialize and deserialize LINQ expressions,
/// which are very finicky. This class uses standard types. 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class QueryFilter<TEntity> where TEntity : class
{
    /// <summary>
    /// If you want to return a subset of the properties, you can specify only
    /// the properties that you want to retrieve in the SELECT clause.
    /// Leave empty to return all columns
    /// </summary>
    public List<string> IncludePropertyNames { get; set; } = new List<string>();

    /// <summary>
    /// Defines the property names and values in the WHERE clause
    /// </summary>
    public List<FilterProperty> FilterProperties { get; set; } = new List<FilterProperty>();
    
    /// <summary>
    /// Specify the property to ORDER BY, if any 
    /// </summary>
    public string OrderByPropertyName { get; set; } = "";
    
    /// <summary>
    /// Set to true if you want to order DESCENDING
    /// </summary>
    public bool OrderByDescending { get; set; } = false;

    /// <summary>
    /// A custome query that returns a list of entities with the current filter settings.
    /// </summary>
    /// <param name="AllItems"></param>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> GetFilteredList(List<TEntity> AllItems)
    {
        // Convert to IQueryable
        var query = AllItems.AsQueryable<TEntity>();

        // the expression will be used for each FilterProperty
        Expression<Func<TEntity, bool>> expression = null;

        // Process each property
        foreach (var filterProperty in FilterProperties)
        {
            // use reflection to get the property info
            PropertyInfo prop = typeof(TEntity).GetProperty(filterProperty.Name);

            // string
            if (prop.PropertyType == typeof(string))
            {
                if (filterProperty.Operator == FilterOperator.Equals)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower() == filterProperty.Value.ToString().ToLower();
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString() == filterProperty.Value.ToString();
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower() != filterProperty.Value.ToString().ToLower();
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString() != filterProperty.Value.ToString();
                else if (filterProperty.Operator == FilterOperator.StartsWith)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().StartsWith(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().StartsWith(filterProperty.Value.ToString());
                else if (filterProperty.Operator == FilterOperator.EndsWith)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().EndsWith(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().EndsWith(filterProperty.Value.ToString());
                else if (filterProperty.Operator == FilterOperator.Contains)
                    if (filterProperty.CaseSensitive == false)
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().ToLower().Contains(filterProperty.Value.ToString().ToLower());
                    else
                        expression = s => s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString().Contains(filterProperty.Value.ToString());
            }
            // int
            if (prop.PropertyType == typeof(int))
            {
                int value = Convert.ToInt32(filterProperty.Value);

                if (filterProperty.Operator == FilterOperator.Equals)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) == value;
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) != value;
                else if (filterProperty.Operator == FilterOperator.LessThan)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) < value;
                else if (filterProperty.Operator == FilterOperator.GreaterThan)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) > value;
                else if (filterProperty.Operator == FilterOperator.LessThanOrEqual)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) <= value;
                else if (filterProperty.Operator == FilterOperator.GreaterThanOrEqual)
                    expression = s => Convert.ToInt32(s.GetType().GetProperty(filterProperty.Name).GetValue(s)) >= value;
            }
            // datetime
            if (prop.PropertyType == typeof(DateTime))
            {
                DateTime value = DateTime.Parse(filterProperty.Value);

                if (filterProperty.Operator == FilterOperator.Equals)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) == value;
                else if (filterProperty.Operator == FilterOperator.NotEquals)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) != value;
                else if (filterProperty.Operator == FilterOperator.LessThan)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) < value;
                else if (filterProperty.Operator == FilterOperator.GreaterThan)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) > value;
                else if (filterProperty.Operator == FilterOperator.LessThanOrEqual)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) <= value;
                else if (filterProperty.Operator == FilterOperator.GreaterThanOrEqual)
                    expression = s => DateTime.Parse(s.GetType().GetProperty(filterProperty.Name).GetValue(s).ToString()) >= value;
            }
            // Add expression creation code for other data types here.

            // apply the expression
            query = query.Where(expression);

        }

        // Include the specified properties
        foreach (var includeProperty in IncludePropertyNames)
        {
            query = query.Include(includeProperty);
        }

        // order by
        if (OrderByPropertyName != "")
        {
            PropertyInfo prop = typeof(TEntity).GetProperty(OrderByPropertyName);
            if (prop != null)
            {
                if (OrderByDescending)
                    query = query.Where(expression).OrderByDescending(x => prop.GetValue(x, null));
                else
                    query = query.Where(expression).OrderBy(x => prop.GetValue(x, null));
            }
        }

        // execute and return the list
        return query.ToList();
    }

}

/// <summary>
/// Defines a property for the WHERE clause
/// </summary>
public class FilterProperty
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public FilterOperator Operator { get; set; }
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Specify the compare operator
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    Contains,
    LessThan,
    GreaterThan,
    LessThanOrEqual,
    GreaterThanOrEqual
}