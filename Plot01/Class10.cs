using System;
using System.Collections.Specialized;
using AutoCADTest;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Exception = System.Exception;

[assembly: CommandClass(typeof(Class10))]
namespace AutoCADTest
{
    class Class10
    {
        [CommandMethod("SetClosestMediaNameCmd")]
        public void SetClosestMediaNameCmd()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PlotSettingsValidator psv = PlotSettingsValidator.Current;

            // Let's first select the device
            //StringCollection devlist = psv.GetPlotDeviceList();
            var devlist = new[] { "PDF.pc3" };
            
            for (int i                 = 0; i < devlist.Length; ++i)
            {
                string device = devlist[i];
                ed.WriteMessage("\nDevice: {0} - {1}", i + 1, devlist[i]);

                using (Transaction Tx = db.TransactionManager.StartTransaction())
                {
                    LayoutManager layoutMgr = LayoutManager.Current;
                    var layout = (Layout) Tx.GetObject(
                        layoutMgr.GetLayoutId(layoutMgr.CurrentLayout),
                        OpenMode.ForWrite);
                    setClosestMediaName(psv, device, layout);
                    //Tx.Commit();
                }
            }
        }

        void setClosestMediaName(PlotSettingsValidator psv2,
            string device, Layout layout)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PlotSettingsValidator psv = PlotSettingsValidator.Current;
            psv.SetPlotType(layout, PlotType.Extents);
            psv.SetPlotPaperUnits(layout, PlotPaperUnit.Millimeters);
            psv.SetUseStandardScale(layout, false);
            psv.SetStdScaleType(layout, StdScaleType.ScaleToFit);
            psv.SetPlotConfigurationName(layout, device, null);
            psv.RefreshLists(layout);

            StringCollection mediaList = psv.GetCanonicalMediaNameList(layout);

            double smallestOffset = 0.0;
            string selectedMedia = string.Empty;
            PlotRotation selectedRot = PlotRotation.Degrees000;

            foreach (string media in mediaList)
            {
                try
                {
                    psv.SetCanonicalMediaName(layout, media);
                    psv.SetPlotPaperUnits(layout, PlotPaperUnit.Millimeters);

                    double mediaPageWidth = layout.PlotPaperSize.X;
                    double mediaPageHeight = layout.PlotPaperSize.Y;

                    if (true)
                    {
                        mediaPageWidth -=
                            (layout.PlotPaperMargins.MinPoint.X +
                             layout.PlotPaperMargins.MaxPoint.X);

                        mediaPageHeight -=
                            (layout.PlotPaperMargins.MinPoint.Y +
                             layout.PlotPaperMargins.MaxPoint.Y);
                    }

                    ed.WriteMessage("\n   media: {6}   {0}-{1}  Min:{2}-{3}  Max:{4}-{5}",
                        layout.PlotPaperSize.X, layout.PlotPaperSize.Y,
                        layout.PlotPaperMargins.MinPoint.X, layout.PlotPaperMargins.MinPoint.Y,
                        layout.PlotPaperMargins.MaxPoint.X, layout.PlotPaperMargins.MaxPoint.Y,
                        media);
                }
                catch (Exception ex)
                {
                    ed.WriteMessage("\n   media: {0} error", media);
                }

                //PlotRotation rotationType = PlotRotation.Degrees090;

                //Check that we are not outside the media print area
                //if (mediaPageWidth < pageWidth ||
                //   mediaPageHeight < pageHeight)
                //{
                //    //Check if 90°Rot will fit, otherwise check next media
                //    if (mediaPageHeight < pageWidth ||
                //       mediaPageWidth >= pageHeight)
                //    {
                //        //Too small, let's check next media
                //        continue;
                //    }

                //    //That's ok 90°Rot will fit
                //    rotationType = PlotRotation.Degrees090;
                //}

                //double offset = Math.Abs(mediaPageWidth * mediaPageHeight -
                //    pageWidth * pageHeight);

                //if (selectedMedia == string.Empty || offset < smallestOffset)
                //{
                //    selectedMedia = media;
                //    smallestOffset = offset;
                //    selectedRot = rotationType;

                //    //Found perfect match so we can quit early
                //    if (smallestOffset == 0)
                //        break;
                //}
            }

            //psv.SetCanonicalMediaName(layout, selectedMedia);
            //psv.SetPlotRotation(layout, selectedRot);

            //string localMedia = psv.GetLocaleMediaName(layout, selectedMedia);

            //Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            //ed.WriteMessage("\n - Closest Media: " + localMedia);
            //ed.WriteMessage("\n - Offset: " + smallestOffset.ToString());
            //ed.WriteMessage("\n - Rotation: " + selectedRot.ToString());
        }


        
        //[CommandMethod("SetClosestMediaNameCmd")]
        //public void SetClosestMediaNameCmd()
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    PlotSettingsValidator psv = PlotSettingsValidator.Current;

        //    // Let's first select the device
        //    StringCollection devlist = psv.GetPlotDeviceList();

        //    ed.WriteMessage("\n--- Plotting Devices ---");

        //    for (int i = 0; i < devlist.Count; ++i)
        //    {
        //        ed.WriteMessage("\n{0} - {1}", i + 1, devlist[i]);
        //    }

        //    PromptIntegerOptions opts = new PromptIntegerOptions(
        //        "\nEnter device number: ");

        //    opts.LowerLimit = 1;
        //    opts.UpperLimit = devlist.Count;
        //    PromptIntegerResult pir = ed.GetInteger(opts);

        //    if (pir.Status != PromptStatus.OK)
        //        return;

        //    string device = devlist[pir.Value - 1];

        //    PromptDoubleOptions pdo1 = new PromptDoubleOptions(
        //        "\nEnter Media Height(mm): ");

        //    PromptDoubleResult pdr1 = ed.GetDouble(pdo1);

        //    if (pdr1.Status != PromptStatus.OK)
        //        return;

        //    PromptDoubleOptions pdo2 = new PromptDoubleOptions(
        //        "\nEnter Media Width(mm): ");

        //    PromptDoubleResult pdr2 = ed.GetDouble(pdo2);

        //    if (pdr2.Status != PromptStatus.OK)
        //        return;

        //    using (Transaction Tx = db.TransactionManager.StartTransaction())
        //    {
        //        LayoutManager layoutMgr = LayoutManager.Current;

        //        Layout layout = Tx.GetObject(
        //            layoutMgr.GetLayoutId(layoutMgr.CurrentLayout),
        //            OpenMode.ForWrite)
        //                 as Layout;

        //        setClosestMediaName(
        //            psv, device, layout,
        //            pdr1.Value, pdr2.Value,
        //            PlotPaperUnit.Millimeters, true);

        //        Tx.Commit();
        //    }
        //}

        //void setClosestMediaName(PlotSettingsValidator psv,
        //    string device,
        //    Layout layout,
        //    double pageWidth,
        //    double pageHeight,
        //    PlotPaperUnit units,
        //    bool matchPrintableArea)
        //{
        //    psv.SetPlotType(
        //        layout,
        //        Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);

        //    psv.SetPlotPaperUnits(layout, units);
        //    psv.SetUseStandardScale(layout, false);
        //    psv.SetStdScaleType(layout, StdScaleType.ScaleToFit);
        //    psv.SetPlotConfigurationName(layout, device, null);
        //    psv.RefreshLists(layout);

        //    StringCollection mediaList =
        //        psv.GetCanonicalMediaNameList(layout);

        //    double smallestOffset = 0.0;
        //    string selectedMedia = string.Empty;
        //    PlotRotation selectedRot = PlotRotation.Degrees000;

        //    foreach (string media in mediaList)
        //    {
        //        psv.SetCanonicalMediaName(layout, media);
        //        psv.SetPlotPaperUnits(layout, units);

        //        double mediaPageWidth = layout.PlotPaperSize.X;
        //        double mediaPageHeight = layout.PlotPaperSize.Y;

        //        if (matchPrintableArea)
        //        {
        //            mediaPageWidth -=
        //                (layout.PlotPaperMargins.MinPoint.X +
        //                 layout.PlotPaperMargins.MaxPoint.X);

        //            mediaPageHeight -=
        //                (layout.PlotPaperMargins.MinPoint.Y +
        //                 layout.PlotPaperMargins.MaxPoint.Y);
        //        }

        //        PlotRotation rotationType = PlotRotation.Degrees090;

        //        //Check that we are not outside the media print area
        //        if (mediaPageWidth < pageWidth ||
        //           mediaPageHeight < pageHeight)
        //        {
        //            //Check if 90°Rot will fit, otherwise check next media
        //            if (mediaPageHeight < pageWidth ||
        //               mediaPageWidth >= pageHeight)
        //            {
        //                //Too small, let's check next media
        //                continue;
        //            }

        //            //That's ok 90°Rot will fit
        //            rotationType = PlotRotation.Degrees090;
        //        }

        //        double offset = Math.Abs(
        //            mediaPageWidth * mediaPageHeight -
        //            pageWidth * pageHeight);

        //        if (selectedMedia == string.Empty || offset < smallestOffset)
        //        {
        //            selectedMedia = media;
        //            smallestOffset = offset;
        //            selectedRot = rotationType;

        //            //Found perfect match so we can quit early
        //            if (smallestOffset == 0)
        //                break;
        //        }
        //    }

        //    psv.SetCanonicalMediaName(layout, selectedMedia);
        //    psv.SetPlotRotation(layout, selectedRot);

        //    string localMedia = psv.GetLocaleMediaName(
        //        layout,
        //        selectedMedia);

        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        //    ed.WriteMessage("\n - Closest Media: " + localMedia);
        //    ed.WriteMessage("\n - Offset: " + smallestOffset.ToString());
        //    ed.WriteMessage("\n - Rotation: " + selectedRot.ToString());
        //}
    }
}
