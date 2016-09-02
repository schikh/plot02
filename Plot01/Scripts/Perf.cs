using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoCADTest.Service;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using BatchPlot;

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

                var filePaths = Directory.GetFiles(@"C:\Test\Plot\Plot01\Files");
                //var serverFilePaths = GetServerFilePaths();
                //var filePaths = ImportServerFiles(serverFilePaths).ToArray();

                OpenFiles(filePaths);
                
                CreateAndConfigureLayout();

                var position = new Point3d(1800, 50, 0);
                AddPlotCartridge(_templateFilePath, position);

                _document.Editor.Regen();
                
                PlotExtents();
                
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
            _document.Editor.WriteMessage("\nARGUMENTS: " + string.Join("; ", args));

            _connectionString = "DATA SOURCE=WALLP1_UNWALL.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
            //_connectionString = "DATA SOURCE=WALLA1.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";

            _fileServerName = "RWA004";
            //_fileServerName = "RWA002AEST";
            _mapCoordinate = GetPointParameter("Get planchette position"); 
        }

        private List<string> GetServerFilePaths()
        {
            var da = new DataAccessService(_connectionString);
            var query = string.Format("SELECT path, fileName "
                + "FROM DESSIN "
                + "WHERE serveur = '{0}' "
                + "AND categorie = 'MAP' "
                + "AND path not like '%#%' "
                + "AND xmax >= {1} AND xmin <= {2} AND ymax >= {3} AND ymin <= {4} ",
                _fileServerName,
                _mapCoordinate.X,
                _mapCoordinate.X + 500,
                _mapCoordinate.Y,
                _mapCoordinate.Y + 250);
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

        private double OpenFiles(IEnumerable<string> filePaths)
        {
            var c = filePaths.Count();
            var i = 0;
            var time = Helper.GetExecutionTime(() =>
            {
                foreach (var filePath in filePaths)
                {
                    i++;
                    Helper.Trace("\n>>> OPEN FILE {1}/{2}   {0}", filePath, i, c);
                    OpenFile(filePath);
                    LogDrawingExtent();
                }
            });
            return time;
        }

        private void OpenFile(string filePath)
        {
            using (var transaction = _document.Database.TransactionManager.StartTransaction())
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
                    var br = new BlockReference(new Point3d(0, 0, 0), id);
                    btr.AppendEntity(br);
                    transaction.AddNewlyCreatedDBObject(br, true);
                }
                transaction.Commit();
            }
        }

        private void AddPlotCartridge(string templateFilePath, Point3d position)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                var layout = GetPlotLayout(tr);
                using (var btr = (BlockTableRecord) tr.GetObject(layout.BlockTableRecordId, OpenMode.ForWrite))
                {
                    var id = default(ObjectId);
                    using (var db = new Database(false, true))
                    {
                        db.ReadDwgFile(_templateFilePath, FileShare.Read, true, "");
                        var blockName = Path.GetFileNameWithoutExtension(templateFilePath);
                        id = _document.Database.Insert(blockName, db, true);
                    }
                    var br = new BlockReference(position, id);
                    btr.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                }
                tr.Commit();
            }
        }
        private void CreateAndConfigureLayout()
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var layout = CreatePlotLayout(tr))
                {
                    SetPlotSettings(layout, "UserDefinedMetric (1000.00 x 2000.00MM)", "monochrome.ctb", "PDF.pc3");
                    using (var viewport = GetOrCreateViewport(layout, tr, 2))
                    {
                        SetViewportSettings(viewport);
                    }
                }
                tr.Commit();
            }
        }

        private void SetViewportSettings(Viewport viewport)
        {
            viewport.Width = 1000;
            viewport.Height = 500;
            viewport.CenterPoint = new Point3d(600, 350, 0);

            viewport.ViewDirection = new Vector3d(0, 0, 1);
            viewport.ViewCenter = new Point2d(185000, 128250);
            //acVport.StandardScale = StandardScaleType.ScaleToFit;
            viewport.CustomScale = 2;

            viewport.Locked = true;
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

        private void SetPlotSettings(Layout layout, string pageSize, string styleSheet, string device)
        {
            using (var ps = new PlotSettings(layout.ModelType))
            {
                ps.CopyFrom(layout);
                var psv = PlotSettingsValidator.Current;
                var devs = psv.GetPlotDeviceList();
                if (devs.Contains(device))
                {
                    psv.SetPlotConfigurationName(ps, device, null);
                    psv.RefreshLists(ps);
                }
                var mns = psv.GetCanonicalMediaNameList(ps);
                if (mns.Contains(pageSize))
                {
                    psv.SetCanonicalMediaName(ps, pageSize);
                }
                var ssl = psv.GetPlotStyleSheetList();
                if (ssl.Contains(styleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, styleSheet);
                }
                layout.CopyFrom(ps);
            }
        }

        private Point2d GetPointParameter(string prompt)
        {
            var pr = _document.Editor.GetPoint("\n" + prompt + ":");
            if (pr.Status != PromptStatus.OK)
            {
                throw new ArgumentException("Invalid input for '" + prompt + "'");
            }
            return new Point2d(pr.Value.X, pr.Value.Y);
        }

        private string GetStringParameter(string prompt)
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

        private void PlotExtents()
        {
            var printer = "PDF.pc3";
            var format = "UserDefinedMetric (1000.00 x 2000.00MM)"; // "ANSI_A_(8.50_x_11.00_Inches)";
            var outputFilePath = @"C:\Test\Plot\Plot01\Scripts\dump2.pdf";

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
                psv.SetPlotConfigurationName(ps, printer, format);
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
                    pe.BeginDocument(pi, _document.Name, null, 1, true, outputFilePath);
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
}