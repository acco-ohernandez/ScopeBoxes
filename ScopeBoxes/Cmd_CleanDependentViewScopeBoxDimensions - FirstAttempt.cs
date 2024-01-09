#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;


using ScopeBoxes.Forms;
#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_CleanDependentViewScopeBoxDimensions_NoGood : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Step 1: Get Selected Dependent Views
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                var dependentViews = GetSelectedDependentViews(doc, selectedIds);

                // Step 2: Process Each Dependent View
                foreach (var dependentView in dependentViews)
                {
                    // Step 3: Get Dimensions Inside Scope Boxes in Parent View
                    var parentView = GetParentView(doc, dependentView);
                    var dimensionsInScopeBoxes = GetDimensionsInScopeBoxes(parentView);

                    // Step 4: Hide Dimensions in Dependent View
                    HideDimensionsInDependentView(doc, dependentView, dimensionsInScopeBoxes);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                TaskDialog.Show("Info", $"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private List<View> GetSelectedDependentViews(Document doc, ICollection<ElementId> selectedIds)
        {
            // Filter out only the dependent views from the selected elements
            return selectedIds
                .Select(id => doc.GetElement(id) as View)
                .Where(view => view != null)
                .ToList();
        }


        private View GetParentView(Document doc, View dependentView)
        {
            // Retrieve the associated parent view for the dependent view
            var parentViewId = dependentView.GetPrimaryViewId();
            return doc.GetElement(parentViewId) as View;
        }

        private List<Element> GetDimensionsInScopeBoxes(View parentView)
        {
            List<Element> dimensionsInScopeBoxes = new List<Element>();

            // Get the document of the parent view
            Document doc = parentView.Document;

            // Get all dimensions in the parent view
            var dimensionsCollector = new FilteredElementCollector(doc, parentView.Id)
                .OfClass(typeof(Dimension))
                .ToElements()
                .Cast<Dimension>();

            foreach (var dimension in dimensionsCollector)
            {
                // Check if the dimension is associated with a scope box
                Element associatedScopeBox = GetAssociatedScopeBox(doc, dimension);
                if (associatedScopeBox != null)
                {
                    dimensionsInScopeBoxes.Add(dimension);
                }
            }

            return dimensionsInScopeBoxes;
        }

        private Element GetAssociatedScopeBox(Document doc, Dimension dimension)
        {
            // Get the reference of the dimension
            ReferenceArray references = dimension.References;

            // Check each reference to find the associated scope box
            foreach (Reference reference in references)
            {
                // Get the element from the reference
                Element element = doc.GetElement(reference);

                // Check if the element is a scope box by comparing its category
                if (element != null && element.Category != null && element.Category.Name == "Scope Boxes")
                {
                    // Return the scope box element
                    return element;
                }
            }

            return null; // No associated scope box found
        }



        private void HideDimensionsInDependentView(Document doc, View dependentView, List<Element> dimensions)
        {
            // Check if the dependentView is a dependent view
            if (dependentView.IsTemplate || IsDependentView(dependentView))
            {
                using (Transaction transaction = new Transaction(doc, "Hide Dimensions in Dependent View"))
                {
                    transaction.Start();

                    // Iterate through each dimension element and hide it in the dependent view
                    foreach (Element dimension in dimensions)
                    {
                        dependentView.HideElements(new List<ElementId> { dimension.Id });
                    }

                    transaction.Commit();
                }
            }
            else
            {
                TaskDialog.Show("Error", "The provided view is not a dependent view.");
            }
        }

        // Check if the view is a dependent view based on template assignment
        private bool IsDependentView(View view)
        {
            // Check if the view has a view template assigned
            if (view.ViewTemplateId != ElementId.InvalidElementId)
            {
                // You can further refine this check based on your specific requirements
                return true;
            }

            return false;
        }





        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCleanDependentDims";
            string buttonTitle = "Clean Dependent Dims";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This Add-in will hide any dimension inside a scopebox");

            return myButtonData1.Data;
        }
    }
}
