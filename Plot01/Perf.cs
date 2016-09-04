using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using AutoCADTest.Service;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using BatchPlot;

//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\test.scr" /id 185127C /r 500 /isolate

[assembly: CommandClass(typeof(Perf))]
namespace BatchPlot
{
    public class Perf : IExtensionApplication
    {
        private string _plotLayoutName = "Energis_plot_layout_name";
        private string _templateFilePath = @"C:\\Test\\Plot\\Plot01\\Scripts\\Gis_cstd_Ouest.dwg";
        private string _fileServerName;
        private Point2d _mapCoordinate;
        private string _connectionString;
        private string _tempFolder;
        private Document _document = Application.DocumentManager.MdiActiveDocument;
        private Extents2d _drawingExtend;
        private Size _drawingSize;
        //private double _drawingScale;
        private PageSettings _pageSettings;
        private Point3d _plotCartridgePosition;
        private string _planchetteId;
        private double _planchetteScale;
        private int _externalBorderWidth = 10;
        private int _internalBorderWidth = 10;

        public void Initialize()
        {
            Logger.Setup();
        }

        public void Terminate()
        {
        }

        [CommandMethod("TestImport")]
        public void TestImport()
        {
            try
            {
                _document.Database.Insunits = UnitsValue.Millimeters;
                _tempFolder = Helper.CreateTempFolder();

                ParseParameters();

                _pageSettings = new PageSettings()
                {
                    PageSize = "Max (1000.00 x 2000.00 MM)",
                    StyleSheet = "acad.ctb",
                    Device = "PDF.pc3",
                    OutputFilePath = @"C:\Test\Plot\Plot01\Scripts\dump2.pdf"
                };

                var filePaths = Directory.GetFiles(@"C:\Test\Plot\Plot01\Files").Take(2);
                //var serverFilePaths = GetServerFilePaths();
                //var filePaths = ImportServerFiles(serverFilePaths).ToArray();

                OpenFiles(filePaths);

                //FillCartridgeInfo();

                CreateAndConfigureLayout(_pageSettings);

                AddPlotCartridge(_templateFilePath, _plotCartridgePosition);


                _document.Editor.Regen();

                AddRectangle();


                //// OK
                //var dwfPageSettings = new PageSettings()
                //{
                //    PageSize = "ISO_full_bleed_2A0_(1189.00_x_1682.00_MM)",
                //    StyleSheet = "Default.ctb",
                //    Device = "DWF6 ePlot.pc3",
                //    OutputFilePath = @"C:\Test\Plot\Plot01\Scripts\dump2.dwf"
                //};

                var dwfPageSettings = new PageSettings()
                {
                    PageSize = "UserDefinedMetric (1000.00 x 2000.00MM)", // "Max (1000.00 x 2000.00 MM)",
                    StyleSheet = "Default.ctb",
                    Device = "DWF6.pc3",
                    OutputFilePath = @"C:\Test\Plot\Plot01\Scripts\dump2.dwf"
                };
                PlotExtents(_pageSettings);

                SaveDwg(@"C:\Test\Plot\Plot01\Scripts\dump2.dwg");

                //DeleteImportedFiles();
            }
            catch (System.Exception ex)
            {
                Helper.Log("*** ERROR ************************************************************");
                Helper.Log(ex.ToString().Replace("\r\n", ";"));
                Helper.Log("**********************************************************************");
            }
        }

        private void ParseParameters()
        {
            var args = Environment.GetCommandLineArgs();
            Helper.Log("ARGUMENTS: " + string.Join("; ", args));

            _drawingSize = new Size(500, 250);

            _planchetteScale = GetPlanchetteScale();

            var position = _drawingSize.Width * _planchetteScale 
                + 2 * _internalBorderWidth 
                + 2 * _externalBorderWidth 
                + 10;
            _plotCartridgePosition = new Point3d(position, 0, 0);

            _planchetteId = GetCommandLineParameterValue("id");
            _mapCoordinate = ParsePlanchetteId(_planchetteId);

            _drawingExtend = new Extents2d(_mapCoordinate.X, _mapCoordinate.Y,
                _mapCoordinate.X + _drawingSize.Width, 
                _mapCoordinate.Y + _drawingSize.Height);

            _connectionString = "DATA SOURCE=WALLP1_UNWALL.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
            //_connectionString = "DATA SOURCE=WALLA1.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";

            _fileServerName = "RWA004";
            //_fileServerName = "RWA002AEST";
        }

        private List<string> GetServerFilePaths()
        {
            var da = new DataAccessService(_connectionString);
            var query = string.Format("SELECT path, fileName "
                + "FROM DESSIN "
                + "WHERE serveur = '{0}' "
                + "AND categorie = 'MAP' "
                + "AND ENERGIE NOT IN ('SYS','COMM','ELEC','GAZ','IC','RE','TPCD','TPDV','TPMD','TPRC') "
                + "AND path not like '%#%' "
                + "AND xmax >= {1} AND xmin <= {2} AND ymax >= {3} AND ymin <= {4} ",
                _fileServerName,
                _drawingExtend.MinPoint.X,
                _drawingExtend.MaxPoint.X,
                _drawingExtend.MinPoint.Y,
                _drawingExtend.MaxPoint.Y);
            var list = da.IterateOverReader(query, x => Path.Combine(x.GetString(0), x.GetString(1))).ToList();
            return list;
        }

        private IEnumerable<string> ImportServerFiles(IEnumerable<string> serverFilePaths)
        {
            foreach (var serverFilePath in serverFilePaths)
            {
                if (File.Exists(serverFilePath))
                {
                    var localFilePath = Helper.GetLocalFilePath(serverFilePath, _tempFolder);
                    File.Copy(serverFilePath, localFilePath, true);
                    yield return localFilePath;
                }
                else
                {
                    Helper.Trace("FILE NOT FOUND {0}", serverFilePath);
                }
            }
        }

        private void OpenFiles(IEnumerable<string> filePaths)
        {
            var c = filePaths.Count();
            var i = 0;
            foreach (var filePath in filePaths)
            {
                i++;
                Helper.Trace(">>> OPEN FILE {1}/{2}   {0}", filePath, i, c);
                OpenFile(filePath);
                LogDrawingExtent();
            }
        }

        private void OpenFile(string filePath)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var bt = (BlockTable)_document.Database.BlockTableId.GetObject(OpenMode.ForRead))
                using (var btr = (BlockTableRecord) bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite))
                {
                    var id = default(ObjectId);
                    using (var db = new Database(false, true))
                    {
                        db.ReadDwgFile(filePath, FileShare.Read, true, "");
                        var blockName = Path.GetFileNameWithoutExtension(filePath);
                        id = _document.Database.Insert(blockName, db, true);
                    }
                    var br = new BlockReference(Point3d.Origin, id);
                    btr.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    //using (DBObjectCollection dbObjCol = new DBObjectCollection())
                    //{
                    //    br.Explode(dbObjCol);
                    //    foreach (DBObject dbObj in dbObjCol)
                    //    {
                    //        Entity acEnt = dbObj as Entity;
                    //        btr.AppendEntity(acEnt);
                    //        tr.AddNewlyCreatedDBObject(dbObj, true);
                    //        Helper.Trace("\nExploded Object: " + acEnt.GetRXClass().DxfName);
                    //    }
                    //}
                }
                tr.Commit();
            }
        }

        private void AddPlotCartridge(string templateFilePath, Point3d position)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var layout = GetPlotLayout(tr))
                {
                    using (var btr = (BlockTableRecord) tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
                    {
                        ObjectId blockId;
                        using (var db = new Database(false, true))
                        {
                            db.ReadDwgFile(_templateFilePath, FileShare.Read, true, "");
                            var blockName = Path.GetFileNameWithoutExtension(templateFilePath);
                            blockId = _document.Database.Insert(blockName, db, true);
                        }

                        var values = GetCartridgeInfo(tr);

                        using (var blockDefinition = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead))
                        using (var br = new BlockReference(position, blockId))
                        {
                            btr.AppendEntity(br);
                            tr.AddNewlyCreatedDBObject(br, true);
                            blockDefinition.CopyAttributeDefinition(br, values);
                            //br.UpdateAttributes("COM", "XXXX RES XXXX");
                      
                            //br.UpdateAttributes("RES", "XXXX RES XXXX");
                            
                            //br.UpdateAttributes("OBJ1", "XXXX OBJ XXXX");
                            //br.UpdateAttributes("PL1", "XXXX PL1 XXXX");
                            //br.UpdateAttributes("PL2", "XXXX PL2 XXXX");
                            //br.UpdateAttributes("PL3", "XXXX PL3 XXXX");
                            //br.UpdateAttributes("PL4", "XXXX PL4 XXXX");
                            //br.UpdateAttributes("PL5", "XXXX PL5 XXXX");
                            //br.UpdateAttributes("PL6", "XXXX PL6 XXXX");
                            //br.UpdateAttributes("PL7", "XXXX PL7 XXXX");
                            //br.UpdateAttributes("PL8", "XXXX PL8 XXXX");
                            //br.UpdateAttributes("PL9", "XXXX PL9 XXXX");
                            //br.UpdateAttributes("ECH", "XXXX ECH XXXX");
                            //br.UpdateAttributes("DAT", "XXXX DAT XXXX");
                            //br.UpdateAttributes("DES", "XXXX DES XXXX");
                        }


                        //_document.Database.TransactionManager.QueueForGraphicsFlush();

                        //cartridge = (BlockReference)_document.Database.TransactionManager.TopTransaction.GetObject(result.CartridgeObjectId, OpenMode.ForWrite);
                        //cartridge.UpdateAttributes(new BlockAttributeValue[] { new BlockAttributeValue("RES", BuildLegend(bbRotate.MinSize)) });
                        //cartridge.UpdateAttributes(GetMapCartridgeAttribute(new string[] { "COM", "COM2" }, 2, 5, "", "Communes diverses", boundaryPoints, "EntityType=TEXT;LayerName=BL02,COMMUNES,COMMUNE,W0980"));
                        //cartridge.UpdateAttributes(GetMapCartridgeAttribute(
                        //new string[] { "RUE" }, 
                        //5, 15, "", "Rues diverses", 
                        //boundaryPoints, 
                        //"EntityType=TEXT;LayerName=BR11,BT02,BW03,RUE,W0989"));
                        //cartridge.UpdateAttributes(GetInsertCartridgeAttribute(new string[] { "MD" }, 5, "", boundaryPoints, "EntityType=INSERT;BlockName=GIS_MODF"));

                        //// Generation des Numero de planchettes se trouvant autour de la planchette active.
                        //cartridge.UpdateAttributes(GetPlanchette(new string[] { "PL" }, 9, "-", blockReference.GetAttribute("NUM", string.Empty)));
                    }
                }
                tr.Commit();
            }
        }

        private void AddRectangle()
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var layout = GetPlotLayout(tr))
                {
                    using (var btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
                    {
                        var x = 0;
                        var y = 0;
                        var width = _drawingSize.Width * _planchetteScale + 2 * _internalBorderWidth + 2 * _externalBorderWidth;
                        var height = _drawingSize.Height * _planchetteScale + 2 * _internalBorderWidth + 2 * _externalBorderWidth;
                        var rectangle = CreateRectangle(x, y, height, width);
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);

                        x = _externalBorderWidth;
                        y = _externalBorderWidth;
                        width = _drawingSize.Width * _planchetteScale + 2 * _internalBorderWidth;
                        height = _drawingSize.Height * _planchetteScale + 2 * _internalBorderWidth;
                        rectangle = CreateRectangle(x, y, height, width);
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);

                        x = _externalBorderWidth + _internalBorderWidth;
                        y = _externalBorderWidth + _internalBorderWidth;
                        width = _drawingSize.Width * _planchetteScale;
                        height = _drawingSize.Height * _planchetteScale;
                        rectangle = CreateRectangle(x, y, height, width);
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);
                    }
                }
                tr.Commit();
            }
        }

        private static Polyline CreateRectangle(int x, int y, double height, double width)
        {
            Polyline line;
            line = new Polyline();
            line.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            line.AddVertexAt(1, new Point2d(x, y + height), 0, 0, 0);
            line.AddVertexAt(2, new Point2d(x + width, y + height), 0, 0, 0);
            line.AddVertexAt(3, new Point2d(x + width, y), 0, 0, 0);
            line.AddVertexAt(4, new Point2d(x, y), 0, 0, 0);
            return line;
        }

        private IEnumerable<T> QueryEntities<T>(Transaction tr, IEnumerable<string> layers, Extents2d extend) where T : Entity
        {
            var bt = (IEnumerable<dynamic>)(dynamic)_document.Database.BlockTableId;
            var list = bt.SelectMany(x => (IEnumerable<dynamic>)x)
                .Where(x => x.IsKindOf(typeof(T)))
                .Select(x => (T)tr.GetObject(x, OpenMode.ForRead));
            if (layers != null)
            {
                list = list.Where(x => layers.Contains(x.Layer));
            }
            list = list.Where(x => x.Bounds.HasValue
                && x.Bounds.Value.MaxPoint.X >= extend.MinPoint.X
                && x.Bounds.Value.MinPoint.X <= extend.MaxPoint.X
                && x.Bounds.Value.MaxPoint.Y >= extend.MinPoint.Y
                && x.Bounds.Value.MinPoint.Y <= extend.MaxPoint.Y)
                .ToArray();
            return list;
        }

        private Dictionary<string, string> GetCartridgeInfo(Transaction tr)
        {
            var values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                
            //new string[] { "RUE" }, 
            //5, 15, "", "Rues diverses", 
            //boundaryPoints, 
            //"EntityType=TEXT;LayerName=BR11,BT02,BW03,RUE,W0989"));
            var i = 0;
            var layers = new[] { "BR11", "BT02", "BW03", "RUE", "W0989" };
            var list = QueryEntities<DBText>(tr, layers, _drawingExtend);
            list.Select(x => x.TextString)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(5) // 15
                .ToList()
                .ForEach(x => values.Add("RUE" + ++i, x));

            //cartridge.UpdateAttributes(GetMapCartridgeAttribute(
            //new string[] { "COM", "COM2" }, 
            //2, 5, "", "Communes diverses", 
            //boundaryPoints, 
            //"EntityType=TEXT;LayerName=BL02,COMMUNES,COMMUNE,W0980"));
            i = 1;
            layers = new[] { "BL02", "COMMUNES", "COMMUNE", "W0980" };
            list = QueryEntities<DBText>(tr, layers, _drawingExtend);
            list.Select(x => x.TextString)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(2) // 5
                .ToList()
                .ForEach(x => values.Add("COM" + ++i, x));

            i = 0;
            var list2 = QueryEntities<BlockReference>(tr, null, _drawingExtend)
                .Where(x => string.Equals(x.Name, "GIS_MODF")).ToArray();
            list2.Select(x => new {
                    date = ParseDate(x.GetAttribute("DATE", string.Empty)),
                    user = x.GetAttribute("USER", string.Empty),
                    desc = x.GetAttribute("DESC", string.Empty),
                })
                .Distinct()
                .OrderByDescending(x => x.date)
                .Take(5)
                .ToList()
                .ForEach(x => values.Add("MD" + ++i, string.Format("{0} {1} {2}", x.date, x.user, x.desc)));

            return values;
        }

        private DateTime? ParseDate(string text)
        {
            var culture = new CultureInfo("fr-FR");
            var formats = new [] { "dd/MM/yyyy", "dd/MM/yy" };
            foreach (var format in formats)
            {
                DateTime dateResult;
                var result = DateTime.TryParseExact(text, format, culture, DateTimeStyles.None, out dateResult);
                if (result)
                {
                    return dateResult;
                }
            }
            return null;
        }

        private void CreateAndConfigureLayout(PageSettings pageSettings)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var layout = CreatePlotLayout(tr))
                {
                    SetPlotSettings(layout, pageSettings);
                    using (var viewport = GetOrCreateViewport(layout, tr, 2))
                    {
                        SetViewportSettings(viewport);
                    }
                }
                tr.Commit();
            }

            //using (var tr = _document.Database.TransactionManager.StartTransaction())
            //{
            //    using (var bt = (BlockTable) tr.GetObject(_document.Database.BlockTableId, OpenMode.ForRead))
            //    using (var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite))
            //    {
            //        Application.SetSystemVariable("TILEMODE", 0);
            //        _document.Editor.SwitchToPaperSpace();
            //        using (var acVport = new Viewport())
            //        {
            //            btr.AppendEntity(acVport);
            //            tr.AddNewlyCreatedDBObject(acVport, true);
            //            SetViewportSettings(acVport);
            //           // _document.Editor.SwitchToModelSpace();
            //        }
            //    }
            //    tr.Commit();
            //}
        }

        private void SetViewportSettings(Viewport viewport)
        {
            viewport.Width = _drawingSize.Width * _planchetteScale + _internalBorderWidth * 2;
            viewport.Height = _drawingSize.Height * _planchetteScale + _internalBorderWidth * 2;
            viewport.CenterPoint = new Point3d(viewport.Width / 2 + _externalBorderWidth,
                viewport.Height / 2 + _externalBorderWidth, 0);

            viewport.ViewDirection = new Vector3d(0, 0, 1);
            viewport.ViewCenter = new Point2d((_drawingExtend.MinPoint.X + _drawingExtend.MaxPoint.X) / 2,
                (_drawingExtend.MinPoint.Y + _drawingExtend.MaxPoint.Y) / 2);
            //acVport.StandardScale = StandardScaleType.ScaleToFit;
            viewport.CustomScale = _planchetteScale;
            viewport.Locked = true;
            viewport.On = true;
        }

        private Layout GetPlotLayout(Transaction tr)
        {
            var id = LayoutManager.Current.GetLayoutId(_plotLayoutName);
            var layout = (Layout) tr.GetObject(id, OpenMode.ForRead);
            return layout;
        }

        private Layout CreatePlotLayout(Transaction tr)
        {
            var id = CreateAndMakeLayoutCurrent(_plotLayoutName);
            var layout = (Layout) tr.GetObject(id, OpenMode.ForWrite);
            return layout;
        }

        private ObjectId CreateAndMakeLayoutCurrent(string name)
        {
            var id = LayoutManager.Current.GetLayoutId(name);
            if (!id.IsValid)
            {
                id = LayoutManager.Current.CreateLayout(name);
            }
            LayoutManager.Current.CurrentLayout = name;
            return id;
        }

        private Viewport GetOrCreateViewport(Layout layout, Transaction tr, int vpNum)
        {
            var vp = GetViewport(layout, tr, vpNum);
            if (vp == null)
            {
                vp = CreateViewport(layout, tr);
            }
            return vp;
        }

        private Viewport GetViewport(Layout layout, Transaction tr, int vpNum)
        {
            var vpIds = layout.GetViewports();
            foreach (ObjectId vpId in vpIds)
            {
                var vp = tr.GetObject(vpId, OpenMode.ForWrite) as Viewport;
                if (vp != null && vp.Number == vpNum)
                {
                    return vp;
                }
            }
            return null;
        }

        private Viewport CreateViewport(Layout layout, Transaction tr)
        {
            var btr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite);
            var vp = new Viewport();
            btr.AppendEntity(vp);
            tr.AddNewlyCreatedDBObject(vp, true);
            vp.On = true;
            vp.GridOn = true;
            return vp;
        }

        private void SetPlotSettings(Layout layout, PageSettings pageSettings)
        {
            using (var ps = new PlotSettings(layout.ModelType))
            {
                ps.CopyFrom(layout);
                var psv = PlotSettingsValidator.Current;
                var devs = psv.GetPlotDeviceList();
                //if (devs.Contains(pageSettings.Device))
                {
                    psv.SetPlotConfigurationName(ps, pageSettings.Device, null);
                    psv.RefreshLists(ps);
                }
                pageSettings.CanonicalMediaName = GetCanonicalMediaName(psv, ps, pageSettings.PageSize);
                if (pageSettings.CanonicalMediaName == null)
                {
                    
                }
                psv.SetCanonicalMediaName(ps, pageSettings.CanonicalMediaName);
                var ssl = psv.GetPlotStyleSheetList();
                //if (ssl.Contains(pageSettings.StyleSheet))
                {
                    //psv.SetCurrentStyleSheet(ps, pageSettings.StyleSheet);
                }
                layout.CopyFrom(ps);
            }
        }

        private string GetCanonicalMediaName(PlotSettingsValidator psv, PlotSettings ps, string pageSize)
        {
            var medlist = psv.GetCanonicalMediaNameList(ps);
            for (var j = 0; j < medlist.Count; j++)
            {
                var name = psv.GetLocaleMediaName(ps, j);
                if (string.Equals(name, pageSize, StringComparison.InvariantCultureIgnoreCase))
                {
                    return medlist[j];
                }
            }
            return null;
        }

        private double GetPlanchetteScale()
        {
            var resolution = GetCommandLineParameterValue("r");
            int value;
            if (!int.TryParse(resolution, out value))
            {
                throw new ArgumentException("Resolution (r) not valid " + resolution);
            }
            var scale = 1000.0 / value;
            return scale;
        }

        private Point2d ParsePlanchetteId(string planchetteId)
        {
            var regex = new Regex(@"^(?<x>[0-9]{3})(?<y>[0-9]{3})(?<letter>[A-H])$");
            var match = regex.Match(planchetteId);
            if (!match.Success)
            {
                throw new ArgumentException("Planchette Id not valid " + planchetteId);
            }
            var x = int.Parse(match.Groups["x"].Value) * 1000;
            var y = int.Parse(match.Groups["y"].Value) * 1000;
            var letter = match.Groups["letter"].Value;
            var dx = 0;
            var dy = 0;
            if ("ACEG".Contains(letter))
            {
                dx = 0;
                dy = "ACEG".IndexOf(letter);
            }
            else
            {
                dx = 1;
                dy = "BDFH".IndexOf(letter);
            }
            y = Convert.ToInt32(y + dy * _drawingSize.Height);
            x = Convert.ToInt32(x + dx * _drawingSize.Height);
            return new Point2d(x, y);
        }

        //private Point2d GetPointParameter(string prompt)
        //{
        //    var pr = _document.Editor.GetPoint("\n" + prompt + ":");
        //    if (pr.Status != PromptStatus.OK)
        //    {
        //        throw new ArgumentException("Invalid input for '" + prompt + "'");
        //    }
        //    return new Point2d(pr.Value.X, pr.Value.Y);
        //}

        private string GetPlanchetteId()
        {
            return PromptStringParameter("Get planchette position");
        }

        private string GetCommandLineParameterValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.FindIndex(args, t => t.Equals(@"/" + name, StringComparison.InvariantCultureIgnoreCase));
            if (i >= 0)
            {
                return args[i + 1].ToUpper();
            }
            return null;
        }

        private string PromptStringParameter(string prompt)
        {
            var pr = _document.Editor.GetString("\n" + prompt + ":");
            if (pr.Status != PromptStatus.OK)
            {
                throw new ArgumentException("Invalid input for '" + prompt + "'");
            }
            return pr.StringResult.ToUpper();
        }

        private void DeleteImportedFiles()
        {
            try
            {
                Helper.DeleteTempFolder(_tempFolder);
            }
            catch (System.Exception ex)
            {
                Helper.Log(ex.ToString());
            }
        }

        private void LogDrawingExtent()
        {
            var extMin = (Point3d)Application.GetSystemVariable("EXTMIN");
            var extMax = (Point3d)Application.GetSystemVariable("EXTMAX");
            var text = string.Format("        ({0},{1})   ({2},{3})\n", extMin.X, extMin.Y, extMax.X, extMax.Y);
            _document.Editor.WriteMessage(text);
        }

        private void SaveDwg(string filePath)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            document.Database.SaveAs(filePath, true, DwgVersion.Current, document.Database.SecurityParameters);
        }

        private void PlotExtents(PageSettings pageSettings)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                var db = (DBDictionary)tr.GetObject(_document.Database.LayoutDictionaryId, OpenMode.ForRead);
                var layoutId = db.GetAt(_plotLayoutName);
                var layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                var ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);
                ps.PlotPlotStyles = true;

                var psv = PlotSettingsValidator.Current;
                psv.SetDefaultPlotConfig(ps);
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                //psv.SetPlotWindowArea(ps, new Extents2d(new Point2d(150000, 150000), new Point2d(190000, 150000)));
                //psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                psv.SetUseStandardScale(ps, true);
                //psv.SetPlotCentered(ps, true);
                psv.SetPlotOrigin(ps, new Point2d(100, 10));
                psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);
                var extent2d = ps.PlotPaperMargins;
                //if (extent2d.MaxPoint.Y > extent2d.MaxPoint.X)
                //{
                //    psv.SetPlotRotation(ps, PlotRotation.Degrees000);
                //}
                //else
                //{
                //    psv.SetPlotRotation(ps, PlotRotation.Degrees090);
                //}
                //psv.SetPlotConfigurationName(ps, printer, null);
                psv.SetPlotConfigurationName(ps, pageSettings.Device, pageSettings.CanonicalMediaName);
                psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);

                var pi = new PlotInfo();
                pi.Layout = layoutId;
                pi.OverrideSettings = ps;

                var piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                using (var pe = PlotFactory.CreatePublishEngine())
                using (var ppi = new PlotPageInfo())
                {
                    pe.BeginPlot(null, null);
                    pe.BeginDocument(pi, _document.Name, null, 1, true, pageSettings.OutputFilePath);
                    pe.BeginPage(ppi, pi, true, null);
                    pe.BeginGenerateGraphics(null);
                    pe.EndGenerateGraphics(null);
                    pe.EndPage(null);
                    pe.EndDocument(null);
                    pe.EndPlot(null);
                }

                tr.Commit();
            }
        }

        [CommandMethod("ListPaperSizes")]
        private void ListPaperSizes()
        {
            Transaction tr = _document.Database.TransactionManager.StartTransaction();
            using (tr)
            {
                string devname = "", medname = "";
                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                var devlist = psv.GetPlotDeviceList();
                for (int i = 0; i < devlist.Count; i++)
                {
                    devname = devlist[i];
                    Helper.Log("\nSelected: {0}\n", devname);

                    PlotSettings ps = new PlotSettings(true);
                    using (ps)
                    {
                        psv.SetPlotConfigurationName(ps, devname, null);
                        psv.RefreshLists(ps);
                        var medlist = psv.GetCanonicalMediaNameList(ps);
                        for (int j = 0; j < medlist.Count; j++)
                        {
                            Helper.Log("    {0}   {1}   {2}    {3}", devname, j + 1, medlist[j], psv.GetLocaleMediaName(ps, j));
                        }
                    }
                }
            }
        }




        ////Tests the following scenario:
        //// theLayout is configured: Plot To PDF, sized custom media 7A4 (== w=7X210, h=297)
        //// when changing to another device, the media 7A4 must be added to that device(pc3), 
        ////      as the "previous media size"
        //// it appeares to work only when the new device is a PC3 device AND
        ////    it already contains a media sized larger than the "previous media size", like 2500x841
        //// We can make a PC3 file for a printer, change \\printserver\printername into printername.pc3, 
        ////    however you cannot attach a PMP file to the PC3 file.
        //// The PMP file contains the non standard media size (among other settings) and thus this will work only for
        //// already defined PC3 devices with PMP files and one custom media size larger than the required media size.   
        //[CommandMethod("Plt")]
        //public void PlotWithPlotStyle()
        //{
        //    Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    //string psetupPathname = @"C:\Apldata\AutoCAD\AcadConfig_INFRA\Menu\psetup-PDF.dwg";
        //    //string plotStyleName = "4Z-PDF";
        //    string printerName = "TDS600_Beta.pc3";

        //    using (Transaction tr = doc.TransactionManager.StartTransaction())
        //    {

        //        db.TileMode = false;
        //        ed.SwitchToPaperSpace();

        //        LayoutManager layMgr = LayoutManager.Current;
        //        Layout theLayout = (Layout)layMgr.GetLayoutId(layMgr.CurrentLayout).GetObject(OpenMode.ForWrite);
        //        PlotSettingsValidator psVal = Autodesk.AutoCAD.DatabaseServices.PlotSettingsValidator.Current;
        //        PlotSettings plotSet = new PlotSettings(theLayout.ModelType);
        //        plotSet.CopyFrom(theLayout);
        //        //psVal.SetPlotConfigurationName(plotSet, printerName, null);

        //        PlotConfigManager.SetCurrentConfig(printerName);
        //        PlotConfigManager.RefreshList(RefreshCode.All);
        //        PlotConfig tds = PlotConfigManager.CurrentConfig;
        //        string plotFile = null;
        //        if (tds.PlotToFileCapability == PlotToFileCapability.PlotToFileAllowed)
        //        {
        //            tds.IsPlotToFile = true;
        //            plotFile = Path.Combine(Path.GetDirectoryName(Application.DocumentManager.MdiActiveDocument.Database.Filename), Path.GetFileNameWithoutExtension(Application.DocumentManager.MdiActiveDocument.Database.Filename));
        //            plotFile = plotFile + tds.DefaultFileExtension;
        //            if (File.Exists(plotFile))
        //                File.Delete(plotFile);

        //        }

        //        //the next lines will fail because the required PMP file will get referenced by the PC3 
        //        //string pc3Dir = @"C:\Documents and Settings\avb\Application Data\Autodesk\AutoCAD 2012 - English\R18.2\enu\Plotters";
        //        //printerName = printerName + ".pc3";
        //        //tds.SaveToPC3(Path.Combine(pc3Dir, printerName ));
        //        //PlotConfigManager.SetCurrentConfig(printerName);
        //        //PlotConfigManager.RefreshList(RefreshCode.All);
        //        //tds = PlotConfigManager.CurrentConfig;

        //        //psVal.RefreshLists(plotSet);
        //        //try
        //        //{
        //        //    psVal.SetClosestMediaName(plotSet, plotSet.PlotPaperSize[0], plotSet.PlotPaperSize[1], PlotPaperUnit.Millimeters, true);
        //        //}
        //        //catch ( Autodesk.AutoCAD.Runtime.Exception exx ) 
        //        //{
        //        //    ed.WriteMessage("\n" + exx.ToString());
        //        //}

        //        psVal.SetPlotPaperUnits(plotSet, PlotPaperUnit.Millimeters);

        //        //theLayout.CopyFrom(plotSet);
        //        ed.Regen();


        //        PlotInfo plotInfo = new PlotInfo();
        //        plotInfo.Layout = theLayout.ObjectId;
        //        plotInfo.OverrideSettings = plotSet;
        //        plotInfo.DeviceOverride = tds;

        //        PlotInfoValidator validator = new PlotInfoValidator();
        //        //int itIs = validator.IsCustomPossible(plotInfo);
        //        validator.MediaMatchingPolicy = Autodesk.AutoCAD.PlottingServices.MatchingPolicy.MatchEnabledCustom;
        //        int itIs = validator.IsCustomPossible(plotInfo);
        //        validator.Validate(plotInfo);


        //        //for now see the results
        //        if (plotInfo.IsValidated && itIs == 0)
        //        {
        //            PlotSettings newNamedPlotStyle = new PlotSettings(theLayout.ModelType);
        //            newNamedPlotStyle.CopyFrom(plotInfo.ValidatedSettings);
        //            theLayout.CopyFrom(newNamedPlotStyle);

        //            newNamedPlotStyle.PlotSettingsName = "7A4-TDS600";
        //            psVal.RefreshLists(newNamedPlotStyle);
        //            newNamedPlotStyle.AddToPlotSettingsDictionary(db);
        //            tr.AddNewlyCreatedDBObject(newNamedPlotStyle, true);
        //            psVal.RefreshLists(newNamedPlotStyle);

        //            ed.Regen();
        //            tr.Commit();
        //            return;
        //        }
        //        PlotEngine plotEngine = PlotFactory.CreatePublishEngine();
        //        //None (initial) -> plot -> document -> page -> graphics
        //        try
        //        {
        //            plotEngine.BeginPlot(null, null);
        //            if (tds.IsPlotToFile)
        //                plotEngine.BeginDocument(plotInfo, Application.DocumentManager.MdiActiveDocument.Database.Filename, null, 1, true, plotFile);
        //            else
        //                plotEngine.BeginDocument(plotInfo, Application.DocumentManager.MdiActiveDocument.Database.Filename, null, 1, false, null);

        //            PlotPageInfo pageInfo = new PlotPageInfo();
        //            ed.WriteMessage("\nPlotting {0} Entities, {1} ", pageInfo.EntityCount, pageInfo.RasterCount);
        //            plotEngine.BeginPage(pageInfo, plotInfo, true, null);
        //            plotEngine.BeginGenerateGraphics(null);
        //            plotEngine.EndGenerateGraphics(null);
        //            plotEngine.EndPage(null);
        //            plotEngine.EndDocument(null);
        //            plotEngine.EndPlot(null);
        //        }
        //        catch (System.Exception ex)
        //        {
        //            ed.WriteMessage(ex.Message);
        //        }
        //        plotEngine.Destroy();

        //        tr.Commit();
        //    }
        //}



    }

    public class PageSettings
    {
        public string PageSize { get; set; }
        public string StyleSheet { get; set; }
        public string Device { get; set; }
        public string OutputFilePath { get; set; }
        public string CanonicalMediaName { get; set; }
    }



    public static class BlockReferenceExtension
    {
        // new KeyValuePair<string,string>("defaultkey", "defaultvalue");
        // new Dictionary<string, CustomClass>(StringComparer.InvariantCultureIgnoreCase)
        public static void UpdateAttributes(this BlockReference blockReference, Dictionary<string, string> values)
        {

            var tr = blockReference.Database.TransactionManager.TopTransaction;
            foreach (ObjectId attribute in blockReference.AttributeCollection)
            {
                if (!attribute.IsErased)
                {
                    using (var reference = (AttributeReference)tr.GetObject(attribute, OpenMode.ForRead))
                    {
                        if (reference != null && values.ContainsKey(reference.Tag))
                        {
                            reference.UpgradeOpen();
                            reference.TextString = values[reference.Tag];
                            reference.AdjustAlignment(blockReference.Database);
                        }
                    }
                }
            }
        }

        public static void CopyAttributeDefinition(this BlockTableRecord tableRecord, BlockReference reference, Dictionary<string, string> values)
        {
            var tr = tableRecord.Database.TransactionManager.TopTransaction;
            if (tableRecord.HasAttributeDefinitions)
            {
                foreach (var objectId in tableRecord)
                {
                    //using (var dbObject = tr.GetObject(objectId, OpenMode.ForRead)) //objectId.GetObject(OpenMode.ForRead);
                    using (var dbObject = objectId.GetObject(OpenMode.ForRead))
                    {
                        var attrDef = dbObject as AttributeDefinition;
                        if (attrDef != null && !attrDef.Constant)
                        {
                            using (var attrRef = new AttributeReference())
                            {
                                attrRef.SetAttributeFromBlock(attrDef, reference.BlockTransform);
                                attrRef.Position = attrDef.Position.TransformBy(reference.BlockTransform);
                                if (values.ContainsKey(attrDef.Tag))
                                {
                                    attrRef.TextString = values[attrDef.Tag]; // "XXXXXX"; //attrDef.TextString;
                                }
                                reference.AttributeCollection.AppendAttribute(attrRef);
                                tr.AddNewlyCreatedDBObject(attrRef, true);
                            }
                        }
                    }
                }
            }
        }

        public static T GetAttribute<T>(this BlockReference blockReference, string attribute, T defaultValue)
        {
            var tr = blockReference.Database.TransactionManager.TopTransaction;
            var returnValue = defaultValue;
            foreach (ObjectId current in blockReference.AttributeCollection)
            {
                if (!current.IsErased)
                {
                    using (var dbObject = tr.GetObject(current, OpenMode.ForRead))
                    {
                        var attributeDefinition = dbObject as AttributeReference;
                        if (attributeDefinition != null 
                            && attributeDefinition.Tag.Equals(attribute, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var stringValue = attributeDefinition.IsMTextAttribute
                                ? attributeDefinition.MTextAttribute.Text
                                : attributeDefinition.TextString;
                            var converter = TypeDescriptor.GetConverter(typeof(T));
                            returnValue = (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, stringValue);
                        }
                    }
                }
            }
            return returnValue;
        }
    }
}



//private List<ObjectId> GetCertainEntityIDs(Database db)
//{
//    List<ObjectId> ids = null;
 
//    using (Transaction tran = db.TransactionManager.StartTransaction())
//    {
//        BlockTable tbl = 
//            (BlockTable)tran.GetObject(db.BlockTableId, OpenMode.ForRead);
 
//        //Get modelspace BloclTableRecord
//        BlockTableRecord br = 
//            (BlockTableRecord)tran.GetObject(tbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);
 
//        //Cast the BlockTableRecord into IEnumeralbe<T> collection 
//        IEnumerable<ObjectId> b = br.Cast<ObjectId>();
 
//        //==============search certain entity========================//
//        //"LINE" for line
//        //"LWPOLYLINE" for polyline
//        //"CIRCLE" for circle
//        //"INSERT" for block reference
//        //...
//        //We can use "||" (or) to search for more then one entity types
//        //============================================================//
 
//        //Use lambda extension method
//        ids = b.Where(id => id.ObjectClass.DxfName.ToUpper() == "LINE" || 
//            id.ObjectClass.DxfName.ToUpper() == "LWPOLYLINE").ToList<ObjectId>();
 
//        //Use LINQ statement. This is more readable
//        ids = (from id in b
//                where id.ObjectClass.DxfName.ToUpper()=="LINE" ||
//                        id.ObjectClass.DxfName.ToUpper() == "LWPOLYLINE"
//                select id).ToList<ObjectId>();
 
//        tran.Commit();
//    }
 
//    return ids;
//}



//public void FindText(string str)
//{
//    using (Transaction tr = _document.Database.TransactionManager.StartTransaction())
//    {
//        dynamic bt = _document.Database.BlockTableId;

//        var list = (from btrs in (IEnumerable<dynamic>) bt
//            from ent in (IEnumerable<dynamic>) btrs
//            where
//                (ent.IsKindOf(typeof(DBText)) && ent.TextString.Contains(str)) ||
//                (ent.IsKindOf(typeof(MText)) && ent.Contents.Contains(str))
//            select ent).ToArray();

//        foreach (var e in list.Where(x => x.IsKindOf(typeof(MText))))
//        {
//            var blkEnt = (MText)tr.GetObject(e, OpenMode.ForRead);
//            Helper.Trace(
//                "\n-------------------- MText {6}   {0}   {1}   {2},{3}   {4},{5}   {7}",
//                blkEnt.GetRXClass().DxfName,
//                blkEnt.Layer,
//                blkEnt.GeometricExtents.MaxPoint.X,
//                blkEnt.GeometricExtents.MaxPoint.Y,
//                blkEnt.GeometricExtents.MinPoint.X,
//                blkEnt.GeometricExtents.MinPoint.Y,
//                "",
//                blkEnt.Contents);
//        }
//        foreach (var e in list.Where(x => x.IsKindOf(typeof(DBText))))
//        {
//            var blkEnt = (DBText)tr.GetObject(e, OpenMode.ForRead);
//            Helper.Trace(
//                "\n-------------------- DBText {6}   {0}   {1}   {2},{3}   {4},{5}   {7}",
//                blkEnt.GetRXClass().DxfName,
//                blkEnt.Layer,
//                blkEnt.GeometricExtents.MaxPoint.X,
//                blkEnt.GeometricExtents.MaxPoint.Y,
//                blkEnt.GeometricExtents.MinPoint.X,
//                blkEnt.GeometricExtents.MinPoint.Y,
//                "",
//                blkEnt.TextString);
//        }
//    }
//}

//private void GetEntityType()
//{
//    using (Transaction tr = _document.Database.TransactionManager.StartTransaction())
//    {
//        BlockTable tbl = (BlockTable)tr.GetObject(_document.Database.BlockTableId, OpenMode.ForRead);
//        BlockTableRecord br = (BlockTableRecord)tr.GetObject(tbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);
//        IEnumerable<ObjectId> b = br.Cast<ObjectId>();
//        //types = b.Select(id => id.ObjectClass.DxfName).Distinct().ToArray();
//        foreach (var blkEntId in b)
//        {
//            var blkEnt = (Entity)tr.GetObject(blkEntId, OpenMode.ForRead);
//            //if (!layers.Contains(blkEnt.Layer)) continue; // && entityTypes.Contains(btRecord.Name)
//            //if (blkEnt.GetRXClass().DxfName != "MTEXT") continue;
//            if (blkEnt != null)
//            {
//                Helper.Trace("\n-------------------- {6}   {0}   {1}   {2},{3}   {4},{5}   {7}",
//                    blkEnt.GetRXClass().DxfName,
//                    blkEnt.Layer,
//                    blkEnt.GeometricExtents.MaxPoint.X,
//                    blkEnt.GeometricExtents.MaxPoint.Y,
//                    blkEnt.GeometricExtents.MinPoint.X,
//                    blkEnt.GeometricExtents.MinPoint.Y,
//                    br.Name,
//                    blkEnt.ToString());
//            }

//        }
//        tr.Commit();
//    }
//}


//private void BlockIterator_Method()
//{
//    var layers = new [] { "BR11","BT02","BW03","RUE","W0989" };
//    var entityTypes = new [] { "TEXT" };
//    using (var tr = _document.Database.TransactionManager.StartTransaction())
//    {
//        var blkTable = (BlockTable)tr.GetObject(_document.Database.BlockTableId, OpenMode.ForRead);
//        foreach (var id in blkTable)
//        {
//            var btRecord = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
//            //if (!btRecord.IsLayout)
//            {
//                //if (btRecord.Name.StartsWith("*")) continue;
//                //Helper.Trace("\nBlock name: {0}   {1}   {2}", btRecord.Name, btRecord.PathName, btRecord.IsLayout);

//                //var blk = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
//                foreach (var blkEntId in btRecord)
//                {
//                    var blkEnt = (Entity)tr.GetObject(blkEntId, OpenMode.ForRead);
//                    if (!layers.Contains(blkEnt.Layer)) continue; // && entityTypes.Contains(btRecord.Name)
//                    if (blkEnt.GetRXClass().DxfName != "MTEXT") continue;
//                    var entity = blkEnt as MText;
//                    if (entity != null)
//                    {
//                        Helper.Trace("\n-------------------- {6}   {0}   {1}   {2},{3}   {4},{5}   {7}",
//                            blkEnt.GetRXClass().DxfName,
//                            blkEnt.Layer,
//                            blkEnt.GeometricExtents.MaxPoint.X,
//                            blkEnt.GeometricExtents.MaxPoint.Y,
//                            blkEnt.GeometricExtents.MinPoint.X,
//                            blkEnt.GeometricExtents.MinPoint.Y,
//                            btRecord.Name,
//                            entity.Text);
//                    }

//                }
//            }
//        }

//        tr.Commit();
//    }
//}