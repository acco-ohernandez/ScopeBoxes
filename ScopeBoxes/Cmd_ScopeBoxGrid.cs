#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using RevitAddinTesting.Forms;

#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_ScopeBoxGrid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            try
            {
                // Get the user's drawn scope box using the provided method
                Element userDrawnScopeBox = GetSelectedScopeBox(doc, uiapp);

                // Check if a valid scope box was selected
                if (userDrawnScopeBox == null)
                {
                    return Result.Failed; // If not no valid scope box selected, Teminate the command
                }

                // Get the dimensions of the userScopeBoxBoundingBox
                BoundingBoxXYZ userScopeBoxBoundingBox = userDrawnScopeBox.get_BoundingBox(null);
                double newScopeBoxX = userScopeBoxBoundingBox.Max.X - userScopeBoxBoundingBox.Min.X;
                double newScopeBoxY = userScopeBoxBoundingBox.Max.Y - userScopeBoxBoundingBox.Min.Y;


                // ================== Form data ===================
                // Access the controls from the form
                ScopeBoxGridForm form = new ScopeBoxGridForm();
                form.txtBaseScopeBoxName.Text = userDrawnScopeBox.Name;
                form.txtHorizontalOverlap.Text = MyUtils.GetViewScaleMultipliedValue(doc.ActiveView, 48, 5).ToString();
                form.txtVerticalOverlap.Text = MyUtils.GetViewScaleMultipliedValue(doc.ActiveView, 48, 5).ToString();
                form.ShowDialog();

                // Read values from textboxes
                int rows = int.Parse(form.txtRows.Text);
                int columns = int.Parse(form.txtColumns.Text);
                double HorizontalFeetOverlap = double.Parse(form.txtHorizontalOverlap.Text);
                double VerticalFeetOverlap = double.Parse(form.txtVerticalOverlap.Text);
                string scopeBoxBaseName = form.txtBaseScopeBoxName.Text;
                //Char nameChar = char.Parse(form.txtNameChar.Text);
                // ================== Form data End===================

                //// Define the number of rows and columns for inner scope boxes
                //int rows = 3; // Number of rows
                //int columns = 3; // Number of columns

                //// Define the Overlap of the scope boxes
                //double HorizontalFeetOverlap = 5;
                //double VerticalFeetOverlap = 5;

                //// Define the Name for the new Scope Box
                //string scopeBoxBaseName = "My ScopeBox";
                //char nameChar = 'A';



                var listOfScopeBoxesCreated = new List<Element>();
                listOfScopeBoxesCreated.Add(userDrawnScopeBox);

                using (Transaction transaction = new Transaction(doc))
                {
                    // Start the transaction to create inner scope boxes and delete the original one
                    transaction.Start("Create Grid Scope Boxes and Delete Original");
                    userDrawnScopeBox.Name = scopeBoxBaseName;

                    // Calculate the starting point for rows and columns
                    XYZ startingPoint = new XYZ(0, 0, 0);

                    // Iterate through rows and columns to create the grid of scope boxes
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < columns; j++)
                        {
                            if (i == 0 && j == 0) continue; // Skip first copy to avoid copying over the existing ScopeBox

                            // Calculate the origin for the new scope box with overlaps
                            // Note: Adding j * (newScopeBoxX - HorizontalFeetOverlap) to move Horizontally
                            // Note: Subtracting i * (newScopeBoxY - VerticalFeetOverlap) to move vertically
                            XYZ origin = new XYZ(
                                                    startingPoint.X + j * (newScopeBoxX - HorizontalFeetOverlap),
                                                    startingPoint.Y - i * (newScopeBoxY - VerticalFeetOverlap),
                                                    startingPoint.Z
                                                );

                            // Use ElementTransformUtils.CopyElements to replicate the outer scope box
                            ICollection<ElementId> copiedScopeBoxes = ElementTransformUtils.CopyElements(
                                                                            doc,
                                                                            new List<ElementId> { userDrawnScopeBox.Id },
                                                                            XYZ.Zero
                                                                        );

                            // Iterate through the copied scope boxes to move them to their respective positions
                            foreach (ElementId copiedScopeBoxId in copiedScopeBoxes)
                            {
                                Element copiedScopeBox = doc.GetElement(copiedScopeBoxId);
                                //copiedScopeBox.Name = $"{scopeBoxBaseName} {nameChar}";  // Rename New Scope Box
                                //nameChar++; // Increase the Char value for nameChar
                                ElementTransformUtils.MoveElement(doc, copiedScopeBoxId, origin); // Move the new Scope Box
                                listOfScopeBoxesCreated.Add(copiedScopeBox);
                            }
                        }
                    }

                    // Delete the original user-drawn scope box
                    //doc.Delete(userDrawnScopeBox.Id);

                    // Commit the transaction
                    transaction.Commit();
                }
                TaskDialog.Show("Info", $"{listOfScopeBoxesCreated.Count} Total Scopeboxes");

            }
            catch (Exception e)
            {

                TaskDialog.Show("Error", $"{e.Message}");

            }
            // Return the result indicating success
            return Result.Succeeded;
        }

        public static Element GetSelectedScopeBox(Document doc, UIApplication uiapp)
        {
            // Get the current selection
            Selection selection = uiapp.ActiveUIDocument.Selection;

            // Retrieve the selected elements
            ICollection<ElementId> selectedElementIds = selection.GetElementIds();

            // Ensure that one and only one element is selected
            if (selectedElementIds.Count != 1)
            {
                TaskDialog.Show("Error", "Please select one and only one Scope Box.");
                return null;
            }

            // Get the selected Scope Box
            var selectedElement = doc.GetElement(selectedElementIds.First());

            // Ensure that the selected element is a Scope Box
            if (selectedElement.Category.Name != "Scope Boxes")
            {
                TaskDialog.Show("Error", "You did not select a Scope Box.");
                return null;
            }
            return selectedElement;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Scope Box \nGrid";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This button will allow you to create a grid of scope boxes based on the number of columns and rows required. You will first have to create a desired size scope box, then select it in order to create a Grid of Scope Boxes." +

                "\n\nSTEP 1: Create first scope box" +
                "\nSTEP 2: Select scope box" +
                "\nSTEP 3: Click Scope Box Grid button" +
                "\nSTEP 4: Specify the amount of columns, rows and the horizontal, vertical overlapping distances");

            return myButtonData1.Data;
        }
    }
}
