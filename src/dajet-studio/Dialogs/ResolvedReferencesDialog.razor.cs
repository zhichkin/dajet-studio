using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Dialogs
{
    public partial class ResolvedReferencesDialog : ComponentBase, IDialogContentComponent<List<string>>
    {
        [Parameter] public List<string> Content { get; set; }
        [CascadingParameter] public FluentDialog Dialog { get; set; }
    }
}