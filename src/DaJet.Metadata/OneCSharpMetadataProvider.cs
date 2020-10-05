using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DaJet.Metadata
{
    public interface IMetadataProvider
    {
        void UseServer(DatabaseServer server);
        void UseDatabase(DatabaseInfo database);
        void InitializeMetadata(DatabaseInfo database);
    }
    internal delegate void SpecialParser(StreamReader reader, string line, MetaObject metaObject);
    public sealed class OneCSharpMetadataProvider: IMetadataProvider
    {
        private Dictionary<string, MetaObject> UUIDs { get; set; } = new Dictionary<string, MetaObject>();
        private Dictionary<string, DBNameEntry> DBNames { get; set; } = new Dictionary<string, DBNameEntry>();

        public OneCSharpMetadataProvider()
        {
            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseDbProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection

            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseDbProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseDbProperties); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseDbProperties); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseDbProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseDbProperties); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseDbProperties); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseDbProperties); // Коллекция реквизитов регистра накопления
        }

        public string ConnectionString { get; private set; }
        public DatabaseServer CurrentServer { get; private set; }
        public DatabaseInfo CurrentDatabase { get; private set; }
        public void UseServer(DatabaseServer server)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            CurrentServer = server;

            SqlConnectionStringBuilder csb;
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                csb = new SqlConnectionStringBuilder() { IntegratedSecurity = true };
            }
            else
            {
                csb = new SqlConnectionStringBuilder(ConnectionString);
            }
            csb.DataSource = string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address;
            
            ConnectionString = csb.ToString();
        }
        public void UseDatabase(DatabaseInfo database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            
            CurrentDatabase = database;

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = database.Name,
                PersistSecurityInfo = false,
                IntegratedSecurity = string.IsNullOrWhiteSpace(database.UserName)
            };
            if (!csb.IntegratedSecurity)
            {
                csb.UserID = database.UserName;
                csb.Password = database.Password;
            }

            ConnectionString = csb.ToString();
        }
        public void InitializeMetadata(DatabaseInfo database)
        {
            Dictionary<string, DBNameEntry> dbnames = new Dictionary<string, DBNameEntry>();
            
            ReadDBNames(dbnames);

            DBNames = dbnames;

            if (dbnames.Count > 0)
            {
                foreach (var item in dbnames)
                {
                    ReadConfig(item.Key, item.Value.MetaObject, database);
                }
                MakeSecondPass(database);
                ReadSQLMetadata(database);
            }
        }
        public void ReadConfig(string fileName, MetaObject metaObject, DatabaseInfo database)
        {
            if (new Guid(fileName) == Guid.Empty) return;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
            
            ParseMetadataObject(stream, fileName, metaObject, database);
            
            //MemoryStream memory = new MemoryStream();
            //stream.CopyTo(memory);
            //memory.Seek(0, SeekOrigin.Begin);
            //WriteBinaryDataToFile(memory, $"{fileName}.txt");
            //memory.Seek(0, SeekOrigin.Begin);
            //ParseMetadataObject(memory, fileName, database);
        }

        private void WriteBinaryDataToFile(Stream binaryData, string fileName)
        {
            //string filePath = Path.Combine(_logger.CatalogPath, fileName);
            //using (FileStream output = File.Create(filePath))
            //{
            //    binaryData.CopyTo(output);
            //}
        }

        #region "Read DBNames"

        private void ReadDBNames(Dictionary<string, DBNameEntry> dbnames)
        {
            SqlBytes binaryData = GetDBNamesFromDatabase();
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            ParseDBNames(stream, dbnames);

            //MemoryStream memory = new MemoryStream();
            //stream.CopyTo(memory);
            //memory.Seek(0, SeekOrigin.Begin);
            //WriteBinaryDataToFile(memory, "DBNames.txt");
            //memory.Seek(0, SeekOrigin.Begin);
            //ParseDBNames(memory, DBNames);
        }
        private SqlBytes GetDBNamesFromDatabase()
        {
            SqlBytes binaryData = null;

            { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT BinaryData FROM Params WHERE FileName = N'DBNames'";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return binaryData;
        }
        private void ParseDBNames(Stream stream, Dictionary<string, DBNameEntry> dbnames)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    int capacity = GetDBNamesCapacity(line);
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseDBNameLine(line, dbnames);
                    }
                }
            }
        }
        private int GetDBNamesCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseDBNameLine(string line, Dictionary<string, DBNameEntry> dbnames)
        {
            string[] items = line.Split(',');
            if (items.Length < 3) return;

            string uuid = items[0].Substring(1); // .Replace("{", string.Empty);
            if (new Guid(uuid) == Guid.Empty) return; // system meta object - global settings etc.

            DBName dbname = new DBName()
            {
                Token = items[1].Trim('\"'), // .Replace("\"", string.Empty),
                TypeCode = int.Parse(items[2].TrimEnd('}')) // .Replace("}", string.Empty))
            };
            dbname.IsMainTable = IsMainTable(dbname.Token);

            if (!dbnames.TryGetValue(uuid, out DBNameEntry entry))
            {
                entry = new DBNameEntry();
                dbnames.Add(uuid, entry);
            }
            entry.DBNames.Add(dbname);

            if (dbname.IsMainTable)
            {
                entry.MetaObject.UUID = new Guid(uuid);
                entry.MetaObject.Token = dbname.Token;
                entry.MetaObject.TypeCode = dbname.TypeCode;
                entry.MetaObject.TableName = CreateTableName(entry.MetaObject, dbname);
            }
        }
        private bool IsMainTable(string token)
        {
            return token == DBToken.Enum
                || token == DBToken.Chrc
                || token == DBToken.Const
                || token == DBToken.InfoRg
                || token == DBToken.AccumRg
                || token == DBToken.Document
                || token == DBToken.Reference;
        }

        #endregion

        #region " Read Config "

        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9
        private readonly Regex rxSpecialUUID = new Regex("^{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12},\\d+(?:})?,$"); // Example: {3daea016-69b7-4ed4-9453-127911372fe6,0}, | {cf4abea7-37b2-11d4-940f-008048da11f9,5,
        private readonly Regex rxDbName = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$");
        private readonly Regex rxDbType = new Regex("^{\"[#BSDN]\""); // Example: {"#",1aaea747-a4ba-4fb2-9473-075b1ced620c}, | {"B"}, | {"S",10,0}, | {"D","T"}, | {"N",10,0,1}
        private readonly Regex rxNestedProperties = new Regex("^{888744e1-b616-11d4-9436-004095e12fc7,\\d+[},]$"); // look rxSpecialUUID
        private readonly Dictionary<string, SpecialParser> _SpecialParsers = new Dictionary<string, SpecialParser>();
        
        private SqlBytes ReadConfigFromDatabase(string fileName)
        {
            SqlBytes binaryData = null;

            { // limited scope for variables declared in it
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT BinaryData FROM Config WHERE FileName = @FileName ORDER BY PartNo ASC";
                command.Parameters.AddWithValue("FileName", fileName);
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return binaryData;
        }
        private void ParseMetadataObject(Stream stream, string fileName, MetaObject metaObject, DatabaseInfo database)
        {
            if (metaObject.Token == null) return;
            if (metaObject.Token == DBToken.Const) return;

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                
                line = reader.ReadLine();
                //ParseInternalIdentifier(line, metaObject);

                _ = reader.ReadLine();
                _ = reader.ReadLine();

                line = reader.ReadLine();
                ParseMetaObjectName(line, metaObject);

                line = reader.ReadLine();
                ParseMetaObjectAlias(line, metaObject);

                SetMetaObjecNamespace(metaObject, database);

                if (metaObject.Token == DBToken.Reference)
                {
                    ParseReferenceOwner(reader, metaObject); // свойство справочника "Владелец"
                }
                else if (metaObject.Token == DBToken.InfoRg || metaObject.Token == DBToken.AccumRg)
                {
                    // TODO: ParseRegisterRecorder(reader, metaObject); // свойство регистра "Регистратор"
                }

                int count = 0;
                string UUID = null;
                Match match = null;
                while ((line = reader.ReadLine()) != null)
                {
                    match = rxSpecialUUID.Match(line);
                    if (!match.Success) continue;

                    string[] lines = line.Split(',');
                    UUID = lines[0].Replace("{", string.Empty);
                    count = int.Parse(lines[1].Replace("}", string.Empty));
                    if (count == 0) continue;

                    if (_SpecialParsers.ContainsKey(UUID))
                    {
                        _SpecialParsers[UUID](reader, line, metaObject);
                    }
                }
            }
        }
        private void ParseInternalIdentifier(string line, MetaObject metaObject)
        {
            string[] items = line.Split(',');
            string uuid = (metaObject.Token == DBToken.Enum ? items[1] : items[3]);
            UUIDs.Add(uuid, metaObject);
        }
        private void ParseMetaObjectName(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            string uuid = lines[2].Replace("}", string.Empty);
            //if (metaObject.UUID != uuid) { /* TODO: error ? */}
            metaObject.Name = lines[3].Replace("\"", string.Empty);
        }
        private void ParseMetaObjectAlias(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            string alias = lines[2].Replace("}", string.Empty);
            metaObject.Alias = alias.Replace("\"", string.Empty);
        }
        private string CreateTableName(MetaObject metaObject, DBName dbname)
        {
            if (dbname.Token == DBToken.VT)
            {
                if (metaObject.Owner == null)
                {
                    return string.Empty;
                }
                else
                {
                    return $"{metaObject.Owner.TableName}_{dbname.Token}{dbname.TypeCode}";
                }
            }
            else
            {
                return $"_{dbname.Token}{dbname.TypeCode}";
            }
        }
        private void SetMetaObjecNamespace(MetaObject dbo, DatabaseInfo database)
        {
            if (dbo.Parent != null) return;

            BaseObject ns = database.BaseObjects.Where(n => n.Name == dbo.Token).FirstOrDefault();
            if (ns == null)
            {
                if (string.IsNullOrEmpty(dbo.Token))
                {
                    ns = database.BaseObjects.Where(n => n.Name == "Unknown").FirstOrDefault();
                    if (ns == null)
                    {
                        ns = new BaseObject(); // { InfoBase = infoBase };
                        ns.Name = "Unknown";
                        database.BaseObjects.Add(ns);
                    }
                }
                else
                {
                    ns = new BaseObject(); // { InfoBase = database };
                    ns.Name = dbo.Token;
                    database.BaseObjects.Add(ns);
                }
            }
            dbo.Parent = ns;
            ns.MetaObjects.Add(dbo);
        }
        private void ParseReferenceOwner(StreamReader reader, MetaObject dbo)
        {
            int count = 0;
            string[] lines;

            //_ = reader.ReadLine(); // строка описания - "Синоним" в терминах 1С
            _ = reader.ReadLine();
            string line = reader.ReadLine();
            if (line != null)
            {
                lines = line.Split(',');
                count = int.Parse(lines[1].Replace("}", string.Empty));
            }
            if (count == 0) return;

            Match match;
            List<TypeInfo> types = new List<TypeInfo>();
            for (int i = 0; i < count; i++)
            {
                _ = reader.ReadLine();
                line = reader.ReadLine();
                if (line == null) return;

                match = rxUUID.Match(line);
                if (match.Success)
                {
                    if (DBNames.TryGetValue(match.Value, out DBNameEntry entry))
                    {
                        types.Add(new TypeInfo()
                        {
                            TypeCode = entry.MetaObject.TypeCode,
                            Name = entry.MetaObject.TableName,
                            MetaObject = entry.MetaObject
                        });
                    }
                }
                _ = reader.ReadLine();
            }

            if (types.Count > 0)
            {
                MetaProperty property = new MetaProperty
                {
                    Parent = dbo,
                    Types = types,
                    Name = "Владелец",
                    DbName = "OwnerID" // [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
                    // TODO: add DbField at once ?
                };
                dbo.Properties.Add(property);
            }
        }
        private void ParseDbProperties(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1].Replace("}", string.Empty));
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseDbProperty(reader, nextLine, dbo);
                        break;
                    }
                }
            }
        }
        private void ParseDbProperty(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaProperty property = new MetaProperty
            {
                Parent = dbo,
                Name = objectName
            };
            dbo.Properties.Add(property);

            if (DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                if (entry.DBNames.Count == 1)
                {
                    property.DbName = CreateDbFieldName(entry.DBNames[0]);
                }
                else if (entry.DBNames.Count > 1)
                {
                    foreach (var dbn in entry.DBNames.Where(dbn => dbn.Token == DBToken.Fld))
                    {
                        property.DbName = CreateDbFieldName(dbn);
                    }
                }
            }
            ParseDbPropertyTypes(reader, property);
        }
        private string CreateDbFieldName(DBName dbname)
        {
            return $"{dbname.Token}{dbname.TypeCode}";
        }
        private void ParseDbPropertyTypes(StreamReader reader, MetaProperty property)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            while (line != "{\"Pattern\",")
            {
                line = reader.ReadLine();
                if (line == null) return;
            }

            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxDbType.Match(line);
                if (!match.Success) break;

                int typeCode = 0;
                string typeName = string.Empty;
                string token = match.Value.Replace("{", string.Empty).Replace("\"", string.Empty);
                switch (token)
                {
                    case DBToken.B: { typeCode = -1; typeName = "Boolean"; break; }
                    case DBToken.S: { typeCode = -2; typeName = "String"; break; }
                    case DBToken.D: { typeCode = -3; typeName = "DateTime"; break; }
                    case DBToken.N: { typeCode = -4; typeName = "Numeric"; break; }
                }
                if (typeCode != 0)
                {
                    property.Types.Add(new TypeInfo()
                    {
                        Name = typeName,
                        TypeCode = typeCode
                    });
                }
                else
                {
                    string[] lines = line.Split(',');
                    string UUID = lines[1].Replace("}", string.Empty);

                    if (UUID == "e199ca70-93cf-46ce-a54b-6edc88c3a296")
                    {
                        // ХранилищеЗначения - varbinary(max)
                        property.Types.Add(new TypeInfo()
                        {
                            Name = "BLOB",
                            TypeCode = -5
                        });
                    }
                    else if (UUID == "fc01b5df-97fe-449b-83d4-218a090e681e")
                    {
                        // УникальныйИдентификатор - binary(16)
                        property.Types.Add(new TypeInfo()
                        {
                            Name = "UUID",
                            TypeCode = -6
                        });
                    }
                    else if (UUIDs.TryGetValue(UUID, out MetaObject dbo))
                    {
                        property.Types.Add(new TypeInfo()
                        {
                            Name = dbo.TableName,
                            TypeCode = dbo.TypeCode,
                            UUID = UUID,
                            MetaObject = dbo
                        });
                    }
                    else // UUID is not loaded yet - leave it for second pass
                    {
                        property.Types.Add(new TypeInfo()
                        {
                            UUID = UUID
                        });
                    }
                }
            }
        }
        private void ParseNestedObjects(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1]);
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseNestedObject(reader, nextLine, dbo);
                        break;
                    }
                }
            }
        }
        private void ParseNestedObject(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaObject nested = new MetaObject()
            {
                Owner = dbo,
                Name = objectName,
                Parent = dbo.Parent
            };
            dbo.MetaObjects.Add(nested);

            if (DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                DBName dbname = entry.DBNames.Where(i => i.IsMainTable).FirstOrDefault();
                if (dbname != null)
                {
                    nested.Token = dbname.Token;
                    nested.TypeCode = dbname.TypeCode;
                    nested.TableName = CreateTableName(nested, dbname);
                }
            }
            ParseNestedDbProperties(reader, nested);
        }
        private void ParseNestedDbProperties(StreamReader reader, MetaObject dbo)
        {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxNestedProperties.Match(line);
                if (match.Success)
                {
                    ParseDbProperties(reader, line, dbo);
                    break;
                }
            }
        }

        #endregion

        public void MakeSecondPass(DatabaseInfo database)
        {
            foreach (var ns in database.BaseObjects)
            {
                foreach (var dbo in ns.MetaObjects)
                {
                    foreach (var property in dbo.Properties)
                    {
                        foreach (var type in property.Types)
                        {
                            if (type.TypeCode == 0)
                            {
                                if (type.MetaObject == null)
                                {
                                    if (!string.IsNullOrEmpty(type.UUID))
                                    {
                                        if (UUIDs.TryGetValue(type.UUID, out MetaObject dbObject))
                                        {
                                            type.Name = dbObject.TableName;
                                            type.MetaObject = dbObject;
                                            type.TypeCode = dbObject.TypeCode;
                                        }
                                    }
                                }
                                else
                                {
                                    type.Name = type.MetaObject.TableName;
                                    type.TypeCode = type.MetaObject.TypeCode;
                                }
                            }
                        }
                    }
                    foreach (var nested in dbo.MetaObjects)
                    {
                        foreach (var property in nested.Properties)
                        {
                            foreach (var type in property.Types)
                            {
                                if (type.TypeCode == 0)
                                {
                                    if (type.MetaObject == null)
                                    {
                                        if (!string.IsNullOrEmpty(type.UUID))
                                        {
                                            if (UUIDs.TryGetValue(type.UUID, out MetaObject dbObject))
                                            {
                                                type.Name = dbObject.TableName;
                                                type.MetaObject = dbObject;
                                                type.TypeCode = dbObject.TypeCode;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        type.Name = type.MetaObject.TableName;
                                        type.TypeCode = type.MetaObject.TypeCode;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void ReadSQLMetadata(DatabaseInfo database)
        {
            (new SQLMetadataLoader()).Load(ConnectionString, database);
        }

        internal void SaveDbObjectToFile(MetaObject dbo)
        {
            var kv = DBNames
                .Where(i => i.Value.MetaObject == dbo)
                .FirstOrDefault();
            string fileName = kv.Key;

            if (new Guid(fileName) == Guid.Empty) return;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
            WriteBinaryDataToFile(stream, $"{fileName}.txt");
        }
    }
}