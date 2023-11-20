#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get the current selection
            Selection selection = uiapp.ActiveUIDocument.Selection;

            // Retrieve the selected elements
            ICollection<ElementId> selectedElementIds = selection.GetElementIds();

            // Ensure that one and only one element is selected
            if (selectedElementIds.Count != 1)
            {
                TaskDialog.Show("Error", "Please select one and only one Scope Box.");
                return Result.Cancelled;
            }

            // Get the selected Scope Box
            Element selectedScopeBox = doc.GetElement(selectedElementIds.First());

            // Ensure that the selected element is a Scope Box
            if (selectedScopeBox.Category.Name != "Scope Boxes")
            {
                TaskDialog.Show("Error", "You did not select a Scope Box.");
                return Result.Cancelled;
            }

            // number of copies
            var rowNum = 3;
            var colNum = 3;

            // Use a transaction to perform operations on the document
            using (Transaction transaction = new Transaction(doc, "Copy Element"))
            {
                transaction.Start();

                // Process the selected elements
                foreach (ElementId elementId in selectedElementIds)
                {
                    Element selectedElement = doc.GetElement(elementId);

                    // Now you can work with the selected element
                    if (selectedElement != null)
                    {
                        // Do something with the selected element
                        Debug.Print("Selected Element => Element Name: " + selectedElement.Name);

                        // make a copy of the selected element
                        ElementId copiedElementId = CopyElement(doc, selectedElement, new XYZ(0, 0, 0));

                        if (copiedElementId != null)
                        {
                            Element copiedElement = doc.GetElement(copiedElementId);
                            if (copiedElement != null)
                            {
                                // Do something with the copied element
                                Debug.Print("Copied Element => Element Name: " + copiedElement.Name);
                            }
                        }
                    }
                }

                // Commit the transaction
                transaction.Commit();
            }
            return Result.Succeeded;
        }

        private ElementId CopyElement(Document document, Element elementToCopy, XYZ insertionPoint)
        {
            // Perform the copy operation using ElementTransformUtils.
            ICollection<ElementId> copiedElementIds = ElementTransformUtils.CopyElement(document, elementToCopy.Id, insertionPoint);

            // Check if the copy operation was successful and at least one element was copied.
            if (copiedElementIds.Count > 0)
            {
                // Retrieve the first copied ElementId.
                ElementId copiedElementId = copiedElementIds.First();

                // Return the ElementId of the copied element.
                return copiedElementId;
            }

            // If no elements were copied, return null to indicate failure.
            return null;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Scope Box";

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
