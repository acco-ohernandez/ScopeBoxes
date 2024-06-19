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

            //// Uncomment these two lines if you want to process all the FloorPlan Views
            //var allParentViews = MyUtils.GetAllParentViews(doc).Where(v => v.Id == doc.ActiveView.Id).ToList();
            //var allFloorPlanViews = allParentViews.Where(v => v.ViewType == ViewType.FloorPlan); 
            List<View> allFloorPlanViews = new List<View> { doc.ActiveView as View }; // This will only do the current active view


            // Form would load Parent views
            // No form currently created



            // Start a new transaction
            using (Transaction trans = new Transaction(doc, "Create Dependent View"))
            {
                trans.Start();
                // Create the dependent views
                foreach (var view in allFloorPlanViews)
                {
                    var scopeBoxesOnCurrentView = MyUtils.GetAllScopeBoxesInView(view);
                    foreach (var box in scopeBoxesOnCurrentView)
                    {
                        MyUtils.CreateDependentViewByScopeBox(doc, view, box);
                    }
                }
                trans.Commit();
            }



            return Result.Succeeded;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateDependentScopeView";
            string buttonTitle = "Create Dependent ScopeView";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is the command template.");

            return myButtonData1.Data;
        }

    }

}
