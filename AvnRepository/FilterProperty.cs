namespace AvnRepository;
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
