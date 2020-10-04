using System;
using System.Collections.Generic;

namespace DaJet.Metadata
{
    /// <summary>
    /// Metadata object base type, ex. Catalog, Document, InfoRegister etc.
    /// </summary>
    public sealed class BaseObject
    {
        public string Name { get; set; }
        public List<MetaObject> MetaObjects { get; set; } = new List<MetaObject>();
        public override string ToString() { return Name; }
    }
    public sealed class MetaObject
    {
        public Guid UUID { get; set; }
        public string Token { get; set; } // object base type: catalog, document, table part etc.
        public int TypeCode { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string TableName { get; set; }
        public string Schema { get; set; }
        public MetaObject Owner { get; set; }
        public BaseObject Parent { get; set; }
        public List<MetaProperty> Properties { get; set; } = new List<MetaProperty>();
        public List<MetaObject> MetaObjects { get; set; } = new List<MetaObject>();
        public override string ToString() { return Name; }
    }
}