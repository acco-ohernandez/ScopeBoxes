#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
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
            PushButtonData btnData1 = Command1.GetButtonData();
            PushButtonData btnData2 = Command2.GetButtonData();
            PushButtonData btnData3 = Cmd_GridDimensions.GetButtonData();
            PushButtonData btnData4 = Cmd_CleanDependentViewScopeBoxDimensions.GetButtonData();
            //PushButtonData btnData5 = Cmd_CreateThickDottedLine.GetButtonData();
            PushButtonData btnData5 = Cmd_GridForMatchLines.GetButtonData();

            //PushButtonData btnData6 = Cmd_DependentViewsBrowserTree.GetButtonData();

            // 4. Create buttons
            PushButton myButton1 = panel.AddItem(btnData1) as PushButton;
            PushButton myButton2 = panel.AddItem(btnData2) as PushButton;
            PushButton myButton3 = panel.AddItem(btnData3) as PushButton;
            PushButton myButton4 = panel.AddItem(btnData4) as PushButton;
            PushButton myButton5 = panel.AddItem(btnData5) as PushButton;
            //PushButton myButton6 = panel.AddItem(btnData5) as PushButton;

            // NOTE:
            // To create a new tool, copy lines 35 and 39 and rename the variables to "btnData3" and "myButton3". 
            // Change the name of the tool in the arguments of line 

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
