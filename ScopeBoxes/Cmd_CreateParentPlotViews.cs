#region Namespaces
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitAddinTesting.Forms;
#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_CreateParentPlotViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Obtain the Revit application and document objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all the Levels and their Element Ids as a dictionary
            Dictionary<ElementId, Level> levels = GetAllElemIDsAndLevelsAsDictionary(doc);
            // Remove the level named "!CAD Link Template"
            DictionaryRemoveEntryByValueName(levels, "!CAD Link Template");

            // Get all the view family types for Floor Plans
            var viewFamilyTypes = new FilteredElementCollector(doc)
                                  .OfClass(typeof(ViewFamilyType))
                                  .Cast<ViewFamilyType>()
                                  .Where(vft => vft.ViewFamily == ViewFamily.FloorPlan)
                                  .ToList();

            // Get all view templates ViewType.FloorPlan /// Commented out -> And exclude any name with 'RCP' Or 'Ceiling'
            var viewTemplates = new FilteredElementCollector(doc)
                                .OfClass(typeof(View))
                                .Cast<View>()
                                //.Where(v => v.IsTemplate && v.ViewType == ViewType.FloorPlan && !v.Name.Contains("RCP") && !v.Name.Contains("Ceiling"))
                                .Where(v => v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                                .OrderBy(v => v.Name)
                                .ToList();

            // Prompt the user to select view templates and levels
            var result = SelectViewTemplatesAndLevels(doc, viewTemplates, levels.Values.ToList());
            if (result == null)
            {
                // No view templates or levels selected
                return Result.Cancelled;
            }

            var selectedViewTemplates = result.Item1;
            var selectedLevels = result.Item2;

            // Dictionary to keep track of the number of views created for each template
            var viewCounts = new Dictionary<string, int>();

            // Create views for each discipline on each selected level and apply the view templates
            using (Transaction trans = new Transaction(doc, "Create Parent Plot Views"))
            {
                trans.Start();

                foreach (var level in selectedLevels)
                {
                    foreach (var viewFamType in viewFamilyTypes)
                    {
                        foreach (var viewTemplate in selectedViewTemplates)
                        {
                            // Create a new Floor Plan view for each level and ViewFamilyType
                            ViewPlan viewPlan = ViewPlan.Create(doc, viewFamType.Id, level.Id);

                            // Set view names
                            string baseName = "N/A";
                            if (viewTemplate.Name.IndexOf("WORKING", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // This Naming convention is only applied to views where the template name contains the word "Working"
                                // Set view names based on Level number starting at 00 and Level Name.
                                var levelNum = GetLevelNumber(level.Id, levels);
                                baseName = $"{FormatLevelNumber(levelNum)} - {ToTitleCase(level.Name)}";
                            }
                            else
                            {
                                // Set view name based on ViewFamilyType and level
                                baseName = $"{viewFamType.Name} - {level.Name} - {viewTemplate.Name}";
                            }

                            viewPlan.Name = GetUniqueViewName(doc, baseName);

                            // Apply the selected view template
                            viewPlan.ViewTemplateId = viewTemplate.Id;

                            // Increment the count for this view template
                            if (!viewCounts.ContainsKey(viewTemplate.Name))
                            {
                                viewCounts[viewTemplate.Name] = 0;
                            }
                            viewCounts[viewTemplate.Name]++;
                        }
                    }
                }

                trans.Commit();
            }

            // Create a task dialog to show the results
            string mainContent = "View Created |  View Template Applied\n";
            mainContent += string.Join(Environment.NewLine, viewCounts.Select(kvp => $"                  {kvp.Value}  |  {kvp.Key}"));

            TaskDialog taskDialog = new TaskDialog("Views Created")
            {
                MainInstruction = "Views Created Summary",
                MainContent = mainContent,
                ExpandedContent = mainContent // Ensure the dialog expands to fit the content
            };

            taskDialog.Show();

            return Result.Succeeded;
        }

        private static int GetLevelNumber(ElementId levelId, Dictionary<ElementId, Level> levels)
        {
            // Sort the levels by their elevation
            var sortedLevels = levels.Values.OrderBy(l => l.Elevation).ToList();

            // Find the index of the specified level
            int levelNumber = sortedLevels.FindIndex(l => l.Id == levelId);

            // Return the level number formatted as required
            return levelNumber >= 0 ? levelNumber : -1; // Return -1 if the level is not found
        }
        private static string FormatLevelNumber(int levelNumber)
        {
            // Format the level number as a two-digit string or return "Not Found"
            return levelNumber >= 0 ? levelNumber.ToString("D2") : "Not Found";
        }
        private static string ToTitleCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }
        private static void DictionaryRemoveEntryByValueName(Dictionary<ElementId, Level> levels, string valueName)
        {
            // Find the key for the level with the name "!CAD Link Template"
            ElementId keyToRemove = levels.FirstOrDefault(pair => pair.Value.Name == valueName).Key;

            // If the key is found, remove the entry
            if (keyToRemove != null)
            {
                levels.Remove(keyToRemove);
            }
        }

        private static Dictionary<ElementId, Level> GetAllElemIDsAndLevelsAsDictionary(Document doc)
        {

            // Get all the levels
            return new FilteredElementCollector(doc)
                         .OfClass(typeof(Level))
                         .Cast<Level>()
                         .ToDictionary(l => l.Id, l => l);
        }

        private string GetUniqueViewName(Document doc, string baseName)
        {
            if (!ViewNameExists(doc, baseName))
            {
                return baseName;
            }

            int suffix = 1;
            string newName;

            do
            {
                newName = $"{baseName}({suffix})";
                suffix++;
            }
            while (ViewNameExists(doc, newName));

            return newName;
        }

        private bool ViewNameExists(Document doc, string viewName)
        {
            return new FilteredElementCollector(doc)
                   .OfClass(typeof(View))
                   .Cast<View>()
                   .Any(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        }

        // Method to display a selection dialog for view templates and levels and return the selected view templates and levels
        private Tuple<List<View>, List<Level>> SelectViewTemplatesAndLevels(Document doc, List<View> viewTemplates, List<Level> levels)
        {
            // Create view model lists for levels and view templates
            var levelSelections = levels
                                  .Select(l => new LevelSelection { Name = l.Name, Id = l.Id })
                                  .ToList();

            var viewTemplateSelections = viewTemplates
                                         .Select(vt => new ViewTemplateSelection { Name = vt.Name, Id = vt.Id })
                                         .ToList();

            // Show the form
            var form = new LevelsParentViewsForm(levelSelections, viewTemplateSelections);
            if (form.ShowDialog() == true)
            {
                // Get the selected levels and view templates
                var selectedViewTemplates = viewTemplates.Where(vt => form.SelectedViewTemplates.Any(f => f.Id == vt.Id)).ToList();
                var selectedLevels = levels.Where(l => form.Levels.Any(ls => ls.IsSelected && ls.Id == l.Id)).ToList();
                return new Tuple<List<View>, List<Level>>(selectedViewTemplates, selectedLevels);
            }
            return null;
        }

        internal static PushButtonData GetButtonData()
        {
            // Use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "Cmd_CreateParentPlotViews";
            string buttonTitle = "Create Parent\nPlot Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This will create parent plot views...");

            return myButtonData1.Data;
        }
    }
}
