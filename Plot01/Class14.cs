using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using System.Collections.Specialized;
using Autodesk.AutoCAD.ApplicationServices.Core;

namespace PlottingApplication
{
    public class PlottingCommands
    {
        static public string[] ChooseDeviceAndMedia()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Assign default return values
            string devname = "", medname = "";

            PlotSettingsValidator psv = PlotSettingsValidator.Current;

            // Let's first select the device
            StringCollection devlist = psv.GetPlotDeviceList();

            for (int i = 0; i < devlist.Count; i++)
            {
                ed.WriteMessage("\n{0} {1}", i + 1, devlist[i]);
            }
            PromptIntegerOptions opts = new PromptIntegerOptions("\nEnter number of device to select: ");
            opts.LowerLimit = 1;
            opts.UpperLimit = devlist.Count;

            PromptIntegerResult pir = ed.GetInteger(opts);
            if (pir.Status == PromptStatus.OK)
            {
                devname = devlist[pir.Value - 1];
                ed.WriteMessage("\nSelected: {0}\n", devname);

                // Now let's select the media
                PlotSettings ps = new PlotSettings(true);
                using (ps)
                {
                    // We should refresh the lists,
                    // in case setting the device impacts
                    // the available media

                    psv.SetPlotConfigurationName(ps, devname, null);
                    psv.RefreshLists(ps);

                    StringCollection medlist = psv.GetCanonicalMediaNameList(ps);
                    for (int i = 0; i < medlist.Count; i++)
                    {
                        ed.WriteMessage("\n{0} {1}", i + 1, medlist[i]);
                    }
                    opts.Message = "\nEnter number of media to select: ";
                    opts.LowerLimit = 1;
                    opts.UpperLimit = medlist.Count;

                    pir = ed.GetInteger(opts);
                    if (pir.Status == PromptStatus.OK)
                    {
                        medname = medlist[pir.Value - 1];
                        ed.WriteMessage("\nSelected: {0}\n", medname);
                    }
                }
            }
            return new string[2] { devname, medname };
        }


        [CommandMethod("mplot")]
        static public void MultiSheetPlot()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                PlotInfo pi = new PlotInfo();
                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;

                // A PlotEngine does the actual plotting
                // (can also create one for Preview)

                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    string[] devmed = ChooseDeviceAndMedia();

                    // Only proceed if we have values for both

                    if (devmed[0] != "" && devmed[1] != "")
                    {
                        string devname = devmed[0];
                        string medname = devmed[1];

                        PlotEngine pe = PlotFactory.CreatePublishEngine();
                        using (pe)
                        {
                            // Collect all the paperspace layouts
                            // for plotting

                            ObjectIdCollection layoutsToPlot = new ObjectIdCollection();

                            foreach (ObjectId btrId in bt)
                            {
                                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId,OpenMode.ForRead);
                                if (btr.IsLayout && btr.Name.ToUpper() != BlockTableRecord.ModelSpace.ToUpper())
                                {
                                    layoutsToPlot.Add(btrId);
                                }
                            }

                            // Create a Progress Dialog to provide info
                            // and allow thej user to cancel

                            PlotProgressDialog ppd = new PlotProgressDialog(false, layoutsToPlot.Count, true);
                            using (ppd)
                            {
                                int numSheet = 1;

                                foreach (ObjectId btrId in layoutsToPlot)
                                {
                                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                                    Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                                    // We need a PlotSettings object
                                    // based on the layout settings
                                    // which we then customize

                                    PlotSettings ps = new PlotSettings(lo.ModelType);
                                    ps.CopyFrom(lo);

                                    // The PlotSettingsValidator helps
                                    // create a valid PlotSettings object

                                    PlotSettingsValidator psv = PlotSettingsValidator.Current;

                                    // We'll plot the extents, centered and
                                    // scaled to fit

                                    psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);
                                    psv.SetUseStandardScale(ps, true);
                                    psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);
                                    psv.SetPlotCentered(ps, true);

                                    // We'll use the standard DWFx PC3, as
                                    // this supports multiple sheets

                                    psv.SetPlotConfigurationName(ps, devname, medname);

                                    // We need a PlotInfo object
                                    // linked to the layout

                                    pi.Layout = btr.LayoutId;

                                    // Make the layout we're plotting current

                                    LayoutManager.Current.CurrentLayout = lo.LayoutName;

                                    // We need to link the PlotInfo to the
                                    // PlotSettings and then validate it

                                    pi.OverrideSettings = ps;
                                    piv.Validate(pi);

                                    if (numSheet == 1)
                                    {
                                        ppd.set_PlotMsgString(
                                          PlotMessageIndex.DialogTitle,
                                          "Custom Plot Progress"
                                        );
                                        ppd.set_PlotMsgString(
                                          PlotMessageIndex.CancelJobButtonMessage,
                                          "Cancel Job"
                                        );
                                        ppd.set_PlotMsgString(
                                          PlotMessageIndex.CancelSheetButtonMessage,
                                          "Cancel Sheet"
                                        );
                                        ppd.set_PlotMsgString(
                                          PlotMessageIndex.SheetSetProgressCaption,
                                          "Sheet Set Progress"
                                        );
                                        ppd.set_PlotMsgString(
                                          PlotMessageIndex.SheetProgressCaption,
                                          "Sheet Progress"
                                        );
                                        ppd.LowerPlotProgressRange = 0;
                                        ppd.UpperPlotProgressRange = 100;
                                        ppd.PlotProgressPos = 0;

                                        // Let's start the plot, at last

                                        ppd.OnBeginPlot();
                                        ppd.IsVisible = true;
                                        pe.BeginPlot(ppd, null);

                                        // We'll be plotting a single document

                                        pe.BeginDocument(
                                          pi,
                                          doc.Name,
                                          null,
                                          1,
                                          true, // Let's plot to file
                                          "c:\\test-multi-sheet"
                                        );
                                    }

                                    // Which may contains multiple sheets

                                    ppd.set_PlotMsgString(
                                      PlotMessageIndex.SheetName,
                                      doc.Name.Substring(
                                        doc.Name.LastIndexOf("\\") + 1
                                      ) +
                                      " - sheet " + numSheet.ToString() +
                                      " of " + layoutsToPlot.Count.ToString()
                                    );

                                    ppd.OnBeginSheet();

                                    ppd.LowerSheetProgressRange = 0;
                                    ppd.UpperSheetProgressRange = 100;
                                    ppd.SheetProgressPos = 0;

                                    PlotPageInfo ppi = new PlotPageInfo();
                                    pe.BeginPage(
                                      ppi,
                                      pi,
                                      (numSheet == layoutsToPlot.Count),
                                      null
                                    );
                                    pe.BeginGenerateGraphics(null);
                                    ppd.SheetProgressPos = 50;
                                    pe.EndGenerateGraphics(null);

                                    // Finish the sheet
                                    pe.EndPage(null);
                                    ppd.SheetProgressPos = 100;
                                    ppd.OnEndSheet();
                                    numSheet++;
                                    ppd.PlotProgressPos +=
                                      (100 / layoutsToPlot.Count);
                                }

                                // Finish the document

                                pe.EndDocument(null);

                                // And finish the plot

                                ppd.PlotProgressPos = 100;
                                ppd.OnEndPlot();
                                pe.EndPlot(null);
                            }
                        }
                    }
                }
                else
                {
                    ed.WriteMessage(
                      "\nAnother plot is in progress."
                    );
                }
            }
        }
    }
}