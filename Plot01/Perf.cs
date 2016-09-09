using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using BatchPlot;
using BatchPlot.Extensions;
using BatchPlot.Services;

//    "C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\test.scr" /id 184128H /r 500 /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /isolate

[assembly: CommandClass(typeof(Perf))]
namespace BatchPlot
{
    public class Perf : IExtensionApplication
    {
        private string _tempFolder;
        private readonly Document _document = Application.DocumentManager.MdiActiveDocument;
        private PageSettings _pageSettings;

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

                var args = Environment.GetCommandLineArgs();
                Helper.Log("ARGUMENTS: " + string.Join(" ", args));
                _pageSettings = new PageSettings(args);

                var filePaths = Directory.GetFiles(@"C:\Test\Plot\Plot01\Files").Take(2);
                //var serverFilePaths = GetServerFilePaths(_pageSettings.Categories,
                //    _pageSettings.Energies);
                //var filePaths = ImportServerFiles(serverFilePaths).ToArray();

                OpenFiles(filePaths);

                CreateAndConfigureLayout(_pageSettings);

                AddPlotCartridge(Configuration.PemplateFilePath, _pageSettings.PlotCartridgePosition);

                AddRectangle();

                PlotExtents(_pageSettings);

                //SaveDwg(@"C:\Test\Plot\Plot01\Scripts\dump2.dwg");

                //DeleteImportedFiles();
            }
            catch (System.Exception ex)
            {
                Helper.Log("*** ERROR ************************************************************");
                Helper.Log(ex.ToString().Replace("\r\n", ";"));
                Helper.Log("**********************************************************************");
            }
        }

        private List<string> GetServerFilePaths(string[] categories, string[] energies)
        {
/*
'NETGIS', null    TOPO, DEFAULT_ENERGIE
'CAPGIS', null    TOPO, DEFAULT_ENERGIE
'RS',     null,   TOPO, IC
'OX',     null,   OX
'DZ',     null,   DZ

'NETGIS', '*'     TOPO, *
'*',      '*'     TOPO, *
*/

            var da = new DataAccessService(Configuration.ConnectionString);
/*
SELECT path, fileName 
FROM DESSIN 
WHERE serveur = 'RWA004' 
AND categorie = 'MAP' 
--AND ENERGIE NOT IN ('SYS','COMM','ELEC','GAZ','IC','RE','TPCD','TPDV','TPMD','TPRC') 
AND ( 
    ENERGIE IN (
      SELECT DISTINCT ENERGY 
      FROM PLOTSRV_REQENERGIEGROUP
      WHERE GROUPNAME IN('TOPO', 'DEFAULT_ENERGIE', 'GAZ', 'MP', 'BP')
    )
    OR
    ENERGIE IN('TOPO', 'DEFAULT_ENERGIE', 'GAZ', 'MP', 'BP')
)
AND path not like '%#%' 
AND xmax >= 185000 AND xmin <= 186000 AND ymax >= 127000 AND ymin <= 128000
*/
            var query = string.Format("SELECT path, fileName "
                + "FROM DESSIN "
                + "WHERE serveur = '{0}' "
                + "AND categorie = '{5}' "
//              + "AND ENERGIE NOT IN ('SYS','COMM','ELEC','GAZ','IC','RE','TPCD','TPDV','TPMD','TPRC') "
                + "AND ( "  
                + "    ENERGIE IN ( "
                + "        SELECT DISTINCT ENERGY " 
                + "        FROM PLOTSRV_REQENERGIEGROUP "
                + "        WHERE GROUPNAME IN('{6}') "
                + "    ) "
                + "    OR "
                + "    ENERGIE IN('{6}') "
                + ") "
                + "AND path not like '%#%' "
                + "AND xmax >= {1} AND xmin <= {2} AND ymax >= {3} AND ymin <= {4} ",
                Configuration.FileServerName,
                _pageSettings.DrawingExtend.MinPoint.X,
                _pageSettings.DrawingExtend.MaxPoint.X,
                _pageSettings.DrawingExtend.MinPoint.Y,
                _pageSettings.DrawingExtend.MaxPoint.Y,
                string.Join("','", categories),
                string.Join("','", energies));
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
                Helper.Trace("OPEN FILE {1}/{2}   {0}", filePath, i, c);
                OpenFile(filePath);
            }
        }

        private void OpenFile(string filePath)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                using (var bt = (BlockTable)_document.Database.BlockTableId.GetObject(OpenMode.ForRead))
                using (var btr = (BlockTableRecord) bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite))
                {
                    ObjectId id;
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
                            db.ReadDwgFile(templateFilePath, FileShare.Read, true, "");
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
                        }
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
                        var width = Configuration.DrawingSize.Width * _pageSettings.Scale
                            + 2 * Configuration.InternalBorderWidth
                            + 2 * Configuration.ExternalBorderWidth;
                        var height = Configuration.DrawingSize.Height * _pageSettings.Scale
                            + 2 * Configuration.InternalBorderWidth
                            + 2 * Configuration.ExternalBorderWidth;
                        var rectangle = CreateRectangle(x, y, height, width);
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);

                        x = Configuration.ExternalBorderWidth;
                        y = Configuration.ExternalBorderWidth;
                        width = Configuration.DrawingSize.Width * _pageSettings.Scale
                            + 2 * Configuration.InternalBorderWidth;
                        height = Configuration.DrawingSize.Height * _pageSettings.Scale
                            + 2 * Configuration.InternalBorderWidth;
                        rectangle = CreateRectangle(x, y, height, width);
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);

                        x = Configuration.ExternalBorderWidth + Configuration.InternalBorderWidth;
                        y = Configuration.ExternalBorderWidth + Configuration.InternalBorderWidth;
                        width = Configuration.DrawingSize.Width * _pageSettings.Scale;
                        height = Configuration.DrawingSize.Height * _pageSettings.Scale;
                        rectangle = CreateRectangle(x, y, height, width);
                        rectangle.ColorIndex = 9;
                        btr.AppendEntity(rectangle);
                        tr.AddNewlyCreatedDBObject(rectangle, true);
                    }
                }
                tr.Commit();
            }
        }

        private static Polyline CreateRectangle(int x, int y, double height, double width)
        {
            var line = new Polyline();
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
            var list = QueryEntities<DBText>(tr, layers, _pageSettings.DrawingExtend);
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
            i = 0;
            layers = new[] { "BL02", "COMMUNES", "COMMUNE", "W0980" };
            list = QueryEntities<DBText>(tr, layers, _pageSettings.DrawingExtend);
            list.Select(x => x.TextString)
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .Take(2) // 5
                .ToList()
                .ForEach(x => values.Add(i == 0 ? "COM" : "COM" + ++i, x));

            i = 0;
            var list2 = QueryEntities<BlockReference>(tr, null, _pageSettings.DrawingExtend)
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
                .ForEach(x => values.Add("MD" + ++i, string.Format("{0:dd/MM/yyyy}-{1}-{2}", x.date, x.user, x.desc)));

            i = 0;
            GetSurroundedPlanchetteIds(_pageSettings.MapCoordinate, _pageSettings.PlanchetteLetter)
                .ToList()
                .ForEach(x => values.Add("PL" + ++i, x));

            values.Add("OBJ1", "XXXX Situation des installations XXXX");
            values.Add("ECH", string.Format("1/{0}", _pageSettings.Resolution));
            values.Add("DAT", DateTime.Now.ToString("dd.MM.yy (HH:mm)"));
            values.Add("DES", "XXXX DES XXXX");
            values.Add("NUM", values["PL5"]);

            return values;
        }

        private IEnumerable<string> GetSurroundedPlanchetteIds(Point2d planchettePosition, string planchetteLetter)
        {
            var letters = "ABCDEFGH";
            var l1 = letters.IndexOf(planchetteLetter, StringComparison.InvariantCultureIgnoreCase);
            var dx = new int[3];
            if("ACEG".Contains(planchetteLetter)) 
                dx[0] = -1;
            else 
                dx[2] = 1;
            var dy = new int[3];
            if ("AB".Contains(planchetteLetter)) dy[2] = -1;
            if ("GH".Contains(planchetteLetter)) dy[0] = 1;
            var lettersOffset = new int[3, 3] { { 3, 1, 7 }, { 2, 0, 6 }, { 3, 1, 7 } };
            for (var y = 2; y >= 0; y--)
            {
                for (var x = 0; x < 3; x++)
                {
                    var l2 = letters[(l1 + lettersOffset[x, y]) % 8];
                    var px = Math.Truncate(planchettePosition.X / 1000) + dx[x];
                    var py = Math.Truncate(planchettePosition.Y / 1000) + dy[y];
                    yield return string.Format("{0}{1}{2}", px, py, l2);
                }
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
        }

        private void SetViewportSettings(Viewport viewport)
        {
            viewport.Width = Configuration.DrawingSize.Width * _pageSettings.Scale 
                + 2 * Configuration.InternalBorderWidth;
            viewport.Height = Configuration.DrawingSize.Height * _pageSettings.Scale 
                + 2 * Configuration.InternalBorderWidth;
            viewport.CenterPoint = new Point3d(viewport.Width / 2 + Configuration.ExternalBorderWidth,
                viewport.Height / 2 + Configuration.ExternalBorderWidth, 0);

            viewport.ViewDirection = new Vector3d(0, 0, 1);
            viewport.ViewCenter = new Point2d((_pageSettings.DrawingExtend.MinPoint.X
                + _pageSettings.DrawingExtend.MaxPoint.X) / 2,
                (_pageSettings.DrawingExtend.MinPoint.Y 
                + _pageSettings.DrawingExtend.MaxPoint.Y) / 2);
            //acVport.StandardScale = StandardScaleType.ScaleToFit;
            viewport.CustomScale = _pageSettings.Scale;
            viewport.Locked = true;
            viewport.On = true;
        }

        private Layout GetPlotLayout(Transaction tr)
        {
            var id = LayoutManager.Current.GetLayoutId(Configuration.PlotLayoutName);
            var layout = (Layout) tr.GetObject(id, OpenMode.ForRead);
            return layout;
        }

        private Layout CreatePlotLayout(Transaction tr)
        {
            var id = CreateAndMakeLayoutCurrent(Configuration.PlotLayoutName);
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
                if (!devs.Contains(pageSettings.Pc3Name))
                {
                    throw new ArgumentException("PC3 not found " + pageSettings.Pc3Name);
                }

                psv.SetPlotConfigurationName(ps, pageSettings.Pc3Name, null);
                psv.RefreshLists(ps);

                var height = Configuration.DrawingSize.Height * _pageSettings.Scale 
                    + 2 * Configuration.InternalBorderWidth
                    + 2 * Configuration.ExternalBorderWidth;
                var width = Configuration.DrawingSize.Width * _pageSettings.Scale 
                    + 2 * Configuration.InternalBorderWidth
                    + 2 * Configuration.ExternalBorderWidth
                    + Configuration.PlotCartridgeWidth 
                    + 2 * Configuration.CartridgeExternalBorderWidth;
                
                var pageFormat = GetPageFormatList(psv, ps)
                    .Where(x => x.PlotPaperSize.X >= width && x.PlotPaperSize.Y >= height)
                    .OrderBy(x => x.PlotPaperSize.X * x.PlotPaperSize.Y)
                    .ToList()
                    .FirstOrDefault();
                if (pageFormat == null)
                {
                    throw new ArgumentException("No paper format found for " + pageSettings.Pc3Name);
                }

                Helper.Trace("Selected CanonicalMediaName: {0}", pageFormat.CanonicalMediaName);
                pageSettings.CanonicalMediaName = pageFormat.CanonicalMediaName;

                psv.SetCanonicalMediaName(ps, pageSettings.CanonicalMediaName);
                psv.RefreshLists(ps);

                var ssl = psv.GetPlotStyleSheetList();
                if (ssl.Contains(pageSettings.StyleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, pageSettings.StyleSheet);
                }
                else if (ssl.Contains(Configuration.DefaultStyleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, Configuration.DefaultStyleSheet);
                }
                else
                {
                    throw new ArgumentException("No styleSheet found for " + pageSettings.Pc3Name);
                }

                layout.CopyFrom(ps);
            }
        }

        private IEnumerable<PageFormat> GetPageFormatList(PlotSettingsValidator psv, PlotSettings ps)
        {
            var medlist = psv.GetCanonicalMediaNameList(ps);
            for (var i = 0; i < medlist.Count; i++)
            {
                psv.SetCanonicalMediaName(ps, medlist[i]);
                psv.RefreshLists(ps);
                var sizex = ps.PlotPaperSize.X - ps.PlotPaperMargins.MinPoint.X - ps.PlotPaperMargins.MaxPoint.X;
                var sizey = ps.PlotPaperSize.Y - ps.PlotPaperMargins.MinPoint.Y - ps.PlotPaperMargins.MaxPoint.Y;
                yield return new PageFormat()
                {
                    CanonicalMediaName = ps.CanonicalMediaName,
                    PlotPaperSize = ps.PlotRotation == PlotRotation.Degrees090 ? new Point2d(sizey, sizex) : new Point2d(sizex, sizey),
                };
                Helper.Trace(">>>>>>>>>> {0,-40} {1}", ps.CanonicalMediaName, ps.PlotPaperSize);
            }
        }

        //private string GetCanonicalMediaName(PlotSettingsValidator psv, PlotSettings ps, string pageSize)
        //{
        //    var list = psv.GetCanonicalMediaNameList(ps);
        //    for (var j = 0; j < list.Count; j++)
        //    {
        //        var name = psv.GetLocaleMediaName(ps, j);
        //        if (string.Equals(name, pageSize, StringComparison.InvariantCultureIgnoreCase))
        //        {
        //            return list[j];
        //        }
        //    }
        //    return null;
        //}

        //private int ParseResolutionParameter()
        //{
        //    var res = GetCommandLineParameterValue("r");
        //    int resolution;
        //    if (!int.TryParse(res, out resolution))
        //    {
        //        throw new ArgumentException("Resolution (r) not valid " + res);
        //    }
        //    return resolution;
        //}

        //private Point2d GetPointParameter(string prompt)
        //{
        //    var pr = _document.Editor.GetPoint("\n" + prompt + ":");
        //    if (pr.Status != PromptStatus.OK)
        //    {
        //        throw new ArgumentException("Invalid input for '" + prompt + "'");
        //    }
        //    return new Point2d(pr.Value.X, pr.Value.Y);
        //}

        //private string GetPlanchetteId()
        //{
        //    return PromptStringParameter("Get planchette position");
        //}

        //private string GetCommandLineParameterValue(string name)
        //{
        //    return null;
        //}

        //private string PromptStringParameter(string prompt)
        //{
        //    var pr = _document.Editor.GetString("\n" + prompt + ":");
        //    if (pr.Status != PromptStatus.OK)
        //    {            var args = Environment.GetCommandLineArgs();
        //        throw new ArgumentException("Invalid input for '" + prompt + "'");
        //    }
        //    return pr.StringResult.ToUpper();
        //}

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

        private void SaveDwg(string filePath)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            document.Database.SaveAs(filePath, true, DwgVersion.Current, document.Database.SecurityParameters);
        }

        private void PlotExtents(PageSettings pageSettings)
        {
            using (var tr = _document.Database.TransactionManager.StartTransaction())
            {
                var db = (DBDictionary) tr.GetObject(_document.Database.LayoutDictionaryId, OpenMode.ForRead);
                
                var layoutId = db.GetAt(Configuration.PlotLayoutName);
                var layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                var ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);
                ps.PlotPlotStyles = true;

                var psv = PlotSettingsValidator.Current;
                psv.SetDefaultPlotConfig(ps);
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                psv.SetUseStandardScale(ps, true);
                //psv.SetPlotCentered(ps, true);
                psv.SetPlotOrigin(ps, Configuration.PlotOrigin);
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
                psv.SetPlotConfigurationName(ps, pageSettings.Pc3Name, pageSettings.CanonicalMediaName);
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

    public class PageFormat 
    {
        public string CanonicalMediaName { get; set; }
        public Point2d PlotPaperSize { get; set; }
    }

    public class PageSettings
    {
        public PageSettings(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case @"/r":
                        Resolution = int.Parse(args[++i]);
                        break;
                    case @"/c":
                        Categories = args[++i].Split(',');
                        break;
                    case @"/e":
                        Energies = args[++i].Split(',');
                        break;
                    case @"/id":
                        SetPlanchetteId(args[++i]);
                        break;
                    case @"/f":
                        SetOutputFilePath(args[++i]);
                        break;
                    case @"/imp":
                        SetImpetrant();
                        break;
                }
            }
            Validate();
        }

        public string JobType = "Structured";
        public string s_plot_ticket = "952926";
        public string s_plot_request = "129415";
        public string l_id_stamp = "1629628-29519681";
        public string c_type_plan = "T";

        public string l_div	= "La Louvière";
        public string n_tot_plan = "7";
        public string n_ord_plan = "1";
        public string userid = "BZT";
        
        public string l_path_plan = "";

        // l_id_planchette
        public string PlanchetteId { get; private set; }
        // l_path_result_pdf
        public string OutputFilePath { get; private set; }
        // n_scale
        public int Resolution { get; private set; }
        // c_type_map
        public string[] Categories { get; private set; }
        // list_energy
        public string[] Energies { get; private set; }

        public bool Impetrant { get; private set; }

        public string Pc3Name { get; private set; }

        public string CanonicalMediaName { get; set; }

        public string PlanchetteLetter { get; private set; }
        
        public Point2d MapCoordinate { get; private set; }

        public string StyleSheet
        {
            get { return Path.GetFileNameWithoutExtension(Pc3Name) + ".ctb"; }
        }

        public double Scale
        {
            get { return 1000.0 / Resolution; }
        }

        public Extents2d DrawingExtend
        {
            get
            {
                return new Extents2d(MapCoordinate.X, MapCoordinate.Y,
                    MapCoordinate.X + Configuration.DrawingSize.Width,
                    MapCoordinate.Y + Configuration.DrawingSize.Height);
            }
        }

        public Point3d PlotCartridgePosition 
        {
            get
            {
                var position = Configuration.DrawingSize.Width * Scale
                    + 2 * Configuration.InternalBorderWidth
                    + 2 * Configuration.ExternalBorderWidth
                    + Configuration.CartridgeExternalBorderWidth;
                return new Point3d(position, 0, 0);
            }
        }

        public void Validate()
        {
        }

        private void SetPlanchetteId(string planchetteId)
        {
            PlanchetteId = planchetteId;
            string letter;
            MapCoordinate = ParsePlanchetteId(planchetteId, out letter);
            PlanchetteLetter = letter;
        }

        private void SetOutputFilePath(string outputFilePath)
        {
            OutputFilePath = outputFilePath;
            SetDiviceAndStyleSheet();
        }

        private void SetImpetrant()
        {
            Impetrant = true;
            SetDiviceAndStyleSheet();
        }

        private Point2d ParsePlanchetteId(string planchetteId, out string planchetteLetter)
        {
            var regex = new Regex(@"^(?<x>[0-9]{3})(?<y>[0-9]{3})(?<letter>[A-H])$");
            var match = regex.Match(planchetteId);
            if (!match.Success)
            {
                throw new ArgumentException("Planchette Id not valid " + planchetteId);
            }
            var x = int.Parse(match.Groups["x"].Value) * 1000;
            var y = int.Parse(match.Groups["y"].Value) * 1000;
            planchetteLetter = match.Groups["letter"].Value;
            var dx = 0;
            var dy = 0;
            if ("ACEG".Contains(planchetteLetter))
            {
                dx = 0;
                dy = "ACEG".IndexOf(planchetteLetter);
            }
            else
            {
                dx = 1;
                dy = "BDFH".IndexOf(planchetteLetter);
            }
            y = Convert.ToInt32(y + dy * Configuration.DrawingSize.Height);
            x = Convert.ToInt32(x + dx * Configuration.DrawingSize.Height);
            return new Point2d(x, y);
        }

        private void SetDiviceAndStyleSheet()
        {
            if (Impetrant)
            {
                Pc3Name = "Impetrant.pc3";
            }
            else
            {
                switch (Path.GetExtension(OutputFilePath))
                {
                    case "Impetrant":
                        break;
                    case ".pdf":
                        Pc3Name = "PDF.pc3";
                        break;
                    case ".dwf":
                    case ".dwg":
                        Pc3Name = "DWF6.pc3";
                        break;
                    default:
                        throw new ArgumentException(string.Format("Output file format {0} not suported", OutputFilePath));
                }
            }
        }
    }

    public static class Configuration
    {
        public static string PlotLayoutName = "Energis_plot_layout_name";
        public static string CartridgeTemplateFilePath = @"C:\\Test\\Plot\\Plot01\\Scripts\\Gis_cstd_est.dwg";
        public static Size DrawingSize = new Size(500, 250);
        public static int ExternalBorderWidth = 5;
        public static int InternalBorderWidth = 15;
        public static double CartridgeExternalBorderWidth = 10;
        public static int PlotCartridgeWidth = 190;
        public static Point2d PlotOrigin = new Point2d(10, 10);
        public static string DefaultStyleSheet = "Default.ctb";
        public static string ConnectionString = "DATA SOURCE=WALLP1_UNWALL.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
        //_connectionString = "DATA SOURCE=WALLA1.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
        public static string FileServerName = "RWA004";
        //_fileServerName = "RWA002AEST";
    }
}