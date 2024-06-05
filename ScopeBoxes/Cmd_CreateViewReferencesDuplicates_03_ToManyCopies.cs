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
    public class Cmd_CreateViewReferencesDuplicates_03_ToManyCopies : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            var viewReference = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ReferenceViewer).WhereElementIsNotElementType().Cast<Element>().FirstOrDefault();

            List<Element> selectedScopBoxes = Cmd_RenameScopeBoxes.GetSelectedScopeBoxes(doc);
            List<BoundingBoxXYZ> scopeBoxBounds = Cmd_CreateMatchlineReference.GetSelectedScopeBoxBounds(selectedScopBoxes);

            var orderScopeBoxes = selectedScopBoxes.OrderBy(box => box.get_BoundingBox(null).Min.X)
                                                   .ThenByDescending(box => box.get_BoundingBox(null).Min.Y)
                                                   .ToList();

            // The original location of the element (for example, the center of its bounding box)
            BoundingBoxXYZ boundingBox = viewReference.get_BoundingBox(doc.ActiveView);
            XYZ originalLocation = (boundingBox.Min + boundingBox.Max) / 2;


            List<ElementId> viewReferenceCopies = new List<ElementId>();
            try
            {
                using (Transaction trans = new Transaction(doc, "Place View Refereces"))
                {
                    trans.Start();

                    foreach (Element scopeBox in orderScopeBoxes)
                    {
                        List<XYZ> insertPoints = GetBoxForInsertPoints(scopeBox); // Corner points for insertion
                        List<Element> otherScopeBoxes = selectedScopBoxes.Where(sb => sb.Id != scopeBox.Id).ToList(); // Other scope boxes for checking overlap
                        BoundingBoxXYZ currentBox = scopeBox.get_BoundingBox(null); // Current scope box bounding box for reference

                        foreach (XYZ insertPoint in insertPoints)
                        {
                            List<string> overlapDirections = CheckOverlapsForCorner(insertPoint, otherScopeBoxes, currentBox);

                            // Only proceed if there is an overlap
                            if (overlapDirections.Count > 0)
                            {
                                // Insert a copy for each direction of overlap detected
                                foreach (var direction in overlapDirections)
                                {
                                    var copiedElem = ElementTransformUtils.CopyElement(doc, viewReference.Id, insertPoint - originalLocation);
                                    ElementId copiedElementId = copiedElem.First();

                                    // Rotate the element for a vertical copy, if needed
                                    if (direction == "Vertical")
                                    {
                                        RotateElementFromCenter(doc, copiedElementId);
                                    }

                                    // Add the copy to the list
                                    viewReferenceCopies.Add(copiedElementId);
                                }
                            }
                        }
                    }

                    trans.Commit();
                }
                TaskDialog.Show("Info", $"{viewReferenceCopies.Count} View Reference(s) placed");

            }
            catch (Exception e)
            {
                TaskDialog.Show("Error", $"{e.Message}");
            }
            // Return the result indicating success
            return Result.Succeeded;
        }



        private List<string> CheckOverlapsForCorner(XYZ cornerPoint, List<Element> allScopeBoxes, BoundingBoxXYZ currentBox)
        {
            List<string> overlaps = new List<string>();

            foreach (Element scopeBox in allScopeBoxes)
            {
                BoundingBoxXYZ bbox = scopeBox.get_BoundingBox(null);
                if (cornerPoint.X > bbox.Min.X && cornerPoint.X < bbox.Max.X &&
                    cornerPoint.Y > bbox.Min.Y && cornerPoint.Y < bbox.Max.Y)
                {
                    // The corner is inside this scope box; now determine the specific overlaps
                    if (cornerPoint.Y >= currentBox.Min.Y && cornerPoint.Y <= currentBox.Max.Y)
                    {
                        overlaps.Add("Horizontal");
                    }
                    if (cornerPoint.X >= currentBox.Min.X && cornerPoint.X <= currentBox.Max.X)
                    {
                        overlaps.Add("Vertical");
                    }
                }
            }

            return overlaps;
        }


        private static List<XYZ> GetBoxForInsertPoints(Element scopeBox)
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
            return insertPoints;
        }
        private static List<XYZ> GetBoxSidesCentertPoints(Element scopeBox)
        {
            GetLeftRightTopBottomCenters scopeboxFourCorners = new GetLeftRightTopBottomCenters(scopeBox.get_BoundingBox(null));

            // Convert the corners into a list to iterate over
            List<XYZ> insertPoints = new List<XYZ>
                                                    {
                                                        scopeboxFourCorners.LeftCenter,
                                                        scopeboxFourCorners.RightCenter,
                                                        scopeboxFourCorners.TopCenter,
                                                        scopeboxFourCorners.BottomCenter
                                                    };
            return insertPoints;
        }


        /// <summary>
        /// Rotates elements with bounding box 90 degrees CounterClockWise
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="elementId"></param>
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
    //public class ExpandedBoundingBox
    //{
    //    public XYZ LeftTop { get; private set; }
    //    public XYZ RightTop { get; private set; }
    //    public XYZ LeftBottom { get; private set; }
    //    public XYZ RightBottom { get; private set; }

    //    public ExpandedBoundingBox(BoundingBoxXYZ boundingBox)
    //    {
    //        // Calculate the adjested corners
    //        CalculateAdjestedCorners(boundingBox);
    //    }
    //    private void CalculateAdjestedCorners(BoundingBoxXYZ boundingBox)
    //    {
    //        // Assuming Z is constant and we're expanding in X and Y directions
    //        double expandDistance = 1; // 1 foot in Revit's internal units

    //        // Original Min and Max points
    //        XYZ min = boundingBox.Min;
    //        XYZ max = boundingBox.Max;

    //        // Inwards Offset BoundingBox Corner Points 
    //        LeftBottom = new XYZ(min.X + expandDistance, min.Y + expandDistance, min.Z);
    //        RightBottom = new XYZ(max.X - expandDistance, min.Y + expandDistance, min.Z);
    //        LeftTop = new XYZ(min.X + expandDistance, max.Y - expandDistance, max.Z);
    //        RightTop = new XYZ(max.X - expandDistance, max.Y - expandDistance, max.Z);
    //    }
    //}

    //public class GetLeftRightTopBottomCenters
    //{
    //    public XYZ LeftCenter { get; private set; }
    //    public XYZ RightCenter { get; private set; }
    //    public XYZ TopCenter { get; private set; }
    //    public XYZ BottomCenter { get; private set; }

    //    public GetLeftRightTopBottomCenters(BoundingBoxXYZ boundingBox)
    //    {
    //        // Calculate the expanded corners
    //        GetBoxCenterPoints(boundingBox);
    //    }
    //    private void GetBoxCenterPoints(BoundingBoxXYZ boundingBox)
    //    {
    //        XYZ min = boundingBox.Min;
    //        XYZ max = boundingBox.Max;

    //        // Center point of the left side
    //        LeftCenter = new XYZ(min.X, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

    //        // Center point of the right side
    //        RightCenter = new XYZ(max.X, (min.Y + max.Y) / 2.0, (min.Z + max.Z) / 2.0);

    //        // Center point of the top side
    //        TopCenter = new XYZ((min.X + max.X) / 2.0, max.Y, (min.Z + max.Z) / 2.0);

    //        // Center point of the bottom side
    //        BottomCenter = new XYZ((min.X + max.X) / 2.0, min.Y, (min.Z + max.Z) / 2.0);
    //    }
    //}

}
