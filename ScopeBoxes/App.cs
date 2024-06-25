#region Namespaces
using System;
using System.Diagnostics;

using Autodesk.Revit.UI;

#endregion

namespace RevitAddinTesting
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            // 1. Create ribbon tab
            string tabName = "Addin_Testing";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            // 2. Create ribbon panel 
            RibbonPanel panel = MyUtils.CreateRibbonPanel(app, tabName, "Revit Tools Testing");

            // 3. Create button data instances
            // 4. Create buttons
            PushButtonData btnData1 = Cmd_ScopeBoxGrid.GetButtonData();
            PushButton myButton1 = panel.AddItem(btnData1) as PushButton;

            PushButtonData btnData2 = Cmd_RenameScopeBoxes.GetButtonData();
            PushButton myButton2 = panel.AddItem(btnData2) as PushButton;

            PushButtonData btnData3 = Cmd_GridDimensions.GetButtonData();
            PushButton myButton3 = panel.AddItem(btnData3) as PushButton;

            PushButtonData btnData4 = Cmd_CleanDependentViewDims.GetButtonData();
            PushButton myButton4 = panel.AddItem(btnData4) as PushButton;

            PushButtonData btnData5 = Cmd_CreateMatchlineReference.GetButtonData();
            PushButton myButton5 = panel.AddItem(btnData5) as PushButton;

            PushButtonData btnData6 = Cmd_CreateViewReferencesDuplicates.GetButtonData();
            PushButton myButton6 = panel.AddItem(btnData6) as PushButton;

            PushButtonData btnData7 = Cmd_ScaleCalculator.GetButtonData();
            PushButton myButton7 = panel.AddItem(btnData7) as PushButton;

            PushButtonData btnData8 = Cmd_RemoveRevisionsNotOnSheet.GetButtonData();
            PushButton myButton8 = panel.AddItem(btnData8) as PushButton;

            PushButtonData btnData9 = Cmd_CreateCatalogPage.GetButtonData();
            PushButton myButton9 = panel.AddItem(btnData9) as PushButton;

            PushButtonData btnData10 = Cmd_CreateBimSetupView.GetButtonData();
            PushButton myButton10 = panel.AddItem(btnData10) as PushButton;

            PushButtonData btnData11 = Cmd_CreateDependentScopeView.GetButtonData();
            PushButton myButton11 = panel.AddItem(btnData11) as PushButton;

            PushButtonData btnData12 = Cmd_CreateParentPlotViews.GetButtonData();
            PushButton myButton12 = panel.AddItem(btnData12) as PushButton;

            PushButtonData btnData13 = Cmd_UpdateAppliedDependentViews.GetButtonData();
            PushButton myButton13 = panel.AddItem(btnData13) as PushButton;

            PushButtonData btnData14 = Cmd_CopyDimsToParentViews.GetButtonData();
            PushButton myButton14 = panel.AddItem(btnData14) as PushButton;


            // Define the URL of the help page
            string helpUrl = "http://www.autodesk.com";
            // Create a new ContextualHelp object with the help type and URL
            ContextualHelp help = new ContextualHelp(ContextualHelpType.Url, helpUrl);
            // Set the contextual help on the button
            myButton1.SetContextualHelp(help);
            // Set ToolTip Image
            //myButton1.ToolTipImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Images\SampleImage_64x64.png"));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
