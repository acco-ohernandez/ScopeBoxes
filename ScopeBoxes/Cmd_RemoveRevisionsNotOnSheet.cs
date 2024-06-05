#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
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
    public class Cmd_RemoveRevisionsNotOnSheet : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            try
            {
                // Get all sheets in the project
                FilteredElementCollector sheetCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Sheets)
                    .WhereElementIsNotElementType();

                if (!sheetCollector.Any())
                {
                    ShowTaskDialog("No Sheets", "No Sheets found. Cancelling command.");
                    return Result.Cancelled;
                }

                // Get all revision clouds in the project
                FilteredElementCollector revisionCloudCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_RevisionClouds)
                    .WhereElementIsNotElementType();

                // Get all revisions on those sheets
                HashSet<ElementId> sheetRevIds = new HashSet<ElementId>();
                foreach (ViewSheet sheet in sheetCollector)
                {
                    foreach (RevisionCloud revCloud in revisionCloudCollector)
                    {
                        if (revCloud.OwnerViewId == sheet.Id || revCloud.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).AsElementId() == sheet.Id)
                        {
                            sheetRevIds.Add(revCloud.get_Parameter(BuiltInParameter.REVISION_CLOUD_REVISION).AsElementId());
                        }
                    }
                }

                // Get all revisions in the project
                FilteredElementCollector revisionCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Revisions);

                if (!revisionCollector.Any())
                {
                    ShowTaskDialog("No Revisions", "No Revisions found. Cancelling command.");
                    return Result.Cancelled;
                }

                // Find revisions not on any sheet and prepare for deletion
                List<ElementId> idsToDelete = new List<ElementId>();
                StringBuilder delRevList = new StringBuilder();
                foreach (Revision rev in revisionCollector)
                {
                    if (!sheetRevIds.Contains(rev.Id))
                    {
                        idsToDelete.Add(rev.Id);
                        delRevList.AppendLine($"{rev.SequenceNumber}: {rev.RevisionDate} - {rev.Description}");
                    }
                }

                // Check if there are revisions to delete
                if (!idsToDelete.Any())
                {
                    ShowTaskDialog("No Revisions to Delete", "All revisions are currently in use. No revisions were deleted.");
                    return Result.Succeeded;
                }

                // Log references to revisions
                StringBuilder refDetails = new StringBuilder();
                foreach (ElementId id in idsToDelete)
                {
                    var rev = doc.GetElement(id) as Revision;
                    if (rev != null)
                    {
                        refDetails.AppendLine($"Revision: {rev.SequenceNumber} - {rev.Description}");

                        // Check for revision clouds that reference this revision
                        var revClouds = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_RevisionClouds)
                            .WhereElementIsNotElementType()
                            .WherePasses(new ElementParameterFilter(new FilterElementIdRule(new ParameterValueProvider(new ElementId(BuiltInParameter.REVISION_CLOUD_REVISION)), new FilterNumericEquals(), id)));

                        if (revClouds.Any())
                        {
                            refDetails.AppendLine($"Referenced by {revClouds.Count()} revision clouds");
                        }
                    }
                }

                // Show task dialog with reference details
                ShowTaskDialog("Revision References", "Details about revisions and their references", refDetails.ToString());

                // Delete unused revisions in smaller transactions
                foreach (ElementId id in idsToDelete)
                {
                    using (Transaction t = new Transaction(doc, $"Delete Revision {id.IntegerValue}"))
                    {
                        t.Start();
                        try
                        {
                            doc.Delete(id);
                            t.Commit();
                        }
                        catch (Exception ex)
                        {
                            t.RollBack();
                            message += $"Failed to delete revision ID {id.IntegerValue}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n";
                        }
                    }
                }

                // Inform the user about deleted revisions
                ShowTaskDialog("Purge Revisions", "The following revisions were no longer in use and have been deleted.", delRevList.ToString());

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
            {
                message = $"An invalid operation occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message = $"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                return Result.Failed;
            }
        }

        private void ShowTaskDialog(string title, string mainInstruction, string mainContent = "")
        {
            TaskDialog td = new TaskDialog(title)
            {
                MainInstruction = mainInstruction,
                MainContent = mainContent
            };
            td.Show();
        }


        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    UIApplication uiapp = commandData.Application;
        //    Document doc = uiapp.ActiveUIDocument.Document;

        //    try
        //    {
        //        // Get all sheets in the project
        //        FilteredElementCollector sheetCollector = new FilteredElementCollector(doc)
        //            .OfCategory(BuiltInCategory.OST_Sheets)
        //            .WhereElementIsNotElementType();

        //        if (!sheetCollector.Any())
        //        {
        //            ShowTaskDialog("No Sheets", "No Sheets found. Cancelling command.");
        //            return Result.Cancelled;
        //        }

        //        // Get all revisions on those sheets
        //        HashSet<ElementId> sheetRevIds = new HashSet<ElementId>();
        //        foreach (ViewSheet sheet in sheetCollector)
        //        {
        //            foreach (ElementId revId in sheet.GetAllRevisionCloudIds())
        //            {
        //                sheetRevIds.Add(revId);
        //            }
        //        }

        //        // Get all revisions in the project
        //        FilteredElementCollector revisionCollector = new FilteredElementCollector(doc)
        //            .OfCategory(BuiltInCategory.OST_Revisions);

        //        if (!revisionCollector.Any())
        //        {
        //            ShowTaskDialog("No Revisions", "No Revisions found. Cancelling command.");
        //            return Result.Cancelled;
        //        }

        //        // Find revisions not on any sheet and prepare for deletion
        //        List<ElementId> idsToDelete = new List<ElementId>();
        //        StringBuilder delRevList = new StringBuilder();
        //        foreach (Revision rev in revisionCollector)
        //        {
        //            if (!sheetRevIds.Contains(rev.Id))
        //            {
        //                idsToDelete.Add(rev.Id);
        //                delRevList.AppendLine($"{rev.SequenceNumber}: {rev.RevisionDate} - {rev.Description}");
        //            }
        //        }

        //        // Check if there are revisions to delete
        //        if (!idsToDelete.Any())
        //        {
        //            ShowTaskDialog("No Revisions to Delete", "All revisions are currently in use. No revisions were deleted.");
        //            return Result.Succeeded;
        //        }

        //        // Log references to revisions
        //        StringBuilder refDetails = new StringBuilder();
        //        foreach (ElementId id in idsToDelete)
        //        {
        //            var rev = doc.GetElement(id) as Revision;
        //            if (rev != null)
        //            {
        //                refDetails.AppendLine($"Revision: {rev.SequenceNumber} - {rev.Description}");
        //                var refs = new FilteredElementCollector(doc).WherePasses(new ElementIntersectsElementFilter(rev)).ToElementIds();
        //                if (refs.Any())
        //                {
        //                    refDetails.AppendLine($"Referenced by {refs.Count} elements");
        //                }
        //            }
        //        }

        //        // Show task dialog with reference details
        //        ShowTaskDialog("Revision References", "Details about revisions and their references", refDetails.ToString());

        //        // Delete unused revisions in smaller transactions
        //        foreach (ElementId id in idsToDelete)
        //        {
        //            using (Transaction t = new Transaction(doc, $"Delete Revision {id.IntegerValue}"))
        //            {
        //                t.Start();
        //                try
        //                {
        //                    doc.Delete(id);
        //                    t.Commit();
        //                }
        //                catch (Exception ex)
        //                {
        //                    t.RollBack();
        //                    message += $"Failed to delete revision ID {id.IntegerValue}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n";
        //                }
        //            }
        //        }

        //        // Inform the user about deleted revisions
        //        ShowTaskDialog("Purge Revisions", "The following revisions were no longer in use and have been deleted.", delRevList.ToString());

        //        return Result.Succeeded;
        //    }
        //    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
        //    {
        //        message = $"An invalid operation occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        //        return Result.Failed;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = $"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        //        return Result.Failed;
        //    }
        //}

        //private void ShowTaskDialog(string title, string mainInstruction, string mainContent = "")
        //{
        //    TaskDialog td = new TaskDialog(title)
        //    {
        //        MainInstruction = mainInstruction,
        //        MainContent = mainContent
        //    };
        //    td.Show();
        //}

        //// Custom failure preprocessor
        //public class DeleteFailurePreprocessor : IFailuresPreprocessor
        //{
        //    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        //    {
        //        foreach (FailureMessageAccessor failure in failuresAccessor.GetFailureMessages())
        //        {
        //            // Handle any specific failures or warnings here
        //            failuresAccessor.DeleteWarning(failure);
        //        }
        //        return FailureProcessingResult.Continue;
        //    }
        //}

        ////public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        ////{
        ////    UIApplication uiapp = commandData.Application;
        ////    Document doc = uiapp.ActiveUIDocument.Document;

        ////    try
        ////    {
        ////        // Get all sheets in the project
        ////        FilteredElementCollector sheetCollector = new FilteredElementCollector(doc)
        ////            .OfCategory(BuiltInCategory.OST_Sheets)
        ////            .WhereElementIsNotElementType();

        ////        if (!sheetCollector.Any())
        ////        {
        ////            ShowTaskDialog("No Sheets", "No Sheets found. Cancelling command.");
        ////            return Result.Cancelled;
        ////        }

        ////        // Get all revisions on those sheets
        ////        HashSet<ElementId> sheetRevIds = new HashSet<ElementId>();
        ////        foreach (ViewSheet sheet in sheetCollector)
        ////        {
        ////            foreach (ElementId revId in sheet.GetAllRevisionCloudIds())
        ////            {
        ////                sheetRevIds.Add(revId);
        ////            }
        ////        }

        ////        // Get all revisions in the project
        ////        FilteredElementCollector revisionCollector = new FilteredElementCollector(doc)
        ////            .OfCategory(BuiltInCategory.OST_Revisions);

        ////        if (!revisionCollector.Any())
        ////        {
        ////            ShowTaskDialog("No Revisions", "No Revisions found. Cancelling command.");
        ////            return Result.Cancelled;
        ////        }

        ////        // Find revisions not on any sheet and prepare for deletion
        ////        List<ElementId> idsToDelete = new List<ElementId>();
        ////        StringBuilder delRevList = new StringBuilder();
        ////        foreach (Revision rev in revisionCollector)
        ////        {
        ////            if (!sheetRevIds.Contains(rev.Id))
        ////            {
        ////                idsToDelete.Add(rev.Id);
        ////                delRevList.AppendLine($"{rev.SequenceNumber}: {rev.RevisionDate} - {rev.Description}");
        ////            }
        ////        }

        ////        // Check if there are revisions to delete
        ////        if (!idsToDelete.Any())
        ////        {
        ////            ShowTaskDialog("No Revisions to Delete", "All revisions are currently in use. No revisions were deleted.");
        ////            return Result.Succeeded;
        ////        }

        ////        // Delete unused revisions
        ////        using (Transaction t = new Transaction(doc, "Purge unused revisions"))
        ////        {
        ////            t.Start();
        ////            try
        ////            {
        ////                foreach (ElementId id in idsToDelete)
        ////                {
        ////                    try
        ////                    {
        ////                        doc.Delete(id);
        ////                    }
        ////                    catch (Exception ex)
        ////                    {
        ////                        // Log specific error for each revision
        ////                        message += $"Failed to delete revision ID {id.IntegerValue}: {ex.Message}\n";
        ////                    }
        ////                }
        ////                t.Commit();
        ////            }
        ////            catch (Exception ex)
        ////            {
        ////                t.RollBack();
        ////                message = $"An error occurred when committing the transaction.\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        ////                return Result.Failed;
        ////            }
        ////        }

        ////        // Inform the user about deleted revisions
        ////        ShowTaskDialog("Purge Revisions", "The following revisions were no longer in use and have been deleted.", delRevList.ToString());

        ////        return Result.Succeeded;
        ////    }
        ////    catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
        ////    {
        ////        message = $"An invalid operation occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        ////        return Result.Failed;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        message = $"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        ////        return Result.Failed;
        ////    }
        ////}

        ////private void ShowTaskDialog(string title, string mainInstruction, string mainContent = "")
        ////{
        ////    TaskDialog td = new TaskDialog(title)
        ////    {
        ////        MainInstruction = mainInstruction,
        ////        MainContent = mainContent
        ////    };
        ////    td.Show();
        ////}

        //public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        //{
        //    //https://www.youtube.com/watch?v=PwAZ08GbgyA&list=PLIUZEmK1-KQZ3CbyoHRPyYRc2oYiTEwLq&index=4

        //    UIApplication uiapp = commandData.Application;
        //    Document doc = uiapp.ActiveUIDocument.Document;

        //    // Get the current view
        //    View currentView = doc.ActiveView;

        //    // get all sheets in the project
        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    collector.OfCategory(BuiltInCategory.OST_Sheets).
        //        WhereElementIsNotElementType();
        //    if (collector == null)
        //    {
        //        TaskDialog taskDialog = new TaskDialog("No Sheets")
        //        {
        //            MainInstruction = "No Sheets found.",
        //            MainContent = "Cancelling command."

        //        };
        //        taskDialog.Show();
        //        return Result.Cancelled;
        //    }

        //    // get all the revisions on those sheets
        //    List<ElementId> sheetRevIds = new List<ElementId>();
        //    foreach (ViewSheet sheet in collector.ToElements())
        //    {
        //        foreach (ElementId id in sheet.GetAllRevisionCloudIds())
        //        {
        //            if (!sheetRevIds.Contains(id))
        //            {
        //                sheetRevIds.Add(id);
        //            }
        //        }
        //    }

        //    // get all the revisions in the project
        //    collector = new FilteredElementCollector(doc).
        //        OfCategory(BuiltInCategory.OST_Revisions);

        //    if (collector == null)
        //    {
        //        TaskDialog taskDialog = new TaskDialog("No Revisions")
        //        {
        //            MainInstruction = "No Revisions found.",
        //            MainContent = "Cancelling command."

        //        };
        //        taskDialog.Show();
        //        return Result.Cancelled;
        //    }



        //    // for all the revisions in the project
        //    string delRevList = "";
        //    List<ElementId> idsToDelete = new List<ElementId>();
        //    foreach (Revision rev in collector.ToElements())
        //    {
        //        if (!sheetRevIds.Contains(rev.Id))
        //        {
        //            //flag it for deletion
        //            idsToDelete.Add(rev.Id);
        //            delRevList += $"{rev.SequenceNumber}: {rev.RevisionDate} - {rev.Description}\n";

        //        }
        //    }

        //    //delete them if they are not on the sheets
        //    using (Transaction t = new Transaction(doc))
        //    {
        //        t.Start("Purge unused revisions");
        //        try
        //        {
        //            doc.Delete(idsToDelete);
        //            t.Commit();
        //        }
        //        catch (Exception ex)
        //        {
        //            message = $"An error occured when deleting the revision.\n{ex.Message}";
        //            return Result.Failed;

        //        }
        //    }

        //    // tell the user which revisions were deleted
        //    TaskDialog td = new TaskDialog("Porge Revisions")
        //    {
        //        MainInstruction = "The following revisions were no longer in use and have been deleted.",
        //        MainContent = delRevList
        //    };
        //    td.Show();

        //    return Result.Succeeded;
        //}


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "Btn_RemoveRevisionsNotOnSheet";
            string buttonTitle = "Remove Revisions\nNot On Sheet";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "Delete unused Revisions.");

            return myButtonData1.Data;
        }

    }


}
