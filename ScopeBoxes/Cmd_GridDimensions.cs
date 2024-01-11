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
    public class Cmd_GridDimensions : IExternalCommand
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
                    //double offSetFeet = 2.5;
                    double offSetFeet = GetFeetOffSet(); // get the OffSet from the form

                    // Create Horizontal dimension lines at MaxX of each scope box
                    foreach (var xyzPointX in firstColumnYMax)
                    {
                        var newXPoint = new XYZ(0, xyzPointX.Y, 0);
                        CreateHorizontalDimensions(doc, gridsCollector, newXPoint, offSetFeet, doc.ActiveView);
                    }

                    // Create Vertical dimension lines at MaxY of each scope box
                    foreach (var xyzPointY in firstRowXMax)
                    {
                        var newXPoint = new XYZ(xyzPointY.X, 0, 0);
                        CreateVerticalDimensions(doc, gridsCollector, newXPoint, offSetFeet, doc.ActiveView);
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
        public static double GetFeetOffSet()
        {
            var offSetForm = new DimOffSet_Form();
            offSetForm.ShowDialog();
            var offSetText = offSetForm.tb_OffSetFeet.Text;

            if (double.TryParse(offSetText, out double doubleResult))
            {
                return doubleResult;
            }
            else
            {
                // Handle parsing error, provide a default value, or throw an exception
                // For example, you can return a default value like 0.0 or show a message to the user.
                // Modify this part based on your application's requirements.
                return 0.0;
            }
        }


        List<Dimension> CreateHorizontalDimensions(Document doc, List<Element> gridsCollector, XYZ vertPoint, double verticalFeetOffSet, View curView)
        {
            var referenceArrayVertical = new ReferenceArray();
            var xyzPointListVert = new List<XYZ>();

            foreach (Autodesk.Revit.DB.Grid curGrid in gridsCollector)
            {
                Line gridLine = curGrid.Curve as Line;

                if (IsLineVertical(gridLine))
                {
                    referenceArrayVertical.Append(new Reference(curGrid));
                    xyzPointListVert.Add(gridLine.GetEndPoint(1));
                }
            }

            XYZ p1 = xyzPointListVert.OrderBy(p => p.X).ThenBy(p => p.Y).First();
            XYZ p2 = xyzPointListVert.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
            XYZ offsetVert = vertPoint + new XYZ(0, verticalFeetOffSet, 0);

            var vertDimensionsList = new List<Dimension>();

            //Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));

            Line line = Line.CreateBound(new XYZ(p1.X, offsetVert.Y, 0), new XYZ(p2.X, offsetVert.Y, 0));


            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);

            if (dim != null)
                vertDimensionsList.Add(dim);

            return vertDimensionsList;
        }

        public List<Dimension> CreateVerticalDimensions(Document doc, List<Element> gridsList, XYZ horizPoint, double horizontalFeetOffSet, View curView)
        {
            // Method to create vertical dimensions for grids
            // Parameters:
            // - doc: The Revit document
            // - gridsList: List of grid elements
            // - horizPoint: Reference point for horizontal offset
            // - horizontalFeetOffSet: Offset value for horizontal dimension lines
            // - curView: Current view in which dimensions are created

            // Initialize a reference array for horizontal dimensions and a list to store horizontal points
            var referenceArrayHorizontal = new ReferenceArray();
            var xyzPointListHoriz = new List<XYZ>();

            // Loop through the grid elements
            foreach (Autodesk.Revit.DB.Grid curGrid in gridsList)
            {
                Line gridLine = curGrid.Curve as Line;

                // Check if the grid line is not vertical
                if (!IsLineVertical(gridLine))
                {
                    // Append the grid reference to the horizontal reference array
                    referenceArrayHorizontal.Append(new Reference(curGrid));

                    // Add the endpoint to the horizontal point list
                    xyzPointListHoriz.Add(gridLine.GetEndPoint(0));
                }
            }

            // Order the horizontal point list
            XYZ p1h = xyzPointListHoriz.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            XYZ p2h = xyzPointListHoriz.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();

            // Calculate the offset for horizontal dimensions
            XYZ offsetHoriz = horizPoint + new XYZ(horizontalFeetOffSet, 0, 0);

            // Initialize a list to store horizontal dimensions
            var horizDimensionsList = new List<Dimension>();

            // Create horizontal dimension line
            //Line lineHoriz = Line.CreateBound(p1h.Subtract(offsetHoriz), p2h.Subtract(offsetHoriz));
            Line lineHoriz = Line.CreateBound(new XYZ(offsetHoriz.X, p1h.Y, 0), new XYZ(offsetHoriz.X, p2h.Y, 0));

            Dimension dimHoriz = doc.Create.NewDimension(curView, lineHoriz, referenceArrayHorizontal);

            // Add the created dimension to the list
            if (dimHoriz != null)
                horizDimensionsList.Add(dimHoriz);

            // Return the list of horizontal dimensions
            return horizDimensionsList;
        }

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
            Line line = Line.CreateBound(p1.Subtract(offsetVert), p2.Subtract(offsetVert));
            Dimension dim = doc.Create.NewDimension(curView, line, referenceArrayVertical);
            if (dim != null)
                vertDimensionsList.Add(dim);

            // Create horizontal dimension line
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
                //return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box)).ToList();
                return firstRowScopeBoxes.Select(box => GetScopeBoxBoundingBoxMax(box)).ToList();
                //return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box) + GetScopeBoxBoundingBoxMax(box)).ToList();

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
                //return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box)).ToList();
                return firstRowScopeBoxes.Select(box => GetScopeBoxBoundingBoxMax(box)).ToList();
                //return firstRowScopeBoxes.Select(box => GetScopeBoxLocation(box) + GetScopeBoxBoundingBoxMax(box)).ToList();

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
                "This command will create dimentions for horizontal and vertical grid lines. If you pre-select the scopeboxes, it will place the dimension on top and to the right of the scopeboxes");

            return myButtonData1.Data;
        }
    }
}