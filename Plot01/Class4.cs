//using System.Collections.Generic;
//using System.Linq;
//using Autodesk.AutoCAD.ApplicationServices;
//using Autodesk.AutoCAD.ApplicationServices.Core;
//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.EditorInput;
//using Autodesk.AutoCAD.Runtime;

//namespace LayerTools
//{
//    public class Commands
//    {
//        [CommandMethod("DisplayLayers", "displa", CommandFlags.Modal)]
//        public void TestDisplayLayers()
//        {
//            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
//            Database db = doc.Database;
//            Editor ed = doc.Editor;
//            List<string> info = LayersToList(db);
//            foreach (string lname in info)
//                ed.WriteMessage("\nLayer Name: \t{0}", lname);

//        }




//        [CommandMethod("IterateLayers")]
//        public static void IterateLayers()
//        {
//            Document acDoc = Application.DocumentManager.MdiActiveDocument;
//            Database acCurDb = acDoc.Database;

//            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
//            {
//                var acLyrTbl = (LayerTable) acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead);
//                foreach (ObjectId acObjId in acLyrTbl)
//                {
//                    var acLyrTblRec = (LayerTableRecord)acTrans.GetObject(acObjId, OpenMode.ForRead);
//                    //acDoc.Editor.WriteMessage("\n" + acLyrTblRec.Name);
//                }
//            }
//        }

//        [CommandMethod("CL")]
//        public void ChangeLayerOfEntitiess()
//        {
//            var doc = Application.DocumentManager.MdiActiveDocument;
//            var db = doc.Database;
//            var ed = doc.Editor;

//            var layerName = "?????????????????????";

//            // We won't validate whether the layer exists -
//            // we'll just see what's returned by the selection.

//            var tvs = new [] { new TypedValue((int)DxfCode.LayerName, layerName) };
//            var sf = new SelectionFilter(tvs);
//            var psr = ed.SelectAll(sf);
//            if (psr.Status == PromptStatus.OK ||
//                psr.Status == PromptStatus.Error)
//            {
                
//            }
//            if (psr.Value.Count == 0)
//            {
                
//            }

//            var newLayerName = "??????????????????";
//            var tr = db.TransactionManager.StartTransaction();
//            using (tr)
//            {
//                var lt = (LayerTable) tr.GetObject(db.LayerTableId, OpenMode.ForRead);
//                if (!lt.Has(newLayerName))
//                {
//                    ed.WriteMessage("\nLayer not found.");
//                }
//                else
//                {
//                    var changedCount = 0;
//                    var lid = lt[newLayerName];
//                    foreach (var id in psr.Value.GetObjectIds())
//                    {
//                        var ent = (Entity) tr.GetObject(id, OpenMode.ForWrite);
//                        ent.LayerId = lid;
//                        // Could also have used:
//                        //  ent.Layer = newLayerName;
//                        // but this way is more efficient and cleaner
//                        changedCount++;
//                    }
//                }
//                tr.Commit();
//            }
//        }


//        public void LayersToList(Database db, Transaction tr)
//        {
//            var layerTable = (LayerTable) tr.GetObject(db.LayerTableId, OpenMode.ForRead);
//            var layerIds = layerTable.Cast<ObjectId>();
//            if (!layerIds.Any())
//            {
//                return;
//            }
//            var destLayerId = layerIds.First();
//            if (layerIds.Count() == 1)
//            {
//                var layerTableRecord = (LayerTableRecord)tr.GetObject(destLayerId, OpenMode.ForWrite);
//                layerTableRecord.Name = "fond_topo";
//            }
//            foreach (var sourceLayerId in layerIds.Skip(1))
//            {
//                var layerTableRecord = (LayerTableRecord)tr.GetObject(sourceLayerId, OpenMode.ForRead);
//                var sourceLayerName = layerTableRecord.Name;
//                foreach (var id in GetEntitiesOnLayer(sourceLayerName))
//                {
//                    var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
//                    ent.LayerId = destLayerId;
//                }
//            }
//        }

//        private static ObjectId[] GetEntitiesOnLayer(string layerName)
//        {
//  //TypedValue[] tvs = new [] {
//  //    new TypedValue(
//  //      (int)DxfCode.Operator,
//  //      "<or"
//  //    ),
//  //    new TypedValue(
//  //      (int)DxfCode.LayerName,
//  //      layerName
//  //    ),
//  //    new TypedValue(
//  //      (int)DxfCode.LayerName,
//  //      layerName2
//  //    ),
//  //    new TypedValue(
//  //      (int)DxfCode.Operator,
//  //      "or>"
//  //    )
//  //};
//            Document doc = Application.DocumentManager.MdiActiveDocument;
//            Editor ed = doc.Editor;
//            var tvs = new [] { new TypedValue( (int)DxfCode.LayerName, layerName ) };
//            var sf = new SelectionFilter(tvs);
//            var psr = ed.SelectAll(sf);
//            if (psr.Status == PromptStatus.OK)
//            {
//                return psr.Value.GetObjectIds();
//            }
//            return new ObjectId[0];
//        }


//        [CommandMethod("CheckLayerDependence")]
//        public void CheckLayerDependence()
//        {
//            Editor ed =
//              Autodesk.AutoCAD.ApplicationServices.Core.Application.
//              DocumentManager.MdiActiveDocument.Editor;

//            Database db = HostApplicationServices.WorkingDatabase;

//            using (Transaction tr = db.TransactionManager.StartTransaction())
//            {
//                LayerTable lt =
//                  (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

//                if (lt.Has("Layer1"))
//                {
//                    ObjectId ltrId = lt["Layer1"];

//                    ObjectIdCollection idCol = new ObjectIdCollection();
//                    idCol.Add(ltrId);
//                    db.Purge(idCol);

//                    if (idCol.Count != 0)
//                    {
//                        ed.WriteMessage("\nLayer1 does not have dependencies.");
//                    }
//                    else
//                    {
//                        ed.WriteMessage("\nLayer1 has dependencies.");
//                    }
//                }
//                else
//                {
//                    ed.WriteMessage("\nLayer1 does not exist!");
//                }

//                tr.Commit();
//            }
//        }


//        [CommandMethod("EraseLayer")]
//        public static void EraseLayer()
//        {
//            // Get the current document and database
//            Document acDoc = Application.DocumentManager.MdiActiveDocument;
//            Database acCurDb = acDoc.Database;

//            // Start a transaction
//            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
//            {
//                // Open the Layer table for read
//                LayerTable acLyrTbl;
//                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;

//                string sLayerName = "ABC";

//                if (acLyrTbl.Has(sLayerName) == true)
//                {
//                    // Check to see if it is safe to erase layer
//                    ObjectIdCollection acObjIdColl = new ObjectIdCollection();
//                    acObjIdColl.Add(acLyrTbl[sLayerName]);
//                    acCurDb.Purge(acObjIdColl);

//                    if (acObjIdColl.Count > 0)
//                    {
//                        LayerTableRecord acLyrTblRec;
//                        acLyrTblRec = acTrans.GetObject(acObjIdColl[0], OpenMode.ForWrite) as LayerTableRecord;

//                        try
//                        {
//                            // Erase the unreferenced layer
//                            acLyrTblRec.Erase(true);
//                            // Save the changes and dispose of the transaction
//                            acTrans.Commit();
//                        }
//                        catch (Autodesk.AutoCAD.Runtime.Exception Ex)
//                        {
//                            // Layer could not be deleted
//                            Application.ShowAlertDialog("Error:\n" + Ex.Message);
//                        }
//                    }
//                }
//            }
//        }

//        [CommandMethod("RenameLayer")]
//        public static void RenameLayer()
//        {
//            // Get the current document and database
//            Document acDoc = Application.DocumentManager.MdiActiveDocument;
//            Database acCurDb = acDoc.Database;

//            // Start a transaction
//            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
//            {
//                // Returns the layer table for the current database
//                var acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForWrite) as LayerTable;

//                // Clone layer 0 (copy it and its properties) as a new layer
//                var acLyrTblRec = acTrans.GetObject(acLyrTbl["0"], OpenMode.ForRead).Clone() as LayerTableRecord;

//                // Change the name of the cloned layer
//                acLyrTblRec.Name = "MyLayer";

//                // Add the cloned layer to the Layer table and transaction
//                acLyrTbl.Add(acLyrTblRec);
//                acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);

//                // Save changes and dispose of the transaction
//                acTrans.Commit();
//            }
//        }
//    }
//}