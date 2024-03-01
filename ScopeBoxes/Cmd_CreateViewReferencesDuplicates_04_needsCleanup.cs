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
    public class Cmd_CreateViewReferencesDuplicates_04 : IExternalCommand
    {
        public int ViewReferenceCopiesCount { get; set; } = 0;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            var curView = doc.ActiveView;

            // Retrieve the first View Reference in the document as a template
            Element viewReference = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_ReferenceViewer)
                .WhereElementIsNotElementType()
                .FirstOrDefault();

            if (viewReference == null)
            {
                TaskDialog.Show("Error", "No View Reference found in the document.");
                return Result.Failed;
            }

            // Get all selected Scope Boxes in the document
            List<Element> selectedScopeBoxes = Cmd_RenameScopeBoxes.GetSelectedScopeBoxes(doc);

            if (selectedScopeBoxes.Count < 2)
            {
                TaskDialog.Show("Error", "You must select at least 2 overlapped Scope Boxes.");
                return Result.Failed;
            }

            try
            {
                using (Transaction trans = new Transaction(doc, "Place View References"))
                {
                    trans.Start();

                    foreach (Element scopeBox in selectedScopeBoxes)
                    {
                        List<XYZ> insertPoints = GetBoxForInsertPoints(scopeBox);
                        List<Element> otherScopeBoxes = selectedScopeBoxes.Except(new[] { scopeBox }).ToList();

                        foreach (XYZ insertPoint in insertPoints)
                        {
                            List<string> overlapDirections = CheckOverlapsForCorner(insertPoint, otherScopeBoxes, scopeBox.get_BoundingBox(null));

                            if (overlapDirections.Contains("Horizontal"))
                            {
                                bool shouldInsertHorizontal = CheckVerticalBoxOverlaps(insertPoint, scopeBox, otherScopeBoxes, curView);
                                if (shouldInsertHorizontal)
                                {
                                    //Insert horizontal View Reference
                                    InsertViewReference(doc, viewReference.Id, insertPoint - GetElementCenter(doc, viewReference));

                                }
                            }

                            if (overlapDirections.Contains("Vertical"))
                            {
                                bool shouldInsertHorizontal = CheckHorizontalBoxOverlaps(insertPoint, scopeBox, otherScopeBoxes, curView);
                                if (shouldInsertHorizontal)
                                {
                                    // Insert vertical View Reference and rotate it
                                    ElementId newVerticalViewRefId = InsertViewReference(doc, viewReference.Id, insertPoint - GetElementCenter(doc, viewReference));
                                    RotateElementFromCenter(doc, newVerticalViewRefId);
                                }
                            }
                        }
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }

            TaskDialog.Show("Success", $"{ViewReferenceCopiesCount} View References placed successfully.");
            return Result.Succeeded;
        }
        private bool CheckHorizontalBoxOverlaps(XYZ insertPoint, Element scopeBox, List<Element> otherScopeBoxes, View view)
        {
            BoundingBoxXYZ scopeBoxBBox = scopeBox.get_BoundingBox(view);
            bool isPointInsideScopeBox = IsPointInsideBox(insertPoint, scopeBoxBBox);

            if (!isPointInsideScopeBox)
            {
                return false;  // The point isn't even inside the primary scope box.
            }

            // Check against all other scope boxes to find a horizontal overlap
            foreach (Element otherBox in otherScopeBoxes)
            {
                BoundingBoxXYZ otherBoxBBox = otherBox.get_BoundingBox(view);

                // Check if the insertPoint is inside otherBox in the X direction
                if (IsPointInsideBox(insertPoint, otherBoxBBox))
                {
                    // Check for horizontal overlap: They overlap horizontally if their Y ranges intersect
                    if (DoBoxesOverlapHorizontally(scopeBoxBBox, otherBoxBBox))
                    {
                        return true;  // Found a horizontal overlap
                    }
                }
            }

            return false;  // No horizontal overlaps found
        }

        private bool CheckVerticalBoxOverlaps(XYZ insertPoint, Element scopeBox, List<Element> otherScopeBoxes, View view)
        {
            BoundingBoxXYZ scopeBoxBBox = scopeBox.get_BoundingBox(view);
            bool isPointInsideScopeBox = IsPointInsideBox(insertPoint, scopeBoxBBox);

            if (!isPointInsideScopeBox)
            {
                return false;  // The point isn't even inside the primary scope box.
            }

            // Check against all other scope boxes to find a horizontal overlap
            foreach (Element otherBox in otherScopeBoxes)
            {
                BoundingBoxXYZ otherBoxBBox = otherBox.get_BoundingBox(view);

                // Check if the insertPoint is inside otherBox in the X direction
                if (IsPointInsideBox(insertPoint, otherBoxBBox))
                {
                    // Check for horizontal overlap: They overlap horizontally if their Y ranges intersect
                    if (DoBoxesOverlapVertically(scopeBoxBBox, otherBoxBBox))
                    {
                        return true;  // Found a horizontal overlap
                    }
                }
            }

            return false;  // No horizontal overlaps found
        }

        private bool IsPointInsideBox(XYZ point, BoundingBoxXYZ bbox)
        {
            return point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                   point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y;// &&
                                                                  //point.Z >= bbox.Min.Z && point.Z <= bbox.Max.Z;
        }
        private bool DoBoxesOverlapHorizontally(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            XYZ box1Center = (box1.Min + box1.Max) / 2;
            XYZ box2Center = (box2.Min + box2.Max) / 2;

            // if box1 and box2 are both in the same horizontal line, they will both have the same Y center point
            return box1Center.Y == box2Center.Y;
        }
        private bool DoBoxesOverlapVertically(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            XYZ box1Center = (box1.Min + box1.Max) / 2;
            XYZ box2Center = (box2.Min + box2.Max) / 2;

            // if box1 and box2 are both in the same horizontal line, they will both have the same Y center point
            return box1Center.X == box2Center.X;
        }
        private bool DoBoxesOverlapHorizontally3(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            var B1MinY = box1.Min.Y;
            var B1MaxY = box1.Max.Y;
            var B2MinY = box2.Min.Y;
            var B2MaxY = box2.Max.Y;

            // Overlap horizontally if one box's Y range intersects the other's Y range
            return (B1MinY <= B2MaxY && B1MaxY >= B2MinY) ||
                   (B2MinY <= B1MaxY && B2MaxY >= B1MinY);
        }
        private bool DoBoxesOverlapHorizontally2(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            // Overlap horizontally if one box's Y range intersects the other's Y range
            return (box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y) ||
                   (box2.Min.Y <= box1.Max.Y && box2.Max.Y >= box1.Min.Y);
        }




        private void InsertViewReferences(Document doc, List<Element> scopeBoxes, Element viewReference)
        {
            XYZ originalLocation = GetElementCenter(doc, viewReference);
            List<ElementId> insertedViewRefs = new List<ElementId>();

            foreach (var scopeBox in scopeBoxes)
            {
                var insertPoints = GetBoxForInsertPoints(scopeBox);
                var otherScopeBoxes = scopeBoxes.Except(new[] { scopeBox }).ToList();
                var currentBox = scopeBox.get_BoundingBox(null);

                foreach (var point in insertPoints)
                {
                    var overlaps = FindOverlappingScopeBoxes(point, otherScopeBoxes);
                    var horizontalOverlap = overlaps.Any(sb => IsHorizontalOverlap(point, sb));
                    var verticalOverlap = overlaps.Any(sb => IsVerticalOverlap(point, sb));

                    if (horizontalOverlap || verticalOverlap)
                    {
                        XYZ translation = point - originalLocation;

                        if (horizontalOverlap)
                        {
                            // Insert horizontal View Reference
                            var horizontalRefId = InsertViewReference(doc, viewReference.Id, translation);
                            insertedViewRefs.Add(horizontalRefId);
                        }

                        if (verticalOverlap)
                        {
                            // Insert vertical View Reference and rotate
                            var verticalRefId = InsertViewReference(doc, viewReference.Id, translation);
                            RotateElementFromCenter(doc, verticalRefId);
                            insertedViewRefs.Add(verticalRefId);
                        }
                    }
                }
            }

            // Process to handle excess view references at any point, if necessary
            RemoveExcessViewReferences(doc, insertedViewRefs);
        }
        private XYZ GetElementCenter(Document doc, Element element)
        {
            // Get the bounding box of the element in the active view
            BoundingBoxXYZ bbox = element.get_BoundingBox(doc.ActiveView);

            // Check if the bounding box is valid
            if (bbox != null)
            {
                // Calculate the center point of the bounding box
                XYZ center = (bbox.Min + bbox.Max) * 0.5;
                return center;
            }
            else
            {
                throw new InvalidOperationException("Cannot find bounding box for the given element.");
            }
        }
        private List<Element> FindOverlappingScopeBoxes(XYZ point, List<Element> scopeBoxes)
        {
            List<Element> overlappingBoxes = new List<Element>();

            foreach (Element scopeBox in scopeBoxes)
            {
                BoundingBoxXYZ bbox = scopeBox.get_BoundingBox(null); // null for the active view
                if (bbox != null)
                {
                    // Check if the point is within the bounding box in the XY plane
                    bool isInside = point.X >= bbox.Min.X && point.X <= bbox.Max.X
                                    && point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y;

                    if (isInside)
                    {
                        overlappingBoxes.Add(scopeBox);
                    }
                }
            }

            return overlappingBoxes;
        }
        private bool IsHorizontalOverlap(XYZ point, Element scopeBox)
        {
            // Retrieve the bounding box of the scope box in the active view
            BoundingBoxXYZ bbox = scopeBox.get_BoundingBox(null);

            // Check if the point is horizontally aligned with the scope box
            // This means the point's X coordinate is within the scope box's X range
            bool isHorizontallyAligned = point.X >= bbox.Min.X && point.X <= bbox.Max.X;

            // Now check if the point is above or below the scope box by checking the Y coordinate
            // Note: This assumes "above" means a larger Y value and "below" means a smaller Y value.
            bool isAboveOrBelow = point.Y < bbox.Min.Y || point.Y > bbox.Max.Y;

            // There is a horizontal overlap if the point is horizontally aligned and is either above or below the scope box
            return isHorizontallyAligned && isAboveOrBelow;
        }
        private bool IsVerticalOverlap(XYZ point, Element scopeBox)
        {
            // Retrieve the bounding box of the scope box in the active view
            BoundingBoxXYZ bbox = scopeBox.get_BoundingBox(null);

            // Check if the point is vertically aligned with the scope box
            // This means the point's Y coordinate is within the scope box's Y range
            bool isVerticallyAligned = point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y;

            // Now check if the point is to the left or right of the scope box by checking the X coordinate
            // Note: This assumes "left" means a smaller X value and "right" means a larger X value.
            bool isLeftOrRight = point.X < bbox.Min.X || point.X > bbox.Max.X;

            // There is a vertical overlap if the point is vertically aligned and is either to the left or right of the scope box
            return isVerticallyAligned && isLeftOrRight;
        }
        private ElementId InsertViewReference(Document doc, ElementId viewReferenceId, XYZ translationVector)
        {
            // Start a sub-transaction if necessary to duplicate the View Reference
            // Note: Ensure that the main transaction is already open before calling this method
            var subTrans = new SubTransaction(doc);
            subTrans.Start();

            // Copy the element to the same place, which creates a duplicate
            ElementId copiedViewRefId = ElementTransformUtils.CopyElement(doc, viewReferenceId, XYZ.Zero).FirstOrDefault();

            // Check if the element was copied successfully
            if (copiedViewRefId == null)
            {
                subTrans.RollBack();
                throw new InvalidOperationException("The View Reference could not be copied.");
            }
            ViewReferenceCopiesCount++;

            // Move the copied element to the desired location
            ElementTransformUtils.MoveElement(doc, copiedViewRefId, translationVector);

            subTrans.Commit();

            return copiedViewRefId;
        }

        private void RemoveExcessViewReferences(Document doc, List<ElementId> insertedViewRefs)
        {
            // Group the inserted references by their location
            var groupedByLocation = insertedViewRefs.GroupBy(
                id => GetElementCenter(doc, doc.GetElement(id))
            ).ToList();

            // Iterate through each group and ensure only one of each orientation remains
            foreach (var group in groupedByLocation)
            {
                // Separate the group into horizontal and vertical references
                var horizontalRefs = new List<ElementId>();
                var verticalRefs = new List<ElementId>();

                foreach (var id in group)
                {
                    Element el = doc.GetElement(id);
                    // Determine the orientation by checking the rotation of the View Reference
                    // This assumes that vertical references are rotated and horizontal are not
                    if (IsElementRotated(el)) // Implement IsElementRotated to check the rotation
                    {
                        verticalRefs.Add(id);
                    }
                    else
                    {
                        horizontalRefs.Add(id);
                    }
                }

                // Remove excess View References if more than one is found per orientation
                RemoveExcess(doc, horizontalRefs);
                RemoveExcess(doc, verticalRefs);
            }
        }

        private void RemoveExcess(Document doc, List<ElementId> viewRefs)
        {
            // If there's more than one reference, keep the first and delete the rest
            if (viewRefs.Count > 1)
            {
                var excessRefs = viewRefs.Skip(1); // Skip the first element and get the rest
                doc.Delete(excessRefs.ToList()); // Convert to List because doc.Delete expects ICollection<ElementId>
            }
        }

        private bool IsElementRotated(Element element)
        {
            Location loc = element.Location;
            if (loc != null && loc is LocationPoint locPoint)
            {
                // Get the rotation value, which is the angle in radians from the X-axis.
                double rotation = locPoint.Rotation;

                // Define the tolerance within which we consider the rotation to be effectively 90 degrees.
                const double tolerance = 0.001;
                const double ninetyDegreesInRadians = Math.PI / 2;

                // Check if the rotation angle is approximately 90 degrees (π/2 radians).
                // Since the rotation can be clockwise or counter-clockwise, check both π/2 and -π/2.
                return Math.Abs(rotation - ninetyDegreesInRadians) < tolerance ||
                       Math.Abs(rotation + ninetyDegreesInRadians) < tolerance;
            }

            // Assume the element is not rotated if not a LocationPoint or no significant rotation is detected.
            return false;
        }




        // Auxiliary methods like GetElementCenter, FindOverlappingScopeBoxes, IsHorizontalOverlap, IsVerticalOverlap, InsertViewReference, and RemoveExcessViewReferences would be defined according to the rules described.



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
    //        double expandDistance = 4; // 1 foot in Revit's internal units

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
