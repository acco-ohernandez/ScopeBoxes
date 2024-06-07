using Autodesk.Revit.DB;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

            LevelsListBox.ItemsSource = Levels;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

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
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
        }

        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSelection) return;

            isUpdatingSelection = true;

            foreach (var template in ViewTemplates)
            {
                template.IsSelected = false;
            }

            foreach (ViewTemplateSelection selectedTemplate in ViewTemplatesListBox.SelectedItems)
            {
                selectedTemplate.IsSelected = true;
            }

            // Refresh the ListBox to update the UI
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;

            isUpdatingSelection = false;
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
            LevelsListBox.ItemsSource = null;
            LevelsListBox.ItemsSource = Levels;
        }

        private void ChkBox_SelectAllLevels_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var level in Levels)
            {
                level.IsSelected = false;
            }
            LevelsListBox.ItemsSource = null;
            LevelsListBox.ItemsSource = Levels;
        }

        private void ChkBox_SelectAllTemplates_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = true;
            }
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
        }

        private void ChkBox_SelectAllTemplates_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = false;
            }
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
        }

        private void ListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                ListBox listBox = sender as ListBox;
                if (listBox == null) return;

                ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (item == null) return;

                int index = listBox.ItemContainerGenerator.IndexFromContainer(item);
                if (index < 0) return;

                int minIndex = listBox.SelectedIndex;
                int maxIndex = index;

                if (minIndex > maxIndex)
                {
                    minIndex = index;
                    maxIndex = listBox.SelectedIndex;
                }

                listBox.SelectedItems.Clear();

                for (int i = minIndex; i <= maxIndex; i++)
                {
                    try
                    {
                        listBox.SelectedItems.Add(listBox.Items[i]);
                        if (listBox.Items[i] is LevelSelection level)
                        {
                            level.IsSelected = true;
                        }
                        else if (listBox.Items[i] is ViewTemplateSelection template)
                        {
                            template.IsSelected = true;
                        }
                    }
                    catch { }
                }

                e.Handled = true;

                // Refresh the ListBox to update the UI
                listBox.ItemsSource = null;
                if (listBox == LevelsListBox)
                {
                    listBox.ItemsSource = Levels;
                }
                else if (listBox == ViewTemplatesListBox)
                {
                    listBox.ItemsSource = FilteredViewTemplates;
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void ClearSelectedTemplates_Click(object sender, RoutedEventArgs e)
        {
            foreach (var template in ViewTemplates)
            {
                template.IsSelected = false;
            }
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
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

//// Working 1
//using Autodesk.Revit.DB;

//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;

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


//////////////////////
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

//        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
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
