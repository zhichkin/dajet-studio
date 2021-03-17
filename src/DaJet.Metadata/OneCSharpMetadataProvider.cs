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
        void UseCredentials(string userName, string password);
        void InitializeMetadata(DatabaseInfo database);

        string ReadDBNames();
        string ReadConfigFile(string fileName);
        string GetRootConfigFileName();
        List<CommonModuleInfo> GetCommonModules();
        string ReadCommonModuleSourceCode(CommonModuleInfo module);

    }
    internal delegate void SpecialParser(StreamReader reader, string line, MetaObject metaObject);
    public sealed class OneCSharpMetadataProvider: IMetadataProvider
    {
        private const string ERROR_SERVER_IS_NOT_DEFINED = "Server is not defined. Try to call \"UseServer\" method first.";
        private const string ERROR_DATABASE_IS_NOT_DEFINED = "Database is not defined. Try to call \"UseDatabase\" method first.";

        private const string COMMON_MODULES_COLLECTION_UUID = "0fe48980-252d-11d6-a3c7-0050bae0a776";

        private Dictionary<string, MetaObject> UUIDs { get; set; } = new Dictionary<string, MetaObject>();
        private Dictionary<string, DBNameEntry> DBNames { get; set; } = new Dictionary<string, DBNameEntry>();

        public OneCSharpMetadataProvider()
        {
            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseMetaObjectProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection

            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseMetaObjectProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра накопления
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
        public void UseCredentials(string userName, string password)
        {
            if (CurrentServer == null) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);
            if (CurrentDatabase == null) throw new InvalidOperationException(ERROR_DATABASE_IS_NOT_DEFINED);

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                UserID = userName,
                Password = password
            };
            csb.IntegratedSecurity = string.IsNullOrWhiteSpace(userName);

            ConnectionString = csb.ToString();
        }
        public void InitializeMetadata(DatabaseInfo database)
        {
            if (database.BaseObjects != null
                && database.BaseObjects.Count > 0)
            {
                database.BaseObjects.Clear();
            }

            Dictionary<string, DBNameEntry> dbnames = new Dictionary<string, DBNameEntry>();
            
            ReadDBNames(dbnames);

            DBNames = dbnames;

            if (dbnames.Count > 0)
            {
                foreach (var item in dbnames)
                {
                    ReadConfig(item.Key, item.Value.MetaObject, database);
                }
                //MakeSecondPass(database); // resolves reference types constraints for properties
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



        public string GetRootConfigFileName()
        {
            string rootContent = ReadConfigFile("root");
            string[] lines = rootContent.Split(',');
            string uuid = lines[1];
            return uuid;
        }
        public List<CommonModuleInfo> GetCommonModules()
        {
            List<CommonModuleInfo> list = new List<CommonModuleInfo>();

            string fileName = GetRootConfigFileName();
            string metadata = ReadConfigFile(fileName);
            using (StringReader reader = new StringReader(metadata))
            {
                string line = reader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    if (line.Substring(1, 36) == COMMON_MODULES_COLLECTION_UUID)
                    {
                        list = ParseCommonModules(line); break;
                    }
                    line = reader.ReadLine();
                }
            }

            return list;
        }
        private List<CommonModuleInfo> ParseCommonModules(string line)
        {
            List<CommonModuleInfo> list = new List<CommonModuleInfo>();
            string[] fileNames = line.TrimStart('{').TrimEnd('}').Split(',');
            if (int.TryParse(fileNames[1], out int count) && count == 0)
            {
                return list;
            }
            int offset = 2;
            for (int i = 0; i < count; i++)
            {
                CommonModuleInfo moduleInfo = ReadCommonModuleMetadata(fileNames[i + offset]);
                list.Add(moduleInfo);
            }
            return list;
        }
        private CommonModuleInfo ReadCommonModuleMetadata(string fileName)
        {
            string metadata = ReadConfigFile(fileName);

            string uuid = string.Empty;
            string name = string.Empty;

            using (StringReader reader = new StringReader(metadata))
            {
                _ = reader.ReadLine(); // 1. line
                _ = reader.ReadLine(); // 2. line
                _ = reader.ReadLine(); // 3. line
                string line = reader.ReadLine(); // 4. line

                string[] lines = line.Split(',');

                uuid = lines[2].TrimEnd('}');
                name = lines[3].Trim('"');
            }
            
            return new CommonModuleInfo(uuid, name);
        }
        public string ReadCommonModuleSourceCode(CommonModuleInfo module)
        {
            return ReadConfigFile(module.UUID + ".0");
        }



        public string ReadDBNames()
        {
            string content = string.Empty;

            SqlBytes binaryData = GetDBNamesFromDatabase();
            if (binaryData == null)
            {
                return content;
            }

            using (DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }
        public string ReadConfigFile(string fileName)
        {
            string content = string.Empty;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null)
            {
                return content;
            }

            using (DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }

            //if (fileName == "root")
            //{
            //    string[] data = content.Split(',');
            //    if (data.Length == 3)
            //    {
            //        content = Encoding.UTF8.GetString(Convert.FromBase64String(data[2].Replace(Environment.NewLine, string.Empty).TrimEnd('}')));
            //    }
            //}

            return content;
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
                || token == DBToken.Node
                || token == DBToken.Chrc
                || token == DBToken.Const
                || token == DBToken.InfoRg
                || token == DBToken.AccumRg
                || token == DBToken.Document
                || token == DBToken.Reference;
        }

        #endregion

        #region " Read Config "

        // Структура ссылки на объект метаданных
        // {"#",157fa490-4ce9-11d4-9415-008048da11f9, - идентификатор класса объекта метаданных
        // {1,fd8fe814-97e6-42d3-a042-b1e429cfb067}   - внутренний идентификатор объекта метаданных
        // }

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
            if (metaObject.Token == DBToken.Const || metaObject.Token == DBToken.Node) return;

            if (metaObject.Token == DBToken.AccumRg) { ParseAccumulationRegister(fileName); }

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine(); // 1. line
                
                line = reader.ReadLine(); // 2. line
                _ = reader.ReadLine(); // 3. line
                _ = reader.ReadLine(); // 4. line

                line = reader.ReadLine(); // 5. line: meta object UUID and Name
                //ParseInternalIdentifier(line, metaObject);
                ParseMetaObjectName(line, metaObject);

                line = reader.ReadLine(); // 6. line meta object alias
                ParseMetaObjectAlias(line, metaObject);

                SetMetaObjecNamespace(metaObject, database);

                _ = reader.ReadLine(); // 7. line

                if (metaObject.Token == DBToken.Reference)
                {
                    // starts from 8. line
                    //ParseReferenceOwner(reader, metaObject); // свойство справочника "Владелец"
                }
                else if (metaObject.Token == DBToken.Document)
                {
                    // starts from 8. line
                    // TODO: Parse объекты метаданных, которые являются основанием для заполнения текущего
                    // starts after count (количество объектов оснований) * 3 (размер ссылки на объект метаданных) + 1 (тэг закрытия блока объектов оснований)
                    // TODO: Parse все регистры (информационные, накопления и бухгалтерские),
                    //             по которым текущий документ выполняет движения
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
        private string GetBaseObjectName(string token)
        {
            if (token == DBToken.Enum) return "Перечисление";
            else if (token == DBToken.Reference) return "Справочник";
            else if (token == DBToken.Document) return "Документ";
            else if (token == DBToken.Chrc) return "ПланВидовХарактеристик";
            else if (token == DBToken.Const) return "Константа";
            else if (token == DBToken.InfoRg) return "РегистрСведений";
            else if (token == DBToken.AccumRg) return "РегистрНакопления";
            else if (token == DBToken.Node) return "ПланОбмена";
            //else if (token == DBToken.Acc) return "РегистрБухгалтерии";
            //else if (token == DBToken.AccumRg) return "ПланСчетов";
            //else if (token == DBToken.Exchange) return "ПланОбмена";
            else return "Unknown";
        }
        private void SetMetaObjecNamespace(MetaObject dbo, DatabaseInfo database)
        {
            if (dbo.Parent != null) return;

            string baseObjectName = GetBaseObjectName(dbo.Token);

            BaseObject ns = database.BaseObjects.Where(n => n.Name == baseObjectName).FirstOrDefault();
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
                    ns.Name = baseObjectName;
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

            string line = reader.ReadLine(); // 8. line
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
        private void ParseMetaObjectProperties(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, MetaPropertyPurpose.Property);
        }
        private void ParseMetaObjectDimensions(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, MetaPropertyPurpose.Dimension);
        }
        private void ParseMetaObjectMeasures(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, MetaPropertyPurpose.Measure);
        }
        private void ParseMetaProperties(StreamReader reader, string line, MetaObject dbo, MetaPropertyPurpose purpose)
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
                        ParseMetaProperty(reader, nextLine, dbo, purpose);
                        break;
                    }
                }
            }
        }
        private void ParseMetaProperty(StreamReader reader, string line, MetaObject dbo, MetaPropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaProperty property = new MetaProperty
            {
                Parent = dbo,
                Name = objectName,
                Purpose = purpose
            };
            dbo.Properties.Add(property);

            if (DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                if (entry.DBNames.Count == 1)
                {
                    property.DbName = CreateMetaFieldName(entry.DBNames[0]);
                }
                else if (entry.DBNames.Count > 1)
                {
                    foreach (var dbn in entry.DBNames.Where(dbn => dbn.Token == DBToken.Fld))
                    {
                        property.DbName = CreateMetaFieldName(dbn);
                    }
                }
            }
            ParseMetaPropertyTypes(reader, property);
        }
        private string CreateMetaFieldName(DBName dbname)
        {
            return $"{dbname.Token}{dbname.TypeCode}";
        }
        private void ParseMetaPropertyTypes(StreamReader reader, MetaProperty property)
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
                        property.PropertyTypes.Add(dbo.TypeCode);
                    }
                    else // UUID is not loaded yet - see MakeSecondPass procedure
                    {
                        // TODO: get type code some how ...
                        property.Types.Add(new TypeInfo()
                        {
                            UUID = UUID
                        });
                        property.PropertyTypes.Add(int.MaxValue); // костыль !?
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
                DBName dbname = entry.DBNames.Where(i => i.Token == DBToken.VT).FirstOrDefault();
                if (dbname != null)
                {
                    nested.Token = dbname.Token;
                    nested.TypeCode = dbname.TypeCode;
                    nested.TableName = CreateTableName(nested, dbname);
                }
            }
            ParseNestedMetaProperties(reader, nested);
        }
        private void ParseNestedMetaProperties(StreamReader reader, MetaObject dbo)
        {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxNestedProperties.Match(line);
                if (match.Success)
                {
                    ParseMetaProperties(reader, line, dbo, MetaPropertyPurpose.Property);
                    break;
                }
            }
        }



        private void ParseAccumulationRegister(string fileName)
        {
            if (!DBNames.TryGetValue(fileName, out DBNameEntry entry)) return;

            foreach (DBName dbname in entry.DBNames)
            {
                string name = GetChildMetaObjectName(dbname.Token);
                if (string.IsNullOrEmpty(name)) { continue; }
                entry.MetaObject.MetaObjects.Add(
                    new MetaObject()
                    {
                        Name = name,
                        Token = dbname.Token,
                        Owner = entry.MetaObject,
                        TypeCode = dbname.TypeCode,
                        TableName = $"_{dbname.Token}{dbname.TypeCode}"
                    });
            }
        }
        private string GetChildMetaObjectName(string token)
        {
            if (token == DBToken.AccumRgT) return "Итоги";
            else if (token == DBToken.AccumRgOpt) return "Настройки";
            else if (token == DBToken.AccumRgChngR) return "Изменения";
            return string.Empty;
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