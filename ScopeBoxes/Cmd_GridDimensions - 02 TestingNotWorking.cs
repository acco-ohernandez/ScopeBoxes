#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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
    public class Cmd_GridDimensions4 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            var gridsCollector = new List<Element>();
            gridsCollector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                                    .OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType()
                                    .ToList();

            var selectedScopeBoxes = Command2.GetSelectedScopeBoxes(doc);
            //  Get all grids
            if (selectedScopeBoxes.Count != 0)
            {
                List<XYZ> FirstRowOfScopeBoxesOnSameHorizontalLine = GetTheFirstRowOfScopeBoxesXMax(selectedScopeBoxes);

                List<XYZ> FirstRowOfScopeBoxesOnSameVerticalLine = GetTheFirstRowOfScopeBoxesYMax(selectedScopeBoxes);


                View curView = doc.ActiveView;
                var HorizotalAndVerticalDimensionsList = new List<List<Dimension>>();
                // Create dimension strings
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Create grid dimensions");

                    foreach (var xyzPoint in FirstRowOfScopeBoxesOnSameHorizontalLine)
                    {

                        int horizontalFeetOffSet = 2;
                        int verticalFeetOffSet = -2;


                        HorizotalAndVerticalDimensionsList = CreateDimensions(doc,
                                                                              gridsCollector,
                                                                              xyzPoint,
                                                                              new XYZ(0, 0, 0),
                                                                              horizontalFeetOffSet,
                                                                              verticalFeetOffSet,
                                                                              curView);
                    }
                    t.Commit();
                }
                var r = HorizotalAndVerticalDimensionsList;
                Debug.Print($" ================ Grid Dimensioned =================\n" +
                            $"Horizontal Grids: {HorizotalAndVerticalDimensionsList[0].Count()}\n" +
                            $"Vertical Grids: {HorizotalAndVerticalDimensionsList[1].Count()}");
                gridsCollector = null;
            }
            else
            {
                //gridsCollector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                //                    .OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType()
                //                    .ToList();




                View curView = doc.ActiveView;

                int horizontalFeetOffSet = 2;
                int verticalFeetOffSet = -2;

                var HorizotalAndVerticalDimensionsList = new List<List<Dimension>>();
                // Create dimension strings
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Create grid dimensions");
                    HorizotalAndVerticalDimensionsList = CreateDimensions(doc,
                                                                          gridsCollector,
                                                                          horizontalFeetOffSet,
                                                                          verticalFeetOffSet,
                                                                          curView);
                    t.Commit();
                }
                var r = HorizotalAndVerticalDimensionsList;
                Debug.Print($" ================ Grid Dimensioned =================\n" +
                            $"Horizontal Grids: {HorizotalAndVerticalDimensionsList[0].Count()}\n" +
                            $"Vertical Grids: {HorizotalAndVerticalDimensionsList[1].Count()}");
            }
            return Result.Succeeded;
        }

        List<List<Dimension>> CreateDimensions(Document doc, List<Element> gridsCollector, int horizontalFeetOffSet, int verticalFeetOffSet, View curView)
        {
            var HorizatalAndVerticalDimensionsList = new List<List<Dimension>>();
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
            XYZ offsetVert = new XYZ(0, verticalFeetOffSet, 0);

            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
            XYZ offsetHoriz = new XYZ(horizontalFeetOffSet, 0, 0);

            var vertDimensionsList = new List<Dimension>();
            var horzDimensionsList = new List<Dimension>();

            var t = new XYZ(0, p2.GetLength(), 0);

            // Create vertical dimension line
            Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));
            //Line line = Line.CreateBound(p1.Subtract(offsetVert), t);
            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);
            if (dim != null)
                vertDimensionsList.Add(dim);

            // Create horizontal dimension line
            Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
            Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);
            if (dimHoriz != null)
                horzDimensionsList.Add(dimHoriz);


            HorizatalAndVerticalDimensionsList.Add(horzDimensionsList);
            HorizatalAndVerticalDimensionsList.Add(vertDimensionsList);
            return HorizatalAndVerticalDimensionsList;

        }
        List<List<Dimension>> CreateDimensions(Document doc, List<Element> gridsCollector, XYZ vertPont, XYZ horizPoint, int horizontalFeetOffSet, int verticalFeetOffSet, View curView)
        {
            var HorizatalAndVerticalDimensionsList = new List<List<Dimension>>();
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
            XYZ offsetVert = new XYZ(0, verticalFeetOffSet, 0);

            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
            XYZ offsetHoriz = new XYZ(horizontalFeetOffSet, 0, 0);

            var vertDimensionsList = new List<Dimension>();
            var horzDimensionsList = new List<Dimension>();

            var t = new XYZ(0, p2.GetLength(), 0);

            // Create vertical dimension line
            Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));
            //Line line = Line.CreateBound(p1.Subtract(offsetVert), t);
            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);
            if (dim != null)
                vertDimensionsList.Add(dim);

            // Create horizontal dimension line
            Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
            Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);
            if (dimHoriz != null)
                horzDimensionsList.Add(dimHoriz);


            HorizatalAndVerticalDimensionsList.Add(horzDimensionsList);
            HorizatalAndVerticalDimensionsList.Add(vertDimensionsList);
            return HorizatalAndVerticalDimensionsList;

        }

        private bool IsLineVertical(Line curLine)
        {
            XYZ p1 = curLine.GetEndPoint(0);
            XYZ p2 = curLine.GetEndPoint(1);

            return Math.Abs(p1.X - p2.X) < Math.Abs(p1.Y - p2.Y);
        }

        public List<XYZ> GetTheFirstRowOfScopeBoxesXMax(List<Element> scopeBoxes)
        {
            // Filter out scope boxes with the maximum X-coordinate (leftmost on the same horizontal line)
            var firstRowScopeBoxes = scopeBoxes
                .OrderBy(box => GetScopeBoxLocation(box).X)
                .GroupBy(box => GetScopeBoxLocation(box).Y)
                .OrderBy(group => group.Key)
                .FirstOrDefault(); // Select the first row

            if (firstRowScopeBoxes != null)
            {
                return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box)).ToList();
            }

            return new List<XYZ>();
        }

        public List<XYZ> GetTheFirstRowOfScopeBoxesYMax(List<Element> scopeBoxes)
        {
            // Filter out scope boxes with the maximum Y-coordinate (topmost on the same vertical line)
            var firstRowScopeBoxes = scopeBoxes
                .OrderBy(box => GetScopeBoxLocation(box).Y)
                .GroupBy(box => GetScopeBoxLocation(box).X)
                .OrderBy(group => group.Key)
                .FirstOrDefault(); // Select the first column

            if (firstRowScopeBoxes != null)
            {
                return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box)).ToList();
            }

            return new List<XYZ>();
        }

        // Helper method to get the location of the scope box
        private XYZ GetScopeBoxLocation(Element scopeBox)
        {
            BoundingBoxXYZ boundingBox = scopeBox.get_BoundingBox(null);
            XYZ minPoint = boundingBox.Min;
            XYZ maxPoint = boundingBox.Max;

            // Assuming the scope box is a rectangular box, calculating the center point
            XYZ centerPoint = new XYZ((minPoint.X + maxPoint.X) / 2, (minPoint.Y + maxPoint.Y) / 2, (minPoint.Z + maxPoint.Z) / 2);

            return centerPoint;
        }

        // Helper method to check if an element is a scope box
        private bool IsScopeBox(Element element)
        {
            // Implement your logic to check if the element is a scope box
            // Example: You can check the category or other properties specific to scope boxes
            return element.Category != null && element.Category.Name == "Scope Boxes";
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