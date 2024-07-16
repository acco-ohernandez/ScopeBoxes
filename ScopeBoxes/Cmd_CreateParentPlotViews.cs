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
        public static string ViewTypeSelected { get; set; }
        public int SelectedScale { get; set; }
        public bool CreateDuplicatesFlag { get; private set; } = true;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Obtain the Revit application and document objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all the Levels and their Element Ids as a dictionary
            Dictionary<ElementId, Level> levels = GetAllElemIDsAndLevelsAsDictionary(doc);
            // Remove the level named "!CAD Link Template"
            DictionaryRemoveEntryByValueName(levels, "!CAD Link Template");

            // Get all view templates ViewType.FloorPlan /// Commented out -> And exclude any name with 'RCP' Or 'Ceiling'
            var viewTemplates = new FilteredElementCollector(doc)
                                .OfClass(typeof(View))
                                .Cast<View>()
                                //.Where(v => v.IsTemplate && v.ViewType == ViewType.FloorPlan && !v.Name.Contains("RCP") && !v.Name.Contains("Ceiling"))
                                .Where(v => v.IsTemplate &&
                                            v.ViewType == ViewType.FloorPlan &&
                                            v.ViewType == ViewType.CeilingPlan)
                                .OrderBy(v => v.Name)
                                .ToList();

            // Prompt the user to select view templates and levels. This method 'SelectViewTemplatesAndLevels' opens the form 'LevelsParentViewsForm'
            var result = SelectViewTemplatesAndLevels(doc, viewTemplates, levels.Values.ToList());
            if (result == null)
            {
                // No view templates or levels selected
                return Result.Cancelled;
            }

            var selectedViewTemplates = result.Item1;
            var selectedLevels = result.Item2;

            List<ViewFamilyType> ViewFamilyTypes = GetViewFamilyTypes(doc, ViewTypeSelected);


            // Dictionary to keep track of the number of views created for each template
            var viewCounts = new Dictionary<string, int>();

            // Create views for each discipline on each selected level and apply the view templates
            using (Transaction trans = new Transaction(doc, "Create Parent Views"))
            {
                trans.Start();

                foreach (var level in selectedLevels)
                {
                    //foreach (var viewFamType in floorPlanViewFamilyTypes)
                    foreach (var viewFamType in ViewFamilyTypes)
                    {
                        foreach (var viewTemplate in selectedViewTemplates)
                        {
                            // Create a new Floor Plan view for each level and ViewFamilyType
                            ViewPlan viewPlan = ViewPlan.Create(doc, viewFamType.Id, level.Id);
                            viewPlan.Scale = SelectedScale; // This Property gets set by the LevelsParentViewsForm

                            string baseName = GenerateViewName(levels, level, viewTemplate);
                            viewPlan.Name = MyUtils.GetUniqueViewName(doc, baseName);

                            //if (MyUtils.isViewNameDuplicate(doc, viewPlan.Name))
                            if (viewPlan.Name.EndsWith(")"))
                            {
                                if (CreateDuplicatesFlag)
                                {

                                    // show TaskDialog with command links: 1- allow the user to cancel the operation 2- create duplicate views
                                    var showDialog = new TaskDialog("Action Required")
                                    {


                                        TitleAutoPrefix = false,
                                        MainIcon = TaskDialogIcon.TaskDialogIconError,
                                        MainInstruction = "You've chosen a level and view template that already contains Parent views.\nHow do you want to proceed?",
                                        MainContent = "Creating duplicate Parent views will add a (#)\nsuffix to the end of the view name.",

                                        AllowCancellation = false

                                    };
                                    // Add command links to the TaskDialog
                                    showDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Cancel and try again");
                                    showDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Create duplicate Parent views");

                                    var dialogResult = showDialog.Show();

                                    if (dialogResult == TaskDialogResult.CommandLink1)
                                    {
                                        trans.RollBack();
                                        return Result.Failed;
                                    }
                                    else if (dialogResult == TaskDialogResult.CommandLink2)
                                    {
                                        CreateDuplicatesFlag = false;
                                    }
                                }
                            }


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

            // Get the string value of the SelectedScale
            string scaleString = MyUtils.ScalesList().First(s => s.Key == SelectedScale).Value;

            string viewsCreated = "Create Parent Views";
            string viewTemplateApplied = "View Template Applied";
            // Create a task dialog to show the results
            string mainContent = $"View Scale: {scaleString}\n\n";
            mainContent += $"Views Created:      {viewTemplateApplied}:\n";
            mainContent += string.Join(Environment.NewLine, viewCounts.Select(kvp => $"                  {kvp.Value}            {kvp.Key}"));

            TaskDialog taskDialog = new TaskDialog(viewsCreated)
            {
                Title = viewsCreated,
                TitleAutoPrefix = false,
                MainInstruction = "Create Parent Views Summary:",
                MainContent = mainContent,
                //ExpandedContent = mainContent // Ensure the dialog expands to fit the content
            };

            taskDialog.Show();

            return Result.Succeeded;
        }

        private List<ViewFamilyType> GetViewFamilyTypes(Document doc, string viewTypeSelected)
        {
            List<ViewFamilyType> vftSelected = new List<ViewFamilyType>();
            // Get all the view family types for Floor Plans
            if (viewTypeSelected == "Floor Plan")
            {
                return new FilteredElementCollector(doc)
                                      .OfClass(typeof(ViewFamilyType))
                                      .Cast<ViewFamilyType>()
                                      .Where(vft => vft.ViewFamily == ViewFamily.FloorPlan) // Only FloorPlans View types will be created.

                                      .ToList();
            }
            else if (viewTypeSelected == "Ceiling Plan")
            {
                return new FilteredElementCollector(doc)
                                   .OfClass(typeof(ViewFamilyType))
                                   .Cast<ViewFamilyType>()
                                   .Where(vft => vft.ViewFamily == ViewFamily.CeilingPlan) // Only CeilingPlan View types will be created.
                                   .ToList();
            }
            else
                return null;
        }

        private static string GenerateViewName(Dictionary<ElementId, Level> levels, Level level, View viewTemplate)
        {

            // Set view names
            string baseName = "N/A";

            if (viewTemplate.Name.IndexOf("WORKING", StringComparison.OrdinalIgnoreCase) >= 0 && ViewTypeSelected == "Ceiling Plan")
            {
                // This Naming convention is only applied to views where the template name contains the word "Working"
                // Set view names based on Level number starting at 00 and Level Name.
                var levelNum = GetLevelNumber(level.Id, levels);
                baseName = $"{FormatLevelNumber(levelNum)} - {ToTitleCase(level.Name)} RCP";
            }
            else if (viewTemplate.Name.IndexOf("WORKING", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // This Naming convention is only applied to views where the template name contains the word "Working"
                // Set view names based on Level number starting at 00 and Level Name.
                var levelNum = GetLevelNumber(level.Id, levels);
                baseName = $"{FormatLevelNumber(levelNum)} - {ToTitleCase(level.Name)}";
            }
            else
            {
                // Set view name based on ViewFamilyType and level
                //baseName = $"{viewFamType.Name} - {level.Name} - {viewTemplate.Name}";
                Parameter tradeParameter = GetViewParameterByName(viewTemplate, "Trade");
                Parameter Sheet_SeriesParameter = GetViewParameterByName(viewTemplate, "Sheet Series");

                string trade = "";
                if (tradeParameter != null && tradeParameter.AsString() != null)
                { trade = $"{tradeParameter.AsString()} "; }

                string Sheet_Series = "";
                if (Sheet_SeriesParameter != null && Sheet_SeriesParameter.AsString() != null)
                { Sheet_Series = $" {Sheet_SeriesParameter.AsString()}"; }

                string levelName = MyUtils.ConvertSpaceToAlt255(level.Name);

                baseName = $"{trade}{levelName}{Sheet_Series} - PARENT";
            }
            //{
            //    // Set view name based on ViewFamilyType and level
            //    //baseName = $"{viewFamType.Name} - {level.Name} - {viewTemplate.Name}";
            //    Parameter tradeParameter = GetViewParameterByName(viewTemplate, "Trade");
            //    Parameter Sheet_SeriesParameter = GetViewParameterByName(viewTemplate, "Sheet Series");

            //    string trade = "";
            //    if (tradeParameter != null && tradeParameter.AsString() != null)
            //    { trade = tradeParameter.AsString(); }

            //    string Sheet_Series = "SHEET SERIES";
            //    if (Sheet_SeriesParameter != null && Sheet_SeriesParameter.AsString() != null)
            //    { Sheet_Series = Sheet_SeriesParameter.AsString(); }

            //    string levelName = MyUtils.ConvertSpaceToAlt255(level.Name);

            //    baseName = $"{trade} {levelName} {Sheet_Series} - PARENT";
            //}

            return baseName;
        }




        private static Parameter GetViewParameterByName(View viewTemplate, string paramName)
        {
            // Set view name based on ViewFamilyType and level
            //baseName = $"{viewFamType.Name} - {level.Name} - {viewTemplate.Name}";
            return viewTemplate.Parameters.Cast<Parameter>().FirstOrDefault(p => p.Definition.Name.Equals(paramName));
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
                         .OrderBy(l => l.Elevation)
                         .ToDictionary(l => l.Id, l => l);
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
                //ViewTypeSelected = (KeyValuePair<string, string>)form.CmBox_ViewType.SelectedItem;

                //var temp = (KeyValuePair<string, string>)form.CmBox_ViewType.SelectedItem;
                //ViewTypeSelected = temp.Value;
                ViewTypeSelected = form.CmBox_ViewType.SelectedItem != null ? ((KeyValuePair<string, string>)form.CmBox_ViewType.SelectedItem).Value : string.Empty;


                SelectedScale = form.SelectedScale;
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
            string buttonTitle = "Create\nParent Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Yellow_32,
                Properties.Resources.Yellow_16,
                "This will create parent views based on the level and view template selected by the user.");

            return myButtonData1.Data;
        }
    }
}