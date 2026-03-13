using DaJet.TypeSystem;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Model
{
    public sealed class MetadataTab
    {
        public string Id { get; } = Identifier.NewId();
        public string Title { get; set; } = string.Empty;
        public EntityDefinition Model { get; set; }
        public MetadataObjectLocation Location { get; set; }
    }
}