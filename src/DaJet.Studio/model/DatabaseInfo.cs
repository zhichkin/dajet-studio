using DaJet.Metadata.Model;
using System.Reflection;

namespace DaJet.UI.Model
{
    public sealed class DatabaseInfo
    {
        public InfoBase InfoBase { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
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
                if (!property.CanWrite && property.Name == "InfoBase")
                {
                    continue;
                }
                property.SetValue(database, property.GetValue(this));
            }
        }
    }
}