using System.Collections.Specialized;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

namespace AutoCADTest
{
    public class PlottingCommands
    {
        static public string[] ChooseDeviceAndMedia()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            string devname = "", medname = "";

            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            StringCollection devlist = psv.GetPlotDeviceList();

            for (int i = 0; i < devlist.Count; i++)
            {
                ed.WriteMessage("\n{0} {1}", i + 1, devlist[i]);
            }

            devname = "PDF.pc3";
            ed.WriteMessage("\nSelected: {0}\n", devname);

            using (PlotSettings ps = new PlotSettings(true))
            {
                psv.SetPlotConfigurationName(ps, devname, null);
                psv.RefreshLists(ps);
                StringCollection medlist = psv.GetCanonicalMediaNameList(ps);
                for (int i = 0; i < medlist.Count; i++)
                {
                    ed.WriteMessage("\n{0} {1}", i + 1, medlist[i]);
                }
                medname = medlist[1];
            }

            return new string[2] { devname, medname };
        }


        //[CommandMethod("mplot")]
        //static public void MultiSheetPlot()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = doc.Editor;
        //    Database db = doc.Database;

        //    Transaction tr = db.TransactionManager.StartTransaction();
        //    using (tr)
        //    {
        //        BlockTable bt = (BlockTable) tr.GetObject(db.BlockTableId, OpenMode.ForRead);

        //        PlotInfo pi = new PlotInfo();
        //        PlotInfoValidator piv = new PlotInfoValidator();
        //        piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;

        //        // A PlotEngine does the actual plotting
        //        // (can also create one for Preview)

        //        if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
        //        {
        //            string[] devmed = ChooseDeviceAndMedia();

        //            // Only proceed if we have values for both

        //            if (devmed[0] != "" && devmed[1] != "")
        //            {
        //                string devname = devmed[0];
        //                string medname = devmed[1];

        //                PlotEngine pe = PlotFactory.CreatePublishEngine();
        //                using (pe)
        //                {
        //                    // Collect all the paperspace layouts
        //                    // for plotting

        //                    ObjectIdCollection layoutsToPlot = new ObjectIdCollection();

        //                    foreach (ObjectId btrId in bt)
        //                    {
        //                        BlockTableRecord btr = (BlockTableRecord) tr.GetObject(btrId, OpenMode.ForRead);
        //                        if (btr.IsLayout && btr.Name.ToUpper() != BlockTableRecord.ModelSpace.ToUpper())
        //                        {
        //                            layoutsToPlot.Add(btrId);
        //                        }
        //                    }

        //                    // Create a Progress Dialog to provide info
        //                    // and allow thej user to cancel

        //                    PlotProgressDialog ppd = new PlotProgressDialog(false, layoutsToPlot.Count, true);
        //                    using (ppd)
        //                    {
        //                        int numSheet = 1;

        //                        foreach (ObjectId btrId in layoutsToPlot)
        //                        {
        //                            BlockTableRecord btr = (BlockTableRecord) tr.GetObject(btrId, OpenMode.ForRead);
        //                            Layout lo = (Layout) tr.GetObject(btr.LayoutId, OpenMode.ForRead);

        //                            // We need a PlotSettings object
        //                            // based on the layout settings
        //                            // which we then customize

        //                            PlotSettings ps = new PlotSettings(lo.ModelType);
        //                            ps.CopyFrom(lo);

        //                            // The PlotSettingsValidator helps
        //                            // create a valid PlotSettings object

        //                            PlotSettingsValidator psv = PlotSettingsValidator.Current;

        //                            // We'll plot the extents, centered and
        //                            // scaled to fit

        //                            psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
        //                            psv.SetUseStandardScale(ps, true);
        //                            psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
        //                            psv.SetPlotCentered(ps, true);

        //                            // We'll use the standard DWFx PC3, as
        //                            // this supports multiple sheets

        //                            psv.SetPlotConfigurationName(ps, devname, medname);

        //                            // We need a PlotInfo object
        //                            // linked to the layout

        //                            pi.Layout = btr.LayoutId;

        //                            // Make the layout we're plotting current

        //                            LayoutManager.Current.CurrentLayout = lo.LayoutName;

        //                            // We need to link the PlotInfo to the
        //                            // PlotSettings and then validate it

        //                            pi.OverrideSettings = ps;
        //                            piv.Validate(pi);

        //                            if (numSheet == 1)
        //                            {
        //                                ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Custom Plot Progress");
        //                                ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
        //                                ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
        //                                ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
        //                                ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
        //                                ppd.LowerPlotProgressRange = 0;
        //                                ppd.UpperPlotProgressRange = 100;
        //                                ppd.PlotProgressPos = 0;

        //                                // Let's start the plot, at last

        //                                ppd.OnBeginPlot();
        //                                ppd.IsVisible = true;
        //                                pe.BeginPlot(ppd, null);

        //                                // We'll be plotting a single document

        //                                pe.BeginDocument(
        //                                    pi,
        //                                    doc.Name,
        //                                    null,
        //                                    1,
        //                                    true,
        //                                    // Let's plot to file
        //                                    "c:\\test-multi-sheet"
        //                                    );
        //                            }

        //                            // Which may contains multiple sheets

        //                            ppd.set_PlotMsgString(
        //                                PlotMessageIndex.SheetName,
        //                                doc.Name.Substring(doc.Name.LastIndexOf("\\") + 1) +
        //                                " - sheet " + numSheet.ToString() +
        //                                " of " + layoutsToPlot.Count.ToString()
        //                                );

        //                            ppd.OnBeginSheet();

        //                            ppd.LowerSheetProgressRange = 0;
        //                            ppd.UpperSheetProgressRange = 100;
        //                            ppd.SheetProgressPos = 0;

        //                            PlotPageInfo ppi = new PlotPageInfo();
        //                            pe.BeginPage(ppi, pi, (numSheet == layoutsToPlot.Count), null);
        //                            pe.BeginGenerateGraphics(null);
        //                            ppd.SheetProgressPos = 50;
        //                            pe.EndGenerateGraphics(null);

        //                            // Finish the sheet
        //                            pe.EndPage(null);
        //                            ppd.SheetProgressPos = 100;
        //                            ppd.OnEndSheet();
        //                            numSheet++;
        //                            ppd.PlotProgressPos += (100 / layoutsToPlot.Count);
        //                        }

        //                        // Finish the document

        //                        pe.EndDocument(null);

        //                        // And finish the plot

        //                        ppd.PlotProgressPos = 100;
        //                        ppd.OnEndPlot();
        //                        pe.EndPlot(null);
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            ed.WriteMessage("\nAnother plot is in progress.");
        //        }
        //    }
        //}

        [CommandMethod("Plt")]
        public void PlotWithPlotStyle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //string psetupPathname = @"C:\Apldata\AutoCAD\AcadConfig_INFRA\Menu\psetup-PDF.dwg";
            //string plotStyleName = "4Z-PDF";
            string printerName = "PDF.pc3";

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {

                db.TileMode = false;
                ed.SwitchToPaperSpace();

                LayoutManager layMgr = LayoutManager.Current;
                Layout theLayout = (Layout)layMgr.GetLayoutId(layMgr.CurrentLayout).GetObject(OpenMode.ForWrite);
                PlotSettingsValidator psVal = Autodesk.AutoCAD.DatabaseServices.PlotSettingsValidator.Current;
                PlotSettings plotSet = new PlotSettings(theLayout.ModelType);
                plotSet.CopyFrom(theLayout);
                //psVal.SetPlotConfigurationName(plotSet, printerName, null);

                PlotConfigManager.SetCurrentConfig(printerName);
                PlotConfigManager.RefreshList(RefreshCode.All);
                PlotConfig tds = PlotConfigManager.CurrentConfig;
                string plotFile = null;
                if (tds.PlotToFileCapability == PlotToFileCapability.PlotToFileAllowed)
                {
                    tds.IsPlotToFile = true;
                    plotFile = Path.Combine(Path.GetDirectoryName(Application.DocumentManager.MdiActiveDocument.Database.Filename), Path.GetFileNameWithoutExtension(Application.DocumentManager.MdiActiveDocument.Database.Filename));
                    plotFile = plotFile + tds.DefaultFileExtension;
                    if (File.Exists(plotFile))
                        File.Delete(plotFile);

                }

                //the next lines will fail because the required PMP file will get referenced by the PC3 
                //string pc3Dir = @"C:\Documents and Settings\avb\Application Data\Autodesk\AutoCAD 2012 - English\R18.2\enu\Plotters";
                //printerName = printerName + ".pc3";
                //tds.SaveToPC3(Path.Combine(pc3Dir, printerName ));
                //PlotConfigManager.SetCurrentConfig(printerName);
                //PlotConfigManager.RefreshList(RefreshCode.All);
                //tds = PlotConfigManager.CurrentConfig;

                //psVal.RefreshLists(plotSet);
                //try
                //{
                //    psVal.SetClosestMediaName(plotSet, plotSet.PlotPaperSize[0], plotSet.PlotPaperSize[1], PlotPaperUnit.Millimeters, true);
                //}
                //catch ( Autodesk.AutoCAD.Runtime.Exception exx ) 
                //{
                //    ed.WriteMessage("\n" + exx.ToString());
                //}

                psVal.SetPlotPaperUnits(plotSet, PlotPaperUnit.Millimeters);

                //theLayout.CopyFrom(plotSet);
                ed.Regen();


                PlotInfo plotInfo = new PlotInfo();
                plotInfo.Layout = theLayout.ObjectId;
                plotInfo.OverrideSettings = plotSet;
                plotInfo.DeviceOverride = tds;

                PlotInfoValidator validator = new PlotInfoValidator();
                //int itIs = validator.IsCustomPossible(plotInfo);
                validator.MediaMatchingPolicy = Autodesk.AutoCAD.PlottingServices.MatchingPolicy.MatchEnabledCustom;
                int itIs = validator.IsCustomPossible(plotInfo);
                validator.Validate(plotInfo);


                //for now see the results
                if (plotInfo.IsValidated && itIs == 0)
                {
                    PlotSettings newNamedPlotStyle = new PlotSettings(theLayout.ModelType);
                    newNamedPlotStyle.CopyFrom(plotInfo.ValidatedSettings);
                    theLayout.CopyFrom(newNamedPlotStyle);

                    newNamedPlotStyle.PlotSettingsName = "7A4-TDS600";
                    psVal.RefreshLists(newNamedPlotStyle);
                    newNamedPlotStyle.AddToPlotSettingsDictionary(db);
                    tr.AddNewlyCreatedDBObject(newNamedPlotStyle, true);
                    psVal.RefreshLists(newNamedPlotStyle);

                    ed.Regen();
                    tr.Commit();
                    return;
                }
                PlotEngine plotEngine = PlotFactory.CreatePublishEngine();
                //None (initial) -> plot -> document -> page -> graphics
                try
                {
                    plotEngine.BeginPlot(null, null);
                    if (tds.IsPlotToFile)
                        plotEngine.BeginDocument(plotInfo, Application.DocumentManager.MdiActiveDocument.Database.Filename, null, 1, true, plotFile);
                    else
                        plotEngine.BeginDocument(plotInfo, Application.DocumentManager.MdiActiveDocument.Database.Filename, null, 1, false, null);

                    PlotPageInfo pageInfo = new PlotPageInfo();
                    ed.WriteMessage("\nPlotting {0} Entities, {1} ", pageInfo.EntityCount, pageInfo.RasterCount);
                    plotEngine.BeginPage(pageInfo, plotInfo, true, null);
                    plotEngine.BeginGenerateGraphics(null);
                    plotEngine.EndGenerateGraphics(null);
                    plotEngine.EndPage(null);
                    plotEngine.EndDocument(null);
                    plotEngine.EndPlot(null);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
                plotEngine.Destroy();

                tr.Commit();
            }
        }
    }
}
