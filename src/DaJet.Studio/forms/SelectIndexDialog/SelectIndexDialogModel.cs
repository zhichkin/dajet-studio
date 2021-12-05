using DaJet.Data;
using DaJet.Metadata.Model;
using DaJet.Studio.MVVM;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DaJet.Studio.UI
{
    public sealed class SelectIndexDialogModel : ViewModelBase
    {
        #region "Icons"

        private const string KEY_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/key.png";
        private readonly BitmapImage KEY_ICON = new BitmapImage(new Uri(KEY_ICON_PATH));

        private readonly BitmapImage INDEX_ICON = new BitmapImage(new Uri(INDEX_ICON_PATH));
        private const string INDEX_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/clustered-index.png";

        private readonly BitmapImage ASCENDING_ICON = new BitmapImage(new Uri(ASCENDING_ICON_PATH));
        private const string ASCENDING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/ascending.png";

        private readonly BitmapImage DESCENDING_ICON = new BitmapImage(new Uri(DESCENDING_ICON_PATH));
        private const string DESCENDING_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/descending.png";

        private const string PROPERTY_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Реквизит.png";
        private readonly BitmapImage PROPERTY_ICON = new BitmapImage(new Uri(PROPERTY_ICON_PATH));

        private const string MEASURE_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Ресурс.png";
        private readonly BitmapImage MEASURE_ICON = new BitmapImage(new Uri(MEASURE_ICON_PATH));

        private const string DIMENSION_ICON_PATH = "pack://application:,,,/DaJet.Studio;component/images/Измерение.png";
        private readonly BitmapImage DIMENSION_ICON = new BitmapImage(new Uri(DIMENSION_ICON_PATH));

        #endregion

        public SelectIndexDialogModel(ApplicationObject metaObject, List<IndexInfo> indexes)
        {
            InitializeDialogModel(metaObject, indexes);

            SelectCommand = new RelayCommand(Select);
            CancelCommand = new RelayCommand(Cancel);
        }
        public TreeNodeViewModel IndexesInfo { get; } = new TreeNodeViewModel();
        public void InitializeDialogModel(ApplicationObject metaObject, List<IndexInfo> indexes)
        {
            foreach (IndexInfo index in indexes)
            {
                TreeNodeViewModel indexNode = new TreeNodeViewModel()
                {
                    IsExpanded = true,
                    NodeIcon = GetIndexIcon(index),
                    NodeText = index.Name,
                    NodeToolTip = GetIndexToolTip(index),
                    NodePayload = index
                };
                IndexesInfo.TreeNodes.Add(indexNode);

                foreach (IndexColumnInfo column in index.Columns)
                {
                    TreeNodeViewModel columnNode = new TreeNodeViewModel()
                    {
                        NodeIcon = GetColumnIcon(column),
                        NodeText = column.Name,
                        NodeToolTip = GetColumnToolTip(column),
                        NodePayload = column
                    };

                    MetadataProperty property = GetPropertyByIndexColumn(metaObject, column);

                    if (property == null)
                    {
                        indexNode.TreeNodes.Add(columnNode);
                    }
                    else
                    {
                        TreeNodeViewModel propertyNode = indexNode.GetDescendant(property);
                        
                        if (propertyNode == null)
                        {
                            propertyNode = new TreeNodeViewModel()
                            {
                                NodeIcon = GetMetaPropertyIcon(property),
                                NodeText = property.Name,
                                NodeToolTip = string.Empty,
                                NodePayload = property
                            };
                            indexNode.TreeNodes.Add(propertyNode);
                        }

                        propertyNode.NodeToolTip +=
                            (string.IsNullOrEmpty(propertyNode.NodeToolTip)
                                ? string.Empty
                                : Environment.NewLine)
                            + columnNode.NodeToolTip;

                        propertyNode.TreeNodes.Add(columnNode);
                    }
                }
            }
        }
        private MetadataProperty GetPropertyByIndexColumn(ApplicationObject metaObject, IndexColumnInfo column)
        {
            foreach (MetadataProperty property in metaObject.Properties)
            {
                foreach (DatabaseField field in property.Fields)
                {
                    if (field.Name == column.Name)
                    {
                        return property;
                    }
                }
            }
            return null;
        }
        private BitmapImage GetMetaPropertyIcon(MetadataProperty property)
        {
            if (property == null) { return null; }
            else if (property.Purpose == PropertyPurpose.System && property.IsPrimaryKey()) { return KEY_ICON; }
            else if (property.Purpose == PropertyPurpose.Property && property.IsPrimaryKey()) { return KEY_ICON; }
            else if (property.Purpose == PropertyPurpose.Property) { return PROPERTY_ICON; }
            else if (property.Purpose == PropertyPurpose.Dimension) { return DIMENSION_ICON; }
            else if (property.Purpose == PropertyPurpose.Measure) { return MEASURE_ICON; }
            return PROPERTY_ICON;
        }
        private BitmapImage GetIndexIcon(IndexInfo index)
        {
            if (index.IsPrimaryKey || index.IsClustered)
            {
                return KEY_ICON;
            }
            return INDEX_ICON;
        }
        private BitmapImage GetColumnIcon(IndexColumnInfo column)
        {
            if (column.IsDescending)
            {
                return DESCENDING_ICON;
            }
            return ASCENDING_ICON;
        }
        private string GetIndexToolTip(IndexInfo index)
        {
            return
                (index.IsUnique ? "Unique" : "Non-Unique")
                + ", "
                + (index.IsClustered ? "Clustered" : "Non-Clustered")
                + (index.IsPrimaryKey ? ", Primary key" : string.Empty);
        }
        private string GetColumnToolTip(IndexColumnInfo column)
        {
            return $"{column.KeyOrdinal}. {column.Name} ({(column.IsDescending ? "DESC" : "ASC")}, {(column.IsNullable ? "NULL" : "NOT NULL")})";
        }
        
        
        
        public ICommand SelectCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public Action OnCancel { get; set; }
        public Action<IndexInfo> OnSelect { get; set; }
        private void Select(object parameter)
        {
            IndexInfo index = IndexesInfo
                .SelectedItem?
                .NodePayload as IndexInfo;

            OnSelect?.Invoke(index);
        }
        private void Cancel(object parameter)
        {
            OnCancel?.Invoke();
        }
    }
}