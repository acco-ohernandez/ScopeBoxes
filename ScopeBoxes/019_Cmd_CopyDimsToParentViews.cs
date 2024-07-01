#region Namespaces
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
    public class Cmd_CopyDimsToParentViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Check if the 'BIM Set Up View' exists, exclude dependent views with the same name
            // Declare a variable to hold the 'BIM Set Up View'
            View BIMSetupView;
            // Call a utility method to get the 'BIM Set Up View' from the document
            // This method sets the BIMSetupView variable if found and returns a Result indicating success or failure
            Result result = MyUtils.GetBIMSetupView(doc, out BIMSetupView);
            // Check if the result indicates that the 'BIM Set Up View' was successfully found
            // If the view was not found, return the result indicating failure
            if (result != Result.Succeeded) { return result; }

            // Get all the FloorPlan Parent views except "BIM Set Up View"
            var allParentFloorPlanViewsExceptBIMSetUpView = MyUtils.GetAllParentViews(doc)
                 .Where(v => v.ViewType == ViewType.FloorPlan && !v.Name.StartsWith("BIM Set Up View") && v.Name != "!CAD Link Template")
                 .OrderBy(v => v.LookupParameter("Browser Sub-Category")?.AsString())
                 .ToList();

            // Group views by ViewType
            var groupedViews = allParentFloorPlanViewsExceptBIMSetUpView
                .GroupBy(v => v.ViewType.ToString())
                .ToList();

            // Create a list of ViewsTreeNode
            var viewsTreeNodes = groupedViews
                .Select(g => new ViewsTreeNode(g.Key, g.ToList()))
                .ToList();

            // ViewsTreeSelectionForm FORM <-----------------------------------
            var viewsTreeSelectionForm = new ViewsTreeSelectionForm();
            viewsTreeSelectionForm.InitializeTreeData(viewsTreeNodes);
            viewsTreeSelectionForm.ShowDialog();    // Show the list of parent views for the user to select them
            if (viewsTreeSelectionForm.DialogResult == false) { return Result.Cancelled; } // if the dialog was canceled
            var selectedTreeData = viewsTreeSelectionForm.TreeData;

            List<View> selectedViews = GetSelectedViews(doc, selectedTreeData);


            // Get all dimensions in BIMSetupView
            var dimensionsElemIdCollector = new FilteredElementCollector(doc, BIMSetupView.Id)
                            .OfClass(typeof(Dimension))
                            .Cast<Dimension>()
                            .Where(d => d.DimensionType.Name == "GRID DIMENSIONS")
                            .Select(d => d.Id)
                            .ToList();
            // Cancel if No dimensions of type 'GRID DIMENSIONS' found 
            if (!dimensionsElemIdCollector.Any()) { MyUtils.M_MyTaskDialog("Info", $"No dimensions of type 'GRID DIMENSIONS' found in the view: {BIMSetupView.Name}"); return Result.Cancelled; }

            // Copy all the dimensions from BIMSetupView to all the selected views
            using (Transaction tx = new Transaction(doc, "Copy Dimensions To Parent Views"))
            {
                tx.Start();

                // Iterate through selected views and copy dimensions
                foreach (var targetView in selectedViews)
                {
                    CopyElements(BIMSetupView, dimensionsElemIdCollector, targetView);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
        private void CopyElements(View sourceView, List<ElementId> elementIds, View targetView)
        {
            // Use ElementTransformUtils to copy elements to the target view
            ElementTransformUtils.CopyElements(sourceView, elementIds, targetView, Transform.Identity, new CopyPasteOptions());
        }
        private void CopyDimensions(Document doc, List<Dimension> dimensions, View sourceView, View targetView)
        {
            foreach (var dimension in dimensions)
            {

                // Create a copy of the dimension in the target view
                CopyDimension(doc, dimension, targetView);
            }
        }

        private void CopyDimension(Document doc, Dimension dimension, View targetView)
        {
            // Implementation to copy dimension element from source view to target view
            // Ensure the curve is valid and bound

            Line dimensionLine = dimension.Curve as Line;
            if (dimensionLine != null && dimensionLine.IsBound)
            {
                XYZ startPoint = dimensionLine.GetEndPoint(0);
                XYZ endPoint = dimensionLine.GetEndPoint(1);

                // Create a new dimension line in the target view
                ReferenceArray refArray = new ReferenceArray();
                foreach (Autodesk.Revit.DB.Reference reference in dimension.References)
                {
                    refArray.Append(reference);
                }

                doc.Create.NewDimension(targetView, Line.CreateBound(startPoint, endPoint), refArray);
            }
        }

        private List<View> GetSelectedViews(Document doc, List<ViewsTreeNode> selectedTreeData)
        {
            List<View> selectedViews = new List<View>();

            foreach (var treeNode in selectedTreeData)
            {
                foreach (var childNode in treeNode.Children)
                {
                    if (childNode.IsSelected)
                    {
                        View view = doc.GetElement(childNode.ViewId) as View;
                        if (view != null)
                        {
                            selectedViews.Add(view);
                        }
                    }
                }
            }

            return selectedViews;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_Cmd_CopyDimsToParentViews";
            string buttonTitle = "Copy Dims To\nParent Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Yellow_32,
                Properties.Resources.Yellow_16,
                "This button will copy dimensions from the 'BIM Setup View' to the parent views selected by the user.");

            return myButtonData1.Data;
        }

    }

}
