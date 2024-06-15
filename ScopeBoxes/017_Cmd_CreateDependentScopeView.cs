#region Namespaces
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

            var allParentViews = MyUtils.GetAllParentViews(doc);
            var allFloorPlanViews = allParentViews.Where(v => v.ViewType == ViewType.FloorPlan);
            foreach (var view in allFloorPlanViews)
            {
                var scopeBoxesOnCurrentView = MyUtils.GetAllScopeBoxesInView(view);
                foreach (var box in scopeBoxesOnCurrentView)
                {
                    MyUtils.CreateDependentView(doc, view);
                }
            }


            return Result.Succeeded;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateDependentScopeView";
            string buttonTitle = "Cmd CreateDependentScopeView";

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
