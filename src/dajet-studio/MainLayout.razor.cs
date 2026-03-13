using Microsoft.AspNetCore.Components;

namespace DaJet.Studio
{
    public partial class MainLayout : LayoutComponentBase
    {
        private readonly AppState App;
        public MainLayout(AppState app)
        {
            App = app;
        }
    }
}