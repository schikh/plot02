//using Autodesk.AutoCAD.ApplicationServices;
//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.Runtime;

//namespace GeoLocationAPI
//{
//    public class Commands
//    {
//        [CommandMethod("IGR")]
//        public void InsertGeoRef()
//        {
//            var doc = Application.DocumentManager.MdiActiveDocument;
//            if (doc == null)
//                return;
//            var ed = doc.Editor;
//            var db = doc.Database;
//            var msId = SymbolUtilityServices.GetBlockModelSpaceId(db);

//            // Check whether the drawing already has geolocation data

//            bool hasGeoData = false;
//            try
//            {
//                var gdId = db.GeoDataObject;
//                hasGeoData = true;
//            }
//            catch { }

//            if (hasGeoData)
//            {
//                // Report and return: could also open the object for
//                // write and modify its properties, of course

//                ed.WriteMessage("\nDrawing already has geo-location data!");
//                return;
//            }

//            // Let's create some geolocation data for this drawing,
//            // using a handy method to add it to the modelspace
//            // (it gets added to the extension dictionary)

//            var data = new GeoLocationData();
//            data.BlockTableRecordId = msId;
//            data.PostToDb();

//            // We're going to define our geolocation in terms of
//            // latitude/longitude using the Mercator projection
//            // http://en.wikipedia.org/wiki/Mercator_projection

//            data.CoordinateSystem = "WORLD-MERCATOR";
//            data.TypeOfCoordinates = TypeOfCoordinates.CoordinateTypeGrid;

//            // Use the lat-long for La Tene, my local "beach"
//            // (it's on a lake, after all :-)     

//            var geoPt = new Point3d(7.019438, 47.005247, 0);

//            // Transform from a geographic to a modelspace point
//            // and add the information to our geolocation data

//            var wcsPt = data.TransformFromLonLatAlt(geoPt);
//            data.DesignPoint = wcsPt;
//            data.ReferencePoint = geoPt;

//            // Let's launch the GEOMAP command to show our geographic
//            // overlay

//            ed.Command("_.GEOMAP", "_AERIAL");

//            // Now we'll add a circle around our location
//            // and that will provide the extents for our zoom

//            using (var tr = db.TransactionManager.StartTransaction())
//            {
//                var ms =
//                  tr.GetObject(msId, OpenMode.ForWrite) as BlockTableRecord;
//                if (ms != null)
//                {
//                    // Add a red circle of 7K units radius
//                    // centred on our point

//                    var circle = new Circle(wcsPt, Vector3d.ZAxis, 7000);
//                    circle.ColorIndex = 1;
//                    ms.AppendEntity(circle);
//                    tr.AddNewlyCreatedDBObject(circle, true);
//                }
//                tr.Commit();
//            }

//            // And we'll zoom to the circle's extents

//            ed.Command("_.ZOOM", "_OBJECT", "_L", "");
//        }
//    }
//}