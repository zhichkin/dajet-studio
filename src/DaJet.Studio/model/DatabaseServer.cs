using System.Collections.Generic;
using System.Reflection;

namespace DaJet.UI.Model
{
    public sealed class DatabaseServer
    {
        public string Name { get; set; }
        public string Address { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<DatabaseInfo> Databases { get; } = new List<DatabaseInfo>();
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Address)
                ? Name
                : string.Format("{0} ({1})", Name, Address);
        }
        public DatabaseServer Copy()
        {
            DatabaseServer server = new DatabaseServer();
            this.CopyTo(server);
            return server;
        }
        public void CopyTo(DatabaseServer server)
        {
            foreach (PropertyInfo property in typeof(DatabaseServer).GetProperties())
            {
                if (!property.CanWrite) continue;
                if (property.Name == nameof(Databases)) continue;
                property.SetValue(server, property.GetValue(this));
            }
        }
    }
}