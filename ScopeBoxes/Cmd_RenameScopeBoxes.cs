#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

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
    public class Cmd_RenameScopeBoxes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Lists to store selected and preselected elements
                var pickedElemsList = new List<Element>();
                var preselectedElemsList = GetSelectedScopeBoxes(doc);

                // Check if there are preselected elements; if not, allow the user to pick
                if (preselectedElemsList.Count != 0)
                    pickedElemsList = preselectedElemsList;
                else
                {
                    // Show info message to the user
                    ShowInfoDialog("Pick scope boxes in the desired order. \nPress ESC to stop picking.\n\n each clicked scopebox will be added to the list.");
                    pickedElemsList = PickScopeBoxes(uidoc, doc);
                }

                // Check if there are selected elements; if not, cancel the command
                if (pickedElemsList == null)
                    return Result.Cancelled;

                // Display the RenameScopeBoxesForm to the user
                var renameScopeBoxesForm = new RenameScopeBoxesForm(pickedElemsList);
                renameScopeBoxesForm.ShowDialog();

                // Check if the user clicked the Rename button; if not, cancel the process
                if (renameScopeBoxesForm.DialogResult != true)
                    return Result.Cancelled;

                // Get the list of new names as a string list
                var newNamesList = renameScopeBoxesForm.lbNewNames.Items.Cast<string>().ToList();

                // Get the list of elements from the form with any new list order
                var returnedNewNamesElementList = renameScopeBoxesForm.lbOriginalNames.Items.Cast<Element>().ToList();

                // Rename the scope boxes using a transaction
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Rename Scopeboxes");

                    // Iterate through the selected elements and update their names
                    for (int i = 0; i < pickedElemsList.Count; i++)
                    {
                        returnedNewNamesElementList[i].Name = newNamesList[i];
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

        private List<Element> PickScopeBoxes(UIDocument uidoc, Document doc)
        {
            HashSet<ElementId> uniqueElementIds = new HashSet<ElementId>();
            //List<Element> pickedElemsList = null;
            List<Element> pickedElemsList = new List<Element>();
            bool flag = true;
            int c = 0;

            while (flag)
            {
                try
                {
                    // Prompt user to pick a scope box
                    Reference reference = uidoc.Selection.PickObject(ObjectType.Element, "Pick scope boxes in the desired order. Press ESC to stop picking.");

                    // Access the element using reference.ElementId
                    Element element = doc.GetElement(reference.ElementId);

                    if (IsScopeBox(element))
                    {
                        // Check for duplicates using HashSet
                        if (uniqueElementIds.Add(reference.ElementId))
                        {
                            // If ElementId is not a duplicate, add the reference to the list
                            pickedElemsList.Add(element);
                            c++;
                            // Do something with the picked element
                            Debug.Print($"========>{c}: {element.Name}");
                        }
                        else
                        {
                            ShowWarningDialog("Duplicate scope box selected. Ignoring the duplicate.");
                        }
                    }
                    else
                    {
                        ShowErrorDialog("That was not a scope box\nTry again");
                        throw new Exception();
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // User pressed ESC or canceled the operation
                    flag = false;
                }
                catch (Exception ex)
                {
                    // Handle specific exceptions or log errors
                    ShowErrorDialog($"An error occurred: {ex.Message}");
                    // You may choose to return Result.Failed here if necessary
                }
            }

            if (pickedElemsList.Count != 0)
                return pickedElemsList;
            return null;
        }

        public static bool IsScopeBox(Element element)
        {
            // Check if the element is a scope box
            return element != null && element.Category != null && element.Category.Name == "Scope Boxes";
        }

        private void ShowWarningDialog(string message)
        {
            TaskDialog.Show("Warning", message);
        }

        public static List<Element> GetSelectedScopeBoxes(Document doc)
        {
            List<Element> scopeBoxes = new List<Element>();

            // Get the handle of current document.
            UIDocument uidoc = new UIDocument(doc);

            // Get the element selection of the current document.
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            // Iterate through the selected element IDs and add scope boxes to the list
            foreach (ElementId id in selectedIds)
            {
                Element element = doc.GetElement(id);
                if (IsScopeBox(element))
                {
                    scopeBoxes.Add(element);
                }
            }

            return scopeBoxes;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_RenameScopeBoxes";
            string buttonTitle = "Rename \nScope Boxes";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This button will rename all selected ScopeBoxes. \n " +
                "OPTIONS: \n" +
                " 1. You can either pre-select a group of scopeboxes before clicking the button \n" +
                "   OR \n" +
                " 2. click the button first and select the individual scopeboxes then hit ESC to continue ");

            return myButtonData1.Data;
        }
    }
}
