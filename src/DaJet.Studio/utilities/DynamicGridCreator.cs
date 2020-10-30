using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using WPF = System.Windows.Controls;

namespace DaJet.Studio
{
    public static class DynamicGridCreator
    {
        public static Grid CreateDynamicGrid(dynamic data)
        {
            Grid grid = new Grid()
            {
                ShowGridLines = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            if (data is IList list)
            {
                RowDefinition rowDef;
                rowDef = new RowDefinition()
                {
                    Height = new GridLength()
                };
                grid.RowDefinitions.Add(rowDef); // headers row

                for (int i = 0; i < list.Count; i++)
                {
                    rowDef = new RowDefinition()
                    {
                        Height = new GridLength()
                    };
                    grid.RowDefinitions.Add(rowDef);
                }

                if (list.Count > 0)
                {
                    ExpandoObject item = list[0] as ExpandoObject;
                    int ii = 0;
                    foreach (var column in item)
                    {
                        WPF.ColumnDefinition colDef = new WPF.ColumnDefinition()
                        {
                            Width = new GridLength(1, GridUnitType.Auto)
                        };
                        grid.ColumnDefinitions.Add(colDef);

                        TextBlock block = new TextBlock()
                        {
                            Text = column.Key,
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            VerticalAlignment = VerticalAlignment.Top
                        };
                        Grid.SetRow(block, 0);
                        Grid.SetColumn(block, ii);
                        grid.Children.Add(block);
                        ii++;
                    }
                }
                int r = 0;
                int c = 0;
                foreach (ExpandoObject obj in list)
                {
                    r++;
                    foreach (var item in obj)
                    {
                        TextBlock block = new TextBlock()
                        {
                            Text = item.Value == null ? string.Empty : item.Value.ToString(),
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetRow(block, r);
                        Grid.SetColumn(block, c);
                        grid.Children.Add(block);
                        c++;
                    }
                }
            }

            return grid;
        }
        public static DataGrid CreateDynamicDataGrid(dynamic data)
        {
            List<Dictionary<string, object>> source = new List<Dictionary<string, object>>();
            if (data is IEnumerable list)
            {
                foreach (ExpandoObject item in list)
                {
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    foreach (var value in item)
                    {
                        row.Add(value.Key.Replace('-', '_'), value.Value);
                    }
                    source.Add(row);
                }
            }
            DataGrid grid = new DataGrid()
            {
                ItemsSource = source.ToDataSource(),
                AutoGenerateColumns = true,
                CanUserResizeColumns = true
            };
            return grid;
        }
    }
}