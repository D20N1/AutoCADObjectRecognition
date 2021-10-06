using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lines
{
    public static class Functions
    {
        public static ObjectIdCollection GetAllEntitiesOfType(string objectName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            TypedValue[] tvs = new TypedValue[1]
            {
            //new TypedValue((int)DxfCode.LayerName,layerName),
            new TypedValue((int)DxfCode.Start, objectName)
            };

            var sf = new SelectionFilter(tvs);

            var psr = ed.SelectAll(sf);

            if (psr.Status == PromptStatus.OK)
                return
                  new ObjectIdCollection(psr.Value.GetObjectIds());
            else
                return new ObjectIdCollection();
        }

        public static ObjectId[] GetSelectedObjectIdsArray()
        {
            // Get the current document editor
            var acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            var acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;
            var selectedObjectArray = new ObjectId[0];

            // Create a TypedValue array to define the filter criteria
            TypedValue[] acTypValAr = new TypedValue[4];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Operator, "<or"), 0);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "CIRCLE"), 1);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "LINE"), 2);
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Operator, "or>"), 3);

            // Assign the filter criteria to a SelectionFilter object
            var acSelFtr = new SelectionFilter(acTypValAr);

            // Get the PickFirst selection set
            PromptSelectionResult acSSPrompt;
            acSSPrompt = acDocEd.GetSelection(acSelFtr);
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                var selectedObject = acSSPrompt.Value;
                selectedObjectArray = selectedObject.GetObjectIds();
            }
            return selectedObjectArray;
        }
        public static void MarkFoundObjects(Transaction acTrans, ObjectIdCollection sortedFoundObjectComponents)
        {
            foreach (ObjectId foundObject in sortedFoundObjectComponents)
            {
                var linecolorchange = (Line)acTrans.GetObject(foundObject, OpenMode.ForWrite);
                //linecolorchange.Layer = "Lines";
                var lcolor = (Line)acTrans.GetObject(foundObject, OpenMode.ForWrite);
                lcolor.ColorIndex = 20;
            }
        }

        public static ObjectId[] GetEntityTypeTFromSelectedObjectArray<T>(Transaction acTrans, ObjectId[] selectedObjectArray) where T : Entity
        {
            var selectedObjectLinesCollection = new ObjectIdCollection();
            foreach (ObjectId entity in selectedObjectArray)
            {
                if ((Object)acTrans.GetObject(entity, OpenMode.ForRead) is T)
                {
                    selectedObjectLinesCollection.Add(entity);
                }
            }
            var selectedObjectLinesCollectionToArray = new ObjectId[selectedObjectLinesCollection.Count];
            for (int i = 0; i < selectedObjectLinesCollection.Count; i++)
            {
                selectedObjectLinesCollectionToArray[i] = selectedObjectLinesCollection[i];
            }

            return selectedObjectLinesCollectionToArray;
        }

        public static void NullifyArrayFrom(ObjectId[] array, int i)
        {
            if (array.Count() > 1)
            {
                for (int j = i; j < array.Count(); j++)
                {
                    array[i] = ObjectId.Null;
                }
            }
        }

        public static bool IsSameLine(Line l1, Line l2)
        {
            if ((l1.Linetype == l2.Linetype) && (l1.LineWeight == l2.LineWeight))
            {
                if (Math.Abs(l1.Length - l2.Length) <= 0.0009)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }
        public static ObjectId FindFirstSameLine(Transaction acTrans, ObjectIdCollection allO, Line firstLineFromSelectedObject)
        {
            Line lA1 = null;
            foreach (ObjectId allobj1 in allO)
            {
                if ((Object)acTrans.GetObject(allobj1, OpenMode.ForRead) is Line)
                {
                    lA1 = (Line)acTrans.GetObject(allobj1, OpenMode.ForRead);

                    if (Functions.IsSameLine(lA1, firstLineFromSelectedObject))
                    {
                        return lA1.Id;
                    }
                    else lA1 = null;
                }
            }
            return ObjectId.Null;
        }

        public static ObjectId FindNextLine(Transaction acTrans, int i, ObjectIdCollection allO, Line firstLineFromSelectedObject, Line l, ObjectId[] componentsOfPotentiallyFoundObject)
        {
            var lA1 = (Line)acTrans.GetObject(componentsOfPotentiallyFoundObject[0], OpenMode.ForRead);

            foreach (ObjectId allobj in allO)
            {
                if ((Object)acTrans.GetObject(allobj, OpenMode.ForRead) is Line)
                {
                    var lA = (Line)acTrans.GetObject(allobj, OpenMode.ForRead);

                    if ((Functions.LineCompare(firstLineFromSelectedObject, l, lA1, lA)) && (!componentsOfPotentiallyFoundObject.Contains(allobj)))
                    {
                        return lA.Id;
                    }
                }
            }
            return ObjectId.Null;
        }

        public static void FindObjectsFromLines(Transaction acTrans, ObjectId[] selectedObjectLinesArray, ObjectIdCollection sorted)
        {

            var acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;
            acDocEd.WriteMessage("Number of lines in selected object: " + selectedObjectLinesArray.Count().ToString() + "\n");

            var allLinesInDrawing = Functions.GetAllEntitiesOfType("LINE");


            var componentsOfPotentiallyFoundObject = new ObjectId[selectedObjectLinesArray.Count()];
            Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);

            var removedFirstLines = new ObjectIdCollection();

            var firstLineFromSelectedObject = (Line)acTrans.GetObject(selectedObjectLinesArray[0], OpenMode.ForRead);

            if (selectedObjectLinesArray.Count() == 1)
            {
                foreach (ObjectId allL in allLinesInDrawing)
                {
                    if ((Object)acTrans.GetObject(allL, OpenMode.ForRead) is Line)
                    {
                        Line lA1 = (Line)acTrans.GetObject(allL, OpenMode.ForRead);

                        if (Functions.IsSameLine(lA1, firstLineFromSelectedObject) && (!sorted.Contains(allL)))
                        {
                            sorted.Add(allL);

                        }
                    }
                }
            }
            else
            {
                do
                {
                    for (int i = 1; i < selectedObjectLinesArray.Count(); i++)
                    {
                        var l = (Line)acTrans.GetObject(selectedObjectLinesArray[i], OpenMode.ForRead);

                        componentsOfPotentiallyFoundObject[0] = Functions.FindFirstSameLine(acTrans, allLinesInDrawing, firstLineFromSelectedObject);

                        if (componentsOfPotentiallyFoundObject[0] != ObjectId.Null)
                        {
                            componentsOfPotentiallyFoundObject[i] = Functions.FindNextLine(acTrans, i, allLinesInDrawing, firstLineFromSelectedObject, l, componentsOfPotentiallyFoundObject);
                            if (componentsOfPotentiallyFoundObject[i] == ObjectId.Null)
                            {
                                break;
                            }
                        }
                    }

                    if (!componentsOfPotentiallyFoundObject.Contains(ObjectId.Null))
                    {
                        foreach (ObjectId foundO in componentsOfPotentiallyFoundObject)
                        {
                            sorted.Add(foundO);
                            allLinesInDrawing.Remove(foundO);
                        }

                    }
                    else
                    {
                        if (componentsOfPotentiallyFoundObject[0] != ObjectId.Null)
                        {
                            removedFirstLines.Add(componentsOfPotentiallyFoundObject[0]);
                        }
                        allLinesInDrawing.Remove(componentsOfPotentiallyFoundObject[0]);
                    }

                    Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);

                } while (componentsOfPotentiallyFoundObject[0] != ObjectId.Null);

                if (removedFirstLines.Count != 0)
                {
                    foreach (ObjectId removedPreviosly in removedFirstLines)
                    {
                        allLinesInDrawing.Add(removedPreviosly);
                    }

                    foreach (ObjectId removedPreviosly in removedFirstLines)
                    {
                        componentsOfPotentiallyFoundObject[0] = removedPreviosly;

                        for (int i = 1; i < selectedObjectLinesArray.Count(); i++)
                        {
                            var l = (Line)acTrans.GetObject(selectedObjectLinesArray[i], OpenMode.ForRead);

                            componentsOfPotentiallyFoundObject[i] = Functions.FindNextLine(acTrans, i, allLinesInDrawing, firstLineFromSelectedObject, l, componentsOfPotentiallyFoundObject);
                            if (componentsOfPotentiallyFoundObject[i] == ObjectId.Null)
                            {
                                break;
                            }
                        }

                        if (!componentsOfPotentiallyFoundObject.Contains(ObjectId.Null))
                        {
                            foreach (ObjectId foundO in componentsOfPotentiallyFoundObject)
                            {
                                sorted.Add(foundO);
                                allLinesInDrawing.Remove(foundO);
                            }

                        }
                        Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);
                    }
                }
            }

        }

        public static bool LineCompare(Line l1, Line l, Line lA1, Line lA)
        {
            Vector3d resultVectorl = l1.StartPoint.GetVectorTo(l.StartPoint);
            Vector3d resultVectorlA = lA1.StartPoint.GetVectorTo(lA.StartPoint);

            if (Math.Abs(resultVectorl.Length - resultVectorlA.Length) <= 0.00009)
            {
                if (Math.Abs(l1.Delta.GetAngleTo(l.Delta) - lA1.Delta.GetAngleTo(lA.Delta)) <= 0.00009)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }

        //CIRCLE        

        public static bool IsSameCircle(Circle c1, Circle c2)
        {
            if ((c1.Linetype == c2.Linetype) && (c1.LineWeight == c2.LineWeight))
            {
                if (Math.Abs(c1.Radius - c2.Radius) <= 0.00009)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }
        public static ObjectId FindFirstSameCircle(Transaction acTrans, ObjectIdCollection allO, Circle firstCircleFromSelectedObject)
        {
            Circle cA1 = null;
            foreach (ObjectId allobj1 in allO)
            {
                if ((Object)acTrans.GetObject(allobj1, OpenMode.ForRead) is Circle)
                {
                    cA1 = (Circle)acTrans.GetObject(allobj1, OpenMode.ForRead);

                    if (Functions.IsSameCircle(cA1, firstCircleFromSelectedObject))
                    {
                        return cA1.Id;
                    }
                    else cA1 = null;
                }
            }
            return ObjectId.Null;
        }
        public static bool CircleCompare(Circle c1, Circle c, Circle cA1, Circle cA)
        {
            Vector3d resultVectorc = c1.StartPoint.GetVectorTo(c.StartPoint);
            Vector3d resultVectorcA = cA1.StartPoint.GetVectorTo(cA.StartPoint);

            if (Math.Abs(resultVectorc.Length - resultVectorcA.Length) <= 0.00009)
            {
                if (Math.Abs(resultVectorc.GetAngleTo(resultVectorcA)) <= 0.00009)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public static ObjectId FindNextCircle(Transaction acTrans, int i, ObjectIdCollection allO, Circle firstCircleFromSelectedObject, Circle c, ObjectId[] componentsOfPotentiallyFoundObject)
        {
            var cA1 = (Circle)acTrans.GetObject(componentsOfPotentiallyFoundObject[0], OpenMode.ForRead);

            foreach (ObjectId allobj in allO)
            {
                if ((Object)acTrans.GetObject(allobj, OpenMode.ForRead) is Circle)
                {
                    Circle cA = (Circle)acTrans.GetObject(allobj, OpenMode.ForRead);

                    if ((Functions.CircleCompare(firstCircleFromSelectedObject, c, cA1, cA)) && (!componentsOfPotentiallyFoundObject.Contains(allobj)))
                    {
                        return cA.Id;
                    }
                }
            }
            return ObjectId.Null;
        }
        public static void FindObjectsFromCircles(Transaction acTrans, ObjectId[] selectedObjectCirclesArray, ObjectIdCollection sorted)
        {

            var acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;
            acDocEd.WriteMessage("Number of lines in selected object: " + selectedObjectCirclesArray.Count().ToString() + "\n");

            var allCirclesInDrawing = Functions.GetAllEntitiesOfType("CIRCLE");


            var componentsOfPotentiallyFoundObject = new ObjectId[selectedObjectCirclesArray.Count()];
            Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);

            var removedFirstLines = new ObjectIdCollection();

            var firstCircleFromSelectedObject = (Circle)acTrans.GetObject(selectedObjectCirclesArray[0], OpenMode.ForRead);


            do
            {
                if (selectedObjectCirclesArray.Count() == 1)
                {
                    foreach (ObjectId allC in allCirclesInDrawing)
                    {
                        var cA1 = (Circle)acTrans.GetObject(allC, OpenMode.ForRead);
                        if ((Object)acTrans.GetObject(allC, OpenMode.ForRead) is Circle)
                        {
                            cA1 = (Circle)acTrans.GetObject(allC, OpenMode.ForRead);

                            if (Functions.IsSameCircle(cA1, firstCircleFromSelectedObject))
                            {
                                sorted.Add(allC);
                                allCirclesInDrawing.Remove(allC);
                            }
                        }
                    }
                    break;
                }

                for (int i = 1; i < selectedObjectCirclesArray.Count(); i++)
                {
                    var c = (Circle)acTrans.GetObject(selectedObjectCirclesArray[i], OpenMode.ForRead);

                    componentsOfPotentiallyFoundObject[0] = Functions.FindFirstSameCircle(acTrans, allCirclesInDrawing, firstCircleFromSelectedObject);

                    if (componentsOfPotentiallyFoundObject[0] != ObjectId.Null)
                    {
                        componentsOfPotentiallyFoundObject[i] = Functions.FindNextCircle(acTrans, i, allCirclesInDrawing, firstCircleFromSelectedObject, c, componentsOfPotentiallyFoundObject);
                        if (componentsOfPotentiallyFoundObject[i] == ObjectId.Null)
                        {
                            break;
                        }
                    }
                }

                if (!componentsOfPotentiallyFoundObject.Contains(ObjectId.Null))
                {
                    foreach (ObjectId foundO in componentsOfPotentiallyFoundObject)
                    {
                        sorted.Add(foundO);
                        allCirclesInDrawing.Remove(foundO);
                    }

                }
                else
                {
                    if (componentsOfPotentiallyFoundObject[0] != ObjectId.Null)
                    {
                        removedFirstLines.Add(componentsOfPotentiallyFoundObject[0]);
                    }
                    allCirclesInDrawing.Remove(componentsOfPotentiallyFoundObject[0]);
                }

                Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);

            } while (componentsOfPotentiallyFoundObject[0] != ObjectId.Null);

            if (removedFirstLines.Count != 0)
            {
                foreach (ObjectId removedPreviosly in removedFirstLines)
                {
                    allCirclesInDrawing.Add(removedPreviosly);
                }

                foreach (ObjectId removedPreviosly in removedFirstLines)
                {
                    componentsOfPotentiallyFoundObject[0] = removedPreviosly;

                    for (int i = 1; i < selectedObjectCirclesArray.Count(); i++)
                    {
                        var l = (Circle)acTrans.GetObject(selectedObjectCirclesArray[i], OpenMode.ForRead);

                        componentsOfPotentiallyFoundObject[i] = Functions.FindNextCircle(acTrans, i, allCirclesInDrawing, firstCircleFromSelectedObject, l, componentsOfPotentiallyFoundObject);
                        if (componentsOfPotentiallyFoundObject[i] == ObjectId.Null)
                        {
                            break;
                        }
                    }

                    if (!componentsOfPotentiallyFoundObject.Contains(ObjectId.Null))
                    {
                        foreach (ObjectId foundO in componentsOfPotentiallyFoundObject)
                        {
                            sorted.Add(foundO);
                            allCirclesInDrawing.Remove(foundO);
                        }

                    }
                    Functions.NullifyArrayFrom(componentsOfPotentiallyFoundObject, 1);
                }
            }

        }

        //CURVE
        public static bool CurveCompare(Curve c1, Curve c, Curve cA1, Curve cA)
        {
            Vector3d resultVectorc = c1.StartPoint.GetVectorTo(c.StartPoint);
            Vector3d resultVectorcA = cA1.StartPoint.GetVectorTo(cA.StartPoint);

            if (Math.Abs(resultVectorc.Length - resultVectorcA.Length) <= 0.00009)
            {
                if (Math.Abs(resultVectorc.GetAngleTo(resultVectorcA)) <= 0.00009)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public static void FindObjectsFromDifferentTypes(Transaction acTrans, ObjectId[] selectedArray, ObjectId[] selectedArrayCurrent, ObjectId[] sorted2, ObjectIdCollection sortedFoundObjectComponents, ObjectIdCollection sortedFoundObjectComponentsCurrent, int objectComponentsCount)
        {
            ObjectId[] potentialyFoundComponents = new ObjectId[selectedArrayCurrent.Count()];
            Curve c1 = null;
            NullifyArrayFrom(potentialyFoundComponents, 0);

            for (int i = 0; i < sortedFoundObjectComponents.Count; i += objectComponentsCount)
            {
                if (selectedArray.Contains(sortedFoundObjectComponents[i]))
                {
                    c1 = (Curve)acTrans.GetObject(selectedArray[0], OpenMode.ForRead);
                }
            }
            
            var cA1 = (Curve)acTrans.GetObject(sortedFoundObjectComponents[0], OpenMode.ForRead);

            for (int i = 0; i < sortedFoundObjectComponents.Count; i+=objectComponentsCount)
            {
                cA1 = (Curve)acTrans.GetObject(sortedFoundObjectComponents[i], OpenMode.ForRead);
                
                for (int k = 0; k < sortedFoundObjectComponentsCurrent.Count; k++)
                {
                    var cA = (Curve)acTrans.GetObject(sortedFoundObjectComponentsCurrent[k], OpenMode.ForRead);

                    for (int j = 0; j < selectedArrayCurrent.Count(); j++)
                    {

                        var c = (Curve)acTrans.GetObject(selectedArrayCurrent[j], OpenMode.ForRead);
                        if (CurveCompare(c1, c, cA1, cA))
                        {
                            potentialyFoundComponents.Append(cA.Id);
                            

                        }
                    }
                }
                if (!potentialyFoundComponents.Contains(ObjectId.Null))
                {

                }
                

            }


        }
    }
}
