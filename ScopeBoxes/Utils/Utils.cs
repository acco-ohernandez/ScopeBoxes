using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System;
using System.Linq;

namespace RevitAddinTesting
{
    public static class Utils
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

    }

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
}
