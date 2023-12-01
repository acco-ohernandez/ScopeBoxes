using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ScopeBoxes
{




    [Transaction(TransactionMode.Manual)]
    public class ElementPicker : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            //try
            //{
            //    // Get the active document
            //    Document doc = commandData.Application.ActiveUIDocument.Document;

            //    // Set the selection options
            //    SelectionOptions selectionOptions = new SelectionOptions();

            //    // Create a new reference array to store the picked references
            //    List<Reference> pickedReferences = new List<Reference>();

            //    // Use PickObjects to allow the user to select elements
            //    ICollection<ElementId> pickedElementIds = commandData.Application.ActiveUIDocument.Selection.PickObjects(
            //        ObjectType.Element,
            //        new MySelectionFilter(), // Replace MySelectionFilter with your actual filter
            //        "Pick elements"
            //    );

            //    // Convert ElementIds to References
            //    foreach (ElementId elementId in pickedElementIds)
            //    {
            //        pickedReferences.Add(new Reference(doc, elementId));
            //    }

            //    // Do something with the picked elements
            //    foreach (Reference reference in pickedReferences)
            //    {
            //        // Access the element using reference.ElementId
            //        Element element = doc.GetElement(reference.ElementId);
            //        // Do something with the element
            //    }

            return Result.Succeeded;
            //}
            //catch (Exception ex)
            //{
            //    // Handle exceptions or log errors
            //    TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
            //    return Result.Failed;
            //}
        }
    }

    // Implement your custom selection filter class
    public class MySelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // Implement your filtering logic here
            return true; // Return true if the element should be selectable
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            // Implement your filtering logic here
            return true; // Return true if the reference should be selectable
        }
    }

}