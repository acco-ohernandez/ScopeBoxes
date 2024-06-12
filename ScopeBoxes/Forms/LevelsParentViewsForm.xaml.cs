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
        private bool isUpdatingSelection = false;

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

        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = true;
            }
            ViewTemplatesDataGrid.ItemsSource = null;
            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
        }

        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
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


//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Media;

//using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        private string _filterText;
//        public string FilterText
//        {
//            get => _filterText;
//            set
//            {
//                _filterText = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        private bool _isWildCardEnabled;
//        public bool IsWildCardEnabled
//        {
//            get => _isWildCardEnabled;
//            set
//            {
//                _isWildCardEnabled = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
//        private bool isUpdatingSelection = false;

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

//            DataContext = this;

//            LevelsDataGrid.ItemsSource = Levels;
//            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;

//            PopulateFilterComboBox();
//        }

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        private void PopulateFilterComboBox()
//        {
//            var uniqueFirstWords = ViewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Clear();
//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterViewTemplates()
//        {
//            string filter = FilterText;
//            if (string.IsNullOrEmpty(filter) || filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else if (IsWildCardEnabled)
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
//            }
//            ViewTemplatesDataGrid.ItemsSource = null;
//            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
//            if (SelectedViewTemplates.Count == 0)
//            {
//                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }

//        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsSelected = true;
//            }
//            LevelsDataGrid.ItemsSource = null;
//            LevelsDataGrid.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsSelected = false;
//            }
//            LevelsDataGrid.ItemsSource = null;
//            LevelsDataGrid.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = true;
//            }
//            ViewTemplatesDataGrid.ItemsSource = null;
//            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
//        }

//        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesDataGrid.ItemsSource = null;
//            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
//        }

//        private void CheckBox_Click(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            if (checkBox == null) return;

//            var dataGridRow = GetParentOfType<DataGridRow>(checkBox);
//            if (dataGridRow == null) return;

//            var item = dataGridRow.Item;
//            bool isChecked = checkBox.IsChecked == true;

//            if (LevelsDataGrid.SelectedItems.Count > 1 && LevelsDataGrid.SelectedItems.Contains(item))
//            {
//                foreach (var selectedItem in LevelsDataGrid.SelectedItems)
//                {
//                    if (selectedItem is LevelSelection level)
//                    {
//                        level.IsSelected = isChecked;
//                    }
//                }
//            }
//            else if (ViewTemplatesDataGrid.SelectedItems.Count > 1 && ViewTemplatesDataGrid.SelectedItems.Contains(item))
//            {
//                foreach (var selectedItem in ViewTemplatesDataGrid.SelectedItems)
//                {
//                    if (selectedItem is ViewTemplateSelection template)
//                    {
//                        template.IsSelected = isChecked;
//                    }
//                }
//            }
//            else
//            {
//                if (item is LevelSelection level)
//                {
//                    level.IsSelected = isChecked;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsSelected = isChecked;
//                }
//            }
//        }

//        private T GetParentOfType<T>(DependencyObject element) where T : DependencyObject
//        {
//            while (element != null)
//            {
//                if (element is T correctlyTyped)
//                {
//                    return correctlyTyped;
//                }
//                element = VisualTreeHelper.GetParent(element);
//            }
//            return null;
//        }

//        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesDataGrid.ItemsSource = null;
//            ViewTemplatesDataGrid.ItemsSource = FilteredViewTemplates;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}



//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;
//        private List<object> _highlightedRange = new List<object>();
//        private int _lastSelectedIndex = -1;

//        private string _filterText;
//        public string FilterText
//        {
//            get => _filterText;
//            set
//            {
//                _filterText = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        private bool _isWildCardEnabled;
//        public bool IsWildCardEnabled
//        {
//            get => _isWildCardEnabled;
//            set
//            {
//                _isWildCardEnabled = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
//        private bool isUpdatingSelection = false;

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

//            DataContext = this;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            PopulateFilterComboBox();
//        }

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        private void PopulateFilterComboBox()
//        {
//            var uniqueFirstWords = ViewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Clear();
//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterViewTemplates()
//        {
//            string filter = FilterText;
//            if (string.IsNullOrEmpty(filter) || filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else if (IsWildCardEnabled)
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
//            }
//            RefreshListBox(ViewTemplatesListBox, FilteredViewTemplates);
//        }
//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (isUpdatingSelection) return;

//            isUpdatingSelection = true;

//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }

//            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
//            {
//                selectedTemplate.IsHighlighted = true;
//            }

//            // Refresh the ListBox to update the UI
//            RefreshListBox<ViewTemplateSelection>(ViewTemplatesListBox, FilteredViewTemplates);

//            isUpdatingSelection = false;
//        }


//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
//            if (SelectedViewTemplates.Count == 0)
//            {
//                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }

//        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = true;
//            }
//            RefreshListBox<LevelSelection>(LevelsListBox, Levels);
//        }

//        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = false;
//            }
//            RefreshListBox<LevelSelection>(LevelsListBox, Levels);
//        }

//        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = true;
//            }
//            RefreshListBox<ViewTemplateSelection>(ViewTemplatesListBox, FilteredViewTemplates);
//        }

//        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            RefreshListBox<ViewTemplateSelection>(ViewTemplatesListBox, FilteredViewTemplates);
//        }

//        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsChecked = true;
//                if (_highlightedRange.Count >= 1)
//                {
//                    ApplyCheckToHighlightedRange(true, Levels);
//                }
//            }
//        }

//        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsChecked = false;
//                if (_highlightedRange.Count >= 1)
//                {
//                    ApplyCheckToHighlightedRange(false, Levels);
//                }
//            }
//        }

//        private void TemplateCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = true;
//                if (_highlightedRange.Count >= 1)
//                {
//                    ApplyCheckToHighlightedRange(true, ViewTemplates);
//                }
//            }
//        }

//        private void TemplateCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = false;
//                if (_highlightedRange.Count >= 1)
//                {
//                    ApplyCheckToHighlightedRange(false, ViewTemplates);
//                }
//            }
//        }

//        private void ApplyCheckToHighlightedRange<T>(bool isChecked, List<T> items) where T : ISelectable
//        {
//            foreach (var item in _highlightedRange.OfType<T>())
//            {
//                if (items.Contains(item))
//                {
//                    if (item is LevelSelection level)
//                    {
//                        level.IsChecked = isChecked;
//                    }
//                    else if (item is ViewTemplateSelection template)
//                    {
//                        template.IsSelected = isChecked;
//                    }
//                }
//            }
//        }

//        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
//        {
//            ListBox listBox = sender as ListBox;
//            if (listBox == null) return;

//            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
//            if (item == null) return;

//            int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
//            if (index < 0 || index >= listBox.Items.Count) return;

//            if (_highlightedRange.Count >= 1)
//            {
//                ApplyCheckToHighlightedRange(true, Levels);
//                ApplyCheckToHighlightedRange(true, ViewTemplates);
//            }

//            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
//            {
//                HandleShiftSelection(listBox, index);
//                e.Handled = true;
//            }
//            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
//            {
//                HandleCtrlSelection(listBox, index);
//                e.Handled = true;
//            }
//            else
//            {
//                HandleSingleSelection(listBox, index);
//                _lastSelectedIndex = index; // Update the last selected index for shift-selection
//            }

//            // Refresh the ListBox to update the UI
//            RefreshListBox<ViewTemplateSelection>(ViewTemplatesListBox, FilteredViewTemplates);
//            RefreshListBox<LevelSelection>(LevelsListBox, Levels);

//        }

//        private void HandleShiftSelection(ListBox listBox, int index)
//        {
//            if (_lastSelectedIndex < 0 || index < 0 || index >= listBox.Items.Count)
//            {
//                return; // Invalid indices, do nothing
//            }

//            int minIndex = Math.Min(_lastSelectedIndex, index);
//            int maxIndex = Math.Max(_lastSelectedIndex, index);

//            _highlightedRange.Clear();
//            for (int i = minIndex; i <= maxIndex; i++)
//            {
//                var item = listBox.Items[i];
//                _highlightedRange.Add(item);

//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true; // Highlight the selected range
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true; // Highlight the selected range
//                }
//            }
//        }

//        private void HandleCtrlSelection(ListBox listBox, int index)
//        {
//            var item = listBox.Items[index];
//            if (_highlightedRange.Contains(item))
//            {
//                _highlightedRange.Remove(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }
//            else
//            {
//                _highlightedRange.Add(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true;
//                }
//            }
//        }

//        private void HandleSingleSelection(ListBox listBox, int index)
//        {
//            _highlightedRange.Clear();
//            foreach (var item in listBox.Items)
//            {
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }

//            var selectedItem = listBox.Items[index];
//            _highlightedRange.Add(selectedItem);
//            if (selectedItem is LevelSelection singleLevel)
//            {
//                singleLevel.IsHighlighted = true;
//            }
//            else if (selectedItem is ViewTemplateSelection singleTemplate)
//            {
//                singleTemplate.IsHighlighted = true;
//            }
//        }

//        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
//        {
//            while (current != null)
//            {
//                if (current is T)
//                {
//                    return (T)current;
//                }
//                current = VisualTreeHelper.GetParent(current);
//            }
//            return null;
//        }

//        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }
//            RefreshListBox(ViewTemplatesListBox, FilteredViewTemplates);
//        }

//        private void RefreshListBox<T>(ListBox listBox, List<T> items)
//        {
//            listBox.ItemsSource = null;
//            listBox.ItemsSource = items;
//        }
//    }

//    public class LevelSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsChecked
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public class ViewTemplateSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsSelected
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public interface ISelectable
//    {
//        bool IsHighlighted { get; set; }
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;
//        private List<object> _selectedRange = new List<object>();
//        private List<object> _highlightedRange = new List<object>();
//        private int _lastSelectedIndex = -1;

//        private string _filterText;
//        public string FilterText
//        {
//            get => _filterText;
//            set
//            {
//                _filterText = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        private bool _isWildCardEnabled;
//        public bool IsWildCardEnabled
//        {
//            get => _isWildCardEnabled;
//            set
//            {
//                _isWildCardEnabled = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
//        private bool isUpdatingSelection = false;
//        public List<LevelSelection> _LevelSelectionRange { get; set; } = new List<LevelSelection>();

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

//            DataContext = this;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            PopulateFilterComboBox();
//        }

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        private void PopulateFilterComboBox()
//        {
//            var uniqueFirstWords = ViewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Clear();
//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterViewTemplates()
//        {
//            string filter = FilterText;
//            if (string.IsNullOrEmpty(filter) || filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else if (IsWildCardEnabled)
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (isUpdatingSelection) return;

//            isUpdatingSelection = true;

//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }

//            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
//            {
//                selectedTemplate.IsHighlighted = true;
//            }

//            // Refresh the ListBox to update the UI
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            isUpdatingSelection = false;
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
//            if (SelectedViewTemplates.Count == 0)
//            {
//                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }

//        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = true;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = false;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = true;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsChecked = true;
//                ApplyCheckToHighlightedRange(true, Levels);
//            }
//        }

//        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                //dataContext.IsChecked = false;
//                ApplyCheckToHighlightedRange(false, Levels);
//            }
//        }

//        private void TemplateCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = true;
//                ApplyCheckToHighlightedRange(true, ViewTemplates);
//            }
//        }

//        private void TemplateCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = false;
//                ApplyCheckToHighlightedRange(false, ViewTemplates);
//            }
//        }

//        //private void ApplyCheckToHighlightedRange<T>(bool isChecked, List<T> items) where T : ISelectable
//        //{
//        //    foreach (var item in _highlightedRange.OfType<T>())
//        //    {
//        //        if (items.Contains(item))
//        //        {
//        //            if (item is LevelSelection level)
//        //            {
//        //                level.IsChecked = isChecked;
//        //            }
//        //            else if (item is ViewTemplateSelection template)
//        //            {
//        //                template.IsSelected = isChecked;
//        //            }
//        //        }
//        //    }
//        //}
//        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
//        {
//            ListBox listBox = sender as ListBox;
//            if (listBox == null) return;

//            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
//            if (item == null) return;

//            int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
//            if (index < 0 || index >= listBox.Items.Count) return;

//            if (_highlightedRange.Count >= 2)
//            {
//                ApplyCheckToHighlightedRange(true, Levels);
//                ApplyCheckToHighlightedRange(true, ViewTemplates);
//            }

//            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
//            {
//                HandleShiftSelection(listBox, index);
//                e.Handled = true;
//            }
//            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
//            {
//                HandleCtrlSelection(listBox, index);
//                e.Handled = true;
//            }
//            else
//            {
//                HandleSingleSelection(listBox, index);
//                _lastSelectedIndex = index; // Update the last selected index for shift-selection
//            }

//            // Refresh the ListBox to update the UI
//            listBox.ItemsSource = null;
//            if (listBox == LevelsListBox)
//            {
//                listBox.ItemsSource = Levels;
//            }
//            else if (listBox == ViewTemplatesListBox)
//            {
//                listBox.ItemsSource = FilteredViewTemplates;
//            }
//        }

//        private void ApplyCheckToHighlightedRange<T>(bool isChecked, List<T> items) where T : ISelectable
//        {
//            foreach (var item in _highlightedRange.OfType<T>())
//            {
//                if (items.Contains(item))
//                {
//                    if (item is LevelSelection level)
//                    {
//                        level.IsChecked = isChecked;
//                    }
//                    else if (item is ViewTemplateSelection template)
//                    {
//                        template.IsSelected = isChecked;
//                    }
//                }
//            }
//        }

//        private void ListBox_PreviewMouseDown2(object sender, MouseButtonEventArgs e)
//        {
//            ListBox listBox = sender as ListBox;
//            if (listBox == null) return;

//            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
//            if (item == null) return;

//            int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
//            if (index < 0 || index >= listBox.Items.Count) return;

//            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
//            {
//                HandleShiftSelection(listBox, index);
//                e.Handled = true;
//            }
//            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
//            {
//                HandleCtrlSelection(listBox, index);
//                e.Handled = true;
//            }
//            else
//            {
//                HandleSingleSelection(listBox, index);
//                _lastSelectedIndex = index; // Update the last selected index for shift-selection
//            }

//            // Refresh the ListBox to update the UI
//            listBox.ItemsSource = null;
//            if (listBox == LevelsListBox)
//            {
//                listBox.ItemsSource = Levels;
//            }
//            else if (listBox == ViewTemplatesListBox)
//            {
//                listBox.ItemsSource = FilteredViewTemplates;
//            }
//        }

//        private void HandleShiftSelection(ListBox listBox, int index)
//        {
//            if (_lastSelectedIndex < 0 || index < 0 || index >= listBox.Items.Count)
//            {
//                return; // Invalid indices, do nothing
//            }

//            int minIndex = Math.Min(_lastSelectedIndex, index);
//            int maxIndex = Math.Max(_lastSelectedIndex, index);

//            _highlightedRange.Clear();
//            for (int i = minIndex; i <= maxIndex; i++)
//            {
//                var item = listBox.Items[i];
//                _highlightedRange.Add(item);

//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true; // Highlight the selected range
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true; // Highlight the selected range
//                }
//            }
//        }

//        private void HandleCtrlSelection(ListBox listBox, int index)
//        {
//            var item = listBox.Items[index];
//            if (_highlightedRange.Contains(item))
//            {
//                _highlightedRange.Remove(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }
//            else
//            {
//                _highlightedRange.Add(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true;
//                }
//            }
//        }

//        private void HandleSingleSelection(ListBox listBox, int index)
//        {
//            _highlightedRange.Clear();
//            foreach (var item in listBox.Items)
//            {
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }

//            var selectedItem = listBox.Items[index];
//            _highlightedRange.Add(selectedItem);
//            if (selectedItem is LevelSelection singleLevel)
//            {
//                singleLevel.IsHighlighted = true;
//            }
//            else if (selectedItem is ViewTemplateSelection singleTemplate)
//            {
//                singleTemplate.IsHighlighted = true;
//            }
//        }

//        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
//        {
//            while (current != null)
//            {
//                if (current is T)
//                {
//                    return (T)current;
//                }
//                current = VisualTreeHelper.GetParent(current);
//            }
//            return null;
//        }

//        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }
//    }

//    public class LevelSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsChecked
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public class ViewTemplateSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsSelected
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public interface ISelectable
//    {
//        bool IsHighlighted { get; set; }
//    }
//}











//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;
//        private List<object> _selectedRange = new List<object>();
//        private List<object> _highlightedRange = new List<object>();
//        private int _lastSelectedIndex = -1;

//        private string _filterText;
//        public string FilterText
//        {
//            get => _filterText;
//            set
//            {
//                _filterText = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        private bool _isWildCardEnabled;
//        public bool IsWildCardEnabled
//        {
//            get => _isWildCardEnabled;
//            set
//            {
//                _isWildCardEnabled = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
//        private bool isUpdatingSelection = false;
//        public List<LevelSelection> _LevelSelectionRange { get; set; } = new List<LevelSelection>();

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

//            DataContext = this;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            PopulateFilterComboBox();
//        }

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        private void PopulateFilterComboBox()
//        {
//            var uniqueFirstWords = ViewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Clear();
//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterViewTemplates()
//        {
//            string filter = FilterText;
//            if (string.IsNullOrEmpty(filter) || filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else if (IsWildCardEnabled)
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (isUpdatingSelection) return;

//            isUpdatingSelection = true;

//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }

//            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
//            {
//                selectedTemplate.IsHighlighted = true;
//            }

//            // Refresh the ListBox to update the UI
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            isUpdatingSelection = false;
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
//            if (SelectedViewTemplates.Count == 0)
//            {
//                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }

//        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = true;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsChecked = false;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = true;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsChecked = true;
//                ApplyCheckToHighlightedRange(true, Levels);
//            }
//        }

//        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as LevelSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsChecked = false;
//                ApplyCheckToHighlightedRange(false, Levels);
//            }
//        }

//        private void TemplateCheckBox_Checked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = true;
//                ApplyCheckToHighlightedRange(true, ViewTemplates);
//            }
//        }

//        private void TemplateCheckBox_Unchecked(object sender, RoutedEventArgs e)
//        {
//            var checkBox = sender as CheckBox;
//            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

//            if (dataContext != null)
//            {
//                dataContext.IsSelected = false;
//                ApplyCheckToHighlightedRange(false, ViewTemplates);
//            }
//        }

//        private void ApplyCheckToHighlightedRange<T>(bool isChecked, List<T> items) where T : ISelectable
//        {
//            foreach (var item in _highlightedRange.OfType<T>())
//            {
//                if (items.Contains(item))
//                {
//                    if (item is LevelSelection level)
//                    {
//                        level.IsChecked = isChecked;
//                    }
//                    else if (item is ViewTemplateSelection template)
//                    {
//                        template.IsSelected = isChecked;
//                    }
//                }
//            }
//        }

//        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
//        {
//            ListBox listBox = sender as ListBox;
//            if (listBox == null) return;

//            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
//            if (item == null) return;

//            int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
//            if (index < 0 || index >= listBox.Items.Count) return;

//            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
//            {
//                HandleShiftSelection(listBox, index);
//                e.Handled = true;
//            }
//            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
//            {
//                HandleCtrlSelection(listBox, index);
//                e.Handled = true;
//            }
//            else
//            {
//                HandleSingleSelection(listBox, index);
//                _lastSelectedIndex = index; // Update the last selected index for shift-selection
//            }

//            // Refresh the ListBox to update the UI
//            listBox.ItemsSource = null;
//            if (listBox == LevelsListBox)
//            {
//                listBox.ItemsSource = Levels;
//            }
//            else if (listBox == ViewTemplatesListBox)
//            {
//                listBox.ItemsSource = FilteredViewTemplates;
//            }
//        }

//        private void HandleShiftSelection(ListBox listBox, int index)
//        {
//            if (_lastSelectedIndex < 0 || index < 0 || index >= listBox.Items.Count)
//            {
//                return; // Invalid indices, do nothing
//            }

//            int minIndex = Math.Min(_lastSelectedIndex, index);
//            int maxIndex = Math.Max(_lastSelectedIndex, index);

//            _highlightedRange.Clear();
//            for (int i = minIndex; i <= maxIndex; i++)
//            {
//                var item = listBox.Items[i];
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true; // Highlight the selected range
//                    _highlightedRange.Add(level);
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true; // Highlight the selected range
//                    _highlightedRange.Add(template);
//                }
//            }
//        }

//        private void HandleCtrlSelection(ListBox listBox, int index)
//        {
//            var item = listBox.Items[index];
//            if (_highlightedRange.Contains(item))
//            {
//                _highlightedRange.Remove(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }
//            else
//            {
//                _highlightedRange.Add(item);
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = true;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = true;
//                }
//            }
//        }

//        private void HandleSingleSelection(ListBox listBox, int index)
//        {
//            _highlightedRange.Clear();
//            foreach (var item in listBox.Items)
//            {
//                if (item is LevelSelection level)
//                {
//                    level.IsHighlighted = false;
//                }
//                else if (item is ViewTemplateSelection template)
//                {
//                    template.IsHighlighted = false;
//                }
//            }

//            var selectedItem = listBox.Items[index];
//            _highlightedRange.Add(selectedItem);
//            if (selectedItem is LevelSelection singleLevel)
//            {
//                singleLevel.IsHighlighted = true;
//            }
//            else if (selectedItem is ViewTemplateSelection singleTemplate)
//            {
//                singleTemplate.IsHighlighted = true;
//            }
//        }

//        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
//        {
//            while (current != null)
//            {
//                if (current is T)
//                {
//                    return (T)current;
//                }
//                current = VisualTreeHelper.GetParent(current);
//            }
//            return null;
//        }

//        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsHighlighted = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }
//    }

//    public class LevelSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsChecked
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public class ViewTemplateSelection : INotifyPropertyChanged, ISelectable
//    {
//        private bool isChecked;
//        private bool isHighlighted;

//        public string Name { get; set; }
//        public bool IsSelected
//        {
//            get => isChecked;
//            set
//            {
//                if (isChecked != value)
//                {
//                    isChecked = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public bool IsHighlighted
//        {
//            get => isHighlighted;
//            set
//            {
//                if (isHighlighted != value)
//                {
//                    isHighlighted = value;
//                    OnPropertyChanged();
//                }
//            }
//        }
//        public ElementId Id { get; set; }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public interface ISelectable
//    {
//        bool IsHighlighted { get; set; }
//    }
//}


////using System;
////using System.Collections.Generic;
////using System.ComponentModel;
////using System.Linq;
////using System.Runtime.CompilerServices;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Input;
////using System.Windows.Media;

////using Autodesk.Revit.DB;

////namespace RevitAddinTesting.Forms
////{
////    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
////    {
////        public event PropertyChangedEventHandler PropertyChanged;
////        private List<object> _selectedRange = new List<object>();
////        private List<object> _highlightedRange = new List<object>();
////        private int _lastSelectedIndex = -1;

////        private string _filterText;
////        public string FilterText
////        {
////            get => _filterText;
////            set
////            {
////                _filterText = value;
////                OnPropertyChanged();
////                FilterViewTemplates();
////            }
////        }

////        private bool _isWildCardEnabled;
////        public bool IsWildCardEnabled
////        {
////            get => _isWildCardEnabled;
////            set
////            {
////                _isWildCardEnabled = value;
////                OnPropertyChanged();
////                FilterViewTemplates();
////            }
////        }

////        public List<LevelSelection> Levels { get; set; }
////        public List<ViewTemplateSelection> ViewTemplates { get; set; }
////        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

////        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
////        private bool isUpdatingSelection = false;
////        public List<LevelSelection> _LevelSelectionRange { get; set; } = new List<LevelSelection>();

////        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
////        {
////            InitializeComponent();
////            Levels = levels;
////            ViewTemplates = viewTemplates;

////            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

////            DataContext = this;

////            LevelsListBox.ItemsSource = Levels;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

////            PopulateFilterComboBox();
////        }

////        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
////        {
////            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
////        }

////        private void PopulateFilterComboBox()
////        {
////            var uniqueFirstWords = ViewTemplates
////                .Select(v => v.Name.Split(' ')[0])
////                .Distinct()
////                .OrderBy(word => word)
////                .ToList();

////            FilterComboBox.Items.Clear();
////            FilterComboBox.Items.Add("All");
////            foreach (var word in uniqueFirstWords)
////            {
////                FilterComboBox.Items.Add(word);
////            }
////            FilterComboBox.SelectedIndex = 0;
////        }

////        private void FilterViewTemplates()
////        {
////            string filter = FilterText;
////            if (string.IsNullOrEmpty(filter) || filter == "All")
////            {
////                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
////            }
////            else if (IsWildCardEnabled)
////            {
////                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
////            }
////            else
////            {
////                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
////            }
////            ViewTemplatesListBox.ItemsSource = null;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
////        }

////        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
////        {
////            if (isUpdatingSelection) return;

////            isUpdatingSelection = true;

////            foreach (var template in ViewTemplates)
////            {
////                template.IsHighlighted = false;
////            }

////            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
////            {
////                selectedTemplate.IsHighlighted = true;
////            }

////            // Refresh the ListBox to update the UI
////            ViewTemplatesListBox.ItemsSource = null;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

////            isUpdatingSelection = false;
////        }

////        private void OKButton_Click(object sender, RoutedEventArgs e)
////        {
////            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
////            if (SelectedViewTemplates.Count == 0)
////            {
////                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
////                return;
////            }
////            DialogResult = true;
////        }

////        private void CancelButton_Click(object sender, RoutedEventArgs e)
////        {
////            DialogResult = false;
////        }

////        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
////        {
////            foreach (var level in Levels)
////            {
////                level.IsChecked = true;
////            }
////            LevelsListBox.ItemsSource = null;
////            LevelsListBox.ItemsSource = Levels;
////        }

////        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
////        {
////            foreach (var level in Levels)
////            {
////                level.IsChecked = false;
////            }
////            LevelsListBox.ItemsSource = null;
////            LevelsListBox.ItemsSource = Levels;
////        }

////        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
////        {
////            foreach (var template in ViewTemplates)
////            {
////                template.IsSelected = true;
////            }
////            ViewTemplatesListBox.ItemsSource = null;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
////        }

////        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
////        {
////            foreach (var template in ViewTemplates)
////            {
////                template.IsSelected = false;
////            }
////            ViewTemplatesListBox.ItemsSource = null;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
////        }

////        private void LevelCheckBox_Checked(object sender, RoutedEventArgs e)
////        {
////            var checkBox = sender as CheckBox;
////            var dataContext = checkBox?.DataContext as LevelSelection;

////            if (dataContext != null)
////            {
////                dataContext.IsChecked = true;
////                if (!_selectedRange.Contains(dataContext))
////                {
////                    _selectedRange.Add(dataContext);
////                }
////            }
////        }

////        private void LevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
////        {
////            var checkBox = sender as CheckBox;
////            var dataContext = checkBox?.DataContext as LevelSelection;

////            if (dataContext != null)
////            {
////                dataContext.IsChecked = false;
////                _selectedRange.Remove(dataContext);
////            }
////        }

////        private void TemplateCheckBox_Checked(object sender, RoutedEventArgs e)
////        {
////            var checkBox = sender as CheckBox;
////            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

////            if (dataContext != null)
////            {
////                dataContext.IsSelected = true;
////                if (!_selectedRange.Contains(dataContext))
////                {
////                    _selectedRange.Add(dataContext);
////                }
////            }
////        }

////        private void TemplateCheckBox_Unchecked(object sender, RoutedEventArgs e)
////        {
////            var checkBox = sender as CheckBox;
////            var dataContext = checkBox?.DataContext as ViewTemplateSelection;

////            if (dataContext != null)
////            {
////                dataContext.IsSelected = false;
////                _selectedRange.Remove(dataContext);
////            }
////        }

////        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
////        {
////            ListBox listBox = sender as ListBox;
////            if (listBox == null) return;

////            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
////            if (item == null) return;

////            int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
////            if (index < 0 || index >= listBox.Items.Count) return;

////            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
////            {
////                HandleShiftSelection(listBox, index);
////                e.Handled = true;
////            }
////            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
////            {
////                HandleCtrlSelection(listBox, index);
////                e.Handled = true;
////            }
////            else
////            {
////                HandleSingleSelection(listBox, index);
////                _lastSelectedIndex = index; // Update the last selected index for shift-selection
////            }

////            // Refresh the ListBox to update the UI
////            listBox.ItemsSource = null;
////            if (listBox == LevelsListBox)
////            {
////                listBox.ItemsSource = Levels;
////            }
////            else if (listBox == ViewTemplatesListBox)
////            {
////                listBox.ItemsSource = FilteredViewTemplates;
////            }
////        }

////        private void HandleShiftSelection(ListBox listBox, int index)
////        {
////            if (_lastSelectedIndex < 0 || index < 0 || index >= listBox.Items.Count)
////            {
////                return; // Invalid indices, do nothing
////            }

////            int minIndex = Math.Min(_lastSelectedIndex, index);
////            int maxIndex = Math.Max(_lastSelectedIndex, index);

////            _highlightedRange.Clear();
////            for (int i = minIndex; i <= maxIndex; i++)
////            {
////                var item = listBox.Items[i];
////                if (item is LevelSelection level)
////                {
////                    level.IsHighlighted = true; // Highlight the selected range
////                    _highlightedRange.Add(level);
////                }
////                else if (item is ViewTemplateSelection template)
////                {
////                    template.IsHighlighted = true; // Highlight the selected range
////                    _highlightedRange.Add(template);
////                }
////            }
////        }

////        private void HandleCtrlSelection(ListBox listBox, int index)
////        {
////            var item = listBox.Items[index];
////            if (_highlightedRange.Contains(item))
////            {
////                _highlightedRange.Remove(item);
////                if (item is LevelSelection level)
////                {
////                    level.IsHighlighted = false;
////                }
////                else if (item is ViewTemplateSelection template)
////                {
////                    template.IsHighlighted = false;
////                }
////            }
////            else
////            {
////                _highlightedRange.Add(item);
////                if (item is LevelSelection level)
////                {
////                    level.IsHighlighted = true;
////                }
////                else if (item is ViewTemplateSelection template)
////                {
////                    template.IsHighlighted = true;
////                }
////            }
////        }

////        private void HandleSingleSelection(ListBox listBox, int index)
////        {
////            _highlightedRange.Clear();
////            foreach (var item in listBox.Items)
////            {
////                if (item is LevelSelection level)
////                {
////                    level.IsHighlighted = false;
////                }
////                else if (item is ViewTemplateSelection template)
////                {
////                    template.IsHighlighted = false;
////                }
////            }

////            var selectedItem = listBox.Items[index];
////            _highlightedRange.Add(selectedItem);
////            if (selectedItem is LevelSelection singleLevel)
////            {
////                singleLevel.IsHighlighted = true;
////            }
////            else if (selectedItem is ViewTemplateSelection singleTemplate)
////            {
////                singleTemplate.IsHighlighted = true;
////            }
////        }

////        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
////        {
////            while (current != null)
////            {
////                if (current is T)
////                {
////                    return (T)current;
////                }
////                current = VisualTreeHelper.GetParent(current);
////            }
////            return null;
////        }

////        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
////        {
////            foreach (var template in ViewTemplates)
////            {
////                template.IsHighlighted = false;
////            }
////            ViewTemplatesListBox.ItemsSource = null;
////            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
////        }
////    }

////    public class LevelSelection : INotifyPropertyChanged, ISelectable
////    {
////        private bool isChecked;
////        private bool isHighlighted;

////        public string Name { get; set; }
////        public bool IsChecked
////        {
////            get => isChecked;
////            set
////            {
////                if (isChecked != value)
////                {
////                    isChecked = value;
////                    OnPropertyChanged();
////                }
////            }
////        }
////        public bool IsHighlighted
////        {
////            get => isHighlighted;
////            set
////            {
////                if (isHighlighted != value)
////                {
////                    isHighlighted = value;
////                    OnPropertyChanged();
////                }
////            }
////        }
////        public ElementId Id { get; set; }

////        public event PropertyChangedEventHandler PropertyChanged;
////        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
////        {
////            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
////        }
////    }

////    public class ViewTemplateSelection : INotifyPropertyChanged, ISelectable
////    {
////        private bool isChecked;
////        private bool isHighlighted;

////        public string Name { get; set; }
////        public bool IsSelected
////        {
////            get => isChecked;
////            set
////            {
////                if (isChecked != value)
////                {
////                    isChecked = value;
////                    OnPropertyChanged();
////                }
////            }
////        }
////        public bool IsHighlighted
////        {
////            get => isHighlighted;
////            set
////            {
////                if (isHighlighted != value)
////                {
////                    isHighlighted = value;
////                    OnPropertyChanged();
////                }
////            }
////        }
////        public ElementId Id { get; set; }

////        public event PropertyChangedEventHandler PropertyChanged;
////        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
////        {
////            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
////        }
////    }

////    public interface ISelectable
////    {
////        bool IsHighlighted { get; set; }
////    }
////}


//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

//using Autodesk.Revit.DB;

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window, INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        private string _filterText;
//        public string FilterText
//        {
//            get => _filterText;
//            set
//            {
//                _filterText = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        private bool _isWildCardEnabled;
//        public bool IsWildCardEnabled
//        {
//            get => _isWildCardEnabled;
//            set
//            {
//                _isWildCardEnabled = value;
//                OnPropertyChanged();
//                FilterViewTemplates();
//            }
//        }

//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public List<ViewTemplateSelection> SelectedViewTemplates { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }
//        private bool isUpdatingSelection = false;

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);

//            DataContext = this;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            PopulateFilterComboBox();
//        }

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        private void PopulateFilterComboBox()
//        {
//            var uniqueFirstWords = ViewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Clear();
//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterViewTemplates()
//        {
//            string filter = FilterText;
//            if (string.IsNullOrEmpty(filter) || filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else if (IsWildCardEnabled)
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToList();
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (isUpdatingSelection) return;

//            isUpdatingSelection = true;

//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }

//            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
//            {
//                selectedTemplate.IsSelected = true;
//            }

//            // Refresh the ListBox to update the UI
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

//            isUpdatingSelection = false;
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplates = ViewTemplates.Where(v => v.IsSelected).ToList();
//            if (SelectedViewTemplates.Count == 0)
//            {
//                MessageBox.Show("Please select at least one view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }

//        private void ChkBox_SelectAllLevels_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsSelected = true;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var level in Levels)
//            {
//                level.IsSelected = false;
//            }
//            LevelsListBox.ItemsSource = null;
//            LevelsListBox.ItemsSource = Levels;
//        }

//        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = true;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
//        {
//            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
//            {
//                ListBox listBox = sender as ListBox;
//                if (listBox == null) return;

//                ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
//                if (item == null) return;

//                int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
//                if (index < 0) return;

//                int minIndex = listBox.SelectedIndex;
//                int maxIndex = index;

//                if (minIndex > maxIndex)
//                {
//                    minIndex = index;
//                    maxIndex = listBox.SelectedIndex;
//                }

//                listBox.SelectedItems.Clear();

//                for (int i = minIndex; i <= maxIndex; i++)
//                {
//                    try
//                    {
//                        listBox.SelectedItems.Add(listBox.Items[i]);
//                        if (listBox.Items[i] is LevelSelection level)
//                        {
//                            level.IsSelected = true;
//                        }
//                        else if (listBox.Items[i] is ViewTemplateSelection template)
//                        {
//                            template.IsSelected = true;
//                        }
//                    }
//                    catch { }
//                }

//                e.Handled = true;

//                // Refresh the ListBox to update the UI
//                listBox.ItemsSource = null;
//                if (listBox == LevelsListBox)
//                {
//                    listBox.ItemsSource = Levels;
//                }
//                else if (listBox == ViewTemplatesListBox)
//                {
//                    listBox.ItemsSource = FilteredViewTemplates;
//                }
//            }
//        }

//        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
//        {
//            while (current != null)
//            {
//                if (current is T)
//                {
//                    return (T)current;
//                }
//                current = VisualTreeHelper.GetParent(current);
//            }
//            return null;
//        }

//        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
//        {
//            foreach (var template in ViewTemplates)
//            {
//                template.IsSelected = false;
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}
