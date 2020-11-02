using System;

namespace DaJet.Metadata
{
    public sealed class MetaScript
    {
        public Guid Identity { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public override string ToString() { return Name; }
    }
}