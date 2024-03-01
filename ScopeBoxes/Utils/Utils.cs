using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ScopeBoxes
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
