using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DaJet.Metadata
{
    public sealed class DatabaseInfo
    {
        public Guid Identity { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<MetaScript> Scripts { get; set; } = new List<MetaScript>();
        [JsonIgnore] public List<BaseObject> BaseObjects { get; set; } = new List<BaseObject>();
        public override string ToString() { return Name; }
        public DatabaseInfo Copy()
        {
            DatabaseInfo copy = new DatabaseInfo();
            this.CopyTo(copy);
            return copy;
        }
        public void CopyTo(DatabaseInfo database)
        {
            foreach (PropertyInfo property in typeof(DatabaseInfo).GetProperties())
            {
                if (property.IsList()) continue;
                if (!property.CanWrite) continue;
                property.SetValue(database, property.GetValue(this));
            }
        }
    }
}