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

using ScopeBoxes.Forms;
#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the Revit application and document
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Show info message to the user
                ShowInfoDialog("Pick scope boxes in the desired order. Press ESC to stop picking.");

                // Pick scope boxes
                var pickedElemsList = PickScopeBoxes(uidoc, doc);
                if (pickedElemsList == null)
                    return Result.Cancelled; // if no selected elements, Cancel this command

                // Send the selected elements to the form
                var renameScopeBoxesForm1 = new RenameScopeBoxesForm(pickedElemsList);
                renameScopeBoxesForm1.ShowDialog();
                if (renameScopeBoxesForm1.DialogResult != true) return Result.Cancelled; // Cancel the process if the rename button in not clicked.

                // Get the list of new names as a string list
                var newNamesList = renameScopeBoxesForm1.lbNewNames.Items.Cast<string>().ToList();
                // Get the list of elements from the form with any new list order that could have been done on the form.
                var returnedNewNamesElementList = renameScopeBoxesForm1.lbOriginalNames.Items.Cast<Element>().ToList();

                // Rename the scope boxes
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Rename Scopeboxes");
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

        private bool IsScopeBox(Element element)
        {
            // Check if the element is a scope box
            return element != null && element.Category != null && element.Category.Name == "Scope Boxes";
        }

        private void ShowWarningDialog(string message)
        {
            TaskDialog.Show("Warning", message);
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "ReName Elems";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 2");

            return myButtonData1.Data;
        }
    }
}
