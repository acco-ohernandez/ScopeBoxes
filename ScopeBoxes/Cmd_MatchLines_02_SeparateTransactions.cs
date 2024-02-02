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
    public class Cmd_MatchLines_02_SeparateTransactionscs : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                // Ensure the dotted line pattern and custom line style are created
                LinePatternElement dottedLinePattern = EnsureDottedLinePattern(doc);
                GraphicsStyle lineGraphicStyle = EnsureCustomLineStyle(doc, "ThickDottedLine", 16, dottedLinePattern);

                // Create and place the detail line
                var point1 = new XYZ(0, 25, 0);
                var point2 = new XYZ(0, -400, 0);
                PlaceDetailLine(doc, lineGraphicStyle, point1, point2);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private LinePatternElement EnsureDottedLinePattern(Document doc)
        {
            LinePatternElement dottedLinePattern;
            using (Transaction trans = new Transaction(doc, "Create Dotted Line Pattern"))
            {
                trans.Start();
                dottedLinePattern = GetOrCreateDottedLinePattern(doc, "DottedPattern");
                trans.Commit();
            }
            return dottedLinePattern;
        }

        private GraphicsStyle EnsureCustomLineStyle(Document doc, string lineStyleName, double lineWeight, LinePatternElement linePattern)
        {
            GraphicsStyle lineGraphicStyle;
            using (Transaction trans = new Transaction(doc, "Create New Line Style"))
            {
                trans.Start();
                lineGraphicStyle = CreateNewLineStyle(doc, lineStyleName, lineWeight, linePattern);
                trans.Commit();
            }
            return lineGraphicStyle;
        }

        private void PlaceDetailLine(Document doc, GraphicsStyle lineGraphicStyle, XYZ endpoint1, XYZ endpoint2)
        {
            using (Transaction trans = new Transaction(doc, "Create Detail Line"))
            {
                trans.Start();
                //Line lineGeometry = Line.CreateBound(new XYZ(0, 25, 0), new XYZ(400, 25, 0));
                Line lineGeometry = Line.CreateBound(endpoint1, endpoint2);
                DetailLine detailCurveLine = doc.Create.NewDetailCurve(doc.ActiveView, lineGeometry) as DetailLine;
                if (detailCurveLine != null && lineGraphicStyle != null)
                {
                    detailCurveLine.LineStyle = lineGraphicStyle;
                }
                trans.Commit();
            }
        }

        private LinePatternElement GetOrCreateDottedLinePattern(Document doc, string patternName)
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

        private GraphicsStyle CreateNewLineStyle(Document doc, string lineStyleName, double lineWeight, LinePatternElement linePattern)
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



        private void ShowInfoDialog(string message)
        {
            TaskDialog.Show("Info", message);
        }

        private void ShowErrorDialog(string message)
        {
            TaskDialog.Show("Error", message);
        }

        private void ShowWarningDialog(string message)
        {
            TaskDialog.Show("Warning", message);
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_Cmd_MatchLines";
            string buttonTitle = "MatchLines";

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
