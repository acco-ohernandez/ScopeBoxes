using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Autodesk.Revit.DB;

namespace RevitAddinTesting.Forms
{
    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                FilterViewTemplates();
            }
        }

        private bool _isWildCardEnabled;
        public bool IsWildCardEnabled
        {
            get => _isWildCardEnabled;
            set
            {
                _isWildCardEnabled = value;
                OnPropertyChanged();
                FilterViewTemplates();
            }
        }

        public List<LevelSelection> Levels { get; set; }
        public List<ViewTemplateSelection> ViewTemplates { get; set; }
        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }
        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
        //private bool isUpdatingSelection = false;

        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
        {
            InitializeComponent();
            Levels = levels;
            ViewTemplates = viewTemplates;

            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

            DataContext = this;

            LevelsDataGrid.ItemsSource = Levels;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;

            PopulateFilterComboBox();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PopulateFilterComboBox()
        {
            var uniqueFirstWords = ViewTemplates
                .Select(v => v.Name.Split(' ')[0])
                .Distinct()
                .OrderBy(word => word)
                .ToList();

            FilterComboBox.Items.Clear();
            FilterComboBox.Items.Add("All");
            foreach (var word in uniqueFirstWords)
            {
                FilterComboBox.Items.Add(word);
            }
            FilterComboBox.SelectedIndex = 0;
        }

        private void FilterViewTemplates()
        {
            string filter = FilterText;
            if (string.IsNullOrEmpty(filter) || filter == "All")
            {
                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
            }
            else if (IsWildCardEnabled)
            {
                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
            else
            {
                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            ViewTemplatesDataGrid.ItemsSource = null;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
            if (SelectedViewTemplates.Count == 0)
            {
                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var level in Levels)
            {
                level.IsSelected = true;
            }
            LevelsDataGrid.ItemsSource = null;
            LevelsDataGrid.ItemsSource = Levels;
        }

        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var level in Levels)
            {
                level.IsSelected = false;
            }
            LevelsDataGrid.ItemsSource = null;
            LevelsDataGrid.ItemsSource = Levels;
        }

        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e) // Not in use. XAML checkbox is commented out.
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = true;
            }
            ViewTemplatesDataGrid.ItemsSource = null;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
        }

        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e) // Not in use. XAML checkbox is commented out.
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = false;
            }
            ViewTemplatesDataGrid.ItemsSource = null;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            var dataGridRow = GetParentOfType<DataGridRow>(checkBox);
            if (dataGridRow == null) return;

            var item = dataGridRow.Item;
            bool isChecked = checkBox.IsChecked == true;

            var dataGrid = GetParentOfType<DataGrid>(dataGridRow);
            if (dataGrid == null) return;

            if (dataGrid.SelectedItems.Count > 1 && dataGrid.SelectedItems.Contains(item))
            {
                foreach (var selectedItem in dataGrid.SelectedItems)
                {
                    if (selectedItem is LevelSelection level)
                    {
                        level.IsSelected = isChecked;
                    }
                    else if (selectedItem is ViewTemplateSelection template)
                    {
                        template.IsSelected = isChecked;
                    }
                }
            }
            else
            {
                if (item is LevelSelection level)
                {
                    level.IsSelected = isChecked;
                }
                else if (item is ViewTemplateSelection template)
                {
                    template.IsSelected = isChecked;
                }
            }

            // Refresh the DataGrid to update the UI
            dataGrid.ItemsSource = null;
            if (dataGrid == LevelsDataGrid)
            {
                dataGrid.ItemsSource = Levels;
            }
            else if (dataGrid == ViewTemplatesDataGrid)
            {
                dataGrid.ItemsSource = FilteredViewTemplates;
            }
        }

        private T GetParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T correctlyTyped)
                {
                    return correctlyTyped;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = false;
            }
            ViewTemplatesDataGrid.ItemsSource = null;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
        }
    }

    public class LevelSelection
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public ElementId Id { get; set; }
    }

    public class ViewTemplateSelection
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public ElementId Id { get; set; }
    }
}