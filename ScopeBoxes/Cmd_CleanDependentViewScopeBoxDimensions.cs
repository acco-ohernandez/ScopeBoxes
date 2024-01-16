#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    public class Cmd_CleanDependentViewScopeBoxDimensions : IExternalCommand
    {
        public int DimensionsHiden { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Step 1: Get Selected Views
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                var selectedViews = GetSelectedViews(doc, selectedIds);

                string noCropBoxFoundList = "";


                using (Transaction transaction = new Transaction(doc, "Hide Dimensions in Dependent View"))
                {
                    transaction.Start();
                    // Step 2: Process Each View
                    foreach (var curView in selectedViews)
                    {
                        // Step 3: Check if the current view has the CropBoxActive
                        if (curView.CropBoxActive == true)
                        {
                            // var cb = curView.CropBox;
                            // Step 4: Get Dimensions Inside Crop Box in Parent View
                            var dimensionsInsideCropBox = GetDimensionsInsideCropBox(curView);

                            // Step 5: Hide Dimensions in Dependent View
                            HideDimensionsInDependentView(doc, curView, dimensionsInsideCropBox);
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
                if (dimension.Name == "Linear - 3/32\" Arial - Grids")
                    dependentView.HideElements(new List<ElementId> { dimension.Id });
            }

            //    transaction.Commit();
            //}
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
                "This Add-in will hide any dimension inside a cropbox");

            return myButtonData1.Data;
        }
    }
}
