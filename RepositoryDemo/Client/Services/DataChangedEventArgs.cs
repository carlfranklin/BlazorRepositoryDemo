public class DataChangedEventArgs : EventArgs
{
    public string Table { get; set; }
    public string Action { get; set; }
    public string Id { get; set; }

    public DataChangedEventArgs(string table, string action, string id)
    {
        Table = table;
        Action = action;
        Id = id;
    }
}
