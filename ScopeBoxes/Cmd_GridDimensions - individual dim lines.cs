#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using ScopeBoxes.Forms;
#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_GridDimensions2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get the current view grid
            List<Element> curViewGrids = GetViewGrids(doc, doc.ActiveView);

            // Dimension the grids
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create grid dimension");
                DimensionGrids(doc, curViewGrids);
                t.Commit();
            }

            return Result.Succeeded;
        }

        List<Element> GetViewGrids(Document doc, View view)
        {
            var grids = new FilteredElementCollector(doc, view.Id)
                            .OfCategory(BuiltInCategory.OST_Grids)
                            .WhereElementIsNotElementType()
                            .ToList();
            return grids;
        }

        void DimensionGrids(Document doc, List<Element> grids)
        {
            // Assume horizontal dimensions for simplicity
            List<ElementId> dimensionIds = new List<ElementId>();

            // Order grids by Y-coordinate (top to bottom)
            List<Autodesk.Revit.DB.Grid> orderedGrids = grids.Cast<Autodesk.Revit.DB.Grid>()
                                                            .OrderBy(grid => (grid.Curve as Line).GetEndPoint(0).Y)
                                                            .ToList();

            // Dimension horizontally
            for (int i = 0; i < orderedGrids.Count - 1; i++)
            {
                var grid1 = orderedGrids[i];
                var grid2 = orderedGrids[i + 1];

                // Create references using the grid ends
                ReferenceArray referenceArray = new ReferenceArray();
                referenceArray.Append(new Reference(grid1));
                referenceArray.Append(new Reference(grid2));

                // Create dimension line
                Line dimensionLine = Line.CreateBound(grid1.Curve.GetEndPoint(0), grid2.Curve.GetEndPoint(0));

                //// Create dimension
                //using (Transaction t = new Transaction(doc))
                //{
                //    t.Start("Create grid dimension");
                Dimension dim = doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray);
                dimensionIds.Add(dim.Id);
                //    t.Commit();
                //}
            }

            //// Assign dimensions to a specific dimension type if needed
            //AssignDimensionType(doc, dimensionIds);
        }

        void AssignDimensionType(Document doc, List<ElementId> dimensionIds)
        {
            // Assume there is a dimension type named "Standard" in the project
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            DimensionType dimensionType = collector.OfClass(typeof(DimensionType))
                                                .Cast<DimensionType>()
                                                .FirstOrDefault(q => q.Name == "Standard");

            if (dimensionType != null)
            {
                //using (Transaction t = new Transaction(doc, "Assign Dimension Type"))
                //{
                //    t.Start();
                foreach (ElementId dimId in dimensionIds)
                {
                    Dimension dim = doc.GetElement(dimId) as Dimension;
                    dim.DimensionType = dimensionType;
                }
                //    t.Commit();
                //}
            }
        }

        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnGridDimensions";
            string buttonTitle = "Grid Dimensions";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}