//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Autodesk.AutoCAD.ApplicationServices;
//using Autodesk.AutoCAD.ApplicationServices.Core;
//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.EditorInput;
//using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.Runtime;

//namespace AutoCADTest
//{
//    class Class3
//    {
//        [CommandMethod("InsertBlock")]
//        static public void InsertBlock() // This method can have any name
//        {
//            Document doc = Application.DocumentManager.MdiActiveDocument;
//            using (Database OpenDb = new Database(false, true))
//            {
//                OpenDb.ReadDwgFile("c:\\temp\\test.dwg", System.IO.FileShare.ReadWrite, true, "");

//                ObjectIdCollection ids = new ObjectIdCollection();
//                using (Transaction tr = OpenDb.TransactionManager.StartTransaction())
//                {
//                    //For example, Get the block by name "TEST"
//                    BlockTable bt = (BlockTable)tr.GetObject(OpenDb.BlockTableId, OpenMode.ForRead);
//                    if (bt.Has("TEST"))
//                    {
//                        ids.Add(bt["TEST"]);
//                    }
//                    tr.Commit();
//                }

//                //if found, add the block
//                if (ids.Count != 0)
//                {
//                    //get the current drawing database
//                    Database destdb = doc.Database;

//                    IdMapping iMap = new IdMapping();
//                    destdb.WblockCloneObjects(ids, destdb.BlockTableId, iMap, DuplicateRecordCloning.Ignore, false);
//                }
//            }
//        }


//        ///////////////////////////////////Start Add/////////////////////////////
//        public class DynamicBlocks
//        {
//            public string BlockName { get; set; }
//            public List<object> VisibilityProperties;

//            public DynamicBlocks(string blockName)
//            {
//                BlockName = blockName;
//                VisibilityProperties = new List<object>();
//            }

//        }
//        ///////////////////////////////////End Add/////////////////////////////

//        //A method to iterate all open drawings and to get all blocks that are inserted in each 
//        //drawing and copying them over to the current active document.
//        [CommandMethod("InsertBlocks", CommandFlags.Session)]
//        public void InsertBlocks()
//        {
//            List<string> blockNames = new List<string>(); // A generic list to keep up with all blocks to check later if a block has already inserted.

//            List<DynamicBlocks> dynamicBlocks = new List<DynamicBlocks>();//Added

//            DocumentCollection documents = Application.DocumentManager; // All the current open documents

//            Document doc = Application.DocumentManager.MdiActiveDocument; // Active Document
//            Editor ed = doc.Editor; // Active Document's Editor
//            Database db = doc.Database; // Active Document Database

//            Document openDocument; // Temporay variable to hold each non-active Document
//            Database openDatabase; //Temporay variable to hold each non-active Document's Database

//            using (DocumentLock doclock = doc.LockDocument()) // Start Lock on Active Document
//            using (Transaction trx = db.TransactionManager.StartTransaction()) // Start Transaction with Active Document's Database
//            {
//                // Start looping through each document
//                foreach (Document document in documents)
//                {
//                    openDocument = document;
//                    string fileName = openDocument.Name;
//                    // If the document is the current or Active document skip it
//                    if (doc.Name == fileName || fileName == null || fileName == String.Empty)
//                        continue;

//                    openDatabase = new Database(false, true);
//                    // 'Open' Open documents database 
//                    openDatabase.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, "");

//                    using (Transaction openDbTrx = openDatabase.TransactionManager.StartTransaction())
//                    {//Start Transaction with Open Document's Database

//                        BlockTable openBlockTable = openDbTrx.GetObject(openDatabase.BlockTableId, OpenMode.ForRead) as BlockTable;

//                        // Loop through each BlockTableRecord 
//                        foreach (ObjectId btrId in openBlockTable)
//                        {
//                            BlockTableRecord btr = openDbTrx.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
//                            // Make sure it is not a ModelSpace or any of the PaperSpaces, any type of Xref
//                            // or MEP type Object, or has already been clone before etc.... & and the block has been inserted at least once
//                            if (btr.IsLayout || btr.IsAnonymous || btr.IsFromExternalReference || btr.IsFromOverlayReference || blockNames.Contains(btr.Name)
//                                || btr.Name.StartsWith("Aec", StringComparison.OrdinalIgnoreCase) || btr.Name.StartsWith("_")
//                                || btr.GetBlockReferenceIds(true, false).Count < 1)
//                                continue;

//                            ///////////////////////////////////Start Add/////////////////////////////
//                            if (btr.IsDynamicBlock)
//                            {
//                                DynamicBlocks dynBlks = new DynamicBlocks(btr.Name);
//                                ObjectIdCollection brefObjidColl = btr.GetBlockReferenceIds(true, true);
//                                ObjectIdCollection anonObjIdsColl = btr.GetAnonymousBlockIds();

//                                foreach (ObjectId anonObjId in anonObjIdsColl)
//                                {
//                                    BlockTableRecord btrAnon = (BlockTableRecord)anonObjId.GetObject(OpenMode.ForRead);
//                                    ObjectIdCollection ObjidColl = btrAnon.GetBlockReferenceIds(true, true);
//                                    foreach (ObjectId Id in ObjidColl)
//                                        brefObjidColl.Add(Id);
//                                }

//                                foreach (ObjectId brefObjId in brefObjidColl)
//                                {
//                                    BlockReference bref = brefObjId.GetObject(OpenMode.ForWrite) as BlockReference;
//                                    DynamicBlockReferencePropertyCollection dynBrefColl = bref.DynamicBlockReferencePropertyCollection;
//                                    foreach (DynamicBlockReferenceProperty dynBrefProps in dynBrefColl)
//                                    {
//                                        // Jeff_M code
//                                        if (dynBrefProps.PropertyTypeCode == 5)
//                                        {
//                                            dynBlks.VisibilityProperties.Add(dynBrefProps.Value);
//                                        }
//                                    }
//                                }
//                                dynamicBlocks.Add(dynBlks);
//                            }
//                            ///////////////////////////////////End Add/////////////////////////////
//                            //Add Block to list of blocks
//                            blockNames.Add(btr.Name);

//                            //Do Cloning
//                            ObjectIdCollection objIdCollection = new ObjectIdCollection();
//                            objIdCollection.Add(btr.ObjectId);
//                            IdMapping idMap = new IdMapping();
//                            openDatabase.WblockCloneObjects(objIdCollection, db.BlockTableId, idMap, DuplicateRecordCloning.Ignore, false);

//                        }
//                        openDbTrx.Commit();
//                    }//End Transaction with Open Document's Database
//                    openDatabase.Dispose(); // Dispose Database
//                }// End foreach 
//                trx.Commit();
//            }// End Lock on Active Document & Transaction with Active Document's Database

//            ///////////////////////////////////Start Add/////////////////////////////
//            using (DocumentLock doclock = doc.LockDocument())
//            using (Transaction trx = db.TransactionManager.StartTransaction()) // Start Transaction with Active Document's Database
//            {
//                BlockTable bt = trx.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
//                BlockTableRecord ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
//                Random rdn = new Random();
//                foreach (DynamicBlocks dynBlks in dynamicBlocks)
//                {
//                    foreach (string visi in dynBlks.VisibilityProperties)
//                    {
//                        BlockTableRecord btr = bt[dynBlks.BlockName].GetObject(OpenMode.ForRead) as BlockTableRecord;
//                        double rdnDouble = rdn.Next(100);
//                        BlockReference bref = new BlockReference(new Point3d(rdnDouble, rdnDouble, 0.0), btr.ObjectId);
//                        ms.AppendEntity(bref);
//                        trx.AddNewlyCreatedDBObject(bref, true);

//                        DynamicBlockReferencePropertyCollection dynBrefColl = bref.DynamicBlockReferencePropertyCollection;
//                        foreach (DynamicBlockReferenceProperty dynBrefProps in dynBrefColl)
//                        {
//                            if (dynBrefProps.PropertyTypeCode == 5)
//                            {
//                                dynBrefProps.Value = visi;
//                            }
//                        }

//                    }

//                }

//                trx.Commit();
//            }
//            ///////////////////////////////////End Add/////////////////////////////
//        }



//    }
//}
