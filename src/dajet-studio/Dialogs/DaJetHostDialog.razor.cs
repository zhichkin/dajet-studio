using DaJet.Studio.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace DaJet.Studio.Dialogs
{
    public partial class DaJetHostDialog : ComponentBase, IDialogContentComponent<DaJetHost>
    {
        [Parameter] public DaJetHost Content { get; set; }
        [CascadingParameter] public FluentDialog Dialog { get; set; }
    }
}