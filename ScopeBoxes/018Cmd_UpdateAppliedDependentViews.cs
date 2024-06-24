#region Namespaces
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitAddinTesting.Forms;

using WinData = System.Windows.Data;
#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_UpdateAppliedDependentViews : IExternalCommand
    {
        public bool DependentViewsMatchBIMSetupViews { get; set; } = true;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Check if the 'BIM Set Up View' exists, exclude dependent views with the same name
            var BIMSetupView = Cmd_DependentViewsBrowserTree.GetAllViews(doc)
                                                            .Where(v => v.Name.StartsWith("BIM Set Up View") && !v.IsTemplate && v.GetPrimaryViewId() == ElementId.InvalidElementId)
                                                            .FirstOrDefault();

            if (BIMSetupView == null)
            {
                TaskDialog.Show("INFO", "No 'BIM Setup View' found");
                return Result.Cancelled;
            }

            // Retrieve dependent views of 'BIM Set Up View'
            List<View> BIMSetupViewDependentViews = MyUtils.GetDependentViewsFromParentView(BIMSetupView);

            if (BIMSetupViewDependentViews.Count == 0)
            {
                TaskDialog.Show("INFO", "'BIM Set Up View' has no dependent views");
                return Result.Cancelled;
            }

            // Get the dependent views assigned Scope Boxes
            var dependentViewsWithScopeBoxParams = new List<Parameter>();
            foreach (View dependentView in BIMSetupViewDependentViews)
            {
                var assignedScopeBox = MyUtils.GetAssignedScopeBox(dependentView);
                dependentViewsWithScopeBoxParams.Add(assignedScopeBox);
            }

            // Check if dependent views exist
            var dependentViews = Cmd_DependentViewsBrowserTree.GetOnlyDependentViews(doc);
            if (dependentViews.Count == 0)
            {
                TaskDialog.Show("Info:", "There are no dependent views.");
                return Result.Cancelled;
            }

            // Retrieve parent IDs of dependent views
            List<ElementId> parentViewIds = new List<ElementId>();
            foreach (View dependentView in dependentViews)
            {
                ElementId parentViewId = dependentView.GetPrimaryViewId();
                parentViewIds.Add(parentViewId);
            }

            // All dependenet views selected by the user from the UpdateAppliedDependentViewsForm
            List<View> selectedDependentViews = GetAllDependentViesFromViewsTreeForm(doc);
            if (selectedDependentViews == null) { return Result.Cancelled; } // Cancel if user closes or cancels the form

            var listOfListsOfViews = GroupViewsByPrimaryViewId(selectedDependentViews);


            List<Dictionary<View, Element>> listOfDependentViewsDictionaries = GetListOfDependentViewsDictionaries(doc, listOfListsOfViews, dependentViewsWithScopeBoxParams);

            if (DependentViewsMatchBIMSetupViews == false)
            {
                TaskDialog.Show("Info", $"The number of dependent views does not match the number of scope boxes.");
                return Result.Cancelled;
            }


            //// Create a dictionary to map views to scope boxes
            //Dictionary<View, Element> viewsAndScopeBoxes = new Dictionary<View, Element>();
            //if (selectedDependentViews == null) { return Result.Cancelled; }
            //if (selectedDependentViews.Count == dependentViewsWithScopeBoxParams.Count)
            //{
            //    for (int i = 0; i < selectedDependentViews.Count; i++)
            //    {
            //        viewsAndScopeBoxes.Add(selectedDependentViews[i], doc.GetElement(dependentViewsWithScopeBoxParams[i].AsElementId()));
            //    }
            //}
            //else
            //{
            //    TaskDialog.Show("Info", $"The number of dependent views does not match the number of scope boxes. \n\n{selectedDependentViews.Count} - Dependent Views\n{dependentViewsWithScopeBoxParams.Count} - Scope Boxes");
            //    return Result.Cancelled;
            //}

            List<ViewsRenameReport> NamesResult = new List<ViewsRenameReport>();
            // Start a transaction to rename the view
            using (Transaction trans = new Transaction(doc, "Update Applied Dependent Views"))
            {
                trans.Start();

                foreach (var viewsAndScopeBoxes in listOfDependentViewsDictionaries)
                {
                    foreach (var keyValuePair in viewsAndScopeBoxes)
                    {
                        //var namesResult = new List<ViewsRenameReport>();
                        View view = keyValuePair.Key;
                        Element scopeBox = keyValuePair.Value;
                        MyUtils.AssignScopeBoxToView(view, scopeBox);
                        var previousName = view.Name;
                        var scopeBoxName = scopeBox.Name;

                        RenameViewWithScopeBoxName(view, scopeBoxName);
                        var newName = view.Name;

                        // Add the NamesResult to the ViewsRenameReport list
                        NamesResult.Add(new ViewsRenameReport
                        {
                            PreviousName = previousName,
                            ScopeBoxName = scopeBoxName,
                            NewName = newName
                        });
                    }

                }
                trans.Commit();
            }


            // Show the rename report in a WPF Window
            ShowRenameReport(NamesResult);

            return Result.Succeeded;
        }


        private List<Dictionary<View, Element>> GetListOfDependentViewsDictionaries(Document doc, List<List<View>> listOfListsOfViews, List<Parameter> dependentViewsWithScopeBoxParams)
        {
            List<Dictionary<View, Element>> listOfDependentViewsDictionaries = new List<Dictionary<View, Element>>();
            foreach (var listOfDictionaries in listOfListsOfViews)
            {

                var selectedDependentViews = listOfDictionaries;
                // Create a dictionary to map views to scope boxes
                Dictionary<View, Element> viewsAndScopeBoxes = new Dictionary<View, Element>();
                if (selectedDependentViews == null) { DependentViewsMatchBIMSetupViews = false; return null; }// Result.Cancelled; }
                if (selectedDependentViews.Count == dependentViewsWithScopeBoxParams.Count)
                {
                    for (int i = 0; i < selectedDependentViews.Count; i++)
                    {
                        viewsAndScopeBoxes.Add(selectedDependentViews[i], doc.GetElement(dependentViewsWithScopeBoxParams[i].AsElementId()));
                    }
                    listOfDependentViewsDictionaries.Add(viewsAndScopeBoxes);
                }
                else
                {
                    DependentViewsMatchBIMSetupViews = false; return null;
                }
            }
            return listOfDependentViewsDictionaries;

        }

        private static List<List<View>> GroupViewsByPrimaryViewId(List<View> selectedDependentViews)
        {
            Dictionary<ElementId, List<View>> viewsGroupedByPrimaryView = new Dictionary<ElementId, List<View>>();

            // Group views by their PrimaryViewId
            foreach (View dependentView in selectedDependentViews)
            {
                ElementId primaryViewId = dependentView.GetPrimaryViewId();

                if (!viewsGroupedByPrimaryView.ContainsKey(primaryViewId))
                {
                    viewsGroupedByPrimaryView[primaryViewId] = new List<View>();
                }

                viewsGroupedByPrimaryView[primaryViewId].Add(dependentView);
            }

            // Convert dictionary values to a list of lists of views
            List<List<View>> listOfListsOfViews = viewsGroupedByPrimaryView.Values.ToList();
            return listOfListsOfViews;
        }

        private void RenameViewWithScopeBoxName(View view, string scopeBoxName)
        {
            // Get the original view name
            string viewName = view.Name;

            // Split the view name at "PARENT"
            string[] splitName = viewName.Split(new string[] { "PARENT" }, StringSplitOptions.None);

            // If the split results in more than one part, use the first part
            string newViewName = splitName.Length > 0 ? splitName[0].Trim() : viewName;

            // Append the scope box name to the first part of the original name
            newViewName = $"{newViewName} {scopeBoxName}";

            //// Start a transaction to rename the view
            //using (Transaction trans = new Transaction(view.Document, "Rename View"))
            //{
            //    trans.Start();
            view.Name = newViewName;
            //    trans.Commit();
            //}
        }


        private bool AreBoundingBoxesEqual(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            return box1.Min.IsAlmostEqualTo(box2.Min) && box1.Max.IsAlmostEqualTo(box2.Max);
        }

        public List<View> GetAllDependentViesFromViewsTreeForm(Document doc)
        {
            // Populate the tree data
            var treeData = Cmd_DependentViewsBrowserTree.PopulateTreeView(doc);

            //// Create and show the WPF form
            var form = new UpdateAppliedDependentViewsForm();
            form.InitializeTreeData(treeData);
            bool? dialogResult = form.ShowDialog();

            if (dialogResult != true) // if user does not click OK, cancel command
                return null;


            var selectedItems = Cmd_DependentViewsBrowserTree.GetSelectedViews(doc, form.TreeData);
            selectedItems = Cmd_DependentViewsBrowserTree.GetDependentViews(selectedItems);

            //TaskDialog.Show("INFO", $"Selected views count {selectedItems.Count}");
            return selectedItems;

        }
        // Method to show the report
        private void ShowRenameReport0(List<ViewsRenameReport> namesResult) // This method works, but causes Revit to close when closing the form. Keeping it posible future use.
        {
            Application app = new Application();
            RenameReportWindow reportWindow = new RenameReportWindow(namesResult);
            app.Run(reportWindow);
        }
        private void ShowRenameReport(List<ViewsRenameReport> namesResult)
        {
            RenameReportWindow reportWindow = new RenameReportWindow(namesResult);
            reportWindow.ShowDialog();
        }


        //public static List<TreeNode> PopulateTreeView(Document doc)
        //{
        //    var treeNodes = new List<TreeNode>();

        //    var viewsNode = new TreeNode { Header = "Views" };
        //    viewsNode.Children.AddRange(GetDependentsViewsTree(doc, isChild: false));
        //    treeNodes.Add(viewsNode);

        //    return treeNodes;
        //}

        //public static List<TreeNode> GetDependentsViewsTree(Document doc, bool isChild)
        //{
        //    var treeNodes = new List<TreeNode>();
        //    var views = GetOnlyDependentViews(doc); // Your method to get views

        //    foreach (var view in views)
        //    {
        //        var treeNode = new TreeNode
        //        {
        //            Header = view.Name,
        //            ViewId = view.Id,
        //            IsChildNode = isChild,
        //            Children = GetDependentsViewsTree(doc, isChild: true) // Recursively get child nodes
        //        };
        //        treeNodes.Add(treeNode);
        //    }

        //    return treeNodes;
        //}
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_Cmd_UpdateAppliedDependentViews";
            string buttonTitle = "Update Applied\nDependent Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This button will use as source the 'BIM Set Up View' to update the dependent views of the selected parent view.");

            return myButtonData1.Data;
        }

    }

    public class ViewsRenameReport
    {
        public string PreviousName { get; set; }
        public string ScopeBoxName { get; set; }
        public string NewName { get; set; }
    }

    //public class RenameReportWindow : Window
    //{
    //    public RenameReportWindow(List<ViewsRenameReport> reports)
    //    {
    //        this.Title = "Rename Report";
    //        this.Width = 700;
    //        this.Height = 500;

    //        DataGrid dataGrid = new DataGrid
    //        {
    //            AutoGenerateColumns = true,
    //            IsReadOnly = true,
    //            ItemsSource = reports

    //        };

    //        this.Content = dataGrid;
    //    }
    //}
    public class RenameReportWindow : Window
    {
        public RenameReportWindow(List<ViewsRenameReport> reports)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Title = "Rename Report";
            this.Width = 700;
            this.Height = 500;

            DataGrid dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                ItemsSource = reports
            };

            DataGridTextColumn previousNameColumn = new DataGridTextColumn
            {
                Header = "Previous Name",
                Binding = new WinData.Binding("PreviousName"),
                CellStyle = GetCenteredCellStyle()
            };
            dataGrid.Columns.Add(previousNameColumn);

            DataGridTextColumn scopeBoxNameColumn = new DataGridTextColumn
            {
                Header = "Scope Box Name",
                Binding = new WinData.Binding("ScopeBoxName"),
                CellStyle = GetCenteredCellStyle()
            };
            dataGrid.Columns.Add(scopeBoxNameColumn);

            DataGridTextColumn newNameColumn = new DataGridTextColumn
            {
                Header = "New Name",
                Binding = new WinData.Binding("NewName"),
                CellStyle = GetCenteredCellStyle()
            };
            dataGrid.Columns.Add(newNameColumn);

            this.Content = dataGrid;
        }

        private Style GetCenteredCellStyle()
        {
            Style style = new Style(typeof(DataGridCell));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            return style;
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }
    }
    //public class TreeNode
    //{
    //    public string Header { get; set; }
    //    public List<TreeNode> Children { get; set; }
    //    public bool IsSelected { get; set; }
    //    public bool IsExpanded { get; set; }
    //    public ElementId ViewId { get; set; }

    //    // Add a property to indicate if the node is a child node
    //    public bool IsChildNode { get; set; }
    //}

}
