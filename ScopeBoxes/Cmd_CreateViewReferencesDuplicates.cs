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
    public class Cmd_CreateViewReferencesDuplicates : IExternalCommand
    {
        public int ViewReferenceCopiesCount { get; private set; } = 0;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            Element viewReference = GetFirstViewReference(doc);
            if (viewReference == null)
            {
                TaskDialog.Show("Error", "No View Reference found in the current view.");
                return Result.Failed;
            }

            List<Element> selectedScopeBoxes = GetSelectedScopeBoxes(doc);
            if (selectedScopeBoxes.Count < 2)
            {
                TaskDialog.Show("Error", "You must select at least 2 overlapped Scope Boxes.");
                return Result.Failed;
            }

            try
            {
                PlaceViewReferences(doc, selectedScopeBoxes, viewReference);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }

            TaskDialog.Show("Success", $"{ViewReferenceCopiesCount} View References placed successfully.");
            return Result.Succeeded;
        }

        private Element GetFirstViewReference(Document doc)
        {
            return new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfCategory(BuiltInCategory.OST_ReferenceViewer)
                .WhereElementIsNotElementType()
                .FirstOrDefault();
        }

        private List<Element> GetSelectedScopeBoxes(Document doc)
        {
            return MyUtils.GetSelectedScopeBoxes(doc);
        }

        private void PlaceViewReferences(Document doc, List<Element> selectedScopeBoxes, Element viewReference)
        {
            using (Transaction trans = new Transaction(doc, "Place View References"))
            {
                trans.Start();

                foreach (Element scopeBox in selectedScopeBoxes)
                {
                    ProcessScopeBoxForViewReferenceInsertion(doc, scopeBox, selectedScopeBoxes, viewReference);
                }

                trans.Commit();
            }
        }

        private void ProcessScopeBoxForViewReferenceInsertion(Document doc, Element scopeBox, List<Element> selectedScopeBoxes, Element viewReference)
        {
            List<XYZ> insertPoints = GetBoxFourInsertPoints(scopeBox);
            List<Element> otherScopeBoxes = selectedScopeBoxes.Except(new[] { scopeBox }).ToList();

            foreach (XYZ insertPoint in insertPoints)
            {
                ProcessInsertPointForViewReferenceInsertion(doc, insertPoint, scopeBox, otherScopeBoxes, viewReference);
            }
        }

        private void ProcessInsertPointForViewReferenceInsertion(Document doc, XYZ insertPoint, Element scopeBox, List<Element> otherScopeBoxes, Element viewReference)
        {
            List<string> overlapDirections = CheckOverlapsForCorner(insertPoint, otherScopeBoxes, scopeBox.get_BoundingBox(null));

            if (overlapDirections.Contains("Horizontal"))
            {
                if (CheckVerticalBoxOverlaps(insertPoint, scopeBox, otherScopeBoxes, doc.ActiveView))
                {
                    InsertViewReference(doc, viewReference.Id, insertPoint - GetElementCenter(doc, viewReference));
                }
            }

            if (overlapDirections.Contains("Vertical"))
            {
                if (CheckHorizontalBoxOverlaps(insertPoint, scopeBox, otherScopeBoxes, doc.ActiveView))
                {
                    ElementId newVerticalViewRefId = InsertViewReference(doc, viewReference.Id, insertPoint - GetElementCenter(doc, viewReference));
                    RotateElementFromCenter(doc, newVerticalViewRefId);
                }
            }
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
                    if (DoBoxesAlignHorizontally(scopeBoxBBox, otherBoxBBox))
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
                    if (DoBoxesAlignVertically(scopeBoxBBox, otherBoxBBox))
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
        private bool DoBoxesAlignHorizontally(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            XYZ box1Center = (box1.Min + box1.Max) / 2;
            XYZ box2Center = (box2.Min + box2.Max) / 2;

            // if box1 and box2 are both in the same horizontal line, they will both have the same Y center point
            return box1Center.Y == box2Center.Y;
        }
        private bool DoBoxesAlignVertically(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            XYZ box1Center = (box1.Min + box1.Max) / 2;
            XYZ box2Center = (box2.Min + box2.Max) / 2;

            // if box1 and box2 are both in the same vertical line, they will both have the same Y center point
            return box1Center.X == box2Center.X;
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

        private static List<XYZ> GetBoxFourInsertPoints(Element scopeBox)
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
            string buttonTitle = "View Reference \nAnotations";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "Cmd_CreateViewReferencesAnotations will be used to create view reverence copies at each corner of overlapped scope boxes.");

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
            // Calculate the adjested corners
            CalculateAdjestedCorners(boundingBox);
        }
        private void CalculateAdjestedCorners(BoundingBoxXYZ boundingBox)
        {
            // Assuming Z is constant and we're expanding in X and Y directions
            double expandDistance = 4; // 1 foot in Revit's internal units

            // Original Min and Max points
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            // Inwards Offset BoundingBox Corner Points 
            LeftBottom = new XYZ(min.X + expandDistance, min.Y + expandDistance, min.Z);
            RightBottom = new XYZ(max.X - expandDistance, min.Y + expandDistance, min.Z);
            LeftTop = new XYZ(min.X + expandDistance, max.Y - expandDistance, max.Z);
            RightTop = new XYZ(max.X - expandDistance, max.Y - expandDistance, max.Z);
        }
    }
}
