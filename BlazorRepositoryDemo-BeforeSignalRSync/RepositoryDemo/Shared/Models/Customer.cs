using Dapper.Contrib.Extensions;

[Table("Customer")]
public class Customer
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}