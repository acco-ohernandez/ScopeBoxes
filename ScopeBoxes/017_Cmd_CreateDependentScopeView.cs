#region Namespaces
using System.Collections.Generic;
using System.Linq;
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
            if (allFloorPlanViews.First().ViewType == ViewType.ProjectBrowser)
            {
                MyUtils.M_MyTaskDialog("Action Required", "Please double click your view in the Project Browser before proceeding.", "Warning");
                return Result.Cancelled;
            }

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
                        MyUtils.M_MyTaskDialog("Action Required", "There are no visible Scope Boxes in your view.", "Warning");
                        return Result.Cancelled;
                    }

                    foreach (var box in scopeBoxesOnCurrentView)
                    {
                        var CreatedView = MyUtils.CreateDependentViewByScopeBox(doc, view, box);
                        if (CreatedView == null)
                            return Result.Cancelled;

                        CreatedViews.Add(CreatedView);
                    }
                }
                trans.Commit();
            }

            if (CreatedViews.Count > 0)
                MyUtils.M_MyTaskDialog("Create Dependent Views", $"{CreatedViews.Count} Dependent views created.", false);

            return Result.Succeeded;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateDependentScopeView";
            string buttonTitle = "Create\nDependent Views";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Yellow_32,
                Properties.Resources.Yellow_16,
                "This button will create dependent views of the Current Active view based on the number of Scope Boxes shown.");

            return myButtonData1.Data;
        }

    }

}
