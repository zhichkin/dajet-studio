using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Windows.Input;

namespace DaJet.Studio.MVVM
{
    public sealed class TabViewModel : ViewModelBase
    {
        private string _header;
        private object _content;
        private AppSettings Settings { get; }
        private IServiceProvider Services { get; }
        public TabViewModel(IServiceProvider serviceProvider, IOptions<AppSettings> options)
        {
            Settings = options.Value;
            Services = serviceProvider;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            CloseTabCommand = new RelayCommand(CloseTab);
        }
        public string Header
        {
            get { return _header; }
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }
        public object Content
        {
            get { return _content; }
            set { _content = value; OnPropertyChanged(nameof(Content)); }
        }
        public ICommand CloseTabCommand { get; private set; }
        private void CloseTab(object parameter)
        {
            MainWindowViewModel mainWindow = Services.GetService<MainWindowViewModel>();
            mainWindow.RemoveTab(this);
        }
    }
}