using DaJet.Studio.MVVM;
using Microsoft.Extensions.Options;
using System;

namespace DaJet.Studio
{
    public sealed class ScriptEditorViewModel : ViewModelBase
    {
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public ScriptEditorViewModel(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {

        }
    }
}