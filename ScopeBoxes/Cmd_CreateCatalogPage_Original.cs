#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
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
    public class Cmd_CreateCatalogPage_Original : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;


            // pick an object on a screen.
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            if (0 == selectedIds.Count)
            {
                // If no elements are selected.
                TaskDialog.Show("Revit", "You haven't selected any elements.");
            }
            else
            {
                try
                {
                    using (Transaction transaction = new Transaction(doc))
                    {
                        // Get ID of selected element
                        ElementId categoryId = doc.GetElement(selectedIds.First()).Category.Id;

                        if (AssemblyInstance.IsValidNamingCategory(doc, categoryId, uidoc.Selection.GetElementIds()))
                        {
                            // Create NEW assembly of selected components
                            transaction.Start("Create Assembly Instance");
                            AssemblyInstance assemblyInstance = AssemblyInstance.Create(doc, uidoc.Selection.GetElementIds(), categoryId);
                            // need to commit the transaction to complete the creation of the assembly instance so it can be accessed in the code below
                            transaction.Commit();
                            // starting another transaction to create views 
                            Transaction transactionB = new Transaction(doc, "Create Assembly Views");
                            transactionB.Start();

                            // Getting Element in the Assembly 
                            ElementId asID = assemblyInstance.AssemblyInstanceId;
                            ICollection<ElementId> memberids = assemblyInstance.GetMemberIds();
                            FilteredElementCollector elems = new FilteredElementCollector(doc, memberids).WhereElementIsNotElementType();

                            // Renaming the Assembly with Catalog Page Number and Family Name
                            foreach (var ei in elems)
                            {
                                Parameter P = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                                string familyName = P.AsValueString();
                                ElementId id_type = ei.GetTypeId();
                                Element element_type = doc.GetElement(id_type);
                                Parameter sharedParameter = element_type.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
                                string catalogPageNumber = sharedParameter.AsString();

                                if (P != null)
                                {
                                    assemblyInstance.AssemblyTypeName = catalogPageNumber + " - " + familyName;
                                }
                            }



                            // Creating Views scaled 3/4" = 1'-0"
                            if (assemblyInstance.AllowsAssemblyViewCreation()) // check to see if views can be created for this assembly
                            {
                                ElementId titleblockId = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_TitleBlocks).Cast<FamilySymbol>().Last().Id;

                                ViewSheet viewSheet = AssemblyViewUtils.CreateSheet(doc, assemblyInstance.Id, titleblockId);
                                // Renaming Sheet Name and Sheet Number 
                                foreach (var ei in elems)
                                {
                                    Parameter P = ei.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM);
                                    string familyName = P.AsValueString();
                                    ElementId id_type = ei.GetTypeId();
                                    Element element_type = doc.GetElement(id_type);
                                    Parameter sharedParameter = element_type.get_Parameter(new Guid("7ca1c138-e50a-4608-a28d-cb149048819d"));
                                    Parameter versionNumber = element_type.get_Parameter(new Guid("c8b268b9-5867-4d10-b3cb-86e7f17fdd33"));
                                    string catalogPageNumber = sharedParameter.AsString();
                                    viewSheet.Name = familyName;
                                    viewSheet.SheetNumber = catalogPageNumber;
                                    viewSheet.LookupParameter("ACCO Version No.").Set(versionNumber.AsString());
                                }


                                // Creating two 3D views where the second will be shaded 
                                View3D view3d = AssemblyViewUtils.Create3DOrthographic(doc, assemblyInstance.Id);
                                view3d.Name = assemblyInstance.AssemblyTypeName.ToString();
                                view3d.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(16);
                                view3d.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                                View3D viewShaded3d = AssemblyViewUtils.Create3DOrthographic(doc, assemblyInstance.Id);
                                viewShaded3d.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE).Set(3);
                                viewShaded3d.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(16);
                                viewShaded3d.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                                viewShaded3d.Name = assemblyInstance.AssemblyTypeName.ToString() + " - Shaded";
                                // Creating two section views, Detail Level: Fine 
                                ViewSection detailSectionA = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.DetailSectionA);
                                detailSectionA.Name = "(2)PROFILE VIEW - " + assemblyInstance.AssemblyTypeName.ToString();
                                detailSectionA.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                                detailSectionA.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(16);
                                // Hiding the section marks in the section views 
                                ElementId sectionCatId = new ElementId(-2000200);
                                if (detailSectionA.CanCategoryBeHidden(sectionCatId))
                                {
                                    detailSectionA.SetCategoryHidden(sectionCatId, true);
                                }
                                ViewSection detailSectionB = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.DetailSectionB);
                                detailSectionB.Name = "PROFILE VIEW - " + assemblyInstance.AssemblyTypeName.ToString();
                                detailSectionB.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                                detailSectionB.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(16);
                                // Hiding the section marks in the section views 
                                if (detailSectionB.CanCategoryBeHidden(sectionCatId))
                                {
                                    detailSectionB.SetCategoryHidden(sectionCatId, true);
                                }
                                // Creating a Plan View, Detail Level: Fine 
                                ViewSection detailPlan = AssemblyViewUtils.CreateDetailSection(doc, assemblyInstance.Id, AssemblyDetailViewOrientation.HorizontalDetail);
                                detailPlan.Name = "TOP VIEW - " + assemblyInstance.AssemblyTypeName.ToString();
                                detailPlan.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set(3);
                                detailPlan.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_IMPERIAL).Set(16);
                                // Hiding the section marks in the section views 
                                if (detailPlan.CanCategoryBeHidden(sectionCatId))
                                {
                                    detailPlan.SetCategoryHidden(sectionCatId, true);
                                }

                                // Placing Views on Sheet 
                                Viewport.Create(doc, viewSheet.Id, view3d.Id, new XYZ(0.5, 0.5, 0));
                                Viewport.Create(doc, viewSheet.Id, viewShaded3d.Id, new XYZ(0.3, 0.7, 0));
                                Viewport.Create(doc, viewSheet.Id, detailSectionA.Id, new XYZ(0.3, 0.5, 0));
                                Viewport.Create(doc, viewSheet.Id, detailSectionB.Id, new XYZ(0.3, 0.42, 0));
                                Viewport.Create(doc, viewSheet.Id, detailPlan.Id, new XYZ(0.5, 0.7, 0));



                                TaskDialog sucessStatus = new TaskDialog("Status");
                                sucessStatus.MainInstruction = "Assembly and views were placed on sheet successfully.";
                                sucessStatus.Show();



                                transactionB.Commit();
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog errorStatus = new TaskDialog("Error");
                    errorStatus.MainInstruction = ex.Message;
                    errorStatus.Show();
                }

            }
            return Result.Succeeded;
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateCatalogPage";
            string buttonTitle = "Create\nCatalog Page";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This will create a catalog page and more...");

            return myButtonData1.Data;
        }

    }
}
