using DaJet.Http.Client;
using DaJet.Studio.Dialogs;
using DaJet.Studio.Model;
using DaJet.TypeSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Pages
{
    public partial class Home : ComponentBase, IDisposable
    {
        private bool _disposed;
        private readonly AppState App;
        public Home(AppState app)
        {
            App = app;
            App.MetadataTabsHasChanged += NotifyStateHasChanged;
        }
        private void NotifyStateHasChanged()
        {
            StateHasChanged();
        }
        private Task CloseTabHandler(FluentTab tab)
        {
            App.RemoveMetadataTab(tab.Id);

            return Task.CompletedTask;
        }
        private async Task ResolveReferenceCommand(MetadataTab tab, PropertyDefinition property)
        {
            DaJetHttpClient client = App.GetHttpClient(tab.Location.Host);

            RequestResult<List<string>> response = await client.ResolveReferences(
                tab.Location.Database, property.References);

            if (response.Success)
            {
                _ = await DialogService.ShowDialogAsync<ResolvedReferencesDialog>(response.Result,
                    new DialogParameters()
                    {
                        Modal = true,
                        TrapFocus = true,
                        Title = property.Name,
                        DismissTitle = "Закрыть",
                        PrimaryActionEnabled = false,
                        SecondaryActionEnabled = true,
                        SecondaryAction = "Закрыть"
                    });
            }
            else
            {
                ToastService.ShowError(response.Message, 5000);
            }
        }
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (_disposed) { return; }
            if (disposing) { App.MetadataTabsHasChanged -= NotifyStateHasChanged; }
            _disposed = true;
        }
    }
}