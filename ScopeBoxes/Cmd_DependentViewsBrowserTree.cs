#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitAddinTesting.Forms;

#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_DependentViewsBrowserTree : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // To be used later
                //var curViewScale = doc.ActiveView.Scale;



                // Populate the tree data
                var treeData = PopulateTreeView(doc);

                //// Create and show the WPF form
                ViewsTreeForm form = new ViewsTreeForm();
                form.InitializeTreeData(treeData);
                bool? dialogResult = form.ShowDialog();

                if (dialogResult != true) // if user does not click OK, cancel command
                    return Result.Cancelled;


                var selectedItems = GetSelectedViews(doc, form.TreeData);
                selectedItems = GetDependentViews(selectedItems);
                TaskDialog.Show("INFO", $"Selected views count {selectedItems.Count}");
                // Now process selectedItems...





                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                TaskDialog.Show("Error", $"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        public static List<View> GetDependentViews(List<View> views)
        {
            var dependentViews = views.Where(view => view.GetPrimaryViewId()
                                                         .IntegerValue != -1 && !view.IsTemplate)
                                                         .ToList();
            return dependentViews;
        }
        public static List<View> GetOnlyDependentViews(Document doc)
        {
            // Get all the views 
            var allViews = GetAllViews(doc);
            //return only dependent views
            return GetDependentViews(allViews);
        }

        public static List<View> GetSelectedViews(Document doc, IEnumerable<TreeNode> nodes)
        {
            var selectedViews = new List<View>();

            if (nodes == null) return selectedViews; // Check if nodes is null

            foreach (var node in nodes)
            {
                if (node != null && node.IsSelected && node.ViewId != null)
                {
                    // Check if node or ViewId is null
                    var view = doc.GetElement(node.ViewId) as View;
                    if (view != null)
                    {
                        selectedViews.Add(view);
                    }
                }

                // Recursively check for selected children
                selectedViews.AddRange(GetSelectedViews(doc, node.Children)); // Ensure Children is never null
            }

            return selectedViews;
        }
        public static List<TreeNode> GetDependentsViewsTree(Document doc)
        {
            var treeNodes = new List<TreeNode>();

            // Collect all views, excluding view templates and "BIM Setup View"
            List<View> allViews = GetAllViews(doc).Where(v => !v.Name.StartsWith("BIM Setup View")).ToList();

            // Group views by their type
            var viewsByType = allViews.GroupBy(v => v.ViewType);

            foreach (var group in viewsByType)
            {
                var viewTypeNode = new TreeNode { Header = group.Key.ToString() };

                // Filter to get only parent views that have dependent views
                var independentViewsWithDependents = group
                    .Where(v => v.GetPrimaryViewId().IntegerValue == -1 &&
                                allViews.Any(dv => dv.GetPrimaryViewId() == v.Id))
                    .ToList();

                foreach (var view in independentViewsWithDependents)
                {
                    var viewNode = new TreeNode
                    {
                        Header = view.Name,
                        ViewId = view.Id,
                        Children = new List<TreeNode>() // Initialize Children
                    };

                    // Add dependent views as children
                    var dependentViews = allViews.Where(dv => dv.GetPrimaryViewId() == view.Id);
                    foreach (var depView in dependentViews)
                    {
                        var depViewNode = new TreeNode
                        {
                            Header = depView.Name,
                            ViewId = depView.Id
                        };
                        viewNode.Children.Add(depViewNode);
                    }

                    viewTypeNode.Children.Add(viewNode);
                }

                if (viewTypeNode.Children.Any())
                {
                    treeNodes.Add(viewTypeNode);
                }
            }

            return treeNodes;
        }

        public static List<View> GetAllViews(Document doc)
        {
            return new FilteredElementCollector(doc)
                            .OfClass(typeof(View))
                            .Cast<View>()
                            .Where(v => !v.IsTemplate)
                            .OrderBy(v => v.LookupParameter("Browser Sub-Category")?.AsString())
                            .ToList();
        }

        public List<TreeNode> GetAllViewsTree(Document doc)
        {
            var treeNodes = new List<TreeNode>();

            // Collect all views, excluding view templates
            var allViews = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .ToList();

            // Group views by their type
            var viewsByType = allViews.GroupBy(v => v.ViewType);

            foreach (var group in viewsByType)
            {
                var viewTypeNode = new TreeNode { Header = group.Key.ToString() };

                // Separate dependent and independent views
                var independentViews = group.Where(v => v.GetPrimaryViewId().IntegerValue == -1);
                var dependentViews = group.Where(v => v.GetPrimaryViewId().IntegerValue != -1);

                // Add independent views
                foreach (var view in independentViews)
                {
                    var viewNode = new TreeNode
                    {
                        Header = view.Name,
                        ViewId = view.Id, // Set the ElementId
                        Children = new List<TreeNode>() // Ensure Children is initialized
                    };
                    viewTypeNode.Children.Add(viewNode);
                }

                // Add dependent views under their parent view
                foreach (var view in dependentViews)
                {
                    var parentView = doc.GetElement(view.GetPrimaryViewId()) as View;
                    var parentViewNode = viewTypeNode.Children.FirstOrDefault(n => n.Header.Equals(parentView?.Name))
                                         ?? new TreeNode { Header = parentView?.Name ?? "Unknown" };

                    if (!viewTypeNode.Children.Contains(parentViewNode))
                    {
                        viewTypeNode.Children.Add(parentViewNode);
                    }

                    var dependentViewNode = new TreeNode
                    {
                        Header = view.Name,
                        ViewId = view.Id,
                        Children = new List<TreeNode>() // Ensure Children is initialized
                    };
                    parentViewNode.Children.Add(dependentViewNode);
                }

                treeNodes.Add(viewTypeNode);
            }

            return treeNodes;
        }


        public List<TreeNode> GetSheetsTree(Document doc)
        {
            var treeNodes = new List<TreeNode>();

            // Collect all sheets, assuming sheets are of type ViewSheet
            var allSheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(vs => !vs.IsTemplate)
                .ToList();

            // Group sheets by some criteria, e.g., by Discipline
            var sheetsByDiscipline = allSheets
                .GroupBy(vs => vs.LookupParameter("Discipline")?.AsString() ?? "Undefined");

            foreach (var group in sheetsByDiscipline)
            {
                var disciplineNode = new TreeNode { Header = group.Key };

                foreach (var sheet in group)
                {
                    var sheetNode = new TreeNode
                    {
                        Header = $"{sheet.SheetNumber} - {sheet.Name}",
                        ViewId = sheet.Id,
                        Children = new List<TreeNode>()
                    };
                    disciplineNode.Children.Add(sheetNode);
                }

                treeNodes.Add(disciplineNode);
            }

            return treeNodes;
        }

        public static List<TreeNode> PopulateTreeView(Document doc)
        {
            var treeNodes = new List<TreeNode>();

            var viewsNode = new TreeNode { Header = "Views" };
            viewsNode.Children.AddRange(GetDependentsViewsTree(doc));
            treeNodes.Add(viewsNode);

            //// Uncomment this part if you want to show the tree view for Sheets
            //var sheetsNode = new TreeNode { Header = "Sheets" };
            //sheetsNode.Children.AddRange(GetSheetsTree(doc));
            //treeNodes.Add(sheetsNode);

            return treeNodes;
        }

        public void PopulateProjectBrowserTree(Document doc)
        {
            // Use BrowserOrganization to understand sorting/grouping
            BrowserOrganization orgViews = BrowserOrganization.GetCurrentBrowserOrganizationForViews(doc);
            BrowserOrganization orgSheets = BrowserOrganization.GetCurrentBrowserOrganizationForSheets(doc);

            // Use FilteredElementCollector and other methods to retrieve views, sheets
            // ...

            // Populate your TreeView based on the retrieved data and organization logic
            // ...
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnDependentViewsBrowserTree";
            string buttonTitle = "Calculate ScopeBox Size";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This calculator can be used to detemine the scopebox size needed to duplicate your defined number of times with overlaps to cover a specified area");

            return myButtonData1.Data;
        }


    }


    /// <summary>
    /// Represents a node in the tree structure for a WPF TreeView control.
    /// Each node corresponds to a Revit view or sheet and can have child nodes.
    /// </summary>
    //public class TreeNode : INotifyPropertyChanged
    //{
    //    private bool _isSelected;
    //    private bool _isEnabled = true;

    //    public string Header { get; set; }
    //    public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    //    public ElementId ViewId { get; set; }

    //    public bool IsSelected
    //    {
    //        get => _isSelected;
    //        set
    //        {
    //            if (_isSelected != value)
    //            {
    //                _isSelected = value;
    //                OnPropertyChanged(nameof(IsSelected));
    //                foreach (var child in Children)
    //                {
    //                    child.IsSelected = value;
    //                }
    //            }
    //        }
    //    }

    //    public bool IsEnabled
    //    {
    //        get => _isEnabled;
    //        set
    //        {
    //            if (_isEnabled != value)
    //            {
    //                _isEnabled = value;
    //                OnPropertyChanged(nameof(IsEnabled));
    //            }
    //        }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    protected virtual void OnPropertyChanged(string propertyName)
    //    {
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }

    //    private bool _isExpanded = true;

    //    public bool IsExpanded
    //    {
    //        get => _isExpanded;
    //        set
    //        {
    //            if (_isExpanded != value)
    //            {
    //                _isExpanded = value;
    //                OnPropertyChanged(nameof(IsExpanded));
    //            }
    //        }
    //    }
    //}

    //public class TreeNode : INotifyPropertyChanged
    //{
    //    private bool _isSelected;

    //    /// <summary>
    //    /// Gets or sets the header text of the tree node.
    //    /// </summary>
    //    public string Header { get; set; }

    //    /// <summary>
    //    /// A list of child nodes under this node. Each child node represents a subordinate view or sheet.
    //    /// </summary>
    //    public List<TreeNode> Children { get; set; } = new List<TreeNode>();

    //    /// <summary>
    //    /// The ElementId of the Revit view or sheet that this node represents.
    //    /// </summary>
    //    public ElementId ViewId { get; set; }

    //    /// <summary>
    //    /// Gets or sets a value indicating whether this node is selected in the UI.
    //    /// This property is bound to the selection state of the corresponding item in the TreeView.
    //    /// </summary>
    //    public bool IsSelected
    //    {
    //        get => _isSelected; // Gets the current selection state of the node.

    //        set
    //        {
    //            // Check if the incoming selection state is different from the current state.
    //            if (_isSelected != value)
    //            {
    //                _isSelected = value; // Update the selection state.

    //                // Notify any listeners (such as the UI) that the property has changed.
    //                // This is crucial for the UI to reflect the change in selection state.
    //                OnPropertyChanged(nameof(IsSelected));

    //                // Cascade the selection state to children
    //                foreach (var child in Children)
    //                {
    //                    child.IsSelected = value;
    //                }
    //            }
    //        }
    //    }


    //    /// <summary>
    //    /// Occurs when a property value changes.
    //    /// </summary>
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    /// <summary>
    //    /// Notifies subscribers about property changes to enable UI updates.
    //    /// </summary>
    //    /// <param name="propertyName">The name of the property that changed.</param>
    //    protected virtual void OnPropertyChanged(string propertyName)
    //    {
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }

    //    // This controls the automatic expansion of the treeview
    //    private bool _isExpanded = true;

    //    /// <summary>
    //    /// Gets or sets a value indicating whether this tree node is expanded in the UI.
    //    /// When set to true, the node's children are visible in the TreeView.
    //    /// </summary>
    //    public bool IsExpanded
    //    {
    //        get => _isExpanded; // Gets the current expansion state of the node.

    //        set
    //        {
    //            // Check if the incoming expansion state is different from the current state.
    //            if (_isExpanded != value)
    //            {
    //                _isExpanded = value; // Update the expansion state.

    //                // Notify any listeners (such as the UI) that the property has changed.
    //                // This update is necessary for the UI to reflect the change in expansion state.
    //                OnPropertyChanged(nameof(IsExpanded));
    //            }
    //        }
    //    }
    //}

}
