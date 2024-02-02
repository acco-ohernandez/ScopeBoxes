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
    public class Cmd_MatchLines_existingLine : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Define your line geometry here
                // Example:
                Line lineGeometry = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(100, 0, 0));

                // Rename the scope boxes using a transaction
                using (Transaction transaction = new Transaction(doc, "Create DetailCurve Line"))
                {
                    transaction.Start();

                    // Create the Detail Line
                    DetailLine detailCurveLine = doc.Create.NewDetailCurve(uidoc.ActiveView, lineGeometry) as DetailLine;

                    // Check and assign the Matchline style
                    GraphicsStyle matchLineStyle = new FilteredElementCollector(doc)
                        .OfClass(typeof(GraphicsStyle))
                        .Cast<GraphicsStyle>()
                        .FirstOrDefault(gs => gs.Name == "Matchline");

                    if (matchLineStyle != null)
                    {

                        detailCurveLine.LineStyle = matchLineStyle;
                    }
                    else
                    {
                        // Optionally handle the case where Matchline style is not found
                        ShowInfoDialog("MatchLine Style Not Found.");
                    }

                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                ShowErrorDialog($"An unexpected error occurred: {ex.Message}");
                return Result.Failed;
            }
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
