using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DaJet.Metadata
{
    public sealed class DatabaseInfo
    {
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        [JsonIgnore] public List<BaseObject> BaseObjects { get; set; } = new List<BaseObject>();
        public override string ToString() { return Name; }
    }
}