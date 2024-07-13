﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitAddinTesting.Forms
{
    /// <summary>
    /// Interaction logic for UpdateAppliedDependentViewsForm.xaml
    /// </summary>
    public partial class UpdateAppliedDependentViewsForm : Window
    {
        // Property to hold tree data
        public List<TreeNode> TreeData { get; set; }
        public UpdateAppliedDependentViewsForm()
        {
            InitializeComponent();
        }


        // Method to be called after InitializeComponent to set the data context
        public void InitializeTreeData(List<TreeNode> treeData)
        {
            void DisableLastChildrenCheckboxes(TreeNode node)
            {
                if (node.Header.StartsWith("BIM Setup View"))
                {
                    node.IsEnabled = false; // This property will be used to disable the checkbox
                }

                if (node.Children.Count == 0)
                {
                    node.IsEnabled = false; // This property will be used to disable the checkbox
                }
                else
                {
                    foreach (var child in node.Children)
                    {
                        DisableLastChildrenCheckboxes(child);
                    }
                }
            }

            foreach (var node in treeData)
            {
                DisableLastChildrenCheckboxes(node);
            }

            TreeData = treeData; // TreeData is a property in this form
            viewsTreeView.ItemsSource = TreeData; // Bind the tree data
            this.DataContext = this;  // Setting the data context of the window
        }



        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {

            //var selectedViewsCount = TreeData.SelectMany(v => v.Children).Count(v => v.IsSelected);
            //var selectedViewsCount = TreeData.SelectMany(v => v.Children).Where(vc => vc.IsSelected).ToList();
            var selectedViewsCount = TreeData.SelectMany(v => v.Children).SelectMany(v => v.Children).Where(v => v.IsSelected).ToList();
            //var selectedViewsCount = TreeData.Where(v => v.IsSelected).ToList();
            // IF NO VIEW IS SELECTED
            if (selectedViewsCount.Count == 0)
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

//using System.Collections.Generic;
//using System.Windows;

//namespace RevitAddinTesting.Forms
//{
//    /// <summary>
//    /// Interaction logic for UpdateAppliedDependentViewsForm.xaml
//    /// </summary>
//    public partial class UpdateAppliedDependentViewsForm : Window
//    {
//        // Property to hold tree data
//        public List<TreeNode> TreeData { get; set; }
//        public UpdateAppliedDependentViewsForm()
//        {
//            InitializeComponent();
//        }


//        // Method to be called after InitializeComponent to set the data context
//        public void InitializeTreeData(List<TreeNode> treeData)
//        {
//            TreeData = treeData; // TreeData is a property in this form
//            viewsTreeView.ItemsSource = TreeData; // Bind the tree data
//            this.DataContext = this;  // Setting the data context of the window
//        }

//        private void btn_OK_Click(object sender, RoutedEventArgs e)
//        {
//            this.DialogResult = true;
//            this.Close();
//        }

//        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
//        {
//            this.DialogResult = false;
//            this.Close();
//        }
//    }

//}
