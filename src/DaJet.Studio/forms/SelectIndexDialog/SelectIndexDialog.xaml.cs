using DaJet.Data;
using DaJet.Metadata.Model;
using System.Collections.Generic;
using System.Windows;

namespace DaJet.Studio.UI
{
    public partial class SelectIndexDialog : Window
    {
        public SelectIndexDialog(ApplicationObject metaObject, List<IndexInfo> indexes)
        {
            InitializeComponent();
            DataContext = new SelectIndexDialogModel(metaObject, indexes)
            {
                OnSelect = OnSelect,
                OnCancel = OnCancel
            };
        }
        public IndexInfo Result { get; private set; }
        private void OnSelect(IndexInfo result)
        {
            Result = result;
            this.Close();
        }
        private void OnCancel()
        {
            Result = null;
            this.Close();
        }
    }
}