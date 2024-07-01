#region Namespaces
using System.Reflection;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion

namespace RevitAddinTesting
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_ScaleCalculator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get the current view
            View currentView = doc.ActiveView;
            //int curViewScaleInt = GetCurrentViewScale(currentView);

            ViewScaleManager viewScaleInfo = new ViewScaleManager(currentView);
            TaskDialog.Show("Info", $"CurrentView Scale Value: {viewScaleInfo.ScaleValue} \n" +
                                    $"Current View Scale: {viewScaleInfo.ViewScaleString}");



            var returnedScopeBoxDistance = MyUtils.GetViewScaleMultipliedValue(currentView, 48, 5);
            //TaskDialog.Show("Info", $"Returned value based of 1/4 scale = {returnedScopeBoxDistance}");

            return Result.Succeeded;
        }





        //private static int GetCurrentViewScale(View curView)
        //{

        //    // Ensure the view is not null and supports scale
        //    if (curView == null || !curView.CanBePrinted)
        //    {
        //        TaskDialog.Show("Error", "The active view does not support scaling.");
        //        return null;
        //    }

        //    // Retrieve and display the view scale
        //    var viewScale = curView.Scale;
        //    TaskDialog.Show("View Scale", $"The scale of the current view is 1:{viewScale}.");
        //    return doc.ActiveView;
        //}

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_ScaleCalculator";
            string buttonTitle = "Scale Calculator";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "Scale Calculator Gets the Scale of the current view.");

            return myButtonData1.Data;
        }

    }



    //public class ViewScaleManager
    //{
    //    public int ScaleValue { get; private set; }
    //    public string ViewScaleString { get; private set; }



    //    // Constructor
    //    public ViewScaleManager(View view)
    //    {
    //        CalculatePropertiesBasedOnViewScale(view);
    //    }

    //    private void CalculatePropertiesBasedOnViewScale(View view)
    //    {
    //        int viewScale = view.Scale;
    //        // Define the scale mappings based on the provided CSV data
    //        var scaleMappings =
    //        new (int scaleNum, string ViewScaleString)[]
    //            {
    //                (1,"12\" = 1'-0\""),
    //                (2,"6\" = 1'-0\""),
    //                (4,"3\" = 1'-0\""),
    //                (8,"1-1/2\" = 1'-0\""),
    //                (12,"1\" = 1'-0\""),
    //                (16,"3/4\" = 1'-0\""),
    //                (24,"1/2\" = 1'-0\""),
    //                (32,"3/8\" = 1'-0\""),
    //                (48,"1/4\" = 1'-0\""),
    //                (64,"3/16\" = 1'-0\""),
    //                (96,"1/8\" = 1'-0\""),
    //                (128,"3/32\" = 1'-0\""),
    //                (192,"1/16\" = 1'-0\""),
    //                (240,"1\" = 20'-0\""),
    //                (256,"3/64\" = 1'-0\""),
    //                (360,"1\" = 30'-0\""),
    //                (384,"1/32\" = 1'-0\""),
    //                (480,"1\" = 40'-0\""),
    //                (600,"1\" = 50'-0\""),
    //                (720,"1\" = 60'-0\""),
    //                (768,"1/64\" = 1'-0\""),
    //                (960,"1\" = 80'-0\""),
    //                (1200,"1\" = 100'-0\""),
    //                (1920,"1\" = 160'-0\""),
    //                (2400,"1\" = 200'-0\""),
    //                (3600,"1\" = 300'-0\""),
    //                (4800,"1\" = 400'-0\""),
    //            };

    //        // Find the corresponding scale mapping
    //        foreach (var mapping in scaleMappings)
    //        {
    //            if (view.Scale == mapping.scaleNum)
    //            {
    //                this.ScaleValue = viewScale;
    //                this.ViewScaleString = mapping.ViewScaleString;
    //                return;
    //            }
    //            else
    //            {
    //                this.ScaleValue = viewScale;
    //                this.ViewScaleString = "Custome Scale";
    //                return;
    //            }
    //        }

    //        // Handle the case where no matching scale is found
    //        throw new ArgumentException("Unsupported view scale.");
    //    }
    //}

}
