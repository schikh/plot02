using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace ViewportCreation
{
    public class Commands
    {
        [CommandMethod("NRVPS")]
        static public void CreateNonRectangularViewports()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // We're accessing drawing objects, so we need a transaction

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ps = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite);

                //Polyline pl = new Polyline(6);
                //pl.AddVertexAt(0, new Point2d(4.92, 5.29), 0, 0, 0);
                //pl.AddVertexAt(1, new Point2d(5.16, 6.02), 0, 0, 0);
                //pl.AddVertexAt(2, new Point2d(6.12, 6.49), 0, 0, 0);
                //pl.AddVertexAt(3, new Point2d(7.29, 6.26), -0.27, 0, 0);
                //pl.AddVertexAt(4, new Point2d(8.11, 5.53), -0.47, 0, 0);
                //pl.AddVertexAt(5, new Point2d(7.75, 5.41), 0, 0, 0);
                //pl.Closed = true;


                // Add our boundary to paperspace and the
                // transaction
                //ObjectId id = ps.AppendEntity(pl);
                //tr.AddNewlyCreatedDBObject(pl, true);

                // Create our viewport, adding that also
                //Viewport vp = new Viewport();
                //ps.AppendEntity(vp);
                //tr.AddNewlyCreatedDBObject(vp, true);

                //// Set the boundary entity and turn the
                //// viewport/clipping on

                //vp.NonRectClipEntityId = id;
                //vp.NonRectClipOn = true;
                //vp.On = true;




                // Create a Viewport
                using (Viewport acVport = new Viewport())
                {
                    acVport.CenterPoint = new Point3d(3.25, 3, 0);
                    acVport.Width = 1000;
                    acVport.Height = 1000;

                    // Add the new object to the block table record and the transaction
                    ps.AppendEntity(acVport);
                    tr.AddNewlyCreatedDBObject(acVport, true);

                    // Change the view direction
                    acVport.ViewDirection = new Vector3d(1, 1, 1);

                    // Enable the viewport
                    acVport.On = true;
                }








                tr.Commit();
            }

            // Let's take a look at the results in paperspace
            db.TileMode = false;
        }
    }
}