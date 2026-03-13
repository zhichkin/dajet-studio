using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Model
{
    public sealed class MetadataTreeItem : TreeViewItem
    {
        public static IEnumerable<TreeViewItem> LoadingItems = new TreeViewItem[1]
        {
            new TreeViewItem
            {
                Text = "Загрузка ...",
                Disabled = true
            }
        };
        public MetadataTreeItem GetAncestor<TModel>()
        {
            MetadataTreeItem parent = Parent;

            while (parent is not null)
            {
                if (parent.Data is TModel)
                {
                    return parent;
                }

                parent = parent.Parent;
            }

            return null;
        }
        public object Data { get; set; }
        public MetadataTreeItem Parent { get; set; }
        public string GetFullPath()
        {
            List<string> segments = new();

            MetadataTreeItem node = this;

            while (node is not null)
            {
                segments.Add(node.Text);

                node = node.Parent;
            }

            segments.Reverse();

            return string.Join('/', segments);
        }
        public string GetUri()
        {
            MetadataTreeItem node = GetAncestor<DaJetHost>();

            if (node.Data is not DaJetHost host)
            {
                return string.Empty;
            }

            node = GetAncestor<InfoBase>();

            if (node.Data is not InfoBase database)
            {
                return string.Empty;
            }

            node = GetAncestor<MetadataGroup>();

            if (node.Data is not MetadataGroup type)
            {
                return string.Empty;
            }

            if (this.Data is not MetadataObject entity)
            {
                return string.Empty;
            }

            string uri = $"{host.Uri}/md/entity/{database.Name}/{type.Name}/{entity.Name}";

            return uri;
        }
        
        public MetadataObjectLocation GetLocation()
        {
            MetadataObjectLocation location = new();

            MetadataTreeItem node = GetAncestor<DaJetHost>();

            if (node.Data is not DaJetHost host)
            {
                return null;
            }

            node = GetAncestor<InfoBase>();

            if (node.Data is not InfoBase database)
            {
                return null;
            }

            node = GetAncestor<MetadataGroup>();

            if (node.Data is not MetadataGroup type)
            {
                return null;
            }

            if (this.Data is not MetadataObject entity)
            {
                return null;
            }

            location.Host = host.Uri;
            location.Database = database.Name;
            location.Type = type.Name;
            location.Name = entity.Name;

            return location;
        }
    }
    public sealed class MetadataObjectLocation
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }
}