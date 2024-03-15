#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows.Controls;
using System.Windows.Markup;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion

namespace ScopeBoxes
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            // 1. Create ribbon tab
            string tabName = "Add-in Testing";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            // 2. Create ribbon panel 
            RibbonPanel panel = Utils.CreateRibbonPanel(app, tabName, "Revit Tools Testing");

            // 3. Create button data instances
            PushButtonData btnData1 = Cmd_ScopeBoxGrid.GetButtonData();
            PushButtonData btnData2 = Cmd_RenameScopeBoxes.GetButtonData();
            PushButtonData btnData3 = Cmd_GridDimensions.GetButtonData();
            PushButtonData btnData4 = Cmd_CleanDependentViewDims.GetButtonData();
            PushButtonData btnData5 = Cmd_CreateMatchlineReference.GetButtonData();
            PushButtonData btnData6 = Cmd_CreateViewReferencesDuplicates.GetButtonData();
            PushButtonData btnData7 = Cmd_ScaleCalculator.GetButtonData();

            // 4. Create buttons
            PushButton myButton1 = panel.AddItem(btnData1) as PushButton;
            PushButton myButton2 = panel.AddItem(btnData2) as PushButton;
            PushButton myButton3 = panel.AddItem(btnData3) as PushButton;
            PushButton myButton4 = panel.AddItem(btnData4) as PushButton;
            PushButton myButton5 = panel.AddItem(btnData5) as PushButton;
            PushButton myButton6 = panel.AddItem(btnData6) as PushButton;
            PushButton myButton7 = panel.AddItem(btnData7) as PushButton;


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
