#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Windows.Annotations;
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
    public class Cmd_CreateParentPlotViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Obtain the Revit application and document objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all the levels
            var levels = new FilteredElementCollector(doc)
                         .OfClass(typeof(Level))
                         .Cast<Level>()
                         .ToList();

            // Get all the view family types for Floor Plans
            var viewFamilyTypes = new FilteredElementCollector(doc)
                                  .OfClass(typeof(ViewFamilyType))
                                  .Cast<ViewFamilyType>()
                                  .Where(vft => vft.ViewFamily == ViewFamily.FloorPlan)
                                  .ToList();

            // Get all view templates
            var viewTemplates = new FilteredElementCollector(doc)
                                .OfClass(typeof(View))
                                .Cast<View>()
                                .Where(v => v.IsTemplate)
                                .OrderBy(v => v.Name)
                                .ToList();

            // Prompt the user to select a view template and levels
            var result = SelectViewTemplate(doc, viewTemplates);
            if (result == null)
            {
                // No view template selected or no levels selected
                return Result.Cancelled;
            }

            var selectedViewTemplate = result.Item1;
            var selectedLevels = result.Item2;

            // Create views for each discipline on each selected level and apply the view template
            using (Transaction trans = new Transaction(doc, "Create Parent Plot Views"))
            {
                trans.Start();

                foreach (var level in selectedLevels)
                {
                    foreach (var viewFamType in viewFamilyTypes)
                    {
                        // Create a new Floor Plan view for each level and ViewFamilyType
                        ViewPlan viewPlan = ViewPlan.Create(doc, viewFamType.Id, level.Id);

                        // Set view name based on ViewFamilyType and level
                        string baseName = $"{viewFamType.Name} - {level.Name}";
                        viewPlan.Name = GetUniqueViewName(doc, baseName);

                        // Apply the selected view template
                        viewPlan.ViewTemplateId = selectedViewTemplate.Id;
                    }
                }

                trans.Commit();
            }

            return Result.Succeeded;
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
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            return collector.OfClass(typeof(View))
                            .Cast<View>()
                            .Any(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase));
        }



        // Method to display a selection dialog for view templates and return the selected view template and levels
        private Tuple<View, List<Level>> SelectViewTemplate(Document doc, List<View> viewTemplates)
        {
            // Create view model lists for levels and view templates
            var levels = new FilteredElementCollector(doc)
                         .OfClass(typeof(Level))
                         .Cast<Level>()
                         .Select(l => new LevelSelection { Name = l.Name, Id = l.Id })
                         .ToList();

            var viewTemplateSelections = viewTemplates
                                         .Select(vt => new ViewTemplateSelection { Name = vt.Name, Id = vt.Id })
                                         .ToList();

            // Show the form
            var form = new LevelsParentViewsForm(levels, viewTemplateSelections);
            if (form.ShowDialog() == true)
            {
                // Get the selected levels and view template
                var selectedViewTemplate = viewTemplates.FirstOrDefault(vt => vt.Id == form.SelectedViewTemplate.Id);
                var selectedLevels = levels.Where(l => l.IsSelected).Select(l => doc.GetElement(l.Id) as Level).ToList();
                return new Tuple<View, List<Level>>(selectedViewTemplate, selectedLevels);
            }
            return null;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
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
