#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;

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
    public class Cmd_GridForMatchLines : IExternalCommand
    {
        // Property to indicate whether a ThickDottedLine has been created
        private bool _isThickDottedLineCreated = false;
        // Property to hold the created detail line
        private ElementId _createdDetailLine = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                var linesCreatedCount = 0;
                using (Transaction trans = new Transaction(doc, "Create Dotted Cyan Line"))
                {
                    trans.Start();

                    if (!_isThickDottedLineCreated)
                    {
                        CreateAndTrackThickDottedLine(doc);
                        // After creation, the flag is set to true to avoid duplicate creations
                        _isThickDottedLineCreated = true;
                    }

                    List<Element> selectedScopeBoxes = Command2.GetSelectedScopeBoxes(doc);
                    List<BoundingBoxXYZ> scopeBoxBounds = GetSelectedScopeBoxBounds(selectedScopeBoxes);

                    List<BoundingBoxXYZ> sortedByX = SortScopeBoxesByX(scopeBoxBounds);
                    List<BoundingBoxXYZ> sortedByY = SortScopeBoxesByY(scopeBoxBounds);

                    List<XYZ> lineMidPointsList = CalculateOverlapMidpoints(sortedByX, sortedByY);
                    lineMidPointsList = FindLongestLines(lineMidPointsList);

                    var listOfLinesCreated = DrawThickDottedLines(doc, lineMidPointsList);
                    linesCreatedCount = listOfLinesCreated.Count;
                    doc.Delete(_createdDetailLine);

                    trans.Commit();
                }

                ShowInfoDialog($"{linesCreatedCount} Lines Created");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"An unexpected error occurred: {ex.Message}");
                return Result.Cancelled;
            }
        }
        public static List<XYZ> FindLongestLines(List<XYZ> lineMidPointsList)
        {
            // This method assumes that the midpoints are paired: each two points define one line.
            if (lineMidPointsList.Count % 2 != 0)
            {
                throw new InvalidOperationException("The midpoint list should contain an even number of points.");
            }

            // Variables to hold the lengths of the longest vertical and horizontal lines
            double longestVerticalLength = 0;
            double longestHorizontalLength = 0;

            // First pass to find the longest lengths
            for (int i = 0; i < lineMidPointsList.Count; i += 2)
            {
                XYZ startPoint = lineMidPointsList[i];
                XYZ endPoint = lineMidPointsList[i + 1];

                // Check if the line is vertical or horizontal by comparing X coordinates
                if (startPoint.X == endPoint.X)
                {
                    // Calculate the length of the vertical line
                    double length = Math.Abs(endPoint.Y - startPoint.Y);
                    longestVerticalLength = Math.Max(longestVerticalLength, length);
                }
                else if (startPoint.Y == endPoint.Y)
                {
                    // Calculate the length of the horizontal line
                    double length = Math.Abs(endPoint.X - startPoint.X);
                    longestHorizontalLength = Math.Max(longestHorizontalLength, length);
                }
            }

            // Lists to hold all lines that are as long as the longest vertical and horizontal lines
            List<XYZ> longestVerticalLines = new List<XYZ>();
            List<XYZ> longestHorizontalLines = new List<XYZ>();

            // Second pass to collect all lines that match the longest lengths
            for (int i = 0; i < lineMidPointsList.Count; i += 2)
            {
                XYZ startPoint = lineMidPointsList[i];
                XYZ endPoint = lineMidPointsList[i + 1];

                // Check if the line is vertical or horizontal by comparing X coordinates
                if (startPoint.X == endPoint.X)
                {
                    // Calculate the length of the vertical line
                    double length = Math.Abs(endPoint.Y - startPoint.Y);
                    if (Math.Abs(length - longestVerticalLength) < 0.0001) // Account for floating point precision
                    {
                        longestVerticalLines.Add(startPoint);
                        longestVerticalLines.Add(endPoint);
                    }
                }
                else if (startPoint.Y == endPoint.Y)
                {
                    // Calculate the length of the horizontal line
                    double length = Math.Abs(endPoint.X - startPoint.X);
                    if (Math.Abs(length - longestHorizontalLength) < 0.0001) // Account for floating point precision
                    {
                        longestHorizontalLines.Add(startPoint);
                        longestHorizontalLines.Add(endPoint);
                    }
                }
            }

            // Combine the longest vertical and horizontal lines into one list
            List<XYZ> longestLines = new List<XYZ>();
            longestLines.AddRange(longestVerticalLines);
            longestLines.AddRange(longestHorizontalLines);

            return longestLines;
        }

        public static List<BoundingBoxXYZ> SortScopeBoxesByX(List<BoundingBoxXYZ> scopeBoxBounds)
        {
            // Sort by X to group into columns
            var sortedByX = scopeBoxBounds.OrderBy(b => b.Min.X).ToList();
            return sortedByX;
        }

        public static List<BoundingBoxXYZ> SortScopeBoxesByY(List<BoundingBoxXYZ> scopeBoxBounds)
        {
            // Sort by Y to group into rows
            var sortedByY = scopeBoxBounds.OrderBy(b => b.Min.Y).ToList();
            return sortedByY;
        }

        public static List<XYZ> CalculateOverlapMidpoints(List<BoundingBoxXYZ> sortedByX, List<BoundingBoxXYZ> sortedByY)
        {
            List<XYZ> midpoints = new List<XYZ>();

            // Calculate vertical midpoints
            for (int i = 0; i < sortedByX.Count - 1; i++)
            {
                var rightEdgeOfLeftBox = sortedByX[i].Max.X;
                var leftEdgeOfRightBox = sortedByX[i + 1].Min.X;
                var verticalMidpointX = (rightEdgeOfLeftBox + leftEdgeOfRightBox) / 2;

                // Assuming that the vertical overlaps span the entire height of the scope boxes
                var bottomOfBoxes = Math.Min(sortedByX[i].Min.Y, sortedByX[i + 1].Min.Y);
                var topOfBoxes = Math.Max(sortedByX[i].Max.Y, sortedByX[i + 1].Max.Y);

                // Create two points for the start and end of the vertical dotted line
                XYZ startPointVertical = new XYZ(verticalMidpointX, bottomOfBoxes, 0);
                XYZ endPointVertical = new XYZ(verticalMidpointX, topOfBoxes, 0);
                midpoints.Add(startPointVertical);
                midpoints.Add(endPointVertical);
            }

            // Calculate horizontal midpoints
            for (int j = 0; j < sortedByY.Count - 1; j++)
            {
                var topEdgeOfLowerBox = sortedByY[j].Max.Y;
                var bottomEdgeOfUpperBox = sortedByY[j + 1].Min.Y;
                var horizontalMidpointY = (topEdgeOfLowerBox + bottomEdgeOfUpperBox) / 2;

                // Assuming that the horizontal overlaps span the entire width of the scope boxes
                var leftOfBoxes = Math.Min(sortedByY[j].Min.X, sortedByY[j + 1].Min.X);
                var rightOfBoxes = Math.Max(sortedByY[j].Max.X, sortedByY[j + 1].Max.X);

                // Create two points for the start and end of the horizontal dotted line
                XYZ startPointHorizontal = new XYZ(leftOfBoxes, horizontalMidpointY, 0);
                XYZ endPointHorizontal = new XYZ(rightOfBoxes, horizontalMidpointY, 0);
                midpoints.Add(startPointHorizontal);
                midpoints.Add(endPointHorizontal);
            }

            return midpoints;
        }
        // Method to check if a horizontal line at midpointY crosses all boxes in the row
        public static List<ElementId> DrawThickDottedLines(Document doc, List<XYZ> midpoints)
        {
            // Check if the midpoints list has an even number of points to form lines
            if (midpoints.Count % 2 != 0)
            {
                throw new InvalidOperationException("The number of midpoint coordinates must be even to form line segments.");
            }

            List<ElementId> lineIdsList = new List<ElementId>();
            for (int i = 0; i < midpoints.Count; i += 2)
            {
                XYZ startPoint = midpoints[i];
                XYZ endPoint = midpoints[i + 1];

                // Use the CreateThickDottedLine method that takes two XYZ points
                // The method should already handle the creation of lines within the active view and applying the desired style
                //ElementId lineId = Cmd_CreateThickDottedLine.CreateThickDottedLine(doc, startPoint, endPoint, null);
                ElementId lineId = Cmd_CreateThickDottedLine.CreateThickDottedLine(doc, startPoint, endPoint);
                lineIdsList.Add(lineId);
            }
            return lineIdsList;
        }

        public static List<BoundingBoxXYZ> GetSelectedScopeBoxBounds(List<Element> selectedScopeBoxes)
        {
            List<BoundingBoxXYZ> scopeBoxBounds = new List<BoundingBoxXYZ>();

            foreach (Element scopeBox in selectedScopeBoxes)
            {
                BoundingBoxXYZ boundingBox = scopeBox.get_BoundingBox(null);
                if (boundingBox != null)
                {
                    scopeBoxBounds.Add(boundingBox);
                }
            }

            return scopeBoxBounds;
        }

        // Method to update or create the detail line
        private void CreateAndTrackThickDottedLine(Document doc)
        {
            // Example call to a method from Cmd_CreateThickDottedLine
            // Assuming an adjusted method in Cmd_CreateThickDottedLine that fits this use case
            // This is a conceptual call; adjust according to your actual implementation
            _createdDetailLine = Cmd_CreateThickDottedLine.CreateThickDottedLine(doc);
        }


        private static void ShowInfoDialog(string message)
        {
            TaskDialog.Show("Info", message);
        }

        private static void ShowErrorDialog(string message)
        {
            TaskDialog.Show("Error", message);
        }

        private static void ShowWarningDialog(string message)
        {
            TaskDialog.Show("Warning", message);
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_Cmd_GridForMatchLines";
            string buttonTitle = "Grid For MatchLines";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This will create MatchLines ");

            return myButtonData1.Data;
        }
    }
}
