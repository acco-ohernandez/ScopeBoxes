using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Autodesk.Revit.DB;

namespace RevitAddinTesting.Forms
{
    public partial class LevelsParentViewsForm : Window
    {
        public List<LevelSelection> Levels { get; set; }
        public List<ViewTemplateSelection> ViewTemplates { get; set; }
        public ViewTemplateSelection SelectedViewTemplate { get; set; }

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

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string filter = FilterComboBox.SelectedItem as string;
            if (filter == "All")
            {
                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
            }
            else
            {
                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
            }
            ViewTemplatesListBox.ItemsSource = null;
            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
        }

        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSelection) return;

            isUpdatingSelection = true;

            ViewTemplateSelection selectedTemplate = null;

            if (ViewTemplatesListBox.SelectedItem is ViewTemplateSelection selectedItem)
            {
                foreach (var template in ViewTemplates)
                {
                    template.IsSelected = false;
                }

                selectedTemplate = selectedItem;
                selectedTemplate.IsSelected = true;
            }

            // Reset the selected item to avoid infinite loop
            ViewTemplatesListBox.SelectedItem = null;
            ViewTemplatesListBox.SelectedItem = selectedTemplate;

            isUpdatingSelection = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
            if (SelectedViewTemplate == null)
            {
                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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


///////////////////////////  10 kinda working
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }

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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//            ViewTemplatesListBox.ItemsSource = null;
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (ViewTemplatesListBox.SelectedItem is ViewTemplateSelection selectedTemplate)
//            {
//                foreach (var template in ViewTemplates)
//                {
//                    template.IsSelected = false;
//                }

//                selectedTemplate.IsSelected = true;
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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

///////////////////////////  9 kinda working, checkbox not working
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        private List<ViewTemplateSelection> FilteredViewTemplates { get; set; }

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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                FilteredViewTemplates = new List<ViewTemplateSelection>(ViewTemplates);
//            }
//            else
//            {
//                FilteredViewTemplates = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//            ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (ViewTemplatesListBox.SelectedItem is ViewTemplateSelection selectedTemplate)
//            {
//                foreach (var template in ViewTemplates)
//                {
//                    template.IsSelected = false;
//                }

//                selectedTemplate.IsSelected = true;
//                ViewTemplatesListBox.ItemsSource = FilteredViewTemplates;
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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

///////////////////////////  8 kinda working
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void ViewTemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if (ViewTemplatesListBox.SelectedItem is ViewTemplateSelection selectedTemplate)
//            {
//                foreach (var template in ViewTemplates)
//                {
//                    template.IsSelected = false;
//                }

//                selectedTemplate.IsSelected = true;
//                ViewTemplatesListBox.ItemsSource = null;
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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

/////////////////////////// 7 working
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}

/////////////////////////// 6 working

//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}

/////////////////////////// 5 not working
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}

/////////////////////////// 4 works
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}

/////////////////////////// 3 works
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            DataContext = this; // Set the DataContext

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}


/////////////////////////// 2
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box with unique first words
//            PopulateFilterComboBox();
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

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}


/////////////////////////// 1
//namespace RevitAddinTesting.Forms
//{
//    public partial class LevelsParentViewsForm : Window
//    {
//        public List<LevelSelection> Levels { get; set; }
//        public List<ViewTemplateSelection> ViewTemplates { get; set; }
//        public ViewTemplateSelection SelectedViewTemplate { get; set; }

//        public LevelsParentViewsForm(List<LevelSelection> levels, List<ViewTemplateSelection> viewTemplates)
//        {
//            InitializeComponent();
//            Levels = levels;
//            ViewTemplates = viewTemplates;

//            LevelsListBox.ItemsSource = Levels;
//            ViewTemplatesListBox.ItemsSource = ViewTemplates;

//            // Populate the filter combo box
//            var uniqueFirstWords = viewTemplates
//                .Select(v => v.Name.Split(' ')[0])
//                .Distinct()
//                .OrderBy(word => word)
//                .ToList();

//            FilterComboBox.Items.Add("All");
//            foreach (var word in uniqueFirstWords)
//            {
//                FilterComboBox.Items.Add(word);
//            }
//            FilterComboBox.SelectedIndex = 0;
//        }

//        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            string filter = FilterComboBox.SelectedItem as string;
//            if (filter == "All")
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates;
//            }
//            else
//            {
//                ViewTemplatesListBox.ItemsSource = ViewTemplates.Where(v => v.Name.StartsWith(filter)).ToList();
//            }
//        }

//        private void OKButton_Click(object sender, RoutedEventArgs e)
//        {
//            SelectedViewTemplate = ViewTemplates.FirstOrDefault(v => v.IsSelected);
//            if (SelectedViewTemplate == null)
//            {
//                MessageBox.Show("Please select a view template.");
//                return;
//            }
//            DialogResult = true;
//        }

//        private void CancelButton_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//        }
//    }

//    public class LevelSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//    }

//    public class ViewTemplateSelection
//    {
//        public string Name { get; set; }
//        public bool IsSelected { get; set; }
//        public ElementId Id { get; set; }
//    }
//}

