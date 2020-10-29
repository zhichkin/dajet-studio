using System.Linq;

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
        public static MetaFieldPurpose ParseFieldPurpose(string fieldName)
        {
            char L = char.Parse(DBToken.L);
            char N = char.Parse(DBToken.N);
            char T = char.Parse(DBToken.T);
            char S = char.Parse(DBToken.S);
            char B = char.Parse(DBToken.B);

            char test = fieldName[fieldName.Count() - 1];

            if (char.IsDigit(test)) return MetaFieldPurpose.Value;

            if (test == L)
            {
                return MetaFieldPurpose.Boolean;
            }
            else if (test == N)
            {
                return MetaFieldPurpose.Numeric;
            }
            else if (test == T)
            {
                return MetaFieldPurpose.DateTime;
            }
            else if (test == S)
            {
                return MetaFieldPurpose.String;
            }
            else if (test == B)
            {
                return MetaFieldPurpose.Binary;
            }

            string TYPE = DBToken.TYPE;
            string TRef = DBToken.TRef;
            string RRef = DBToken.RRef;

            string postfix = fieldName.Substring(fieldName.Count() - 4);

            if (postfix == TYPE)
            {
                return MetaFieldPurpose.Discriminator;
            }
            else if (postfix == TRef)
            {
                return MetaFieldPurpose.TypeCode;
            }
            else if (postfix == RRef)
            {
                return MetaFieldPurpose.Object;
            }

            return MetaFieldPurpose.Value;
        }
    }
}