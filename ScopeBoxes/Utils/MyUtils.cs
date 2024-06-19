﻿using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitAddinTesting
{
    public static class MyUtils
    {
        internal static RibbonPanel CreateRibbonPanel(UIControlledApplication app, string tabName, string panelName)
        {
            RibbonPanel currentPanel = GetRibbonPanelByName(app, tabName, panelName);

            if (currentPanel == null)
                currentPanel = app.CreateRibbonPanel(tabName, panelName);

            return currentPanel;
        }

        internal static RibbonPanel GetRibbonPanelByName(UIControlledApplication app, string tabName, string panelName)
        {
            foreach (RibbonPanel tmpPanel in app.GetRibbonPanels(tabName))
            {
                if (tmpPanel.Name == panelName)
                    return tmpPanel;
            }

            return null;
        }

        /// <summary>
        /// Must pass in a View and the required distance in feet at a known scale.
        /// Example 1: Revit view scale = 48 => 2 feet
        /// Example 2: offSet = Utils.GetViewScaleMultipliedValue(doc.ActiveView, 48, 2);
        /// This will returned the value needed at the current view scale
        /// </summary>
        /// <param name="currentView"></param>
        /// <param name="baseNum"></param>
        /// <returns></returns>
        public static double GetViewScaleMultipliedValue(View currentView, double baseScaleNum, double baseNum)
        {
            double viewScale = currentView.Scale;
            //double baseScaleNum = 48;
            double multiplier = baseScaleNum / viewScale;
            double calculatedDistance = baseNum / multiplier;
            return calculatedDistance;
        }

        public static ElementId GetViewSheetIdByName(Document doc, string viewSheetName)
        {
            // Create a filtered element collector for ViewSheets
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                                                 .OfClass(typeof(ViewSheet));

            // Find the ViewSheet by its name
            ViewSheet viewSheet = collector
                                    .Cast<ViewSheet>()
                                    .FirstOrDefault(vs => vs.Name.Equals(viewSheetName, StringComparison.OrdinalIgnoreCase));

            // Return the Id of the ViewSheet, or ElementId.InvalidElementId if not found
            return viewSheet?.Id ?? ElementId.InvalidElementId;
        }


        public static ViewSheet GetViewSheetByName(Document doc, string viewSheetName)
        {
            // Create a filtered element collector for ViewSheets
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                                                 .OfClass(typeof(ViewSheet));

            // Find the ViewSheet by its name
            ViewSheet viewSheet = collector
                                    .Cast<ViewSheet>()
                                    .FirstOrDefault(vs => vs.Name.Equals(viewSheetName, System.StringComparison.OrdinalIgnoreCase));

            return viewSheet;
        }

        public static ViewSheet GetViewSheetById(Document doc, ElementId viewSheetId)
        {
            // Get the element from the document using the provided ElementId
            Element element = doc.GetElement(viewSheetId);

            // Cast the element to ViewSheet and return it
            ViewSheet viewSheet = element as ViewSheet;

            return viewSheet;
        }

        public static List<View> GetAllParentViews(Document doc)
        {
            List<View> parentViews = new List<View>();

            // Get all views in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(View));

            foreach (Element element in collector)
            {
                View view = element as View;
                // Check if the view is a parent view
                if (view != null && !view.IsTemplate && !IsDependentView(view))
                {
                    parentViews.Add(view);
                }
            }

            return parentViews;
        }
        public static List<View> GetAllDependentViews(Document doc)
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

        public static bool IsDependentView(View view)
        {
            ElementId paretnId = view.GetPrimaryViewId();
            return paretnId.IntegerValue != -1 && !view.IsTemplate;
        }

        public static List<Element> GetAllScopeBoxesInView(View view)
        {
            List<Element> scopeBoxes = new List<Element>();
            Document doc = view.Document;

            // Create a filtered element collector for the view
            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id);

            // Filter for elements that are scope boxes
            ElementCategoryFilter scopeBoxFilter = new ElementCategoryFilter(BuiltInCategory.OST_VolumeOfInterest);

            // Collect all scope boxes in the view
            foreach (Element element in collector.WherePasses(scopeBoxFilter))
            {
                if (IsScopeBox(element))
                {
                    scopeBoxes.Add(element);
                }
            }

            return scopeBoxes;
        }
        public static List<Element> GetSelectedScopeBoxes(Document doc)
        {
            List<Element> scopeBoxes = new List<Element>();

            // Get the handle of current document.
            UIDocument uidoc = new UIDocument(doc);

            // Get the element selection of the current document.
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            // Iterate through the selected element IDs and add scope boxes to the list
            foreach (ElementId id in selectedIds)
            {
                Element element = doc.GetElement(id);
                if (IsScopeBox(element))
                {
                    scopeBoxes.Add(element);
                }
            }

            return scopeBoxes;
        }
        public static bool IsScopeBox(Element element)
        {
            // Check if the element is a scope box
            return element != null && element.Category != null && element.Category.Name == "Scope Boxes";
        }

        public static View CreateDependentView(Document doc, View parentView)
        {
            // Check if the view can have dependent views
            if (!parentView.CanViewBeDuplicated(ViewDuplicateOption.AsDependent))
            {
                throw new InvalidOperationException("The specified view cannot have dependent views.");
            }

            //// Start a new transaction
            //using (Transaction trans = new Transaction(doc, "Create Dependent View"))
            //{
            //    trans.Start();

            // Create the dependent view
            ElementId dependentViewId = parentView.Duplicate(ViewDuplicateOption.AsDependent);
            View dependentView = doc.GetElement(dependentViewId) as View;

            //trans.Commit();

            return dependentView;
            //}
        }
        public static View CreateDependentViewByScopeBox(Document doc, View parentView, Element scopeBox)
        {
            // Check if the view can have dependent views
            if (!parentView.CanViewBeDuplicated(ViewDuplicateOption.AsDependent))
            {
                throw new InvalidOperationException("The specified view cannot have dependent views.");
            }

            var parentViewName = parentView.Name;
            var scopeBoxName = scopeBox.Name;

            // Create the dependent view
            ElementId dependentViewId = parentView.Duplicate(ViewDuplicateOption.AsDependent);
            View dependentView = doc.GetElement(dependentViewId) as View;
            dependentView.Name = $"{parentViewName} {scopeBoxName}";

            // assign the scopeBox to the DependentView
            dependentView.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP).Set(scopeBox.Id);


            return dependentView;
        }
        public static void ChangeViewReferenceTargetView(Document doc, Element viewReference, View newTargetView)
        {
            // Check if the view reference and the target view are valid
            if (viewReference == null)
            {
                TaskDialog.Show("Error", "No view reference found.");
                return;
            }

            if (newTargetView == null)
            {
                TaskDialog.Show("Error", "Target view is not valid.");
                return;
            }

            // Change the target view of the view reference
            using (Transaction trans = new Transaction(doc, "Change View Reference Target"))
            {
                trans.Start();
                try
                {
                    // Get the parameter that defines the target view
                    Parameter targetViewParam = viewReference.get_Parameter(BuiltInParameter.REFERENCE_VIEWER_TARGET_VIEW);

                    // Check if the parameter is valid and not read-only
                    if (targetViewParam != null && !targetViewParam.IsReadOnly)
                    {
                        // Set the parameter to the new target view's ID
                        targetViewParam.Set(newTargetView.Id);
                        TaskDialog.Show("Success", "Target view changed successfully.");
                    }
                    else
                    {
                        TaskDialog.Show("Error", "The target view parameter is not valid or is read-only.");
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                    trans.RollBack();
                }
            }
        }

        // This method returns a single element selected by the user
        internal static Element GetSingleSelectedElement(UIApplication uiapp)
        {
            // Get the active UIDocument
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null)
            {
                TaskDialog.Show("Info", "No active document.");
                return null;
            }

            // Get the selection object
            Selection selection = uidoc.Selection;
            if (selection == null)
            {
                TaskDialog.Show("Info", "No selection found.");
                return null;
            }

            // Get the selected element IDs
            ICollection<ElementId> selectedIds = selection.GetElementIds();
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Info", "No element selected.");
                return null;
            }
            else if (selectedIds.Count > 1)
            {
                TaskDialog.Show("Info", "Multiple elements selected. Please select only one element.");
                return null;
            }

            // Get the first selected element
            ElementId selectedId = selectedIds.First();
            Element selectedElement = uidoc.Document.GetElement(selectedId);

            return selectedElement;
        }

        public static ViewPlan CreateFloorPlanView(Document doc, string viewName, Level level = null)
        {
            // Ensure a valid document and view name are provided
            if (doc == null || string.IsNullOrWhiteSpace(viewName))
            {
                throw new ArgumentNullException("Document and view name cannot be null or empty.");
            }

            try
            {
                // Find the appropriate view family type for the floor plan view
                ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

                if (viewFamilyType == null)
                {
                    throw new InvalidOperationException("No ViewFamilyType found for FloorPlan.");
                }

                // Get the first level in the document to create the floor plan view
                if (level == null)
                {
                    level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .FirstOrDefault() as Level;
                }

                if (level == null)
                {
                    throw new InvalidOperationException("No levels found in the document.");
                }

                // Create the new floor plan view
                ViewPlan newFloorPlanView = ViewPlan.Create(doc, viewFamilyType.Id, level.Id);

                // Assign a name to the newly created floor plan view
                if (newFloorPlanView != null)
                {
                    newFloorPlanView.Name = viewName;
                }


                return newFloorPlanView;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create floor plan view.", ex);
            }
        }

        public static List<Level> GetAllLevels(Document doc)
        {

            // Get all the levels
            return new FilteredElementCollector(doc)
                         .OfClass(typeof(Level))
                         .Cast<Level>()
                         .OrderBy(l => l.Elevation)
                         .ToList();
        }
        public static string GetUniqueViewName(Document doc, string baseName)
        {
            if (!ViewNameExists(doc, baseName))
            {
                return baseName;
            }

            int suffix = 1;
            string newName;

            do
            {
                newName = $"{baseName}({suffix})";
                suffix++;
            }
            while (ViewNameExists(doc, newName));

            return newName;
        }

        public static bool ViewNameExists(Document doc, string viewName)
        {
            return new FilteredElementCollector(doc)
                   .OfClass(typeof(View))
                   .Cast<View>()
                   .Any(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        }

        public static void SetViewBrowserCategory(ViewPlan view)
        {
            if (view != null)
            {
                // Assuming "Browser Category" and "Browser Sub-Category" are shared parameters
                Parameter browserCategoryParam = view.LookupParameter("Browser Category");
                Parameter browserSubCategoryParam = view.LookupParameter("Browser Sub-Category");
                if (browserCategoryParam != null)
                {
                    browserCategoryParam.Set("_BIM Setup");
                    if (browserSubCategoryParam != null)
                    {
                        browserSubCategoryParam.Set("BIM FloorPlan");
                    }
                }
                else
                {
                    TaskDialog.Show("Info", $"The 'Browser Category' Parameter was not found. \nThe View '{view.Name}' will be placed in the default 'Floor Plans' Category on the project browser.");
                }
            }
        }

        /// <summary>
        /// This method returns a Dictionary with the Mapped out View Scales
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, string> ScalesList()
        {
            var scaleMappings = new (int scaleNum, string ViewScaleString)[]
            {
                (1, "12\" = 1'-0\""),
                (2, "6\" = 1'-0\""),
                (4, "3\" = 1'-0\""),
                (8, "1-1/2\" = 1'-0\""),
                (12, "1\" = 1'-0\""),
                (16, "3/4\" = 1'-0\""),
                (24, "1/2\" = 1'-0\""),
                (32, "3/8\" = 1'-0\""),
                (48, "1/4\" = 1'-0\""),
                (64, "3/16\" = 1'-0\""),
                (96, "1/8\" = 1'-0\""),
                (120, "1\" = 10'-0\""),
                (128, "3/32\" = 1'-0\""),
                (192, "1/16\" = 1'-0\""),
                (240, "1\" = 20'-0\""),
                (256, "3/64\" = 1'-0\""),
                (360, "1\" = 30'-0\""),
                (384, "1/32\" = 1'-0\""),
                (480, "1\" = 40'-0\""),
                (600, "1\" = 50'-0\""),
                (720, "1\" = 60'-0\""),
                (768, "1/64\" = 1'-0\""),
                (960, "1\" = 80'-0\""),
                (1200, "1\" = 100'-0\""),
                (1920, "1\" = 160'-0\""),
                (2400, "1\" = 200'-0\""),
                (3600, "1\" = 300'-0\""),
                (4800, "1\" = 400'-0\""),
            };
            var scaleDictionary = new Dictionary<int, string>();

            foreach (var (scaleNum, viewScaleString) in scaleMappings)
            {
                scaleDictionary[scaleNum] = viewScaleString;
            }

            return scaleDictionary;
        }
    } // EndOf MyUtils

    public class GetLeftRightTopBottomCenters
    {
        public XYZ LeftCenter { get; private set; }
        public XYZ RightCenter { get; private set; }
        public XYZ TopCenter { get; private set; }
        public XYZ BottomCenter { get; private set; }

        public GetLeftRightTopBottomCenters(BoundingBoxXYZ boundingBox)
        {
            // Calculate the expanded corners
            GetBoxCenterPoints(boundingBox);
        }
        private void GetBoxCenterPoints(BoundingBoxXYZ boundingBox)
        {
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            // Center point of the left side
            LeftCenter = new XYZ(min.X, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

            // Center point of the right side
            RightCenter = new XYZ(max.X, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

            // Center point of the top side
            TopCenter = new XYZ((min.X + max.X) / 2.0, max.Y, (min.Z + max.Z) / 2.0);

            // Center point of the bottom side
            BottomCenter = new XYZ((min.X + max.X) / 2.0, min.Y, (min.Z + max.Z) / 2.0);
        }
    }

    public class ViewScaleManager
    {
        public int ScaleValue { get; private set; }
        public string ViewScaleString { get; private set; }



        // Constructor
        public ViewScaleManager(View view)
        {
            CalculatePropertiesBasedOnViewScale(view);
        }

        private void CalculatePropertiesBasedOnViewScale(View view)
        {
            int viewScale = view.Scale;
            // Define the scale mappings based on the provided CSV data
            var scaleMappings =
            new (int scaleNum, string ViewScaleString)[]
                {
                    (1,"12\" = 1'-0\""),
                    (2,"6\" = 1'-0\""),
                    (4,"3\" = 1'-0\""),
                    (8,"1-1/2\" = 1'-0\""),
                    (12,"1\" = 1'-0\""),
                    (16,"3/4\" = 1'-0\""),
                    (24,"1/2\" = 1'-0\""),
                    (32,"3/8\" = 1'-0\""),
                    (48,"1/4\" = 1'-0\""),
                    (64,"3/16\" = 1'-0\""),
                    (96,"1/8\" = 1'-0\""),
                    (128,"3/32\" = 1'-0\""),
                    (192,"1/16\" = 1'-0\""),
                    (240,"1\" = 20'-0\""),
                    (256,"3/64\" = 1'-0\""),
                    (360,"1\" = 30'-0\""),
                    (384,"1/32\" = 1'-0\""),
                    (480,"1\" = 40'-0\""),
                    (600,"1\" = 50'-0\""),
                    (720,"1\" = 60'-0\""),
                    (768,"1/64\" = 1'-0\""),
                    (960,"1\" = 80'-0\""),
                    (1200,"1\" = 100'-0\""),
                    (1920,"1\" = 160'-0\""),
                    (2400,"1\" = 200'-0\""),
                    (3600,"1\" = 300'-0\""),
                    (4800,"1\" = 400'-0\""),
                };

            // Find the corresponding scale mapping
            foreach (var mapping in scaleMappings)
            {
                if (view.Scale == mapping.scaleNum)
                {
                    this.ScaleValue = viewScale;
                    this.ViewScaleString = mapping.ViewScaleString;
                    return;
                }
                else
                {
                    this.ScaleValue = viewScale;
                    this.ViewScaleString = "Custome Scale";
                    return;
                }
            }

            // Handle the case where no matching scale is found
            throw new ArgumentException("Unsupported view scale.");
        }
    }
} //EndOf namespace RevitAddinTesting
