#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
    public class Cmd_GridDimensions3 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;


            //  Get all grids
            var gridsCollector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                                .OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType()
                                .ToList();



            // Create reference arrays and point lists
            var referenceArrayVertical = new ReferenceArray();
            var referenceArrayHorizontal = new ReferenceArray();

            var xyzPointListVert = new List<XYZ>();
            var xyzPointListHoriz = new List<XYZ>();

            // Loop through grids and check if vertical or horizontal
            foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
            {
                Line gridLine = curGrid.Curve as Line;

                if (IsLineVertical(gridLine))
                {
                    // Append the grid reference to the vertical reference array
                    referenceArrayVertical.Append(new Reference(curGrid));

                    // Add the endpoint to the vertical point list
                    xyzPointListVert.Add(gridLine.GetEndPoint(1));
                }
                else
                {
                    // Append the grid reference to the horizontal reference array
                    referenceArrayHorizontal.Append(new Reference(curGrid));

                    // Add the endpoint to the horizontal point list
                    xyzPointListHoriz.Add(gridLine.GetEndPoint(1));
                }
            }

            // Order point lists
            XYZ p1 = xyzPointListVert.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            XYZ p2 = xyzPointListVert.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
            XYZ offset = new XYZ(0, -21, 0);

            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
            XYZ offsetHoriz = new XYZ(-22, 0, 0);

            // Create dimension strings
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create grid dimensions");

                // Create vertical dimension line
                Line line = Line.CreateBound(p1.Subtract(offset), p2.Subtract(offset));
                Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, referenceArrayVertical);

                // Create horizontal dimension line
                Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
                Dimension dimHoriz = doc.Create.NewDimension(doc.ActiveView, lineHoriz, referenceArrayHorizontal);

                t.Commit();
            }

            return Result.Succeeded;
        }





        private bool IsLineVertical(Line curLine)
        {
            XYZ p1 = curLine.GetEndPoint(0);
            XYZ p2 = curLine.GetEndPoint(1);

            return Math.Abs(p1.X - p2.X) < Math.Abs(p1.Y - p2.Y);
        }



        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnGridDimensions";
            string buttonTitle = "Grid Dimensions";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}