#region Namespaces
using System;
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
    public class Cmd_CreateCatalogPage : IExternalCommand
    {
        private ElementId catalogViewSheetId;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Pick an object on the screen.
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            if (0 == selectedIds.Count)
            {
                // If no elements are selected.
                TaskDialog.Show("Revit", "You haven't selected any elements.");
                return Result.Failed;
            }

            try
            {
                using (Transaction transaction = new Transaction(doc))
                {
                    ElementId categoryId = doc.GetElement(selectedIds.First()).Category.Id;

                    if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, uidoc.Selection.GetElementIds()))
                    {
                        // Create a new assembly instance
                        transaction.Start("Create Assembly Instance");
                        AssemblyInstance assemblyInstance = AssemblyInstance.Create(doc, uidoc.Selection.GetElementIds(), categoryId);
                        transaction.Commit();

                        // Create views for the new assembly instance
                        using (Transaction transactionB = new Transaction(doc, "Create Assembly Views"))
                        {
                            transactionB.Start();

                            // Rename the assembly with catalog page number and family name
                            RenameAssembly(doc, assemblyInstance);

                            if (assemblyInstance.AllowsAssemblyViewCreation())
                            {
                                // Create views and place them on a sheet
                                CreateAndPlaceViews(doc, assemblyInstance);
                            }

                            transactionB.Commit();
                        }

                        // Copy template sheet fields to the new sheet
                        using (Transaction transactionC = new Transaction(doc, "Copy Template Sheet Fields"))
                        {
                            transactionC.Start();
                            CopyTemplateSheetFields(doc, catalogViewSheetId);
                            transactionC.Commit();
                        }

                        TaskDialog successStatus = new TaskDialog("Status");
                        successStatus.MainInstruction = "Assembly and views were placed on sheet successfully.\n And copied View Sheet Template fields.";
                        successStatus.Show();
                    }
                    else
                    {
                        TaskDialog successStatus = new TaskDialog("Cancelling");
                        successStatus.MainInstruction = "Request cancelled. The catalog may already exist\n OR\nThe Selected element is not a valid one.";
                        successStatus.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog errorStatus = new TaskDialog("Error");
                errorStatus.MainInstruction = ex.Message;
                errorStatus.Show();
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        private void CopyTemplateSheetFields(Document doc, ElementId catalogViewSheetId)
        {
            // Get the template and target view sheets
            ViewSheet viewSheetTemplate = MyUtils.GetViewSheetByName(doc, "Sheet Name");
            ViewSheet newViewSheet = MyUtils.GetViewSheetById(doc, catalogViewSheetId);

            // Ensure both sheets are valid
            if (viewSheetTemplate == null || newViewSheet == null)
            {
                TaskDialog.Show("Error", "One of the ViewSheets could not be found.");
                return;
            }

            // Collect all the elements in the template sheet
            FilteredElementCollector collector = new FilteredElementCollector(doc, viewSheetTemplate.Id)
                                                 .WhereElementIsNotElementType();

            ICollection<ElementId> elementIds = collector.ToElementIds();

            // Copy the elements from the template sheet to the new sheet
            ElementTransformUtils.CopyElements(viewSheetTemplate, elementIds, newViewSheet, Transform.Identity, new CopyPasteOptions());
        }

        void RenameAssembly(Document doc, AssemblyInstance assemblyInstance)
        {
            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

            foreach (var ei in elems)
            {
                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                if (familyParam != null)
                {
                    string familyName = familyParam.AsValueString();
                    ElementId typeId = ei.GetTypeId();
                    Element typeElement = doc.GetElement(typeId);
                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
                    if (catalogParam != null)
                    {
                        string catalogPageNumber = catalogParam.AsString();
                        assemblyInstance.AssemblyTypeName = $"{catalogPageNumber} - {familyName}";
                    }
                }
            }
        }

        void CreateAndPlaceViews(Document doc, AssemblyInstance assemblyInstance)
        {
            ElementId titleblockId = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .Last()
                .Id;

            ViewSheet viewSheet = AssemblyViewUtils.CreateSheet(doc, assemblyInstance.Id, titleblockId);
            catalogViewSheetId = viewSheet.Id; // Store the ElementId for later use

            RenameSheet(doc, viewSheet, assemblyInstance);

            // Create 3D views
            View3D view3d = Create3DView(doc, assemblyInstance, "Orthographic View", 16, 3);
            View3D viewShaded3d = Create3DView(doc, assemblyInstance, "Orthographic Shaded View", 16, 3, true);

            // Create section views
            ViewSection detailSectionA = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionA, "(2)PROFILE VIEW", 16, 3);
            ViewSection detailSectionB = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionB, "PROFILE VIEW", 16, 3);
            ViewSection detailPlan = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.HorizontalDetail, "TOP VIEW", 16, 3);

            // Place views on the sheet
            PlaceViewsOnSheet(doc, viewSheet, new View[] { view3d, viewShaded3d, detailSectionA, detailSectionB, detailPlan });
        }

        void RenameSheet(Document doc, ViewSheet viewSheet, AssemblyInstance assemblyInstance)
        {
            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

            foreach (var ei in elems)
            {
                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                if (familyParam != null)
                {
                    string familyName = familyParam.AsValueString();
                    ElementId typeId = ei.GetTypeId();
                    Element typeElement = doc.GetElement(typeId);
                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
                    Parameter versionParam = typeElement.get_Parameter(new Guid("c8b268b9-5867-4d10-b3cb-86e7f17fdd33"));
                    if (catalogParam != null)
                    {
                        viewSheet.Name = familyName;
                        viewSheet.SheetNumber = catalogParam.AsString();
                        viewSheet.LookupParameter("ACCO Version No.").Set(versionParam.AsString());
                    }
                }
            }
        }

        View3D Create3DView(Document doc, AssemblyInstance assemblyInstance, string name, int scale, int detailLevel, bool isShaded = false)
        {
            View3D view = AssemblyViewUtils.Create3DOrthographic(doc, assemblyInstance.Id);
            view.Name = $"{assemblyInstance.AssemblyTypeName} - {name}";
            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);
            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);

            if (isShaded)
            {
                view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(3);
            }

            return view;
        }

        ViewSection CreateSectionView(Document doc, AssemblyInstance assemblyInstance, AssemblyDetailViewOrientation orientation, string name, int scale, int detailLevel)
        {
            ViewSection view = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, orientation);
            view.Name = $"{name} - {assemblyInstance.AssemblyTypeName}";
            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);
            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);

            ElementId sectionCatId = new ElementId(-2000200);
            if (view.CanCategoryBeHidden(sectionCatId))
            {
                view.SetCategoryHidden(sectionCatId, true);
            }

            return view;
        }

        void PlaceViewsOnSheet(Document doc, ViewSheet viewSheet, View[] views)
        {
            XYZ[] locations = { new XYZ(0.5, 0.5, 0), new XYZ(0.3, 0.7, 0), new XYZ(0.3, 0.5, 0), new XYZ(0.3, 0.42, 0), new XYZ(0.5, 0.7, 0) };
            for (int i = 0; i < views.Length; i++)
            {
                Viewport.Create(doc, viewSheet.Id, views[i].Id, locations[i]);
            }
        }

        internal static PushButtonData GetButtonData()
        {
            // Use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateCatalogPage";
            string buttonTitle = "Create\nCatalog Page";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Yellow_32,
                Properties.Resources.Yellow_16,
                "This will create a catalog page and more...");

            return myButtonData1.Data;
        }
    }
}


//#region Namespaces
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;

//#endregion

//namespace RevitAddinTesting
//{
//    [Transaction(TransactionMode.Manual)]
//    public class Cmd_CreateCatalogPage : IExternalCommand
//    {
//        public ElementId catalogViewSheetId { get; set; } = null;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiapp = commandData.Application;
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uiapp.ActiveUIDocument.Document;


//            // pick an object on a screen.
//            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

//            if (0 == selectedIds.Count)
//            {
//                // If no elements are selected.
//                TaskDialog.Show("Revit", "You haven't selected any elements.");
//            }
//            else
//            {
//                try
//                {
//                    using (Transaction transaction = new Transaction(doc))
//                    {
//                        ElementId categoryId = doc.GetElement(selectedIds.First()).Category.Id;

//                        if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, uidoc.Selection.GetElementIds()))
//                        {
//                            // Create a new assembly instance
//                            transaction.Start("Create Assembly Instance");
//                            AssemblyInstance assemblyInstance = AssemblyInstance.Create(doc, uidoc.Selection.GetElementIds(), categoryId);
//                            transaction.Commit();

//                            // Create views for the new assembly instance
//                            using (Transaction transactionB = new Transaction(doc, "Create Assembly Views"))
//                            {
//                                transactionB.Start();

//                                // Rename the assembly with catalog page number and family name
//                                RenameAssembly(doc, assemblyInstance);

//                                if (assemblyInstance.AllowsAssemblyViewCreation())
//                                {
//                                    // Create views and place them on a sheet
//                                    CreateAndPlaceViews(doc, assemblyInstance);
//                                    transactionB.Start();

//                                    TaskDialog successStatus = new TaskDialog("Status");
//                                    successStatus.MainInstruction = "Assembly and views were placed on sheet successfully.";
//                                    successStatus.Show();
//                                }

//                                transactionB.Commit();

//                                using (Transaction transactionC = new Transaction(doc, "CopyTemplateSheetFields"))
//                                {
//                                    transactionC.Start();
//                                    CopyTemplateSheetFields(doc, uiapp, catalogViewSheetId);
//                                    transactionC.Commit();

//                                }

//                                transaction.Commit();
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    TaskDialog errorStatus = new TaskDialog("Error");
//                    errorStatus.MainInstruction = ex.Message;
//                    errorStatus.Show();
//                }
//            }
//            return Result.Succeeded;
//        }

//        private void CopyTemplateSheetFields2(Document doc, UIApplication uiapp, ElementId catalogViewSheetId)
//        {
//            var viewSheetTemplate = Utils.GetViewSheetByName(doc, "Sheet - name");
//            var newViewSheet = Utils.GetViewSheetById(doc, catalogViewSheetId);

//            // copy all the elements from viewSheetTemplate and Paste to newViewSheet AllignedToSamePlace
//        }

//        private void CopyTemplateSheetFields(Document doc, UIApplication uiapp, ElementId catalogViewSheetId)
//        {
//            // Get the template and target view sheets
//            ViewSheet viewSheetTemplate = Utils.GetViewSheetByName(doc, "Sheet Number - Sheet Name");
//            ViewSheet newViewSheet = Utils.GetViewSheetById(doc, catalogViewSheetId);

//            // Ensure both sheets are valid
//            if (viewSheetTemplate == null || newViewSheet == null)
//            {
//                TaskDialog.Show("Error", "One of the ViewSheets could not be found.");
//                return;
//            }

//            // Collect all the elements in the template sheet
//            FilteredElementCollector collector = new FilteredElementCollector(doc, viewSheetTemplate.Id)
//                                                 .WhereElementIsNotElementType();

//            ICollection<ElementId> elementIds = collector.ToElementIds();

//            // Copy the elements from the template sheet to the new sheet
//            //using (Transaction trans = new Transaction(doc, "Copy Sheet Elements"))
//            //{
//            //    trans.Start();

//            // Copy elements
//            ElementTransformUtils.CopyElements(viewSheetTemplate, elementIds, newViewSheet, Transform.Identity, new CopyPasteOptions());

//            //    trans.Commit();
//            //}
//        }


//        void RenameAssembly(Document doc, AssemblyInstance assemblyInstance)
//        {
//            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
//            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

//            foreach (var ei in elems)
//            {
//                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
//                if (familyParam != null)
//                {
//                    string familyName = familyParam.AsValueString();
//                    ElementId typeId = ei.GetTypeId();
//                    Element typeElement = doc.GetElement(typeId);
//                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
//                    if (catalogParam != null)
//                    {
//                        string catalogPageNumber = catalogParam.AsString();
//                        assemblyInstance.AssemblyTypeName = $"{catalogPageNumber} - {familyName}";
//                    }
//                }
//            }
//        }

//        void CreateAndPlaceViews(Document doc, AssemblyInstance assemblyInstance)
//        {
//            ElementId titleblockId = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilySymbol))
//                .OfCategory(BuiltInCategory.OST_TitleBlocks)
//                .Cast<FamilySymbol>()
//                .Last()
//                .Id;

//            ViewSheet viewSheet = AssemblyViewUtils.CreateSheet(doc, assemblyInstance.Id, titleblockId);
//            //UPdate ViewSheet Prop
//            catalogViewSheetId = viewSheet.Id;

//            RenameSheet(doc, viewSheet, assemblyInstance);

//            // Create 3D views
//            View3D view3d = Create3DView(doc, assemblyInstance, "Orthographic View", 16, 3);
//            View3D viewShaded3d = Create3DView(doc, assemblyInstance, "Orthographic Shaded View", 16, 3, true);

//            // Create section views
//            ViewSection detailSectionA = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionA, "(2)PROFILE VIEW", 16, 3);
//            ViewSection detailSectionB = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionB, "PROFILE VIEW", 16, 3);
//            ViewSection detailPlan = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.HorizontalDetail, "TOP VIEW", 16, 3);

//            // Place views on the sheet
//            PlaceViewsOnSheet(doc, viewSheet, new View[] { view3d, viewShaded3d, detailSectionA, detailSectionB, detailPlan });
//        }


//        void RenameSheet(Document doc, ViewSheet viewSheet, AssemblyInstance assemblyInstance)
//        {
//            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
//            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

//            foreach (var ei in elems)
//            {
//                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
//                if (familyParam != null)
//                {
//                    string familyName = familyParam.AsValueString();
//                    ElementId typeId = ei.GetTypeId();
//                    Element typeElement = doc.GetElement(typeId);
//                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
//                    Parameter versionParam = typeElement.get_Parameter(new Guid("c8b268b9-5867-4d10-b3cb-86e7f17fdd33"));
//                    if (catalogParam != null)
//                    {
//                        viewSheet.Name = familyName;
//                        viewSheet.SheetNumber = catalogParam.AsString();
//                        viewSheet.LookupParameter("ACCO Version No.").Set(versionParam.AsString());
//                    }
//                }
//            }
//        }

//        View3D Create3DView(Document doc, AssemblyInstance assemblyInstance, string name, int scale, int detailLevel, bool isShaded = false)
//        {
//            View3D view = AssemblyViewUtils.Create3DOrthographic(doc, assemblyInstance.Id);
//            view.Name = $"{assemblyInstance.AssemblyTypeName} - {name}";
//            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);
//            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);

//            if (isShaded)
//            {
//                view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(3);
//            }

//            return view;
//        }

//        ViewSection CreateSectionView(Document doc, AssemblyInstance assemblyInstance, AssemblyDetailViewOrientation orientation, string name, int scale, int detailLevel)
//        {
//            ViewSection view = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, orientation);
//            view.Name = $"{name} - {assemblyInstance.AssemblyTypeName}";
//            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);
//            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);

//            ElementId sectionCatId = new ElementId(-2000200);
//            if (view.CanCategoryBeHidden(sectionCatId))
//            {
//                view.SetCategoryHidden(sectionCatId, true);
//            }

//            return view;
//        }

//        void PlaceViewsOnSheet(Document doc, ViewSheet viewSheet, View[] views)
//        {
//            XYZ[] locations = { new XYZ(0.5, 0.5, 0), new XYZ(0.3, 0.7, 0), new XYZ(0.3, 0.5, 0), new XYZ(0.3, 0.42, 0), new XYZ(0.5, 0.7, 0) };
//            for (int i = 0; i < views.Length; i++)
//            {
//                Viewport.Create(doc, viewSheet.Id, views[i].Id, locations[i]);
//            }
//        }


//        internal static PushButtonData GetButtonData()
//        {
//            // use this method to define the properties for this command in the Revit ribbon
//            string buttonInternalName = "btn_CreateCatalogPage";
//            string buttonTitle = "Create\nCatalog Page";

//            ButtonDataClass myButtonData1 = new ButtonDataClass(
//                buttonInternalName,
//                buttonTitle,
//                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
//                Properties.Resources.Blue_32,
//                Properties.Resources.Blue_16,
//                "This will create a catalog page and more...");

//            return myButtonData1.Data;
//        }

//    }
//}


//////////////////////////////////////////
//#region Namespaces
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;
//using System.Security.AccessControl;
//using System.Windows.Annotations;
//using System.Windows.Controls;
//using System.Windows.Controls.Primitives;

//using Autodesk.Revit.ApplicationServices;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;

//using RevitAddinTesting.Forms;

//#endregion

//namespace RevitAddinTesting
//{
//    [Transaction(TransactionMode.Manual)]
//    public class Cmd_CreateCatalogPage : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiapp = commandData.Application;
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uiapp.ActiveUIDocument.Document;


//            // pick an object on a screen.
//            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

//            if (0 == selectedIds.Count)
//            {
//                // If no elements are selected.
//                TaskDialog.Show("Revit", "You haven't selected any elements.");
//            }
//            else
//            {
//                try
//                {
//                    using (Transaction transaction = new Transaction(doc))
//                    {
//                        ElementId categoryId = doc.GetElement(selectedIds.First()).Category.Id;

//                        if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, uidoc.Selection.GetElementIds()))
//                        {
//                            // Create a new assembly instance
//                            transaction.Start("Create Assembly Instance");
//                            AssemblyInstance assemblyInstance = AssemblyInstance.Create(doc, uidoc.Selection.GetElementIds(), categoryId);
//                            transaction.Commit();

//                            // Create views for the new assembly instance
//                            using (Transaction transactionB = new Transaction(doc, "Create Assembly Views"))
//                            {
//                                transactionB.Start();

//                                // Rename the assembly with catalog page number and family name
//                                RenameAssembly(doc, assemblyInstance);

//                                if (assemblyInstance.AllowsAssemblyViewCreation())
//                                {
//                                    // Create views and place them on a sheet
//                                    CreateAndPlaceViews(doc, assemblyInstance);

//                                    TaskDialog successStatus = new TaskDialog("Status");
//                                    successStatus.MainInstruction = "Assembly and views were placed on sheet successfully.";
//                                    successStatus.Show();
//                                }

//                                transactionB.Commit();
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    TaskDialog errorStatus = new TaskDialog("Error");
//                    errorStatus.MainInstruction = ex.Message;
//                    errorStatus.Show();
//                }
//            }
//            return Result.Succeeded;
//        }

//        void RenameAssembly(Document doc, AssemblyInstance assemblyInstance)
//        {
//            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
//            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

//            foreach (var ei in elems)
//            {
//                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
//                if (familyParam != null)
//                {
//                    string familyName = familyParam.AsValueString();
//                    ElementId typeId = ei.GetTypeId();
//                    Element typeElement = doc.GetElement(typeId);
//                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
//                    if (catalogParam != null)
//                    {
//                        string catalogPageNumber = catalogParam.AsString();
//                        assemblyInstance.AssemblyTypeName = $"{catalogPageNumber} - {familyName}";
//                    }
//                }
//            }
//        }

//        void CreateAndPlaceViews(Document doc, AssemblyInstance assemblyInstance)
//        {
//            ElementId titleblockId = new FilteredElementCollector(doc)
//                .OfClass(typeof(FamilySymbol))
//                .OfCategory(BuiltInCategory.OST_TitleBlocks)
//                .Cast<FamilySymbol>()
//                .Last()
//                .Id;

//            ViewSheet viewSheet = AssemblyViewUtils.CreateSheet(doc, assemblyInstance.Id, titleblockId);
//            RenameSheet(doc, viewSheet, assemblyInstance);

//            // Create 3D views
//            View3D view3d = Create3DView(doc, assemblyInstance, "Orthographic View", 16, 3);
//            View3D viewShaded3d = Create3DView(doc, assemblyInstance, "Orthographic Shaded View", 16, 3, true);

//            // Create section views
//            ViewSection detailSectionA = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionA, "(2)PROFILE VIEW", 16, 3);
//            ViewSection detailSectionB = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.DetailSectionB, "PROFILE VIEW", 16, 3);
//            ViewSection detailPlan = CreateSectionView(doc, assemblyInstance, AssemblyDetailViewOrientation.HorizontalDetail, "TOP VIEW", 16, 3);

//            // Place views on the sheet
//            PlaceViewsOnSheet(doc, viewSheet, new View[] { view3d, viewShaded3d, detailSectionA, detailSectionB, detailPlan });
//        }


//        void RenameSheet(Document doc, ViewSheet viewSheet, AssemblyInstance assemblyInstance)
//        {
//            ICollection<ElementId> memberIds = assemblyInstance.GetMemberIds();
//            FilteredElementCollector elems = new FilteredElementCollector(doc, memberIds).WhereElementIsNotElementType();

//            foreach (var ei in elems)
//            {
//                Parameter familyParam = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
//                if (familyParam != null)
//                {
//                    string familyName = familyParam.AsValueString();
//                    ElementId typeId = ei.GetTypeId();
//                    Element typeElement = doc.GetElement(typeId);
//                    Parameter catalogParam = typeElement.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
//                    Parameter versionParam = typeElement.get_Parameter(new Guid("c8b268b9-5867-4d10-b3cb-86e7f17fdd33"));
//                    if (catalogParam != null)
//                    {
//                        viewSheet.Name = familyName;
//                        viewSheet.SheetNumber = catalogParam.AsString();
//                        viewSheet.LookupParameter("ACCO Version No.").Set(versionParam.AsString());
//                    }
//                }
//            }
//        }

//        View3D Create3DView(Document doc, AssemblyInstance assemblyInstance, string name, int scale, int detailLevel, bool isShaded = false)
//        {
//            View3D view = AssemblyViewUtils.Create3DOrthographic(doc, assemblyInstance.Id);
//            view.Name = $"{assemblyInstance.AssemblyTypeName} - {name}";
//            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);
//            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);

//            if (isShaded)
//            {
//                view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(3);
//            }

//            return view;
//        }

//        ViewSection CreateSectionView(Document doc, AssemblyInstance assemblyInstance, AssemblyDetailViewOrientation orientation, string name, int scale, int detailLevel)
//        {
//            ViewSection view = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, orientation);
//            view.Name = $"{name} - {assemblyInstance.AssemblyTypeName}";
//            view.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(detailLevel);
//            view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(scale);

//            ElementId sectionCatId = new ElementId(-2000200);
//            if (view.CanCategoryBeHidden(sectionCatId))
//            {
//                view.SetCategoryHidden(sectionCatId, true);
//            }

//            return view;
//        }

//        void PlaceViewsOnSheet(Document doc, ViewSheet viewSheet, View[] views)
//        {
//            XYZ[] locations = { new XYZ(0.5, 0.5, 0), new XYZ(0.3, 0.7, 0), new XYZ(0.3, 0.5, 0), new XYZ(0.3, 0.42, 0), new XYZ(0.5, 0.7, 0) };
//            for (int i = 0; i < views.Length; i++)
//            {
//                Viewport.Create(doc, viewSheet.Id, views[i].Id, locations[i]);
//            }
//        }


//        internal static PushButtonData GetButtonData()
//        {
//            // use this method to define the properties for this command in the Revit ribbon
//            string buttonInternalName = "btn_CreateCatalogPage";
//            string buttonTitle = "Create\nCatalog Page";

//            ButtonDataClass myButtonData1 = new ButtonDataClass(
//                buttonInternalName,
//                buttonTitle,
//                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
//                Properties.Resources.Blue_32,
//                Properties.Resources.Blue_16,
//                "This will create a catalog page and more...");

//            return myButtonData1.Data;
//        }

//    }
//}
