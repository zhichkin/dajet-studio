using System.Collections.Generic;

namespace DaJet.Metadata
{
    public enum MetaPropertyPurpose
    {
        /// <summary>The property is being used by system.</summary>
        System,
        /// <summary>The property is being used as a property.</summary>
        Property,
        /// <summary>The property is being used as a dimension.</summary>
        Dimension,
        /// <summary>The property is being used as a measure.</summary>
        Measure,
        /// <summary>This property is used to reference parent (adjacency list).</summary>
        Hierarchy
    }
    public sealed class MetaProperty
    {
        public MetaObject Parent { get; set; }
        public int Ordinal { get; set; }
        public string Name { get; set; }
        public string DbName { get; set; }
        public MetaPropertyPurpose Purpose { get; set; }
        public List<int> PropertyTypes { get; set; } = new List<int>();
        public List<TypeInfo> Types { get; set; } = new List<TypeInfo>();
        public List<MetaField> Fields { get; set; } = new List<MetaField>();
        public override string ToString() { return Name; }
    }
    public sealed class TypeInfo
    {
        public int TypeCode { get; set; }
        public string UUID { get; set; }
        public string Name { get; set; }
        public MetaObject MetaObject { get; set; }
    }
}