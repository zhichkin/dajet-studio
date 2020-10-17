namespace DaJet.Metadata
{
    public static class SqlUtility
    {
        public static string CreateTableFieldScript(MetaField field)
        {
            string SIZE = string.Empty;
            if (field.TypeName == "char"
                | field.TypeName == "nchar"
                | field.TypeName == "binary"
                | field.TypeName == "varchar"
                | field.TypeName == "nvarchar"
                | field.TypeName == "varbinary")
            {
                SIZE = (field.Length < 0) ? "(MAX)" : $"({field.Length})";
            }
            else if (field.Precision > 0 && field.TypeName != "bit")
            {
                SIZE = $"({field.Precision}, {field.Scale})";
            }
            string NULLABLE = field.IsNullable ? " NULL" : " NOT NULL";
            return $"[{field.Name}] [{field.TypeName}]{SIZE}{NULLABLE}";
        }
    }
}