using DaJet.Http.Client;
using DaJet.Http.Model;
using DaJet.Studio.Model;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Reflection.Metadata;

namespace DaJet.Studio.Components
{
    public partial class MetadataTreeView : ComponentBase, IDisposable
    {
        private bool _disposed;
        internal AppState App { get; private set; }
        public MetadataTreeView(AppState app)
        {
            App = app;

            App.MetadataTreeHasChanged += NotifyStateHasChanged;
        }
        private IEnumerable<ITreeViewItem> Items = new List<MetadataTreeItem>();
        [Parameter] public EventCallback<MetadataTreeItem> SelectedNodeChanged { get; set; }
        private Task NotifySelectedNodeChanged(MetadataTreeItem node)
        {
            if (SelectedNodeChanged.HasDelegate)
            {
                return SelectedNodeChanged.InvokeAsync(node);
            }

            return Task.CompletedTask;
        }
        protected override void OnInitialized()
        {
            Items = GetRootTreeItems();
        }
        private void NotifyStateHasChanged()
        {
            Items = GetRootTreeItems();

            StateHasChanged();

            NotifySelectedNodeChanged(null);
        }
        private IEnumerable<ITreeViewItem> GetRootTreeItems()
        {
            List<TreeViewItem> list = new();

            foreach (DaJetHost host in App.Hosts)
            {
                list.Add(new MetadataTreeItem()
                {
                    Data = host,
                    Text = host.Uri,
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                });
            }

            return list.AsEnumerable();
        }
        
        private Task OnExpandedAsync(TreeViewItemExpandedEventArgs args)
        {
            if (!args.Expanded)
            {
                args.CurrentItem.Items = MetadataTreeItem.LoadingItems;

                return Task.CompletedTask;
            }

            if (args.CurrentItem is not MetadataTreeItem node)
            {
                return Task.CompletedTask;
            }

            if (node.Items is null)
            {
                return Task.CompletedTask;
            }

            if (node.Data is DaJetHost)
            {
                return OpenDaJetHostNode(node);
            }
            else if (node.Data is InfoBase)
            {
                return OpenInfoBaseNode(node);
            }
            else if (node.Data is InfoBaseConfig)
            {
                node.Items = GetMetadataGroupItems(node).AsEnumerable();

                return Task.CompletedTask;
            }
            else if (node.Data is MetadataGroup)
            {
                return OpenMetadataGroupNode(node);
            }

            return Task.CompletedTask;
        }
        private List<MetadataTreeItem> GetMetadataGroupItems(MetadataTreeItem parent)
        {
            List<MetadataTreeItem> items = new(12)
            {
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Publication,
                    Data = new MetadataGroup() { Name = MetadataNames.Publication },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Constant,
                    Data = new MetadataGroup() { Name = MetadataNames.Constant },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Enumeration,
                    Data = new MetadataGroup() { Name = MetadataNames.Enumeration },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Catalog,
                    Data = new MetadataGroup() { Name = MetadataNames.Catalog },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Document,
                    Data = new MetadataGroup() { Name = MetadataNames.Document },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Characteristic,
                    Data = new MetadataGroup() { Name = MetadataNames.Characteristic },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.InformationRegister,
                    Data = new MetadataGroup() { Name = MetadataNames.InformationRegister },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.AccumulationRegister,
                    Data = new MetadataGroup() { Name = MetadataNames.AccumulationRegister },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.Account,
                    Data = new MetadataGroup() { Name = MetadataNames.Account },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.AccountingRegister,
                    Data = new MetadataGroup() { Name = MetadataNames.AccountingRegister },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.BusinessTask,
                    Data = new MetadataGroup() { Name = MetadataNames.BusinessTask },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                },
                new MetadataTreeItem()
                {
                    Parent = parent,
                    Text = MetadataNames.BusinessProcess,
                    Data = new MetadataGroup() { Name = MetadataNames.BusinessProcess },
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                }
            };

            return items;
        }
        public async Task OpenDaJetHostNode(MetadataTreeItem node)
        {
            if (node.Data is not DaJetHost host)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            List<DataSourceStatus> databases = await client.GetDataSources();

            List<MetadataTreeItem> list = new();

            foreach (DataSourceStatus database in databases)
            {
                InfoBase infoBase = new()
                {
                    Name = database.Name
                };

                list.Add(new MetadataTreeItem()
                {
                    Parent = node,
                    Data = infoBase,
                    Text = infoBase.Name,
                    OnExpandedAsync = OnExpandedAsync,
                    Items = MetadataTreeItem.LoadingItems
                });
            }

            node.Items = list.AsEnumerable();
        }
        public async Task OpenInfoBaseNode(MetadataTreeItem node)
        {
            if (node.Data is not InfoBase infoBase)
            {
                return;
            }

            MetadataTreeItem root = node.GetAncestor<DaJetHost>();

            if (root is null || root.Data is not DaJetHost host)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            RequestResult<List<InfoBaseConfig>> result = await client.GetConfigurations(infoBase.Name);

            if (result.Success)
            {
                List<MetadataTreeItem> list = new();

                foreach (InfoBaseConfig config in result.Result)
                {
                    list.Add(new MetadataTreeItem()
                    {
                        Parent = node,
                        Data = config,
                        Text = config.Name,
                        OnExpandedAsync = OnExpandedAsync,
                        Items = MetadataTreeItem.LoadingItems
                    });
                }

                node.Items = list.AsEnumerable();
            }
        }
        public async Task OpenMetadataGroupNode(MetadataTreeItem node)
        {
            if (node.Data is not MetadataGroup group)
            {
                return;
            }

            MetadataTreeItem item = node.GetAncestor<DaJetHost>();

            if (item is null || item.Data is not DaJetHost host)
            {
                return;
            }

            item = node.GetAncestor<InfoBase>();

            if (item is null || item.Data is not InfoBase infoBase)
            {
                return;
            }

            item = node.GetAncestor<InfoBaseConfig>();

            if (item is null || item.Data is not InfoBaseConfig config)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            RequestResult<List<string>> result = await client.GetMetadataNames(infoBase.Name, config.Name, group.Name);

            if (result.Success)
            {
                List<MetadataTreeItem> list = new();

                foreach (string name in result.Result)
                {
                    list.Add(new MetadataTreeItem()
                    {
                        Parent = node,
                        Data = new MetadataObject() { Name = name },
                        Text = name,
                        Items = null // Листовой узел
                    });
                }

                node.Items = list.AsEnumerable();
            }
        }

        private async Task NodeClickHandler(ITreeViewItem node)
        {
            if (node is not MetadataTreeItem item)
            {
                return;
            }

            await NotifySelectedNodeChanged(item);

            if (item.Data is MetadataObject)
            {
                MetadataObjectLocation location = item.GetLocation();

                DaJetHttpClient client = App.GetHttpClient(location.Host);

                RequestResult<EntityDefinition> response = await client.GetMetadataObject(
                    location.Database, location.Type, location.Name);

                if (response.Success)
                {
                    App.AddMetadataTab(location, response.Result);
                }
                else
                {
                    //TODO: show error toast
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                App.MetadataTreeHasChanged -= NotifyStateHasChanged;
            }

            _disposed = true;
        }
    }
}