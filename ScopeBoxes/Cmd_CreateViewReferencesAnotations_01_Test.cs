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

using ScopeBoxes.Forms;

#endregion

namespace ScopeBoxes
{
    [Transaction(TransactionMode.Manual)]
    public class Cmd_CreateViewReferencesAnotations_01_Test : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            //public static void CreateReferenceSection(
            //                                                Document document,
            //                                                ElementId parentViewId,
            //                                                ElementId viewIdToReference,
            //                                                XYZ headPoint,
            //                                                XYZ tailPoint
            //                                         )

            //ElementId parentViewId = doc.ActiveView.Id;
            //ElementId viewIdToReference = Cmd_DependentViewsBrowserTree.GetOnlyDependentViews(doc).First().Id;
            //XYZ headPoint = new XYZ(0,0,0);
            //XYZ tailPoint = new XYZ(1,0,0);


            //var selectedElem = refElement(doc, uiapp);
            //Element selectedElement = doc.GetElement(selectedElem.Id); // Assuming selectedElementId is the ID of the element
            //                                                           //XYZ elementLocation = null;

            //doc.Application.Create.NewPointOnPlane(plane, UV.Zero, UV.BasisU, 0.0);

            //var elemXYZ_test1 = ElemXYZ_1(selectedElement);
            //var elemXYZ_test2= ElementXYZ_2(selectedElement);


            //ViewFamilyTypes
            //var viewFT = new FilteredElementCollector(doc)
            //                 .OfClass(typeof(ViewFamilyType))
            //                 .Cast<ViewFamilyType>()
            //                 .FirstOrDefault(vt => vt.ViewFamily == ViewFamily.FloorPlan) as ViewFamilyType;


            var allFamilySymbols = new FilteredElementCollector(doc)
                                .OfClass(typeof(FamilySymbol))
                                .WhereElementIsElementType()
                                .ToList();
            var allFamilies = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .ToList();


            ElementId parentViewId = doc.ActiveView.Id;
            List<View> dependentViews = Cmd_DependentViewsBrowserTree.GetOnlyDependentViews(doc);




            try
            {
                using (Transaction trans = new Transaction(doc, "Added view reference annotations"))
                {
                    trans.Start();

                    // Assuming refElement, uiapp, and Cmd_DependentViewsBrowserTree.GetOnlyDependentViews are defined elsewhere
                    // and provide appropriate values for selectedElem, parentViewId, and viewIdToReference.
                    if (dependentViews.Count > 0)
                    {
                        ElementId viewIdToReference = dependentViews.First().Id;
                        XYZ headPoint = new XYZ(0, -60, 0); // Example start point
                        XYZ tailPoint = new XYZ(10, -100, 0); // Example end point, 10 units in the X direction from the start point

                        // Create the reference section. Make sure to replace 'ViewSection.CreateReferenceSection' with the correct method call
                        // if you are using a specific utility method or class for this operation.
                        //Autodesk.Revit.DB.ViewSection.CreateReferenceSection(doc, parentViewId, viewIdToReference, headPoint, tailPoint);
                        Autodesk.Revit.DB.ViewSection.CreateReferenceCallout(doc, parentViewId, viewIdToReference, headPoint, tailPoint);

                        //Autodesk.Revit.DB.ViewSection.CreateCallout(doc, parentViewId, viewIdToReference, headPoint, tailPoint); //Not working

                        //CreateViewSection(doc, dependentViews.First().get_BoundingBox(null)); // Not Working
                        //CreateAnnotationReferenceLink(doc, GetAllMatchlines(doc).First().Id, dependentViews.First().Name); // Not working
                    }

                    trans.Commit();
                }
                //TaskDialog.Show("Info", $"Results");

            }
            catch (Exception e)
            {

                TaskDialog.Show("Error", $"{e.Message}");

            }
            // Return the result indicating success
            return Result.Succeeded;
        }
        public static List<Element> GetAllMatchlines(Document doc)
        {
            // Create a new filtered element collector in the given document
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            // Apply a category filter for OST_Matchline
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Matchline);

            // Apply the filter to the collector and return the results
            List<Element> matchlines = collector.WherePasses(filter).ToElements().ToList();

            return matchlines;
        }
        private static void CreateAnnotationReferenceLink(Document doc, ElementId matchlineId, string targetViewName)
        {
            // Retrieve the matchline element as a CurveElement
            CurveElement matchlineElement = doc.GetElement(matchlineId) as CurveElement;
            if (matchlineElement == null) throw new InvalidOperationException("Matchline not found.");

            // Get the curve from the CurveElement, which could be a Line, Arc, etc.
            Curve matchlineCurve = matchlineElement.GeometryCurve;
            if (matchlineCurve == null) throw new InvalidOperationException("Matchline curve not found.");

            // Determine placement location for the annotation (e.g., end of the matchline)
            XYZ placementLocation = matchlineCurve.GetEndPoint(0); // Or calculate specific placement

            // Define text note options (customize as needed)
            TextNoteOptions opts = new TextNoteOptions()
            {
                VerticalAlignment = VerticalTextAlignment.Middle,
                HorizontalAlignment = HorizontalTextAlignment.Left
            };

            // Ensure a transaction is started before creating the text note

            // Create the text note next to the matchline
            TextNote note = TextNote.Create(doc, doc.ActiveView.Id, placementLocation, $"See View: {targetViewName}", opts);


        }


        private static void CreateViewSection(Document doc, BoundingBoxXYZ sectionBox)
        {
            //ViewSection
            var viewFamilySection = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .Where(vft => vft.ViewFamily == ViewFamily.Section)
                                .ToList()
                                .First();
            Autodesk.Revit.DB.ViewSection.CreateSection(doc, viewFamilySection.Id, sectionBox);
        }

        public static XYZ ElementXYZ_2(Element selectedElement)
        {
            XYZ elementLocation = null;
            // Check if the element's location is a point
            if (selectedElement.Location is LocationPoint locationPoint)
            {
                elementLocation = locationPoint.Point;
            }
            // Alternatively, for elements with a curve location, you might want to get the start point, end point, or midpoint of the curve
            else if (selectedElement.Location is LocationCurve locationCurve)
            {
                Curve curve = locationCurve.Curve;
                elementLocation = curve.GetEndPoint(0); // Or use curve.Evaluate(0.5, true) for the midpoint
            }
            return elementLocation;
        }

        public static XYZ ElemXYZ_1(Element selectedElement)
        {
            XYZ elementLocation = null;
            var geometryElement = selectedElement.get_Geometry(new Options());
            foreach (GeometryObject geomObj in geometryElement)
            {
                if (geomObj is GeometryInstance)
                {
                    GeometryInstance instance = geomObj as GeometryInstance;
                    var instanceGeometry = instance.GetInstanceGeometry();
                    foreach (var obj in instanceGeometry)
                    {
                        if (obj is Solid solid)
                        {
                            // You can try to get the center of the solid or any specific point you're interested in
                            elementLocation = solid.ComputeCentroid();
                            break;
                        }
                    }
                }
                if (elementLocation != null)
                    break;
            }
            return elementLocation;
        }

        private Element refElement(Document doc, UIApplication uiapp)
        {
            // Get the current selection
            Selection selection = uiapp.ActiveUIDocument.Selection;

            // Retrieve the selected elements
            ICollection<ElementId> selectedElementIds = selection.GetElementIds();

            var selectedElement = doc.GetElement(selectedElementIds.First());

            return selectedElement;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btn_CreateViewReferencesAnotations";
            string buttonTitle = "ViewReference \nAnotations";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "Cmd_CreateViewReferencesAnotations will be used to create view reverence anotaions.");

            return myButtonData1.Data;
        }
    }
}

////To develop an add-in that places view reference annotations and assigns them to views, follow these pseudocode steps inside the transaction:

////1. * *Get Parent Views**:
////   -Collect all views in the document that could have dependent views (e.g., floor plans, sections).
////   - Use `FilteredElementCollector` and filter by `ViewType`.

////2. **Identify Dependent Views**:
////   -For each parent view, use `GetDependentViewIds()` to find its dependent views.

////3. **Collect Matchlines**:
////   -For each parent view, collect matchlines using `FilteredElementCollector` filtered by `BuiltInCategory.OST_Matchline`.

////4. **Determine Insertion Points**:
////   -Calculate insertion points for view reference annotations, possibly at matchline intersections or predefined points relative to view boundaries.

////5. **Create View Reference Annotations**:
////   -For each insertion point, create a new annotation element(e.g., a text note or a symbol) that references the view.
////   - Use `Document.Create.NewTextNote` or similar methods depending on the type of annotation.

////6. **Assign References to Views**:
////   -Set properties or parameters on the annotation elements to link them to the specific views they reference.
////   - This might involve setting a parameter to store the view name or ID.

////7. **Adjust Annotation Appearance**:
////   -Customize the appearance of annotations as needed (e.g., text size, color) using their properties.

////Remember to handle exceptions and ensure all database modifications are within a transaction. This pseudocode outlines the logic and Revit API calls you might use, and you'll need to adapt it to fit your specific requirements and the Revit API's capabilities.