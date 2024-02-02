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
    //[Transaction(TransactionMode.Manual)]
    public class Cmd_CreateThickDottedLine //: IExternalCommand
    {
        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    try
        //    {
        //        UIApplication uiapp = commandData.Application;
        //        Document doc = uiapp.ActiveUIDocument.Document;

        //        using (Transaction trans = new Transaction(doc, "Create Dotted Cyan Line"))
        //        {
        //            trans.Start();
        //            // Ensure the dotted line pattern and custom line style are created
        //            LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
        //            GraphicsStyle lineGraphicStyle = EnsureCustomLineStyle(doc, dottedLinePattern);

        //            // Create and place the detail line
        //            var point1 = new XYZ(0, 25, 0);
        //            var point2 = new XYZ(400, 25, 0);
        //            PlaceDetailLine(doc, lineGraphicStyle, point1, point2);
        //            trans.Commit();
        //        }

        //        return Result.Succeeded;
        //    }
        //    catch (Exception ex)
        //    {
        //        ShowErrorDialog($"An unexpected error occurred: {ex.Message}");
        //        return Result.Failed;
        //    }
        //}


        // Adjusted method to create a thick dotted line and return its ElementId
        public static ElementId CreateThickDottedLine(Document doc)
        {
            //using (Transaction trans = new Transaction(doc, "Create Thick Dotted Line"))
            //{
            //    trans.Start();

            // Example of creating a line; adjust this to fit your actual line creation logic
            XYZ start = new XYZ(0, 0, 0); // Starting point of the line
            XYZ end = new XYZ(10, 0, 0); // Ending point of the line
            Line geometryLine = Line.CreateBound(start, end);

            // Ensure the dotted line pattern and custom line style are created
            LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);

            // Assume EnsureCustomLineStyle returns a GraphicsStyle for the line
            GraphicsStyle lineStyle = EnsureCustomLineStyle(doc, dottedLinePattern);

            // Create the line
            DetailLine detailLine = doc.Create.NewDetailCurve(doc.ActiveView, geometryLine) as DetailLine;
            if (detailLine != null && lineStyle != null)
            {
                // Set the line style
                detailLine.LineStyle = lineStyle;
            }

            //trans.Commit();

            // Return the ElementId of the created line
            return detailLine?.Id ?? ElementId.InvalidElementId;
            //}
        }
        public static ElementId CreateThickDottedLine(Document doc, DetailLine referenceLine)
        {
            XYZ start = new XYZ(0, 0, 0); // Starting point of the line
            XYZ end = new XYZ(10, 0, 0); // Ending point of the line
            Line geometryLine = Line.CreateBound(start, end);

            GraphicsStyle lineStyle = null;

            // If a reference line is provided, use its line style for the new line
            if (referenceLine != null)
            {
                lineStyle = doc.GetElement(referenceLine.LineStyle.Id) as GraphicsStyle;
            }
            else
            {
                // Ensure the dotted line pattern and custom line style are created if no reference line is provided
                LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
                lineStyle = EnsureCustomLineStyle(doc, dottedLinePattern);
            }

            DetailLine detailLine = null;
            //// Ensure you're in a transaction when creating Revit DB objects
            //using (Transaction trans = new Transaction(doc, "Create Thick Dotted Line"))
            //{
            //    trans.Start();

            // Create the line
            detailLine = doc.Create.NewDetailCurve(doc.ActiveView, geometryLine) as DetailLine;
            if (detailLine != null && lineStyle != null)
            {
                // Set the line style
                detailLine.LineStyle = lineStyle;
            }

            //    trans.Commit();
            //}

            // Return the ElementId of the created line
            return detailLine?.Id ?? ElementId.InvalidElementId;
        }
        public static ElementId CreateThickDottedLine(Document doc, XYZ point1, XYZ point2)
        {
            // Ensure the dotted line pattern and custom line style are created
            LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
            GraphicsStyle lineStyle = EnsureCustomLineStyle(doc, dottedLinePattern);

            // Create the geometry line between point1 and point2
            Line geometryLine = Line.CreateBound(point1, point2);

            DetailLine detailLine = null;
            // No transaction is started here, assuming this method is called within an existing transaction

            // Create the detail line in the active view
            detailLine = doc.Create.NewDetailCurve(doc.ActiveView, geometryLine) as DetailLine;
            if (detailLine != null && lineStyle != null)
            {
                // Apply the determined line style to the new line
                detailLine.LineStyle = lineStyle;
            }

            // Return the ElementId of the newly created line
            return detailLine?.Id ?? ElementId.InvalidElementId;
        }

        public static ElementId CreateThickDottedLine(Document doc, XYZ point1, XYZ point2, DetailLine referenceLine)
        {
            // Initialize the line style variable
            GraphicsStyle lineStyle = null;

            // Check if a reference line is provided to use its style
            if (referenceLine != null)
            {
                // Use the style of the reference line
                lineStyle = doc.GetElement(referenceLine.LineStyle.Id) as GraphicsStyle;
            }
            else
            {
                // If no reference line is provided, create or ensure a custom line style
                LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
                lineStyle = EnsureCustomLineStyle(doc, dottedLinePattern);
            }

            // Create the geometry line between point1 and point2
            Line geometryLine = Line.CreateBound(point1, point2);

            DetailLine detailLine = null;
            //// Start a transaction to create the new detail line
            //using (Transaction trans = new Transaction(doc, "Create Thick Dotted Line"))
            //{
            //    trans.Start();

            // Create the detail line in the active view
            detailLine = doc.Create.NewDetailCurve(doc.ActiveView, geometryLine) as DetailLine;
            if (detailLine != null && lineStyle != null)
            {
                // Apply the determined line style to the new line
                detailLine.LineStyle = lineStyle;
            }

            //    trans.Commit();
            //}

            // Return the ElementId of the newly created line
            return detailLine?.Id ?? ElementId.InvalidElementId;
        }

        //// Method to ensure custom line style exists and return it
        //private GraphicsStyle EnsureCustomLineStyle(Document doc)
        //{
        //    // Implementation to create or retrieve a custom line style
        //    // Return a GraphicsStyle object for the line
        //    return null; // Placeholder return, implement according to your needs
        //}


        /// <summary>
        /// This method is to be used from another command/class and be able to create the dotted Cyan line along with 
        /// any other functions.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        public static void CreateThikDottedLine(Document doc, XYZ point1, XYZ point2)
        {
            // Ensure the dotted line pattern and custom line style are created
            LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
            GraphicsStyle lineGraphicStyle = EnsureCustomLineStyle(doc, dottedLinePattern);
            // Create and place the detail line
            PlaceDetailLine(doc, lineGraphicStyle, point1, point2);
        }
        private static LinePatternElement EnsureDottedLinePattern(Document doc)
        {
            LinePatternElement dottedLinePattern;

            dottedLinePattern = GetOrCreateDottedLinePattern(doc, "DottedPattern");

            return dottedLinePattern;
        }

        private static GraphicsStyle EnsureCustomLineStyle(Document doc, LinePatternElement linePattern)
        {

            GraphicsStyle lineGraphicStyle;
            var lineStyleName = "ThickDottedLine";
            double lineWeight = 16;
            lineGraphicStyle = CreateNewLineStyle(doc, lineStyleName, lineWeight, linePattern);

            return lineGraphicStyle;
        }

        private static void PlaceDetailLine(Document doc, GraphicsStyle lineGraphicStyle, XYZ endpoint1, XYZ endpoint2)
        {

            //Line lineGeometry = Line.CreateBound(new XYZ(0, 25, 0), new XYZ(400, 25, 0));
            Line lineGeometry = Line.CreateBound(endpoint1, endpoint2);
            DetailLine detailCurveLine = doc.Create.NewDetailCurve(doc.ActiveView, lineGeometry) as DetailLine;
            if (detailCurveLine != null && lineGraphicStyle != null)
            {
                detailCurveLine.LineStyle = lineGraphicStyle;
            }

        }

        private static LinePatternElement GetOrCreateDottedLinePattern(Document doc, string patternName)
        {
            // Attempt to find an existing line pattern
            LinePatternElement linePatternElement = new FilteredElementCollector(doc)
                .OfClass(typeof(LinePatternElement))
                .Cast<LinePatternElement>()
                .FirstOrDefault(elem => elem.Name == patternName);

            // If not found, create it
            if (linePatternElement == null)
            {
                LinePattern linePattern = new LinePattern(patternName);
                linePattern.SetSegments(new LinePatternSegment[] {
                    new LinePatternSegment(LinePatternSegmentType.Dot, 0),
                    new LinePatternSegment(LinePatternSegmentType.Space, 0.1) // Adjust spacing as needed
                });

                linePatternElement = LinePatternElement.Create(doc, linePattern);
            }

            return linePatternElement;
        }

        private static GraphicsStyle CreateNewLineStyle(Document doc, string lineStyleName, double lineWeight, LinePatternElement linePattern)
        {
            GraphicsStyle graphicsStyle = null;

            try
            {
                // Get the 'Lines' category
                Category linesCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);

                // Check if the subcategory already exists
                Category newLineStyle = linesCategory.SubCategories.Cast<Category>().FirstOrDefault(c => c.Name == lineStyleName);

                if (newLineStyle == null)
                {
                    // Create a new subcategory
                    newLineStyle = doc.Settings.Categories.NewSubcategory(linesCategory, lineStyleName);
                }

                // Set the line pattern and weight
                if (linePattern != null)
                {
                    newLineStyle.SetLinePatternId(linePattern.Id, GraphicsStyleType.Projection);
                }
                // Set the line Font size (Font size)
                newLineStyle.SetLineWeight((int)lineWeight, GraphicsStyleType.Projection);
                // Set the line color to cyan
                var cyanColor = new Autodesk.Revit.DB.Color(0, 255, 255); // RGB values for cyan
                newLineStyle.LineColor = cyanColor;

                // Retrieve the GraphicsStyle associated with the line style
                graphicsStyle = newLineStyle.GetGraphicsStyle(GraphicsStyleType.Projection);
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"Error in CreateNewLineStyle: {ex.Message}");
            }

            return graphicsStyle;
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
            string buttonInternalName = "btn_Cmd_CreateThickDottedLine";
            string buttonTitle = "CreateThickDottedLine";

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
