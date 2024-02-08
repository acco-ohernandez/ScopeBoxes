#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
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
    public class Cmd_CleanDependentViewDims : IExternalCommand
    {
        public int DimensionsHiden { get; set; }
        // Define a static field to hold the original value of the "Tick Mark" parameter
        private static ElementId originalTickMarkValue = null;
        //private static ElementId _originalTickMarkId = ElementId.InvalidElementId;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // All dependenet views
                //List<View> selectedViews = GetAllDependentVies(doc);
                List<View> selectedViews = GetAllDependentViesFromViewsTreeForm(doc);
                if (selectedViews == null)
                {
                    TaskDialog.Show("INFO", "No dependent views selected. \n Command cancelled.");
                    return Result.Cancelled;
                }

                string noCropBoxFoundList = "";


                using (Transaction transaction = new Transaction(doc, "Tick Marks Off Temporarilly"))
                {
                    transaction.Start();
                    // Step 2: Process Each View
                    foreach (var curView in selectedViews)
                    {
                        // Step 3: Check if the current view has the CropBoxActive
                        if (curView.CropBoxActive == true)
                        {
                            var gridDimensionInCurrentView = GetGridDimensionsInView(doc, curView);
                            // Ensure StoreAndSetTickMarkToZero accepts an ElementId as parameter
                            gridDimensionInCurrentView
                                .Select(dim => dim.DimensionType) // Extract DimensionType Ids
                                .Distinct() // Ensure unique DimensionType Ids
                                .ToList() // Convert to List for iteration
                                .ForEach(dimType => StoreAndSetTickMarkToZero(doc, dimType)); // Apply operation
                        }
                        else
                        {
                            noCropBoxFoundList += $"{curView.Name}\n";
                        }
                    }
                    transaction.Commit();
                }

                using (Transaction transaction = new Transaction(doc, "Hide Dimensions in Dependent View"))
                {
                    transaction.Start();
                    // Step 2: Process Each View
                    foreach (var curView in selectedViews)
                    {
                        // Step 3: Check if the current view has the CropBoxActive
                        if (curView.CropBoxActive == true)
                        {
                            var gridDimensionInCurrentView = GetGridDimensionsInView(doc, curView);

                            // Step 4: Get Dimensions Inside Crop Box in Parent View
                            var dimensionsInsideCropBox = GetDimensionsInsideCropBox(curView);

                            // Step 5: Hide Dimensions in Dependent View
                            HideDimensionsInDependentView(doc, curView, dimensionsInsideCropBox);


                            gridDimensionInCurrentView
                                .Select(dim => dim.DimensionType) // Extract DimensionType Ids
                                .Distinct() // Ensure unique DimensionType Ids
                                .ToList() // Convert to List for iteration
                                .ForEach(dimType => RestoreTickMarkToOriginal(doc, dimType)); // Apply operation
                        }
                        else
                        {
                            noCropBoxFoundList += $"{curView.Name}\n";
                        }
                    }
                    transaction.Commit();
                }

                if (!string.IsNullOrEmpty(noCropBoxFoundList))
                    TaskDialog.Show("Info", $"CropBox is no active in view(s):\n {noCropBoxFoundList}");


                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                TaskDialog.Show("Info", $"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
        public static void RestoreTickMarkToOriginal(Document doc, DimensionType dimensionType)
        {
            // Get the "Tick Mark" parameter
            Parameter tickMarkParam = dimensionType.LookupParameter("Tick Mark");

            if (tickMarkParam != null && !tickMarkParam.IsReadOnly)
            {
                if (originalTickMarkValue == null)
                    TaskDialog.Show("Info", $"The value of originalTickMarkValue is: {originalTickMarkValue}");

                bool setResult = tickMarkParam.Set(originalTickMarkValue);

                // Check if the parameter was set successfully
                if (!setResult)
                {
                    TaskDialog.Show("Error", "Unable to set 'Tick Mark' parameter to None.");
                }
            }
        }
        public static void StoreAndSetTickMarkToZero(Document doc, DimensionType dimensionType)
        {
            // Get the "Tick Mark" parameter
            Parameter tickMarkParam = dimensionType.LookupParameter("Tick Mark");

            if (tickMarkParam != null && !tickMarkParam.IsReadOnly)
            {
                if (originalTickMarkValue == null)
                    originalTickMarkValue = tickMarkParam.AsElementId();

                // Attempt to set the parameter to ElementId.InvalidElementId
                // Note: This might not be directly possible for all parameter types, especially for system parameters
                // You might need to find the specific method or workaround for setting this parameter to "None"
                bool setResult = tickMarkParam.Set(ElementId.InvalidElementId);

                // Check if the parameter was set successfully
                if (!setResult)
                {
                    TaskDialog.Show("Error", "Unable to set 'Tick Mark' parameter to None.");
                }
            }
        }





        public static List<Dimension> GetGridDimensionsInView(Document doc, View view)
        {
            List<Dimension> gridDimensions = new List<Dimension>();

            // Collect all dimensions in the view
            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id);
            ICollection<Element> allDimensions = collector.OfClass(typeof(Dimension)).ToElements();

            // Iterate through the collected dimensions
            foreach (Element elem in allDimensions)
            {
                Dimension dim = elem as Dimension;
                if (dim != null)
                {
                    // Retrieve the DimensionType of the current dimension
                    DimensionType dimType = doc.GetElement(dim.GetTypeId()) as DimensionType;

                    // Check if the DimensionType name matches "GRID DIMENSIONS"
                    if (dimType != null && dimType.Name.Equals("GRID DIMENSIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        gridDimensions.Add(dim);
                    }
                }
            }

            return gridDimensions;
        }


        //<---###########
        public List<View> GetAllDependentViesFromViewsTreeForm(Document doc)
        {
            // Populate the tree data
            var treeData = Cmd_DependentViewsBrowserTree.PopulateTreeView(doc);

            //// Create and show the WPF form
            ViewsTreeForm form = new ViewsTreeForm();
            form.InitializeTreeData(treeData);
            bool? dialogResult = form.ShowDialog();

            if (dialogResult != true) // if user does not click OK, cancel command
                return null;


            var selectedItems = Cmd_DependentViewsBrowserTree.GetSelectedViews(doc, form.TreeData);
            selectedItems = Cmd_DependentViewsBrowserTree.GetDependentViews(selectedItems);
            //TaskDialog.Show("INFO", $"Selected views count {selectedItems.Count}");
            return selectedItems;

        }
        private List<View> GetAllDependentVies(Document doc)
        {
            var views = new FilteredElementCollector(doc).OfClass(typeof(View));
            var DependentViews = new List<View>();
            foreach (View view in views)
            {
                ElementId paretnId = view.GetPrimaryViewId();
                if (paretnId.IntegerValue == -1 && !view.IsTemplate)
                {
                    // View is Not a dependent
                }
                else if (paretnId.IntegerValue != -1 && !view.IsTemplate)
                {
                    // View is dependent
                    DependentViews.Add(view);
                }
            }
            return DependentViews;
        }

        private List<View> GetSelectedViews(Document doc, ICollection<ElementId> selectedIds)
        {
            // Filter out only the views from the selected elements
            return selectedIds
                .Select(id => doc.GetElement(id))
                .OfType<View>()
                .ToList();
        }
        private List<Element> GetDimensionsInsideCropBox(View view)
        {
            List<Element> dimensionsInsideCropBox = new List<Element>();

            // Step 1: Get all dimensions in the current view
            FilteredElementCollector collector = new FilteredElementCollector(view.Document, view.Id);
            List<Element> allDimensions = collector.OfClass(typeof(Dimension)).ToList();

            // Step 2: Check if each dimension is inside the CropBox
            foreach (var dimension in allDimensions)
            {
                // Ensure the dimension and its bounding box are not null
                if (dimension == null || dimension.get_BoundingBox(view) == null)
                {
                    continue;
                }

                BoundingBoxXYZ dimensionBoundingBox = dimension.get_BoundingBox(view);

                // Ensure the CropBox is not null
                if (view.CropBoxActive && view.CropBox != null)
                {
                    BoundingBoxXYZ cropBoxBoundingBox = view.CropBox;

                    if (IsBoundingBoxInsideBoundingBox(dimensionBoundingBox, cropBoxBoundingBox))
                    {
                        dimensionsInsideCropBox.Add(dimension);
                    }
                }
            }

            return dimensionsInsideCropBox;
        }

        private bool IsBoundingBoxInsideBoundingBox(BoundingBoxXYZ innerBox, BoundingBoxXYZ outerBox)
        {
            // Check if the innerBox is completely inside the outerBox
            bool xInside = innerBox.Min.X >= outerBox.Min.X && innerBox.Max.X <= outerBox.Max.X;
            bool yInside = innerBox.Min.Y >= outerBox.Min.Y && innerBox.Max.Y <= outerBox.Max.Y;
            //bool zInside = innerBox.Min.Z >= outerBox.Min.Z && innerBox.Max.Z <= outerBox.Max.Z;

            // Check if all dimensions are inside the crop box
            //return xInside && yInside && zInside;
            return xInside && yInside;
        }

        private void HideDimensionsInDependentView(Document doc, View dependentView, List<Element> dimensions)
        {
            // Check if the dependentView is a dependent view
            if (dependentView == null)
            {
                TaskDialog.Show("Error", "The provided view is null.");
                return;
            }

            //using (Transaction transaction = new Transaction(doc, "Hide Dimensions in Dependent View"))
            //{
            //    transaction.Start();

            // Iterate through each dimension element and hide it in the dependent view
            foreach (Element dimension in dimensions)
            {
                // 		FamilyName	"Linear Dimension Style"	string
                //if (dimension.Name == "Linear - 3/32\" Arial")
                if (dimension.Name == "GRID DIMENSIONS")
                    dependentView.HideElements(new List<ElementId> { dimension.Id });
            }

            //    transaction.Commit();
            //}
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCleanDependentDims";
            string buttonTitle = "Clean Dependent \nView Dims";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This button will hide any GRID DIMENSION's inside a dependent view's Crop Box.");

            return myButtonData1.Data;
        }
    }
}
