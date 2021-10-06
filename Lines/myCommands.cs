using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: CommandClass(typeof(Lines.MyCommands))]

namespace Lines
{
    public class MyCommands
    {        
        [CommandMethod("Lines", CommandFlags.UsePickSet)]
        public static void Lines()
        {
            var acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            var acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;

            var selectedObjectArray = Functions.GetSelectedObjectIdsArray();

            var FoundObjects = new FoundObjects();
            var sortedFoundObjectComponents = new ObjectIdCollection();
            var countedObjects = 0;
            var objectComponentsCount = 0;
            
            try
            {
                using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
                {
                    var selectedObjectLinesArray = Functions.GetEntityTypeTFromSelectedObjectArray<Line>(acTrans, selectedObjectArray);
                    var selectedObjectCirclesArray = Functions.GetEntityTypeTFromSelectedObjectArray<Circle>(acTrans, selectedObjectArray);

                    if (selectedObjectCirclesArray.Count() != 0)
                    {
                        Functions.FindObjectsFromCircles(acTrans, selectedObjectCirclesArray, FoundObjects.SortedFromCircles);
                        sortedFoundObjectComponents = FoundObjects.SortedFromCircles;
                        objectComponentsCount += selectedObjectCirclesArray.Count();
                    }

                    if (selectedObjectLinesArray.Count() != 0)
                    {
                        Functions.FindObjectsFromLines(acTrans, selectedObjectLinesArray, FoundObjects.SortedFromLines);
                        if (sortedFoundObjectComponents.Count==0)
                        {
                            sortedFoundObjectComponents = FoundObjects.SortedFromLines;
                            objectComponentsCount += selectedObjectLinesArray.Count();
                        }
                        else
                        {
                            objectComponentsCount = 0;
                        }
                    }
                    








                    Functions.MarkFoundObjects(acTrans, sortedFoundObjectComponents);

                    countedObjects = sortedFoundObjectComponents.Count / objectComponentsCount;
                    
                    acDocEd.WriteMessage("Number of objects that are same as selected one: " + countedObjects.ToString() + "\n");
                    Application.ShowAlertDialog("Number of objects that are same as selected one: " + countedObjects.ToString() + "\n");

                    acTrans.Commit();
                }
            }
            catch
            {
                Application.ShowAlertDialog("Try again!");
            }
        }
    }
}