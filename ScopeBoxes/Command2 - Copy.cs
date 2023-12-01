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
#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Command2_Backup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get selected elements in the order the user selected
            List<Element> selectedElements = GetSelectedElements(uiapp);
            if (selectedElements.Count == 0)
            {
                TaskDialog.Show("Error", "No elements selected. Please select elements to rename.");
                return Result.Failed;
            }

            // Get user input for prefix or suffix
            string userPrefix = GetUserInput("Enter a prefix (optional): ");
            string userSuffix = GetUserInput("Enter a suffix (optional): ");

            // Rename selected elements
            if (!RenameElements(doc, selectedElements, userPrefix, userSuffix))
            {
                TaskDialog.Show("Error", "Some elements could not be renamed. Check the element names and try again.");
                return Result.Failed;
            }

            // Display a TaskDialog with the count of renamed elements
            TaskDialog.Show("Success", $"{selectedElements.Count} elements have been successfully renamed.");

            return Result.Succeeded;
        }

        private List<Element> GetSelectedElements(UIApplication uiapp)
        {
            // Get the current selection
            Selection selection = uiapp.ActiveUIDocument.Selection;

            // Retrieve the selected elements
            ICollection<ElementId> selectedElementIds = selection.GetElementIds();

            // Convert ElementId to Element
            List<Element> selectedElements = selectedElementIds.Select(id => uiapp.ActiveUIDocument.Document.GetElement(id)).ToList();

            return selectedElements;
        }
        private string GetUserInput(string prompt)
        {
            try
            {
                // Display an input box for the user to enter prefix or suffix
                TaskDialog userInputDialog = new TaskDialog("User Input");
                userInputDialog.MainContent = prompt;

                // Add a text box for user input
                System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
                userInputDialog.CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
                userInputDialog.FooterText = "Note: Leave it blank if not needed.";

                List<System.Windows.FrameworkElement> expandedContent = new List<System.Windows.FrameworkElement>
        {
            new Label() { Content = "Input:" },
            textBox
        };

                //userInputDialog.ExpandedContent = expandedContent;
                userInputDialog.ExpandedContent = "Test2";

                TaskDialogResult result = userInputDialog.Show();

                if (result == TaskDialogResult.Ok)
                {
                    return textBox.Text;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions or log errors
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return string.Empty;
        }


        private bool RenameElements(Document doc, List<Element> elements, string prefix, string suffix)
        {
            using (Transaction transaction = new Transaction(doc))
            {
                try
                {
                    transaction.Start("Rename Elements");

                    foreach (Element element in elements)
                    {
                        // Get the current name of the element
                        string currentName = element.Name;

                        // Generate the new name based on the specified criteria
                        string newName = GenerateNewName(currentName, prefix, suffix);

                        // Rename the element
                        element.Name = newName;
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., elements that cannot be renamed)
                    TaskDialog.Show("Error", $"Error during renaming: {ex.Message}");
                    transaction.RollBack();
                    return false;
                }
            }
        }

        private string GenerateNewName(string currentName, string prefix, string suffix)
        {
            // Increment the last character or handle numeric increments
            // For example, if the currentName is "Area Y", the new name will be "Area Z"
            // If the currentName is "Area 9", the new name will be "Area 10"

            // You can customize this logic based on your specific requirements

            // Split the current name into prefix and base name
            string[] nameParts = currentName.Split(' ');
            string baseName = nameParts.Length > 1 ? nameParts[1] : currentName;

            // Handle numeric increments
            if (int.TryParse(baseName, out int baseNumber))
            {
                baseNumber++;
                return $"{prefix} {baseNumber}{suffix}";
            }

            // Increment the last character
            char lastChar = baseName.Last();
            char newLastChar = (char)(lastChar + 1);

            return $"{prefix} {baseName.Substring(0, baseName.Length - 1)}{newLastChar}{suffix}";
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
