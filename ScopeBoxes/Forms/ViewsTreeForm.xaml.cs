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

namespace RevitAddinTesting.Forms
{
    /// <summary>
    /// Interaction logic for ViewsTreeForm.xaml
    /// </summary>
    public partial class ViewsTreeForm : Window
    {
        // Property to hold tree data
        public List<TreeNode> TreeData { get; set; }

        public ViewsTreeForm()
        {
            InitializeComponent();
        }

        // Method to be called after InitializeComponent to set the data context
        public void InitializeTreeData(List<TreeNode> treeData)
        {
            TreeData = treeData; // TreeData is a property in this form
            viewsTreeView.ItemsSource = TreeData; // Bind the tree data
            this.DataContext = this;  // Setting the data context of the window
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        //private void viewsTreeView_Loaded(object sender, RoutedEventArgs e)
        //{
        //    ExpandTreeViewItems(viewsTreeView.Items);
        //}

        //private void ExpandTreeViewItems(ItemCollection items)
        //{
        //    foreach (object item in items)
        //    {
        //        if (item is TreeViewItem treeViewItem)
        //        {
        //            treeViewItem.IsExpanded = true;
        //            ExpandTreeViewItems(treeViewItem.Items);
        //        }
        //    }
        //}

    }

}
