using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitAddinTesting.Forms
{
    /// <summary>
    /// Interaction logic for ViewsTreeSelectionForm.xaml
    /// </summary>
    public partial class ViewsTreeSelectionForm : Window
    {
        //    public ViewsTreeSelectionForm()
        //    {
        //        InitializeComponent();
        //    }

        public List<ViewsTreeNode> TreeData { get; set; }

        public ViewsTreeSelectionForm()
        {
            InitializeComponent();
        }

        // Method to be called after InitializeComponent to set the data context
        public void InitializeTreeData(List<ViewsTreeNode> treeData)
        {
            TreeData = treeData; // TreeData is a property in this form
            viewsTreeView.ItemsSource = TreeData; // Bind the tree data
            this.DataContext = this;  // Setting the data context of the window
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            var selectedViewsCount = TreeData.SelectMany(v => v.Children).Count(v => v.IsSelected);
            if (selectedViewsCount == 0)
            {
                MessageBox.Show("Please select at least one view before proceeding.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
