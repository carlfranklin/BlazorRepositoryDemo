public class DapperSqlHelper
{
    public static string GetDapperInsertStatement(object Entity, string TableName)
    {
        // let's get the SQL string started.
        string sql = $"insert into {TableName} (";

        // Get the type, and the list of public properties
        var EntityType = Entity.GetType();
        var Properties = EntityType.GetProperties();

        foreach (var property in Properties)
        {
            // Is this property nullable?
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                // yes. get the value.
                var value = property.GetValue(Entity);
                // is the value null?
                if (value != null)
                    // only add if the value is not null
                    sql += $"{property.Name}, ";
            }
            // is this property virtual (like Customer.Invoices)?
            else if (property.GetGetMethod().IsVirtual == false)
            {
                // not virtual. Include
                sql += $"{property.Name}, ";
            }
        }

        // At this point there is a trailing ", " that we need to remove
        sql = sql.Substring(0, sql.Length - 2);

        // add the start of the values clause
        sql += ") values (";

        // Once more through the properties
        foreach (var property in Properties)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                var value = property.GetValue(Entity);
                if (value != null)
                    // inserts in Dapper are paramterized, so at least
                    // we don't have to figure out data types, quotes, etc.
                    sql += $"@{property.Name}, ";
            }
            else if (property.GetGetMethod().IsVirtual == false)
            {
                sql += $"@{property.Name}, ";
            }
        }

        // again, remove the trailing ", " and finish with a closed paren 
        sql = sql.Substring(0, sql.Length - 2) + ")";

        // we're outta here!
        return sql;
    }
}