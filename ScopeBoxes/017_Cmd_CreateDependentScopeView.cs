#region Namespaces
using System.Collections.Generic;
using System.Reflection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_CreateDependentScopeView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            //// Uncomment these two lines if you want to process all the FloorPlan Views, then comment out the third line.
            //var allParentViews = MyUtils.GetAllParentViews(doc).Where(v => v.Id == doc.ActiveView.Id).ToList();
            //var allFloorPlanViews = allParentViews.Where(v => v.ViewType == ViewType.FloorPlan); 
            List<View> allFloorPlanViews = new List<View> { doc.ActiveView as View }; // This will only do the current active view


            // Form would load Parent views
            // No form currently created


            List<View> CreatedViews = new List<View>();

            // Start a new transaction
            using (Transaction trans = new Transaction(doc, "Create Dependent View"))
            {
                trans.Start();
                // Create the dependent views
                foreach (var view in allFloorPlanViews)
                {
                    var scopeBoxesOnCurrentView = MyUtils.GetAllScopeBoxesInView(view);

                    if (scopeBoxesOnCurrentView.Count == 0)
                    {
                        TaskDialog.Show("INFO", "No scope boxes found, you may need to turn them ON in the Visibility Graphic settings.");
                        return Result.Cancelled;
                    }

                    foreach (var box in scopeBoxesOnCurrentView)
                    {
                        var CreatedView = MyUtils.CreateDependentViewByScopeBox(doc, view, box);
                        CreatedViews.Add(CreatedView);
                    }
                }
                trans.Commit();
            }

            if (CreatedViews.Count > 0)
                TaskDialog.Show("INFO", $"{CreatedViews.Count} Dependent Views Created");

            return Result.Succeeded;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateDependentScopeView";
            string buttonTitle = "Create\nDependent Views\nFor Current View";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Green_32,
                Properties.Resources.Green_16,
                "This button will create dependent views of the Current Active view based on the number of scope boxes.");

            return myButtonData1.Data;
        }

    }

}
