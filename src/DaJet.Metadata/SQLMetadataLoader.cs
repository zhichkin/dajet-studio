using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaJet.Metadata
{
    public sealed class SQLMetadataLoader
    {
        private sealed class FieldSqlInfo
        {
            public FieldSqlInfo() { }
            public int ORDINAL_POSITION;
            public string COLUMN_NAME;
            public string DATA_TYPE;
            public int CHARACTER_MAXIMUM_LENGTH;
            public byte NUMERIC_PRECISION;
            public int NUMERIC_SCALE;
            public bool IS_NULLABLE;
        }
        private sealed class ClusteredIndexInfo
        {
            public ClusteredIndexInfo() { }
            public string NAME;
            public bool IS_UNIQUE;
            public bool IS_PRIMARY_KEY;
            public List<ClusteredIndexColumnInfo> COLUMNS = new List<ClusteredIndexColumnInfo>();
            public bool HasNullableColumns
            {
                get
                {
                    bool result = false;
                    foreach (ClusteredIndexColumnInfo item in COLUMNS)
                    {
                        if (item.IS_NULLABLE)
                        {
                            return true;
                        }
                    }
                    return result;
                }
            }
            public ClusteredIndexColumnInfo GetColumnByName(string name)
            {
                ClusteredIndexColumnInfo info = null;
                for (int i = 0; i < COLUMNS.Count; i++)
                {
                    if (COLUMNS[i].NAME == name) return COLUMNS[i];
                }
                return info;
            }
        }
        private sealed class ClusteredIndexColumnInfo
        {
            public ClusteredIndexColumnInfo() { }
            public byte KEY_ORDINAL;
            public string NAME;
            public bool IS_NULLABLE;
        }
        private string ConnectionString { get; set; }
        public void Load(string connectionString, DatabaseInfo database)
        {
            this.ConnectionString = connectionString;
            foreach (BaseObject bo in database.BaseObjects)
            {
                GetSQLMetadata(bo);
            }
        }
        private void GetSQLMetadata(BaseObject bo)
        {
            foreach (MetaObject @object in bo.MetaObjects)
            {
                ReadSQLMetadata(@object);
                foreach (MetaObject nested in @object.MetaObjects)
                {
                    ReadSQLMetadata(nested);
                }
            }
        }
        private List<FieldSqlInfo> GetSqlFields(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,");
            sb.AppendLine(@"    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,");
            sb.AppendLine(@"    ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,");
            sb.AppendLine(@"    ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,");
            sb.AppendLine(@"    CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE");
            sb.AppendLine(@"FROM");
            sb.AppendLine(@"    INFORMATION_SCHEMA.COLUMNS");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    TABLE_NAME = N'{0}'");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"    ORDINAL_POSITION ASC;");

            string sql = string.Format(sb.ToString(), tableName);

            List<FieldSqlInfo> list = new List<FieldSqlInfo>();
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FieldSqlInfo item = new FieldSqlInfo()
                            {
                                ORDINAL_POSITION = reader.GetInt32(0),
                                COLUMN_NAME = reader.GetString(1),
                                DATA_TYPE = reader.GetString(2),
                                CHARACTER_MAXIMUM_LENGTH = reader.GetInt32(3),
                                NUMERIC_PRECISION = reader.GetByte(4),
                                NUMERIC_SCALE = reader.GetInt32(5),
                                IS_NULLABLE = reader.GetBoolean(6)
                            };
                            list.Add(item);
                        }
                    }
                }
            }
            return list;
        }
        private void ReadSQLMetadata(MetaObject @object)
        {
            if (string.IsNullOrWhiteSpace(@object.TableName)) return;

            List<FieldSqlInfo> sql_fields = GetSqlFields(@object.TableName);

            ClusteredIndexInfo indexInfo = this.GetClusteredIndexInfo(@object.TableName);

            foreach (FieldSqlInfo info in sql_fields)
            {
                bool found = false; MetaField field = null;
                foreach (MetaProperty p in @object.Properties)
                {
                    if (string.IsNullOrEmpty(p.DbName)) { continue; }

                    if (info.COLUMN_NAME.TrimStart('_') == DBToken.Periodicity) { continue; }

                    if (info.COLUMN_NAME.TrimStart('_').StartsWith(p.DbName))
                    {
                        field = new MetaField()
                        {
                            Name = info.COLUMN_NAME,
                            Purpose = SqlUtility.ParseFieldPurpose(info.COLUMN_NAME)
                        };
                        p.Fields.Add(field);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    string propertyName = string.Empty;
                    List<int> propertyTypes = null;

                    if (@object.Token == DBToken.AccumRgT && @object.Owner != null && @object.Owner.Token == DBToken.AccumRg)
                    {
                        // find property name in main table object by field name
                        foreach (MetaProperty ownerProperty in @object.Owner.Properties)
                        {
                            if (ownerProperty.Fields.Where(f => f.Name == info.COLUMN_NAME).FirstOrDefault() != null)
                            {
                                propertyName = ownerProperty.Name;
                                if (ownerProperty.PropertyTypes.Count > 0)
                                {
                                    propertyTypes = ownerProperty.PropertyTypes.ToList();
                                }
                                break;
                            }
                        }
                    }

                    MetaProperty property = @object.Properties.Where(p => p.Name == propertyName).FirstOrDefault();
                    if (property == null)
                    {
                        property = new MetaProperty()
                        {
                            Parent = @object,
                            Name = string.IsNullOrEmpty(propertyName) ? info.COLUMN_NAME : propertyName,
                            Purpose = MetaPropertyPurpose.System
                        };
                        @object.Properties.Add(property);
                        
                        MatchFieldToProperty(info, property);

                        if (property.PropertyTypes.Count == 0 && propertyTypes != null)
                        {
                            property.PropertyTypes = propertyTypes;
                        }
                    }

                    field = new MetaField()
                    {
                        Name = info.COLUMN_NAME,
                        Purpose = SqlUtility.ParseFieldPurpose(info.COLUMN_NAME)
                    };
                    property.Fields.Add(field);
                }
                field.TypeName = info.DATA_TYPE;
                field.Length = info.CHARACTER_MAXIMUM_LENGTH;
                field.Precision = info.NUMERIC_PRECISION;
                field.Scale = info.NUMERIC_SCALE;
                field.IsNullable = info.IS_NULLABLE;

                if (indexInfo != null)
                {
                    ClusteredIndexColumnInfo columnInfo = indexInfo.GetColumnByName(info.COLUMN_NAME);
                    if (columnInfo != null)
                    {
                        field.IsPrimaryKey = true;
                        field.KeyOrdinal = columnInfo.KEY_ORDINAL;
                    }
                }
            }
        }
        private ClusteredIndexInfo GetClusteredIndexInfo(string tableName)
        {
            ClusteredIndexInfo info = null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    i.name,");
            sb.AppendLine(@"    i.is_unique,");
            sb.AppendLine(@"    i.is_primary_key,");
            sb.AppendLine(@"    c.key_ordinal,");
            sb.AppendLine(@"    f.name,");
            sb.AppendLine(@"    f.is_nullable");
            sb.AppendLine(@"FROM sys.indexes AS i");
            sb.AppendLine(@"INNER JOIN sys.tables AS t ON t.object_id = i.object_id");
            sb.AppendLine(@"INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id");
            sb.AppendLine(@"INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    t.object_id = OBJECT_ID(@table) AND i.type = 1 -- CLUSTERED");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"c.key_ordinal ASC;");
            string sql = sb.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("table", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info = new ClusteredIndexInfo()
                        {
                            NAME = reader.GetString(0),
                            IS_UNIQUE = reader.GetBoolean(1),
                            IS_PRIMARY_KEY = reader.GetBoolean(2)
                        };
                        info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                        {
                            KEY_ORDINAL = reader.GetByte(3),
                            NAME = reader.GetString(4),
                            IS_NULLABLE = reader.GetBoolean(5)
                        });
                        while (reader.Read())
                        {
                            info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                            {
                                KEY_ORDINAL = reader.GetByte(3),
                                NAME = reader.GetString(4),
                                IS_NULLABLE = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            return info;
        }

        private void MatchFieldToProperty(FieldSqlInfo field, MetaProperty property)
        {
            string columnName = field.COLUMN_NAME.TrimStart('_');
            if (columnName.StartsWith(DBToken.IDRRef))
            {
                property.Name = "Ссылка";
                property.DbName = DBToken.IDRRef;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName.StartsWith(DBToken.Version))
            {
                property.Name = "ВерсияДанных";
                property.DbName = DBToken.Version;
                property.PropertyTypes.Add((int)TypeCodes.Binary);
            }
            else if (columnName.StartsWith(DBToken.Marked))
            {
                property.Name = "ПометкаУдаления";
                property.DbName = DBToken.Marked;
                property.PropertyTypes.Add((int)TypeCodes.Boolean);
            }
            else if (columnName.StartsWith(DBToken.PredefinedID))
            {
                property.Name = "ИмяПредопределённыхДанных";
                property.DbName = DBToken.PredefinedID;
                property.PropertyTypes.Add((int)TypeCodes.Guid);
            }
            else if (columnName.StartsWith(DBToken.Code))
            {
                property.Name = "Код";
                property.DbName = DBToken.Code;
                //property.PropertyTypes.Add((int)TypeCodes.String);
            }
            else if (columnName.StartsWith(DBToken.Description))
            {
                property.Name = "Наименование";
                property.DbName = DBToken.Description;
                property.PropertyTypes.Add((int)TypeCodes.String);
            }
            else if (columnName.StartsWith(DBToken.Folder))
            {
                property.Name = "ЭтоГруппа";
                property.DbName = DBToken.Folder;
                property.PropertyTypes.Add((int)TypeCodes.Boolean);
            }
            else if (columnName.StartsWith(DBToken.ParentIDRRef))
            {
                property.Name = "Родитель";
                property.DbName = DBToken.ParentIDRRef;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName.StartsWith(DBToken.OwnerID))
            {
                property.Name = "Владелец";
                property.DbName = DBToken.OwnerID;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName.StartsWith(DBToken.DateTime))
            {
                property.Name = "Дата";
                property.DbName = DBToken.DateTime;
                property.PropertyTypes.Add((int)TypeCodes.DateTime);
            }
            else if (columnName == DBToken.Number)
            {
                property.Name = "Номер";
                property.DbName = DBToken.Number;
                //property.PropertyTypes.Add((int)TypeCodes.String);
            }
            else if (columnName.StartsWith(DBToken.Posted))
            {
                property.Name = "Проведён";
                property.DbName = DBToken.Posted;
                property.PropertyTypes.Add((int)TypeCodes.Boolean);
            }
            else if (columnName == DBToken.NumberPrefix)
            {
                property.Name = "МоментВремени";
                property.DbName = DBToken.NumberPrefix;
            }
            else if (columnName.StartsWith(DBToken.Periodicity))
            {
                property.Name = "Периодичность";
                property.DbName = DBToken.Periodicity;
            }
            else if (columnName.StartsWith(DBToken.Period))
            {
                property.Name = "Период";
                property.DbName = DBToken.Period;
            }
            else if (columnName.StartsWith(DBToken.ActualPeriod))
            {
                property.Name = "ПериодАктуальности";
                property.DbName = DBToken.ActualPeriod;
            }
            else if (columnName.StartsWith(DBToken.Recorder))
            {
                property.Name = "Регистратор";
                property.DbName = DBToken.Recorder;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName.StartsWith(DBToken.Active))
            {
                property.Name = "Активность";
                property.DbName = DBToken.Active;
                property.PropertyTypes.Add((int)TypeCodes.Boolean);
            }
            else if (columnName.StartsWith(DBToken.LineNo))
            {
                property.Name = "НомерСтроки";
                property.DbName = DBToken.LineNo;
            }
            else if (columnName.StartsWith(DBToken.RecordKind))
            {
                property.Name = "ВидДвижения";
                property.DbName = DBToken.RecordKind;
            }
            else if (columnName.StartsWith(DBToken.KeyField))
            {
                property.Name = "КлючСтроки";
                property.DbName = DBToken.KeyField;
            }
            else if (columnName.EndsWith(DBToken.IDRRef)) // табличные части
            {
                property.Name = "Ссылка";
                property.DbName = DBToken.IDRRef;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName == DBToken.EnumOrder)
            {
                property.Name = "Порядок";
                property.DbName = DBToken.EnumOrder;
            }
            else if (columnName == DBToken.Type) // ПланВидовХарактеристик
            {
                property.Name = "ТипЗначения"; // ОписаниеТипов - TypeConstraint
                property.DbName = DBToken.Type;
            }
            else if (columnName == DBToken.Splitter)
            {
                property.Name = "РазделительИтогов";
                property.DbName = DBToken.Splitter;
            }
            else if (columnName == DBToken.NodeTRef)
            {
                property.Name = "Узел";
                property.DbName = DBToken.Node;
                property.PropertyTypes.Add(int.MaxValue);
            }
            else if (columnName == DBToken.NodeRRef)
            {
                property.Name = "Узел";
                property.DbName = DBToken.Node;
            }
            else if (columnName == DBToken.MessageNo)
            {
                property.Name = "НомерСообщения";
                property.DbName = DBToken.MessageNo;
            }
            else if (columnName == DBToken.RepetitionFactor)
            {
                property.Name = "КоэффициентПериодичности";
                property.DbName = DBToken.RepetitionFactor;
            }
            else if (columnName == DBToken.UseTotals)
            {
                property.Name = "ИспользоватьИтоги";
                property.DbName = DBToken.UseTotals;
            }
            else if (columnName == DBToken.UseSplitter)
            {
                property.Name = "ИспользоватьРазделительИтогов";
                property.DbName = DBToken.UseSplitter;
            }
            else if (columnName == DBToken.MinPeriod)
            {
                property.Name = "МинимальныйПериод";
                property.DbName = DBToken.MinPeriod;
            }
            else if (columnName == DBToken.MinCalculatedPeriod)
            {
                property.Name = "МинимальныйПериодРассчитанныхИтогов";
                property.DbName = DBToken.MinCalculatedPeriod;
            }
        }
    }
}