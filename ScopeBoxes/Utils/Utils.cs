using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAddinTesting
{
    internal static class Utils
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
