//using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AutoCADTest
{
    //[assembly: CommandClass(typeof(Perf))]
    //[assembly: CommandClass(typeof(PlottingCommands))]

    //public class Perf //: IExtensionApplication
    //{
    //    [CommandMethod("TestAll")]
    //    public void AllTests()
    //    {
    //        Helper.Trace("DONE");
    //    }

    //    [CommandMethod("TestExtend")]
    //    public void TestExtend()
    //    {
    //        var document = Application.DocumentManager.MdiActiveDocument;
    //        using (document.LockDocument())
    //        {
    //            var db = document.Database;
    //            using (var tr = db.TransactionManager.StartTransaction())
    //            {
    //                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    //                foreach (ObjectId btrId in bt)
    //                {
    //                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
    //                    foreach (ObjectId entId in btr)
    //                    {
    //                        var entity = tr.GetObject(entId, OpenMode.ForRead) as Entity;
    //                        if (entity != null)
    //                        {
    //                        }
    //                    }
    //                }
    //                tr.Commit();
    //            }
    //        }
    //    }
    //}

    public class PlottingCommands2
    {
        // Command in "session" context will execute synchronously.
        //[CommandMethod("Async")]
        //public static void Async()
        //{
        //    AcadApplication app = Application.AcadApplication as AcadApplication;
        //    app.ActiveDocument.SendCommand("._CIRCLE 0,0 5 ._ZOOM Extents ");
        //    Application.ShowAlertDialog("Rectangle created!");
        //}

        //// Command in "session" context will execute synchronously.    
        //[CommandMethod("Sync", CommandFlags.Session)]
        //public static void Sync()
        //{
        //    AcadApplication app = Application.AcadApplication as AcadApplication;
        //    app.ActiveDocument.SendCommand("._CIRCLE 0,0 5 ._ZOOM Extents ");
        //    Application.ShowAlertDialog("Rectangle created!");
        //}

        //[CommandMethod("SayHello", CommandFlags.Modal)]
        //public void SayHelloMethod()
        //{
        //    DocumentCollection docs = Application.DocumentManager;
        //    Document activeDoc = docs.MdiActiveDocument;
        //    Editor ed = activeDoc.Editor;
            
        //    ed.WriteMessage(string.Format("{0}Hello !!", Environment.NewLine));
        //}

        ////[CommandMethod("Count", CommandFlags.Modal | CommandFlags.Session)]
        //[CommandMethod("Count2")]
        //public void Count2() // This method can have any name
        //{
            
        //}

        [CommandMethod("Count", CommandFlags.Modal)]
        public void Count() // This method can have any name
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // get the model space block table record
                BlockTableRecord ms_btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                ed.WriteMessage("hello world!");

                Circle a = new Circle();
                a.Center = new Point3d(0, 0, 0);
                a.Radius = 50;

                ms_btr.AppendEntity(a);
                tr.AddNewlyCreatedDBObject(a, true);

                int j = 0;
                // prints out messages to editor line
                foreach (ObjectId id in ms_btr)
                {
                    DBObject object1 = tr.GetObject(id, OpenMode.ForRead) as DBObject;
                    ed.WriteMessage(object1.ToString());
                    j++;
                }

                ed.WriteMessage("Count: " + j);

                tr.Commit();
            }

            //try
            //{
            //    doc.SendStringToExecute("_POINT 1,1,0 ", false, false, true);
            //}
            //catch (System.Exception ex)
            //{
            //    ed.WriteMessage(ex.ToString());
            //}
        }
    }
}

//foreach (string filename in filenames)
//{
//Process acc = new Process();
//const string quote = "\"";
//optionsaccoreconsole = quote + pathsprog + "\\" + script;
//options = "/i " + filename + " /s d:\\plot.scr";
//acc.StartInfo.Arguments = options;
//acc.StartInfo.FileName = acccoreconsole;
//acc.Start();
//}