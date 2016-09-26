using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using BatchPlot.Configuration;

namespace BatchPlot.Extensions
{
    public static class AutocadExtensions
    {
        public static void ApplyOceGrayStyle(this Database db, Transaction tr, Extents2d extents)
        {
            //using (var tr = _document.Database.TransactionManager.StartTransaction())
            //{
            var regex = new Regex(PlotConfiguration.Config.TopoLayersRegexFilter, RegexOptions.IgnoreCase);
            var list = db.QueryEntities(tr, null, extents);
            foreach (var entity in list)
            {
                if (regex.IsMatch(entity.Layer))
                {
                    if (!(entity is DBText || entity is MText))
                    {
                        entity.UpgradeOpen();
                        entity.ColorIndex = 252;
                        entity.DowngradeOpen();
                    }
                }
                else
                {
                    if (!(entity is Image))
                    {
                        entity.UpgradeOpen();
                        entity.ColorIndex = 7;
                        entity.DowngradeOpen();
                    }
                }
            }
            //tr.Commit();
            //}
        }

        public static void ApplyImpetrantStyle(this Database db, Transaction tr, Extents2d extents)
        {
            //using (var tr = _document.Database.TransactionManager.StartTransaction())
            //{
            var list = db.QueryEntities(tr, PlotConfiguration.Config.TopoLayersRegexFilter, extents);
            foreach (var entity in list)
            {
                entity.UpgradeOpen();
                if (entity is DBText)
                {
                    entity.ColorIndex = 7;
                }
                else if (entity is MText)
                {
                    //var mtext = (MText)entity;
                    //if (mtext.TextHeight < 1.5) mtext.TextHeight = 1.5;
                    entity.ColorIndex = 7;
                }
                else
                {
                    entity.ColorIndex = 252;
                }
                entity.DowngradeOpen();
            }
            //tr.Commit();
            //}
        }

        public static void DeleteNotNeededLayers(this Database db, string layersToDeleteRegexFilter)
        {
            var regex = new Regex(layersToDeleteRegexFilter, RegexOptions.IgnoreCase);
            using (var tr = db.TransactionManager.StartTransaction())
            using (var layerIds = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead))
            {
                foreach (var id in layerIds)
                {
                    using (var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForWrite))
                    {
                        if (regex.IsMatch(layer.Name))
                        {
                            layer.IsLocked = false;

                            using (var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))
                            {
                                foreach (var btrId in blockTable)
                                {
                                    var block = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                                    foreach (var entId in block)
                                    {
                                        var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                                        if (string.Equals(ent.Layer, layer.Name, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            ent.UpgradeOpen();
                                            ent.Erase();
                                        }
                                    }
                                }
                            }

                            layer.Erase(true);
                        }
                    }
                }
                tr.Commit();
            }
        }

        public static Polyline InflateRectangle(this Polyline rectangle, int size)
        {
            var line = new Polyline();
            line.AddVertexAt(0, rectangle.GetPoint2dAt(0).Add(new Vector2d(-size, -size)), 0, 0, 0);
            line.AddVertexAt(1, rectangle.GetPoint2dAt(1).Add(new Vector2d(-size, +size)), 0, 0, 0);
            line.AddVertexAt(2, rectangle.GetPoint2dAt(2).Add(new Vector2d(+size, +size)), 0, 0, 0);
            line.AddVertexAt(3, rectangle.GetPoint2dAt(3).Add(new Vector2d(+size, -size)), 0, 0, 0);
            line.AddVertexAt(4, rectangle.GetPoint2dAt(4).Add(new Vector2d(-size, -size)), 0, 0, 0);
            return line;
        }

        public static Polyline CreateRectangle(double x, double y, double height, double width)
        {
            var line = new Polyline();
            line.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            line.AddVertexAt(1, new Point2d(x, y + height), 0, 0, 0);
            line.AddVertexAt(2, new Point2d(x + width, y + height), 0, 0, 0);
            line.AddVertexAt(3, new Point2d(x + width, y), 0, 0, 0);
            line.AddVertexAt(4, new Point2d(x, y), 0, 0, 0);
            return line;
        }

        public static IEnumerable<T> QueryEntities<T>(this Database db, Transaction tr, IEnumerable<string> layers,
            Extents2d extend) where T : Entity
        {
            var bt = (IEnumerable<dynamic>)(dynamic)db.BlockTableId;
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

        public static IEnumerable<Entity> QueryEntities(this Database db, Transaction tr, 
            string layerRegexFilter, Extents2d extend)
        {
            var bt = (IEnumerable<dynamic>)(dynamic)db.BlockTableId;
            var list = bt.SelectMany(x => (IEnumerable<dynamic>)x)
                .Select(x => (Entity)tr.GetObject(x, OpenMode.ForRead));
            if (layerRegexFilter != null)
            {
                var regex = new Regex(layerRegexFilter, RegexOptions.IgnoreCase);
                list = list.Where(x => regex.IsMatch(x.Layer));
            }
            list = list.Where(x => x.Bounds.HasValue
                && x.Bounds.Value.MaxPoint.X >= extend.MinPoint.X
                && x.Bounds.Value.MinPoint.X <= extend.MaxPoint.X
                && x.Bounds.Value.MaxPoint.Y >= extend.MinPoint.Y
                && x.Bounds.Value.MinPoint.Y <= extend.MaxPoint.Y)
                .ToArray();
            return list;
        }

        public static void PlotLayout(this Document document, Transaction tr, Layout layout, PlotParameters plotParameters)
        {
            //using (var tr = _document.Database.TransactionManager.StartTransaction())
            //using (var layout = GetPlotLayout(tr))
            {
                var ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);
                ps.PlotPlotStyles = true;

                var psv = PlotSettingsValidator.Current;
                psv.SetDefaultPlotConfig(ps);
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                psv.SetUseStandardScale(ps, true);
                psv.SetPlotOrigin(ps, plotParameters.PlotOrigin);
                if (plotParameters.PaperFormat.ShrinkDrawing)
                {
                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                }
                else
                {
                    psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);
                }
                psv.SetPlotConfigurationName(ps, plotParameters.Pc3Name, plotParameters.PaperFormat.CanonicalMediaName);
                psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);

                var pi = new PlotInfo();
                pi.Layout = layout.ObjectId;
                pi.OverrideSettings = ps;

                var piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                var d = Path.GetDirectoryName(plotParameters.OutputFilePath);
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }

                using (var pe = PlotFactory.CreatePublishEngine())
                using (var ppi = new PlotPageInfo())
                {
                    pe.BeginPlot(null, null);
                    pe.BeginDocument(pi, document.Name, null, 1, plotParameters.PlotToFile, plotParameters.OutputFilePath);
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
