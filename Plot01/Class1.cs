using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

namespace BatchPlot
{
    public static class Extensions
    {
        public static ObjectId CreateAndMakeLayoutCurrent(this LayoutManager lm, string name, bool select = true)
        {
            var id = lm.GetLayoutId(name);
            if (!id.IsValid)
            {
                id = lm.CreateLayout(name);
            }
            if (select)
            {
                lm.CurrentLayout = name;
            }
            return id;
        }

        public static void ApplyToViewport(this Layout lay, Transaction tr, int vpNum, Action<Viewport> f)
        {
            var vpIds = lay.GetViewports();
            Viewport vp = null;

            foreach (ObjectId vpId in vpIds)
            {
                var vp2 = tr.GetObject(vpId, OpenMode.ForWrite) as Viewport;
                if (vp2 != null && vp2.Number == vpNum)
                {
                    // We have found our viewport, so call the action
                    vp = vp2;
                    break;
                }
            }

            if (vp == null)
            {
                // We have not found our viewport, so create one
                var btr = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForWrite);

                vp = new Viewport();

                // Add it to the database
                btr.AppendEntity(vp);
                tr.AddNewlyCreatedDBObject(vp, true);

                // Turn it - and its grid - on
                vp.On = true;
                vp.GridOn = true;
            }

            // Finally we call our function on it
            f(vp);
        }

        public static void Plot(this Layout lay, string outputFilePath)
        {
            var ps = new PlotSettings(lay.ModelType);
            ps.CopyFrom(lay);

            var pi = new PlotInfo();
            pi.Layout = lay.Id;
            pi.OverrideSettings = ps;

            PlotInfoValidator piv = new PlotInfoValidator();
            piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
            piv.Validate(pi);

            if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
            {
                using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                using (PlotPageInfo ppi = new PlotPageInfo())
                {
                    pe.BeginPlot(null, null);
                    pe.BeginDocument(pi, Application.DocumentManager.MdiActiveDocument.Name, null, 1, true, outputFilePath);
                    pe.BeginPage(ppi, pi, true, null);
                    pe.BeginGenerateGraphics(null);
                    pe.EndGenerateGraphics(null);
                    pe.EndPage(null);
                    pe.EndDocument(null);
                    pe.EndPlot(null);
                }
            }
            else
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nCannot plot. System is busy plotting another one.");
            }
        }

        public static void SetPlotSettings(this Layout lay, string pageSize, string styleSheet, string device)
        {
            using (var ps = new PlotSettings(lay.ModelType))
            {

                ps.CopyFrom(lay);
                var psv = PlotSettingsValidator.Current;

                // Set the device
                var devs = psv.GetPlotDeviceList();
                if (devs.Contains(device))
                {
                    psv.SetPlotConfigurationName(ps, device, null);
                    psv.RefreshLists(ps);
                }

                // Set the media name/size
                var mns = psv.GetCanonicalMediaNameList(ps);
                if (mns.Contains(pageSize))
                {
                    psv.SetCanonicalMediaName(ps, pageSize);
                }

                // Set the pen settings
                var ssl = psv.GetPlotStyleSheetList();
                if (ssl.Contains(styleSheet))
                {
                    psv.SetCurrentStyleSheet(ps, styleSheet);
                }

                //
                //psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);




                ////ps.PlotPlotStyles = true;
                ////psv.SetDefaultPlotConfig(ps);
                //psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                ////psv.SetPlotWindowArea(ps, new Extents2d(new Point2d(150000, 150000), new Point2d(190000, 150000)));
                ////psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                ////psv.SetUseStandardScale(ps, true);
                ////psv.SetPlotCentered(ps, true);
                //psv.SetPlotOrigin(ps, new Point2d(10, 10));
                //psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);

                // Copy the PlotSettings data back to the Layout
                //var upgraded = false;
                //if (!lay.IsWriteEnabled)
                //{
                //    lay.UpgradeOpen();
                //    upgraded = true;
                //}

                lay.CopyFrom(ps);

                //if (upgraded)
                //{
                //    lay.DowngradeOpen();
                //}
            }
        }
    }
}