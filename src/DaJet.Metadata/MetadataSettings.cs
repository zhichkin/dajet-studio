using System.Collections.Generic;

namespace DaJet.Metadata
{
    public sealed class MetadataSettings
    {
        public string Catalog { get; set; } = string.Empty;
        public List<DatabaseServer> Servers { get; set;  } = new List<DatabaseServer>();
        public MetadataSettings SettingsCopy() // This method is used to save settings to file
        {
            MetadataSettings copy = new MetadataSettings()
            {
                Catalog = this.Catalog
            };
            foreach (DatabaseServer server in Servers)
            {
                DatabaseServer serverCopy = new DatabaseServer()
                {
                    Name = server.Name,
                    Address = server.Address,
                    UserName = server.UserName,
                    Password = server.Password
                };
                foreach (DatabaseInfo database in server.Databases)
                {
                    serverCopy.Databases.Add(new DatabaseInfo()
                    {
                        Name = database.Name,
                        UserName = database.UserName,
                        Password = database.Password
                    });
                }
                copy.Servers.Add(serverCopy);
            }
            return copy;
        }
    }
}