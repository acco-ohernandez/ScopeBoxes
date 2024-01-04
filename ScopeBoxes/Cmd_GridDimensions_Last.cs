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
    public class Cmd_GridDimensions_Last : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all grids in the active view
            var gridsCollector = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_Grids)
                .WhereElementIsNotElementType()
                .ToList();

            // Get selected scope boxes
            var selectedScopeBoxes = Command2.GetSelectedScopeBoxes(doc);

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create grid dimension");

                // Check if there are selected scope boxes
                if (selectedScopeBoxes.Count != 0)
                {
                    var firstRowXMax = GetTheFirstRowOfScopeBoxesMax(selectedScopeBoxes);
                    var firstColumnYMax = GetTheFirstColumnOfScopeBoxesMax(selectedScopeBoxes);

                    // Create horizontal dimension lines at MaxY of each scope box
                    foreach (var xyzPointY in firstColumnYMax)
                    {
                        CreateHorizontalDimensions(doc, gridsCollector, new XYZ(xyzPointY.X, 0, 0), 0, doc.ActiveView);
                    }

                    // Create vertical dimension lines at MaxX of each scope box
                    foreach (var xyzPointX in firstRowXMax)
                    {
                        CreateVerticalDimensions(doc, gridsCollector, new XYZ(0, xyzPointX.Y, 0), 0, doc.ActiveView);
                    }
                }
                else
                {
                    CreateDimensions(doc, gridsCollector, new XYZ(0, 0, 0), new XYZ(0, 0, 0), 2, -2, doc.ActiveView);
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
        private ReferenceArray GetReferenceArray(List<Element> gridsList)
        {
            var referenceArray = new ReferenceArray();

            foreach (Autodesk.Revit.DB.Grid curGrid in gridsList)
            {
                Line gridLine = curGrid.Curve as Line;

                if (!IsLineVertical(gridLine))
                {
                    referenceArray.Append(new Reference(curGrid));
                }
            }

            return referenceArray;
        }

        public List<Dimension> CreateHorizontalDimensions(Document doc, List<Element> gridsCollector, XYZ vertPoint, int verticalFeetOffSet, View curView)
        {
            var referenceArrayVertical = GetReferenceArray(gridsCollector);
            var xyzPointVertGridList = new List<XYZ>();

            foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
            {
                Line gridLine = curGrid.Curve as Line;

                if (IsLineVertical(gridLine))
                {
                    xyzPointVertGridList.Add(gridLine.GetEndPoint(1));
                }
            }

            return CreateDimensionsFromPoints(doc, curView, referenceArrayVertical, xyzPointVertGridList, vertPoint, verticalFeetOffSet);
        }

        public List<Dimension> CreateVerticalDimensions(Document doc, List<Element> gridsList, XYZ horizPoint, int horizontalFeetOffSet, View curView)
        {
            var referenceArrayHorizontal = GetReferenceArray(gridsList);
            var xyzPointHorizGridList = new List<XYZ>();

            foreach (Autodesk.Revit.DB.Grid curGrid in gridsList)
            {
                Line gridLine = curGrid.Curve as Line;

                if (!IsLineVertical(gridLine))
                {
                    xyzPointHorizGridList.Add(gridLine.GetEndPoint(0));
                }
            }

            return CreateDimensionsFromPoints(doc, curView, referenceArrayHorizontal, xyzPointHorizGridList, horizPoint, horizontalFeetOffSet);
        }

        private List<Dimension> CreateDimensionsFromPoints(Document doc, View curView, ReferenceArray referenceArray, List<XYZ> pointList, XYZ offset, double offsetValue)
        {
            if (pointList == null || referenceArray == null || pointList.Count < 2 || offset == null)
            {
                // Not enough points or references to create a dimension
                return new List<Dimension>();
            }

            var dimensionsList = new List<Dimension>();

            for (int i = 0; i < pointList.Count; i++)
            {
                var point = pointList[i];
                var reference = referenceArray.get_Item(i);  // Get the reference directly

                if (point == null || reference == null || reference.GlobalPoint == null)
                {
                    // Handle the case where the point or reference is null
                    continue;
                }

                // Calculate the offsetPoint for each dimension individually
                XYZ offsetPoint = new XYZ(offset.X * offsetValue, offset.Y * offsetValue, offset.Z * offsetValue);

                var globalPoint = reference.GlobalPoint;

                if (globalPoint != null)
                {
                    Line line = Line.CreateBound(point.Add(offsetPoint), globalPoint.Add(offsetPoint));
                    Dimension dim = doc.Create.NewDimension(curView, line, referenceArray);

                    if (dim != null)
                        dimensionsList.Add(dim);
                }
            }


            return dimensionsList;
        }




        ////List<Dimension> CreateHorizontalDimensions(Document doc, List<Element> gridsCollector, XYZ vertPoint, int verticalFeetOffSet, View curView)
        ////{
        ////    var referenceArrayVertical = new ReferenceArray();
        ////    var xyzPointVertGridList = new List<XYZ>();

        ////    foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
        ////    {
        ////        Line gridLine = curGrid.Curve as Line;

        ////        if (IsLineVertical(gridLine))
        ////        {
        ////            referenceArrayVertical.Append(new Reference(curGrid));
        ////            xyzPointVertGridList.Add(gridLine.GetEndPoint(1));
        ////        }
        ////    }

        ////    if (xyzPointVertGridList.Count < 2)
        ////    {
        ////        // Not enough points to create a dimension
        ////        return new List<Dimension>();
        ////    }

        ////    XYZ p1 = xyzPointVertGridList.OrderBy(p => p.X).First();
        ////    XYZ p2 = xyzPointVertGridList.OrderByDescending(p => p.X).First();
        ////    XYZ offsetVert = vertPoint + new XYZ(0, verticalFeetOffSet, 0);

        ////    var vertDimensionsList = new List<Dimension>();

        ////    Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));
        ////    Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);

        ////    if (dim != null)
        ////        vertDimensionsList.Add(dim);

        ////    return vertDimensionsList;
        ////}

        ////public List<Dimension> CreateVerticalDimensions(Document doc, List<Element> gridsList, XYZ horizPoint, int horizontalFeetOffSet, View curView)
        ////{
        ////    var referenceArrayHorizontal = new ReferenceArray();
        ////    var xyzPointHorizGridList = new List<XYZ>();

        ////    foreach (Autodesk.Revit.DB.Grid curGrid in gridsList)
        ////    {
        ////        Line gridLine = curGrid.Curve as Line;

        ////        if (!IsLineVertical(gridLine))
        ////        {
        ////            referenceArrayHorizontal.Append(new Reference(curGrid));
        ////            xyzPointHorizGridList.Add(gridLine.GetEndPoint(0));
        ////        }
        ////    }

        ////    if (xyzPointHorizGridList.Count < 2)
        ////    {
        ////        // Not enough points to create a dimension
        ////        return new List<Dimension>();
        ////    }

        ////    XYZ p1h = xyzPointHorizGridList.OrderBy(p => p.Y).First();
        ////    XYZ p2h = xyzPointHorizGridList.OrderByDescending(p => p.Y).First();
        ////    XYZ offsetHoriz = horizPoint + new XYZ(horizontalFeetOffSet, 0, 0);

        ////    var horizDimensionsList = new List<Dimension>();

        ////    Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
        ////    Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);

        ////    if (dimHoriz != null)
        ////        horizDimensionsList.Add(dimHoriz);

        ////    return horizDimensionsList;
        ////}


        //public List<Dimension> CreateHorizontalDimensions(Document doc, List<Element> gridsCollector, XYZ vertPoint, int verticalFeetOffSet, View curView)
        //{
        //    var referenceArrayVertical = new ReferenceArray();
        //    var xyzPointVertGridList = new List<XYZ>();

        //    foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
        //    {
        //        Line gridLine = curGrid.Curve as Line;

        //        if (IsLineVertical(gridLine))
        //        {
        //            referenceArrayVertical.Append(new Reference(curGrid));
        //            xyzPointVertGridList.Add(gridLine.GetEndPoint(1));
        //        }
        //    }

        //    XYZ p1 = xyzPointVertGridList.OrderBy(p => p.X).ThenBy(p => p.Y).First();
        //    XYZ p2 = xyzPointVertGridList.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
        //    XYZ offsetVert = vertPoint + new XYZ(0, verticalFeetOffSet, 0);

        //    var vertDimensionsList = new List<Dimension>();

        //    Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));


        //    Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);

        //    if (dim != null)
        //        vertDimensionsList.Add(dim);

        //    return vertDimensionsList;
        //}

        //public List<Dimension> CreateVerticalDimensions(Document doc, List<Element> gridsList, XYZ horizPoint, int horizontalFeetOffSet, View curView)
        //{
        //    var referenceArrayHorizontal = new ReferenceArray();
        //    var xyzPointHorizGridList = new List<XYZ>();

        //    foreach (Autodesk.Revit.DB.Grid curGrid in gridsList)
        //    {
        //        Line gridLine = curGrid.Curve as Line;

        //        if (!IsLineVertical(gridLine))
        //        {
        //            referenceArrayHorizontal.Append(new Reference(curGrid));
        //            xyzPointHorizGridList.Add(gridLine.GetEndPoint(0));
        //        }
        //    }

        //    XYZ p1h = xyzPointHorizGridList.OrderBy(p => p.Y).ThenBy(p => p.X).First();
        //    XYZ p2h = xyzPointHorizGridList.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
        //    XYZ offsetHoriz = horizPoint + new XYZ(horizontalFeetOffSet, 0, 0);

        //    var horizDimensionsList = new List<Dimension>();

        //    Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
        //    Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);

        //    if (dimHoriz != null)
        //        horizDimensionsList.Add(dimHoriz);

        //    return horizDimensionsList;
        //}

        List<List<Dimension>> CreateDimensions(Document doc, List<Element> gridsCollector, XYZ vertPoint, XYZ horizPoint, int horizontalFeetOffSet, int verticalFeetOffSet, View curView)
        {
            var horizAndVerticalDimensionsList = new List<List<Dimension>>();

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
            XYZ offsetVert = new XYZ(0, verticalFeetOffSet, 0) + vertPoint;

            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
            XYZ offsetHoriz = new XYZ(horizontalFeetOffSet, 0, 0) + horizPoint;

            var vertDimensionsList = new List<Dimension>();
            var horizDimensionsList = new List<Dimension>();

            // Create vertical dimension line
            Line line = Line.CreateBound(p1.Add(offsetVert), p2.Add(offsetVert));
            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);
            if (dim != null)
                vertDimensionsList.Add(dim);

            // Create horizontal dimension line
            Line lineHoriz = Line.CreateBound(p1h.Add(offsetHoriz), p2h.Add(offsetHoriz));
            Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);
            if (dimHoriz != null)
                horizDimensionsList.Add(dimHoriz);

            horizAndVerticalDimensionsList.Add(horizDimensionsList);
            horizAndVerticalDimensionsList.Add(vertDimensionsList);

            return horizAndVerticalDimensionsList;
        }

        List<List<Dimension>> CreateDimensions2(Document doc, List<Element> gridsCollector, XYZ vertPoint, XYZ horizPoint, int horizontalFeetOffSet, int verticalFeetOffSet, View curView)
        {
            var horizAndVerticalDimensionsList = new List<List<Dimension>>();

            var referenceArrayVertical = new ReferenceArray();
            var referenceArrayHorizontal = new ReferenceArray();

            var xyzPointListVert = new List<XYZ>();
            var xyzPointListHoriz = new List<XYZ>();

            foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
            {
                Line gridLine = curGrid.Curve as Line;

                if (IsLineVertical(gridLine))
                {
                    referenceArrayVertical.Append(new Reference(curGrid));
                    xyzPointListVert.Add(gridLine.GetEndPoint(1));
                }
                else
                {
                    referenceArrayHorizontal.Append(new Reference(curGrid));
                    xyzPointListHoriz.Add(gridLine.GetEndPoint(1));
                }
            }

            XYZ p1 = xyzPointListVert.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            XYZ p2 = xyzPointListVert.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
            XYZ offsetVert = vertPoint + new XYZ(0, verticalFeetOffSet, 0);

            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
            XYZ offsetHoriz = horizPoint + new XYZ(horizontalFeetOffSet, 0, 0);

            var vertDimensionsList = new List<Dimension>();
            var horizDimensionsList = new List<Dimension>();

            Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));
            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);
            if (dim != null)
                vertDimensionsList.Add(dim);

            Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
            Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);
            if (dimHoriz != null)
                horizDimensionsList.Add(dimHoriz);

            horizAndVerticalDimensionsList.Add(horizDimensionsList);
            horizAndVerticalDimensionsList.Add(vertDimensionsList);

            return horizAndVerticalDimensionsList;
        }

        public static bool IsLineVertical(Line curLine)
        {
            // Method to determine if a line is vertical based on its endpoints

            // Get the start and end points of the line
            XYZ p1 = curLine.GetEndPoint(0);
            XYZ p2 = curLine.GetEndPoint(1);

            // Calculate the difference in X and Y coordinates
            var x = Math.Abs(p1.X - p2.X);
            var y = Math.Abs(p1.Y - p2.Y);

            // Compare the differences to determine if the line is more vertical than horizontal
            var result = x < y;

            // Return true if the difference in X coordinates is less than the difference in Y coordinates
            return result;
        }

        public List<XYZ> GetTheFirstRowOfScopeBoxesMax(List<Element> scopeBoxes)
        {
            // Filter out scope boxes with the maximum X-coordinate (leftmost on the same horizontal line)
            var firstRowScopeBoxes = scopeBoxes
                .OrderBy(box => GetScopeBoxLocation(box).X)
                .GroupBy(box => GetScopeBoxLocation(box).Y)
                .OrderBy(group => group.Key)
                .FirstOrDefault(); // Select the first row

            if (firstRowScopeBoxes != null)
            {
                return firstRowScopeBoxes.Select(box => GetScopeBoxBoundingBoxMax(box)).ToList();
            }

            return new List<XYZ>();
        }

        public List<XYZ> GetTheFirstColumnOfScopeBoxesMax(List<Element> scopeBoxes)
        {
            // Filter out scope boxes with the maximum Y-coordinate (topmost on the same vertical line)
            var firstRowScopeBoxes = scopeBoxes
                .OrderBy(box => GetScopeBoxLocation(box).Y)
                .GroupBy(box => GetScopeBoxLocation(box).X)
                .OrderBy(group => group.Key)
                .FirstOrDefault(); // Select the first column

            if (firstRowScopeBoxes != null)
            {
                return firstRowScopeBoxes.Select(box => GetScopeBoxBoundingBoxMax(box)).ToList();
            }

            return new List<XYZ>();
        }

        public static XYZ GetScopeBoxBoundingBoxMax(Element scopeBox)
        {
            BoundingBoxXYZ boundingBox = scopeBox.get_BoundingBox(null);
            return boundingBox.Max;
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