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
    public class Cmd_CreateViewReferencesDuplicates : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            var viewReference = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_ReferenceViewer)
                .WhereElementIsNotElementType()
                .Cast<Element>()
                .FirstOrDefault();


            List<Element> selectedScopBoxes = Cmd_RenameScopeBoxes.GetSelectedScopeBoxes(doc);
            List<BoundingBoxXYZ> scopeBoxBounds = Cmd_CreateMatchlineReference.GetSelectedScopeBoxBounds(selectedScopBoxes);

            var orderScopeBoxes = selectedScopBoxes
                                    .OrderBy(box => box.get_BoundingBox(null).Min.X)
                                    .ThenByDescending(box => box.get_BoundingBox(null).Min.Y)
                                    .ToList();

            // The original location of the element (for example, the center of its bounding box)
            BoundingBoxXYZ boundingBox = viewReference.get_BoundingBox(doc.ActiveView);
            XYZ originalLocation = (boundingBox.Min + boundingBox.Max) / 2;


            List<ElementId> viewReferenceCopies = new List<ElementId>();
            try
            {
                using (Transaction trans = new Transaction(doc, "Copy elem"))
                {
                    trans.Start();
                    RotateElementFromCenter(doc, viewReference.Id);




                    foreach (Element scopeBox in orderScopeBoxes)
                    {


                        ExpandedBoundingBox scopeboxFourCorners = new ExpandedBoundingBox(scopeBox.get_BoundingBox(null));

                        // Convert the corners into a list to iterate over
                        List<XYZ> insertPoints = new List<XYZ>
                                                    {
                                                        scopeboxFourCorners.LeftTop,
                                                        scopeboxFourCorners.RightTop,
                                                        scopeboxFourCorners.LeftBottom,
                                                        scopeboxFourCorners.RightBottom
                                                    };

                        foreach (XYZ insertPoint in insertPoints)
                        {
                            // Calculate the translation vector needed to move the original location to the new location
                            XYZ translatedVectorPoint = insertPoint - originalLocation;

                            //if (Cmd_CreateMatchlineReference.StartPointIsInOverlapArea(translatedVectorPoint, scopeBoxBounds))
                            //{
                            var copiedElem = ElementTransformUtils.CopyElement(doc, viewReference.Id, translatedVectorPoint);

                            viewReferenceCopies.Add(copiedElem.First());
                            //}

                        }

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
        public void RotateElementFromCenter(Document doc, ElementId elementId)
        {
            Element element = doc.GetElement(elementId);
            if (element == null) return;

            // Assuming the element has a bounding box, calculate its center
            //BoundingBoxXYZ bbox = element.get_BoundingBox(null); // null for the active view
            BoundingBoxXYZ bbox = element.get_BoundingBox(doc.ActiveView); // null for the active view
            if (bbox == null) return;

            XYZ bboxCenter = (bbox.Min + bbox.Max) * 0.5;
            Line rotationAxis = Line.CreateBound(bboxCenter, bboxCenter + XYZ.BasisZ);

            double angleRadians = Math.PI / 2; // 90 degrees counterclockwise

            if (element.Location is Location location)
            {
                bool rotated = location.Rotate(rotationAxis, angleRadians);

                if (!rotated)
                {
                    TaskDialog.Show("Info", "Rotation failed.");
                }
            }
        }

        public void RotateElement90Degrees(Document doc, Element element)
        {
            if (element.Location == null)
                TaskDialog.Show("Info", "Element location is null");

            // Check if the element has a Location property that can be rotated
            if (element.Location is LocationPoint locationPoint)
            {
                // Define the axis of rotation (here, global Z-axis at the element's location)
                XYZ point1 = locationPoint.Point;
                XYZ point2 = point1 + XYZ.BasisZ; // Adding Z basis vector to create a vertical line

                Line rotationAxis = Line.CreateBound(point1, point2);

                // Convert 90 degrees to radians
                double angleRadians = 90 * (Math.PI / 180);

                // Start a new transaction to apply changes in the document
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Rotate Element 90 Degrees CCW");

                    // Rotate the element counterclockwise by 90 degrees
                    locationPoint.Rotate(rotationAxis, angleRadians);

                    tx.Commit(); // Commit the changes
                }
            }
        }
        public void RotateElement90Degrees2(Document doc, ElementId elementId)
        {
            // Retrieve the element from its ID
            Element element = doc.GetElement(elementId);

            // Check if the element has a Location property that can be rotated
            if (element.Location is LocationPoint locationPoint)
            {
                // Define the axis of rotation (here, global Z-axis at the element's location)
                XYZ point1 = locationPoint.Point;
                XYZ point2 = point1 + XYZ.BasisZ; // Adding Z basis vector to create a vertical line

                Line rotationAxis = Line.CreateBound(point1, point2);

                // Convert 90 degrees to radians
                double angleRadians = 90 * (Math.PI / 180);

                // Start a new transaction to apply changes in the document
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("Rotate Element 90 Degrees CCW");

                    // Rotate the element counterclockwise by 90 degrees
                    locationPoint.Rotate(rotationAxis, angleRadians);

                    tx.Commit(); // Commit the changes
                }
            }
        }
        private static void RotateElement90DegCCWise(Document doc, ICollection<ElementId> copiedElements)
        {
            // Ensure there's at least one element in the collection
            if (copiedElements == null || copiedElements.Count == 0)
            {
                TaskDialog.Show("Error", "No elements to rotate.");
                return;
            }

            // Get the first copied element ID
            ElementId copiedElementId = copiedElements.First();


            // Get the element to rotate
            Element element = doc.GetElement(copiedElementId);
            if (element == null)
            {
                TaskDialog.Show("Error", "Element not found.");
                return;
            }

            Location location = element.Location;
            if (!(location is LocationPoint locationPoint))
            {
                TaskDialog.Show("Error", "Element cannot be rotated.");
                return;
            }

            // Define the rotation axis (vertical axis through the element's location point)
            XYZ point = locationPoint.Point;
            XYZ axis = new XYZ(0, 0, 1); // Z-axis for vertical rotation

            // Create the rotation axis
            Line rotationAxis = Line.CreateBound(point, point + axis);

            // Rotate the element 90 degrees counterclockwise (in radians)
            double angle = Math.PI / 2; // 90 degrees in radians

            // Perform the rotation
            ElementTransformUtils.RotateElement(doc, copiedElementId, rotationAxis, angle);
        }

        private static void RotateElement90DegCCWise3(Document doc, ICollection<ElementId> elementIds)
        {
            ElementId elementId = elementIds.First(); // Assuming there's at least one element.
            Element element = doc.GetElement(elementId);
            Location location = element.Location;

            // Check if the location is a point.
            if (location is LocationPoint locationPoint)
            {
                using (Transaction trans = new Transaction(doc, "Rotate Element"))
                {
                    trans.Start();

                    XYZ rotationPoint = locationPoint.Point; // The point around which to rotate.
                    XYZ axis = new XYZ(rotationPoint.X, rotationPoint.Y, rotationPoint.Z + 10); // Axis of rotation.

                    // Creating a line that represents the axis of rotation.
                    Line rotationAxis = Line.CreateBound(rotationPoint, axis);

                    // Rotate the element 90 degrees counterclockwise (in radians).
                    double angle = Math.PI / 2; // 90 degrees in radians.

                    ElementTransformUtils.RotateElement(doc, elementId, rotationAxis, angle);

                    trans.Commit();
                }
            }
            else
            {
                TaskDialog.Show("Error", "The element does not have a LocationPoint and cannot be rotated this way.");
            }
        }

        private static void RotateElement90DegCCWise2(Document doc, ICollection<ElementId> _element)
        {
            // Assuming '_element' is the collection returned from ElementTransformUtils.CopyElement
            // and you've already checked that it contains at least one element.
            ElementId copiedElementId = _element.First();


            // Get the copied element
            Element copiedElement = doc.GetElement(copiedElementId);

            // Try to cast the Location of the copied element to a LocationCurve
            LocationCurve curve = copiedElement.Location as LocationCurve;
            if (curve != null)
            {
                // Define the rotation axis
                Curve line = curve.Curve;
                XYZ startPoint = line.GetEndPoint(0); // Start point of the curve
                XYZ endPoint = new XYZ(startPoint.X, startPoint.Y, startPoint.Z + 10); // End point of the axis, 10 units up in Z direction

                // Create a line that represents the axis of rotation
                Line axis = Line.CreateBound(startPoint, endPoint);

                // Rotate the element 90 degrees counterclockwise (in radians)
                double angle = Math.PI / 2; // 90 degrees in radians
                bool rotated = curve.Rotate(axis, angle);

                // Check if the rotation was successful
                if (rotated)
                {

                    TaskDialog.Show("Rotation", "Element rotated successfully.");
                }
                else
                {

                    TaskDialog.Show("Rotation", "Rotation failed.");
                }
            }
            else
            {
                // If the copied element doesn't have a LocationCurve, it cannot be rotated in this way.
                TaskDialog.Show("Error", "The copied element does not have a LocationCurve and cannot be rotated.");

            }


        }
        private static void RotateElement(Document doc, ElementId elementId, XYZ point, XYZ axis, double angle)
        {
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Rotate Element");

                // Create the rotation axis
                Line rotationAxis = Line.CreateBound(point, point + axis);

                // Rotate the element
                ElementTransformUtils.RotateElement(doc, elementId, rotationAxis, angle);

                tx.Commit();
            }
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
    public class ExpandedBoundingBox
    {
        public XYZ LeftTop { get; private set; }
        public XYZ RightTop { get; private set; }
        public XYZ LeftBottom { get; private set; }
        public XYZ RightBottom { get; private set; }

        public ExpandedBoundingBox(BoundingBoxXYZ boundingBox)
        {
            // Calculate the expanded corners
            CalculateExpandedCorners(boundingBox);
        }
        private void CalculateExpandedCorners(BoundingBoxXYZ boundingBox)
        {
            // Assuming Z is constant and we're expanding in X and Y directions
            double expandDistance = 1; // 1 foot in Revit's internal units

            // Original corners
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            // Expanded corners
            LeftBottom = new XYZ(min.X + expandDistance, min.Y + expandDistance, min.Z);
            RightBottom = new XYZ(max.X - expandDistance, min.Y + expandDistance, min.Z);
            LeftTop = new XYZ(min.X + expandDistance, max.Y - expandDistance, max.Z);
            RightTop = new XYZ(max.X - expandDistance, max.Y - expandDistance, max.Z);
        }

    }

}
