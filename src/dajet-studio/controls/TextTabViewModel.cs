using DaJet.Studio.MVVM;
using DaJet.UI.Model;
using System;

namespace DaJet.Studio.UI
{
    public sealed class TextTabViewModel : ViewModelBase
    {
        private IServiceProvider Services { get; }
        public TextTabViewModel(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            
        }
        private string _Text = string.Empty;
        public string Text
        {
            get { return _Text; }
            set { _Text = value; OnPropertyChanged(); }
        }
    }
}