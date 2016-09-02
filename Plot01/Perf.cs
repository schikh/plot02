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
                    PageSize = "UserDefinedMetric (1000.00 x 2000.00MM)",   // "Max (1000.00 x 2000.00MM)",
                    StyleSheet = "Default.ctb",
                    Device = "PDF.pc3",
                    OutputFilePath = @"C:\Test\Plot\Plot01\Scripts\dump2.pdf"
                };

                var filePaths = Directory.GetFiles(@"C:\Test\Plot\Plot01\Files");
                //var serverFilePaths = GetServerFilePaths();
                //var filePaths = ImportServerFiles(serverFilePaths).ToArray();

                OpenFiles(filePaths);

                //FillCartridgeInfo();

                CreateAndConfigureLayout(_pageSettings);

                AddPlotCartridge(_templateFilePath, _plotCartridgePosition);

                _document.Editor.Regen();

                //PlotExtents(_pageSettings);

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
                Helper.Log(ex.ToString());
            }
        }

        private void ParseParameters()
        {
            var args = Environment.GetCommandLineArgs();
            Helper.Log("ARGUMENTS: " + string.Join("; ", args));

            _drawingSize = new Size(500, 250);

            _planchetteScale = GetPlanchetteScale();

            _plotCartridgePosition = new Point3d(_drawingSize.Width * _planchetteScale + 70, 50, 0);

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

                        var values = GetCartridgeInfo();

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

        private Dictionary<string, string> GetCartridgeInfo()
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
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
            viewport.Width = _drawingSize.Width * _planchetteScale;
            viewport.Height = _drawingSize.Height * _planchetteScale;
            viewport.CenterPoint = new Point3d(viewport.Width / 2 + 50, viewport.Height / 2 + 50, 0);

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

                //Thread.Sleep(180000);
                var mns = psv.GetCanonicalMediaNameList(ps);
                //if (mns.Contains(pageSettings.PageSize))
                {
                    psv.SetCanonicalMediaName(ps, pageSettings.PageSize);
                }
                var ssl = psv.GetPlotStyleSheetList();
                //if (ssl.Contains(pageSettings.StyleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, pageSettings.StyleSheet);
                }
                layout.CopyFrom(ps);
            }
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
                psv.SetPlotOrigin(ps, new Point2d(10, 10));
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
                psv.SetPlotConfigurationName(ps, pageSettings.Device, pageSettings.PageSize);
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
    }

    public class PageSettings
    {
        public string PageSize { get; set; }
        public string StyleSheet { get; set; }
        public string Device { get; set; }
        public string OutputFilePath { get; set; }
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