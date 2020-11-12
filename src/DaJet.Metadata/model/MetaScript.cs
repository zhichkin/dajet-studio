using System;
using System.Reflection;

namespace DaJet.Metadata
{
    public sealed class MetaScript
    {
        public Guid Identity { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string SourceCode { get; set; }
        public override string ToString() { return Name; }
        public MetaScript Copy()
        {
            MetaScript copy = new MetaScript();
            this.CopyTo(copy);
            return copy;
        }
        public void CopyTo(MetaScript script)
        {
            foreach (PropertyInfo property in typeof(MetaScript).GetProperties())
            {
                if (property.IsList()) continue;
                if (!property.CanWrite) continue;
                property.SetValue(script, property.GetValue(this));
            }
        }
    }
}