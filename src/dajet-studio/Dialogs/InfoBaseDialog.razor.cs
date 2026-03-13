using DaJet.Http.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Dialogs
{
    public partial class InfoBaseDialog : ComponentBase, IDialogContentComponent<DataSourceRecord>
    {
        private static List<Option<string>> DataSourceTypeOptions = new()
        {
            { new Option<string> { Value = "SQLServer", Text = "SQL Server", Selected = true } },
            { new Option<string> { Value = "PostgreSql", Text = "PostgreSQL" } }
        };
        [Parameter] public DataSourceRecord Content { get; set; }
        [CascadingParameter] public FluentDialog Dialog { get; set; }
        private void SelectedOptionChangedHandler(Option<string> option)
        {
            if (option.Value == "SQLServer")
            {
                Content.Path = "Data Source=MyServer;Initial Catalog=MyDatabase;Integrated Security=True;Encrypt=False;";
            }
            else if (option.Value == "PostgreSql")
            {
                Content.Path = "Host=localhost;Port=5432;Database=MyDatabase;Username=postgres;Password=postgres;";
            }
        }
    }
}