using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DaJet.Metadata
{
    public interface IMetadataService
    {
        DatabaseServer CurrentServer { get; }
        DatabaseInfo CurrentDatabase { get; }
        string ConnectionString { get; }
        MetadataSettings Settings { get; }
        void Configure(MetadataSettings settings);
        void UseServer(string serverName);
        void UseDatabase(string databaseName);
        void AttachDatabase(string serverName, DatabaseInfo database);

        List<DatabaseInfo> GetDatabases(DatabaseServer server);
        IMetadataProvider GetMetadataProvider(DatabaseInfo database);

        string MapSchemaIdentifier(string schemaName);
        string MapTableIdentifier(string databaseName, string tableIdentifier);
        MetaObject GetMetaObject(IList<string> tableIdentifiers);
        MetaObject GetMetaObject(string databaseName, string tableIdentifier);
        MetaProperty GetProperty(string databaseName, string tableIdentifier, string columnIdentifier);
    }
    public sealed class MetadataService : IMetadataService
    {
        private const string ERROR_SERVICE_IS_NOT_CONFIGURED = "Metadata service is not configured properly!";
        private const string ERROR_SERVER_IS_NOT_DEFINED = "Current database server is not defined!";
        private XMLMetadataLoader XMLLoader { get; } = new XMLMetadataLoader();
        private SQLMetadataLoader SQLLoader { get; } = new SQLMetadataLoader();
        public MetadataSettings Settings { get; private set; }
        public DatabaseServer CurrentServer { get; private set; }
        public DatabaseInfo CurrentDatabase { get; private set; }
        public string ConnectionString { get; private set; }
        private string ServerCatalogPath(string serverName)
        {
            return Path.Combine(Settings.Catalog, serverName);
        }
        private string MetadataFilePath(string serverName, string databaseName)
        {
            return Path.Combine(ServerCatalogPath(serverName), databaseName + ".xml");
        }
        public void Configure(MetadataSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.Catalog)) throw new ArgumentNullException(nameof(settings.Catalog));
            if (!Directory.Exists(settings.Catalog)) throw new DirectoryNotFoundException(settings.Catalog);

            Settings = settings;

            if (settings.Servers.Count == 0) return;

            int s = 0;
            int i = 0;
            DatabaseInfo database;
            DatabaseServer server;
            string serverCatalogPath;
            string metadataFilePath;
            while (s < settings.Servers.Count)
            {
                server = settings.Servers[s];
                serverCatalogPath = ServerCatalogPath(server.Name);

                if (server == null || string.IsNullOrWhiteSpace(server.Name) || !Directory.Exists(serverCatalogPath))
                {
                    settings.Servers.RemoveAt(s);
                    continue;
                }
                s++;

                if (server.Databases.Count == 0) continue;

                i = 0;
                while (i < server.Databases.Count)
                {
                    database = server.Databases[i];
                    metadataFilePath = MetadataFilePath(server.Name, database.Name);

                    if (database == null || string.IsNullOrWhiteSpace(database.Name) || !File.Exists(metadataFilePath))
                    {
                        server.Databases.RemoveAt(i);
                        continue;
                    }
                    i++;

                    InitializeMetadata(database, metadataFilePath);
                }
            }
        }
        private void InitializeMetadata(DatabaseInfo database, string metadataFilePath)
        {
            XMLLoader.Load(metadataFilePath, database);
            //SQLLoader.Load(ConnectionString, infobase); // TODO: optimize loading of SQL metadata time !
        }
        public void UseServer(string serverAddress)
        {
            if (string.IsNullOrWhiteSpace(serverAddress)) throw new ArgumentNullException(nameof(serverAddress));
            //if (Settings == null) throw new InvalidOperationException(ERROR_SERVICE_IS_NOT_CONFIGURED);

            //string catalogPath = ServerCatalogPath(serverName);
            //if (!Directory.Exists(catalogPath)) throw new DirectoryNotFoundException(catalogPath);

            //DatabaseServer server = Settings.Servers.Where(s => s.Name == serverName).FirstOrDefault();
            //if (server == null)
            //{
            //    server = new DatabaseServer() { Name = serverName };
            //    Settings.Servers.Add(server);
            //}
            
            SqlConnectionStringBuilder csb;
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                csb = new SqlConnectionStringBuilder() { IntegratedSecurity = true };
            }
            else
            {
                csb = new SqlConnectionStringBuilder(ConnectionString);
            }
            csb.DataSource = serverAddress; /*string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address;*/
            ConnectionString = csb.ToString();

            CurrentServer = new DatabaseServer()
            {
                Name = serverAddress,
                Address = serverAddress
            };
        }
        public void UseDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            //if (Settings == null) throw new InvalidOperationException(ERROR_SERVICE_IS_NOT_CONFIGURED);
            if (CurrentServer == null) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            //string metadataFilePath = MetadataFilePath(CurrentServer.Name, databaseName);
            //if (!File.Exists(metadataFilePath)) throw new DirectoryNotFoundException(metadataFilePath);

            DatabaseInfo database = CurrentServer.Databases.Where(db => db.Name == databaseName).FirstOrDefault();
            if (database == null)
            {
                database = new DatabaseInfo() { Name = databaseName };
                //InitializeMetadata(database, metadataFilePath);
                CurrentServer.Databases.Add(database);
            }
            
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = database.Name
            };
            ConnectionString = csb.ToString();

            CurrentDatabase = database;
        }
        public void AttachDatabase(string serverAddress, DatabaseInfo database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(serverAddress)) throw new ArgumentNullException(nameof(serverAddress));

            if (Settings == null) Settings = new MetadataSettings();

            DatabaseServer server = Settings.Servers.Where(s => s.Name == serverAddress).FirstOrDefault();
            if (server == null)
            {
                server = new DatabaseServer() { Name = serverAddress, Address = serverAddress };
                Settings.Servers.Add(server);
            }
            CurrentServer = server;

            DatabaseInfo test = server.Databases.Where(db => db.Name == database.Name).FirstOrDefault();
            if (test == null)
            {
                server.Databases.Add(database);
            }
            CurrentDatabase = database;

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                DataSource = string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address,
                InitialCatalog = database.Name,
                IntegratedSecurity = true,
                PersistSecurityInfo = false
            };
            ConnectionString = csb.ToString();
        }




        private bool IsSpecialSchema(string schemaName)
        {
            return (schemaName == "Перечисление"
                || schemaName == "Справочник"
                || schemaName == "Документ"
                || schemaName == "ПланВидовХарактеристик"
                || schemaName == "ПланСчетов"
                || schemaName == "ПланОбмена"
                || schemaName == "РегистрСведений"
                || schemaName == "РегистрНакопления"
                || schemaName == "РегистрБухгалтерии");
        }
        public MetaObject GetMetaObject(IList<string> tableIdentifiers)
        {
            if (tableIdentifiers == null || tableIdentifiers.Count != 4) { return null; }

            string databaseName = null;
            string serverIdentifier = tableIdentifiers[0];
            string databaseIdentifier = tableIdentifiers[1];
            string schemaIdentifier = tableIdentifiers[2];
            string tableIdentifier = tableIdentifiers[3];

            if (serverIdentifier != null)
            {
                if (tableIdentifier.Contains('+')) // [server].[database].Документ.[ПоступлениеТоваровУслуг+Товары]
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = string.Empty; // dbo
                }
                else
                {
                    if (IsSpecialSchema(databaseIdentifier)) // [database].Документ.ПоступлениеТоваровУслуг.Товары
                    {
                        databaseName = tableIdentifiers[0];
                        tableIdentifiers[3] = $"{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}";
                        tableIdentifiers[2] = string.Empty; // dbo
                        tableIdentifiers[1] = serverIdentifier;
                        tableIdentifiers[0] = null;
                    }
                    else if (IsSpecialSchema(schemaIdentifier)) // [server].[database].Документ.ПоступлениеТоваровУслуг
                    {
                        databaseName = tableIdentifiers[1];
                        tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                        tableIdentifiers[2] = string.Empty; // dbo
                    }
                }
            }
            else if (databaseIdentifier != null)
            {
                if (IsSpecialSchema(databaseIdentifier)) // Документ.ПоступлениеТоваровУслуг.Товары
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = null;
                    tableIdentifiers[1] = null;
                }
                else if (IsSpecialSchema(schemaIdentifier)) // [database].Документ.ПоступлениеТоваровУслуг
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = string.Empty; // dbo
                }
            }
            else if (schemaIdentifier != null)
            {
                if (IsSpecialSchema(schemaIdentifier)) // Документ.ПоступлениеТоваровУслуг
                {
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = null;
                }
            }
            else // ПоступлениеТоваровУслуг or some normal table
            {
                return null;
            }

            return GetMetaObject(databaseName, tableIdentifiers[3]);
        }
        public MetaObject GetMetaObject(string databaseName, string tableIdentifier) // $"[Документ+ПоступлениеТоваровУслуг+Товары]"
        {
            if (!tableIdentifier.Contains('+')) return null; // this is not special format, but schema object (table)

            DatabaseInfo database;
            if (string.IsNullOrEmpty(databaseName))
            {
                database = CurrentDatabase;
            }
            else
            {
                database = CurrentServer.Databases.Where(db => db.Name == databaseName).FirstOrDefault();
            }
            if (database == null) return null;

            string tableName = tableIdentifier.TrimStart('[').TrimEnd(']');
            string[] identifiers = tableName.Split('+');

            // TODO: сделать нормальный поиск базового объекта
            string baseObjectName = identifiers[0];
            if (baseObjectName == "Справочник") baseObjectName = "Reference";
            else if (baseObjectName == "Документ") baseObjectName = "Document";
            else if (baseObjectName == "РегистрСведений") baseObjectName = "InfoRg";
            else if (baseObjectName == "РегистрНакопления") baseObjectName = "AccumRg";

            BaseObject bo = database.BaseObjects.Where(bo => bo.Name == baseObjectName).FirstOrDefault();
            if (bo == null) return null;

            MetaObject @object = bo.MetaObjects.Where(mo => mo.Name == identifiers[1]).FirstOrDefault();
            if (@object == null) return null;

            if (identifiers.Length == 3)
            {
                @object = @object.MetaObjects.Where(mo => mo.Name == identifiers[2]).FirstOrDefault();
                if (@object == null) return null;
            }

            return @object;
        }
        public MetaProperty GetProperty(string databaseName, string tableIdentifier, string columnIdentifier)
        {
            MetaObject @object = GetMetaObject(databaseName, tableIdentifier);
            if (@object == null) return null;

            MetaProperty @property = @object.Properties.Where(p => p.Name == columnIdentifier).FirstOrDefault();
            if (@property == null) return null;
            if (@property.Fields.Count == 0) return null;

            return @property;
        }

        public string MapSchemaIdentifier(string schemaName)
        {
            if (schemaName == "Перечисление"
                || schemaName == "Справочник"
                || schemaName == "Документ"
                || schemaName == "ПланВидовХарактеристик"
                || schemaName == "ПланСчетов"
                || schemaName == "ПланОбмена"
                || schemaName == "РегистрСведений"
                || schemaName == "РегистрНакопления"
                || schemaName == "РегистрБухгалтерии")
            {
                return string.Empty; // default schema name = dbo
            }
            return schemaName;
        }
        public string MapTableIdentifier(string databaseName, string tableIdentifier)
        {
            MetaObject @object = GetMetaObject(databaseName, tableIdentifier);
            if (@object == null)
            {
                return tableIdentifier;
            }
            return @object.TableName;
        }


        public List<DatabaseInfo> GetDatabases(DatabaseServer server)
        {
            UseServer(string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address);

            List<DatabaseInfo> list = new List<DatabaseInfo>();

            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT [name] FROM [sys].[databases] WHERE [owner_sid] > 0x01 ORDER BY [name] ASC;";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        DatabaseInfo database = new DatabaseInfo()
                        {
                            Identity = Guid.NewGuid(),
                            Name = reader.GetString(0)
                        };
                        list.Add(database);
                    }
                }
                catch (Exception error)
                {
                    throw error;
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
            }

            return list;
        }
        private IMetadataProvider MetadataProvider { get; set; } = new OneCSharpMetadataProvider();
        public IMetadataProvider GetMetadataProvider(DatabaseInfo database)
        {
            return MetadataProvider;
        }
    }
}