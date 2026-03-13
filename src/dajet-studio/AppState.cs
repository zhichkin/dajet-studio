using DaJet.Http.Client;
using DaJet.Studio.Model;
using DaJet.TypeSystem;
using Microsoft.JSInterop;
using System.Reflection;
using System.Text.Json;

namespace DaJet.Studio
{
    public sealed class AppState
    {
        private readonly IJSRuntime JSRuntime;
        private readonly IHttpClientFactory HttpFactory;
        public AppState(IJSRuntime runtime, IHttpClientFactory factory)
        {
            JSRuntime = runtime;
            HttpFactory = factory;
        }
        public string Version { get; private set; }
        public List<DaJetHost> Hosts { get; } = new();
        public MetadataTreeItem CurrentMetadataNode { get; set; }
        public List<MetadataTab> MetadataTabs { get; } = new();
        
        public event Action MetadataTreeHasChanged;
        public async Task NotifyMetadataTreeHasChanged()
        {
            MetadataTreeHasChanged?.Invoke();
        }

        public event Action MetadataTabsHasChanged;
        public void NotifyMetadataTabsHasChanged()
        {
            MetadataTabsHasChanged?.Invoke();
        }

        public async Task InitializeOnStartup()
        {
            Version = GetDaJetStudioVersion();

            string json = await GetStorageItem("DaJetHosts").ConfigureAwait(false);

            List<DaJetHost> hosts;

            if (json is not null)
            {
                hosts = JsonSerializer.Deserialize<List<DaJetHost>>(json);
            }
            else
            {
                hosts = new List<DaJetHost>();
            }

            if (hosts.Count == 0)
            {
                hosts.Add(new DaJetHost()
                {
                    Uri = "http://localhost:5000"
                });

                json = JsonSerializer.Serialize(hosts);

                await SetStorageItem("DaJetHosts", json).ConfigureAwait(false);
            }

            Hosts.AddRange(hosts);

            //foreach (DaJetHost host in hosts)
            //{
            //    TreeNodeViewModel node = new()
            //    {
            //        Model = host,
            //        Title = host.Uri,
            //        Nodes = [TreeNodeViewModel.NoDataNode],
            //        OnExpanded = OpenDaJetHostNode
            //    };

            //    MainTree.Nodes.Add(node);
            //}
        }
        private static string GetDaJetStudioVersion()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            string versionNumber = string.Empty;

            if (version is not null)
            {
                versionNumber += $"{version.Major}.{version.Minor}.{version.Build}";
            }

            return versionNumber;
        }
        internal string GetHeaderText()
        {
            return $"DaJet Studio {Version}";
        }
        internal string GetFooterText()
        {
            MetadataTreeItem node = CurrentMetadataNode;

            if (node is null)
            {
                return "DaJet Studio © 2026";
            }

            return node.GetFullPath();
        }
        internal Task SelectedMetadataNodeChanged(MetadataTreeItem node)
        {
            CurrentMetadataNode = node;

            return Task.CompletedTask;
        }

        public async Task SetStorageItem(string key, string value)
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", key, value).ConfigureAwait(false);
        }
        public async Task<string> GetStorageItem(string key)
        {
            try
            {
                return await JSRuntime.InvokeAsync<string>("localStorage.getItem", key).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                return error.Message;
            }
        }
        public async Task RemoveStorageItem(string key)
        {
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", key).ConfigureAwait(false);
        }

        public DaJetHttpClient GetHttpClient(in string host)
        {
            HttpClient client = HttpFactory.CreateClient(host);

            client.BaseAddress = new Uri(host);

            return new DaJetHttpClient(client);
        }

        public async Task RegisterNewHost(DaJetHost host)
        {
            Hosts.Add(host);

            string json = JsonSerializer.Serialize(Hosts);

            await SetStorageItem("DaJetHosts", json).ConfigureAwait(false);

            await NotifyMetadataTreeHasChanged();
        }
        public async Task UnregisterHost(DaJetHost host)
        {
            if (Hosts.Remove(host))
            {
                if (CurrentMetadataNode is not null && CurrentMetadataNode.Data == host)
                {
                    CurrentMetadataNode = null;
                }

                string json = JsonSerializer.Serialize(Hosts);

                await SetStorageItem("DaJetHosts", json).ConfigureAwait(false);

                await NotifyMetadataTreeHasChanged();
            }
        }

        public void AddMetadataTab(MetadataObjectLocation location, EntityDefinition entity)
        {
            MetadataTab tab = new()
            {
                Model = entity,
                Title = entity.Name,
                Location = location
            };

            MetadataTabs.Add(tab);

            NotifyMetadataTabsHasChanged();
        }
        public void RemoveMetadataTab(string id)
        {
            MetadataTab toRemove = null;

            foreach (MetadataTab tab in MetadataTabs)
            {
                if (tab.Id == id)
                {
                    toRemove = tab; break;
                }
            }

            if (toRemove is not null && MetadataTabs.Remove(toRemove))
            {
                NotifyMetadataTabsHasChanged();
            }
        }
    }
}