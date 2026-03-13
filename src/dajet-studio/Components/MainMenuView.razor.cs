using DaJet.Http.Client;
using DaJet.Http.Model;
using DaJet.Studio.Dialogs;
using DaJet.Studio.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Components
{
    public partial class MainMenuView : ComponentBase
    {
        private readonly AppState App;
        public MainMenuView(AppState app) { App = app; }
        
        private bool DaJetHostMenuIsOpen { get; set; }
        private async Task DaJetHostMenuChangeHandler(MenuChangeEventArgs args)
        {
            if (args.Value == "Добавить ...")
            {
                await NewHostDialog();
            }
            else if (args.Value == "Удалить ...")
            {
                await RemoveHostDialog();
            }
        }
        private async Task NewHostDialog()
        {
            DaJetHost host = new()
            {
                Uri = string.Empty
            };

            await DialogService.ShowDialogAsync<DaJetHostDialog>(host, new DialogParameters()
            {
                Modal = true,
                TrapFocus = true,
                Title = $"Добавление",
                DismissTitle = "Закрыть",
                OnDialogResult = DialogService.CreateDialogCallback(this, NewHostDialogHandler),
                PrimaryAction = "Добавить",
                PrimaryActionEnabled = true,
                SecondaryAction = "Отмена"
            });
        }
        private async Task NewHostDialogHandler(DialogResult result)
        {
            if (result.Cancelled)
            {
                return;
            }

            if (result.Data is not DaJetHost host)
            {
                return;
            }

            if (Uri.TryCreate(host.Uri, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                await App.RegisterNewHost(host);
            }
            else
            {
                _ = await DialogService.ShowErrorAsync(
                    "Указан некорректный URL!",
                    "Добавление нового севера",
                    "Закрыть");
            }
        }
        private async Task RemoveHostDialog()
        {
            MetadataTreeItem node = App.CurrentMetadataNode;

            if (node is null || node.Data is not DaJetHost host)
            {
                await DialogService.ShowInfoAsync(
                    "Сервер не выбран.",
                    "Удаление",
                    "Выберите сервер в дереве левой панели");
                
                return;
            }

            IDialogReference dialog = await DialogService
                .ShowConfirmationAsync($"Удалить {host.Uri} из списка ?", "Да", "Нет", "Удаление");

            DialogResult result = await dialog.Result;

            if (result.Cancelled)
            {
                return;
            }

            await App.UnregisterHost(host);
        }

        private bool DatabaseMenuIsOpen { get; set; }
        private async Task DatabaseMenuChangeHandler(MenuChangeEventArgs args)
        {
            if (args.Value == "Добавить ...")
            {
                await NewInfoBaseDialog();
            }
            else if (args.Value == "Обновить ...")
            {
                await ResetInfoBaseDialog();
            }
            else if (args.Value == "Удалить ...")
            {
                await RemoveInfoBaseDialog();
            }
        }
        private async Task NewInfoBaseDialog()
        {
            MetadataTreeItem node = App.CurrentMetadataNode;

            if (node is null || node.Data is not DaJetHost host)
            {
                await DialogService.ShowInfoAsync(
                    "Сервер не выбран.",
                    "Удаление",
                    "Выберите сервер в дереве левой панели");

                return;
            }

            DataSourceRecord data = new()
            {
                Name = "ms-database",
                Type = "SqlServer",
                Path = "Data Source=MyServer;Initial Catalog=MyDatabase;Integrated Security=True;Encrypt=False;"
            };

            IDialogReference dialog = await DialogService.ShowDialogAsync<InfoBaseDialog>(data, new DialogParameters()
            {
                Modal = true,
                TrapFocus = true,
                Title = $"Добавление",
                DismissTitle = "Закрыть",
                PrimaryAction = "Добавить",
                PrimaryActionEnabled = true,
                SecondaryAction = "Отмена"
            });

            DialogResult answer = await dialog.Result;

            if (answer.Cancelled) { return; }

            if (answer.Data is not DataSourceRecord infoBase)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            RequestResult response = await client.CreateDataSource(infoBase);

            if (response.Success)
            {
                //TODO: MetadataTreeView.AddDatabaseNode();

                await App.NotifyMetadataTreeHasChanged();

                ToastService.ShowSuccess($"{infoBase.Name}: {response.Message}", 3000);
            }
            else
            {
                ToastService.ShowError(response.Message, 5000);
            }
        }
        private async Task ResetInfoBaseDialog()
        {
            MetadataTreeItem node = App.CurrentMetadataNode;

            if (node is null || node.Data is not InfoBase infoBase)
            {
                await DialogService.ShowInfoAsync(
                    "База данных не выбрана.",
                    "Удаление",
                    "Выберите базу данных в дереве левой панели");

                return;
            }

            IDialogReference dialog = await DialogService
                .ShowConfirmationAsync($"Обновить кэш базы данных {infoBase.Name} ?", "Да", "Нет", "Обновление");

            DialogResult result = await dialog.Result;

            if (result.Cancelled)
            {
                return;
            }

            MetadataTreeItem root = node.GetAncestor<DaJetHost>();

            if (root is null || root.Data is not DaJetHost host)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            RequestResult response = await client.ResetDataSource(infoBase.Name);

            if (response.Success)
            {
                ToastService.ShowSuccess($"{infoBase.Name}: {response.Message}", 3000);
            }
            else
            {
                ToastService.ShowError(response.Message, 5000);
            }
        }
        private async Task RemoveInfoBaseDialog()
        {
            MetadataTreeItem node = App.CurrentMetadataNode;

            if (node is null || node.Data is not InfoBase infoBase)
            {
                await DialogService.ShowInfoAsync(
                    "База данных не выбрана.",
                    "Удаление",
                    "Выберите базу данных в дереве левой панели");

                return;
            }

            IDialogReference dialog = await DialogService
                .ShowConfirmationAsync($"Удалить {infoBase.Name} из списка ?", "Да", "Нет", "Удаление");

            DialogResult result = await dialog.Result;

            if (result.Cancelled)
            {
                return;
            }

            MetadataTreeItem root = node.GetAncestor<DaJetHost>();

            if (root is null || root.Data is not DaJetHost host)
            {
                return;
            }

            DaJetHttpClient client = App.GetHttpClient(host.Uri);

            RequestResult response = await client.DeleteDataSource(infoBase.Name);

            if (response.Success)
            {
                if (root.Items is List<MetadataTreeItem> list)
                {
                    MetadataTreeItem toRemove = null;

                    foreach (MetadataTreeItem test in list)
                    {
                        if (test.Text == infoBase.Name)
                        {
                            toRemove = test; break;
                        }
                    }

                    if (toRemove is not null && list.Remove(toRemove))
                    {
                        //TODO: MetadataTreeView.RemoveDatabaseNode();

                        await App.NotifyMetadataTreeHasChanged();
                    }
                }
            }

            if (response.Success)
            {
                ToastService.ShowSuccess($"{infoBase.Name}: {response.Message}", 3000);
            }
            else
            {
                ToastService.ShowError(response.Message, 5000);
            }
        }

        private async Task ScriptingMenuButtonClickHandler(MouseEventArgs args)
        {
            _ = await DialogService.ShowInfoAsync(
                "Опция находится в разработке ...",
                "Разработка",
                "Принято");
        }
    }
}