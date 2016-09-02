//using Autodesk.AutoCAD.Runtime;
//using Autodesk.AutoCAD.ApplicationServices;
//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.EditorInput;
//using Autodesk.AutoCAD.Colors;

//namespace LayerCreation
//{
//    public class Commands
//    {
//        // For fun let's assign each new layer with a new colour

//        static short _colorIndex = 0;

//        [CommandMethod("CL")]
//        public void CreateLayer()
//        {
//            Document doc =
//              Application.DocumentManager.MdiActiveDocument;
//            Database db = doc.Database;
//            Editor ed = doc.Editor;

//            Transaction tr =
//              db.TransactionManager.StartTransaction();
//            using (tr)
//            {
//                // Get the layer table from the drawing

//                LayerTable lt =
//                  (LayerTable)tr.GetObject(
//                    db.LayerTableId,
//                    OpenMode.ForRead
//                  );

//                // Check the layer name, to see whether it's
//                // already in use

//                PromptStringOptions pso =
//                  new PromptStringOptions(
//                    "\nEnter new layer name: "
//                  );
//                pso.AllowSpaces = true;

//                // A variable for the layer name

//                string layName = "";

//                do
//                {
//                    PromptResult pr = ed.GetString(pso);

//                    // Just return if the user cancelled
//                    // (will abort the transaction as we drop out of the using
//                    // statement's scope)

//                    if (pr.Status != PromptStatus.OK)
//                        return;

//                    try
//                    {
//                        // Validate the provided symbol table name

//                        SymbolUtilityServices.ValidateSymbolName(
//                          pr.StringResult,
//                          false
//                        );

//                        // Only set the layer name if it isn't in use

//                        if (lt.Has(pr.StringResult))
//                            ed.WriteMessage(
//                              "\nA layer with this name already exists."
//                            );
//                        else
//                            layName = pr.StringResult;
//                    }
//                    catch
//                    {
//                        // An exception has been thrown, indicating the
//                        // name is invalid

//                        ed.WriteMessage(
//                          "\nInvalid layer name."
//                        );
//                    }

//                } while (layName == "");

//                // Create our new layer table record...

//                LayerTableRecord ltr = new LayerTableRecord();

//                // ... and set its properties

//                ltr.Name = layName;
//                ltr.Color =
//                  Color.FromColorIndex(ColorMethod.ByAci, _colorIndex);

//                // Add the new layer to the layer table

//                lt.UpgradeOpen();
//                ObjectId ltId = lt.Add(ltr);
//                tr.AddNewlyCreatedDBObject(ltr, true);

//                // Set the layer to be current for this drawing

//                db.Clayer = ltId;

//                // Commit the transaction

//                tr.Commit();

//                // Report what we've done

//                ed.WriteMessage(
//                  "\nCreated layer named \"{0}\" with " +
//                  "a color index of {1}.",
//                  layName, _colorIndex++
//                );
//            }
//        }
//    }
//}