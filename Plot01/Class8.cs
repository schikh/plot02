using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(Rivilis.PDFPlot))]

namespace Rivilis
{
    public class PDFPlot
    {
        //[CommandMethod("GenPdf", CommandFlags.Session)]
        //public void GenPdf()
        //{
        //    string[] files = { "C:\\FieldPlot1.dwg", "C:\\FieldPlot2.dwg", "C:\\FieldPlot3.dwg" }; // Test files with PLOTDATE field
        //    object oldBgPlot = Application.GetSystemVariable("BACKGROUNDPLOT");
        //    Application.SetSystemVariable("BACKGROUNDPLOT", 0);
        //    Document oldDoc = Application.DocumentManager.MdiActiveDocument;
        //    Database oldDb = HostApplicationServices.WorkingDatabase;
        //    try
        //    {
        //        foreach (string file in files)
        //        {
        //            Document doc = Application.DocumentManager.Open(file, true);
        //            Application.DocumentManager.MdiActiveDocument = doc;
        //            HostApplicationServices.WorkingDatabase = doc.Database;
        //            using (DocumentLock docloc = doc.LockDocument())
        //            {
        //                PlotExtents(
        //                    "DWG To PDF.pc3",
        //                    "ISO_expand_A4_(210.00_x_297.00_MM)",
        //                    "monochrome.ctb",
        //                    file + ".pdf");
        //            }
        //            doc.CloseAndDiscard();
        //        }
        //        Application.DocumentManager.MdiActiveDocument = oldDoc;
        //        HostApplicationServices.WorkingDatabase = oldDb;
        //        Application.SetSystemVariable("BACKGROUNDPLOT", oldBgPlot);
        //    }
        //    catch (System.Exception e) { }
        //}

        // Plotting progress based on 
        // http://through-the-interface.typepad.com/through_the_interface/2007/09/driving-a-multi.html
        static public void PlotExtents(string printer, string format, string styleSheet, string pdfFilePath)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btr = (BlockTableRecord) tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                    Layout lo = (Layout) tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                    PlotInfo pi = new PlotInfo();
                    pi.Layout = btr.LayoutId;

                    PlotSettings ps = new PlotSettings(lo.ModelType);
                    PlotConfigInfo pci = new PlotConfigInfo();

                    ps.CopyFrom(lo);
                    ps.PlotPlotStyles = true;

                    PlotSettingsValidator psv = PlotSettingsValidator.Current;
                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                    psv.SetDefaultPlotConfig(ps);

                    psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                    psv.SetUseStandardScale(ps, true);
                    psv.SetPlotCentered(ps, true);
                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                    ////PS-PlotRotation
                    Extents2d extent2d = ps.PlotPaperMargins;
                    if (extent2d.MaxPoint.Y >  extent2d.MaxPoint.X)
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees000);
                    }
                    else
                    {
                        psv.SetPlotRotation(ps, PlotRotation.Degrees090);
                    }

                    try
                    {
                        psv.SetPlotConfigurationName(ps, printer, null);
                        psv.SetPlotConfigurationName(ps, printer, format);
                        psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);
                    }
                    catch (System.Exception e)
                    {
                        e.GetBaseException().ToString();
                    }

                    psv.GetCanonicalMediaNameList(ps);
                    pi.OverrideSettings = ps;

                    PlotInfoValidator piv = new PlotInfoValidator();
                    piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                    piv.Validate(pi);

                    if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                    {
                        using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                        {
                            PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true);
                            using (ppd)
                            {
                                ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plot Progress");
                                ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");

                                ppd.LowerPlotProgressRange = 0;
                                ppd.UpperPlotProgressRange = 100;
                                ppd.PlotProgressPos = 0;

                                ppd.OnBeginPlot();
                                ppd.IsVisible = true;
                                ppd.OnBeginPlot();
                                ppd.IsVisible = true;
                                pe.BeginPlot(ppd, null);

                                pe.BeginDocument(pi, doc.Name, null, 1, true, pdfFilePath);

                                ppd.OnBeginSheet();
                                ppd.LowerSheetProgressRange = 0;
                                ppd.UpperSheetProgressRange = 100;
                                ppd.SheetProgressPos = 0;

                                PlotPageInfo ppi = new PlotPageInfo();
                                pe.BeginPage(ppi, pi, true, null);
                                pe.BeginGenerateGraphics(null);
                                ppd.SheetProgressPos = 50;
                                pe.EndGenerateGraphics(null);

                                pe.EndPage(null);
                                ppd.SheetProgressPos = 100;
                                ppd.OnEndSheet();

                                pe.EndDocument(null);

                                ppd.PlotProgressPos = 100;
                                ppd.OnEndPlot();

                                pe.EndPlot(null);
                            }
                        }
                    }
                    else
                    {
                        ed.WriteMessage("\nCannot plot. System is busy plotting another one.");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception e)
            {
                // Display the error message here.
            }
        }
    }
}
