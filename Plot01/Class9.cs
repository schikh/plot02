using System.Collections.Specialized;
using AutoCADTest;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(Class9))]
namespace AutoCADTest
{
    public class Class9
    {
        [CommandMethod("DeviceAndMedia")]
        public void DeviceAndMedia()
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
            }
        }

        // Lists the available local media names for a specified plot configuration (PC3) file
        [CommandMethod("PlotterLocalMediaNameList")]
        public void PlotterLocalMediaNameList()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            using (PlotSettings plSet = new PlotSettings(true))
            {
                PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                // Set the Plotter and page size
                acPlSetVdr.SetPlotConfigurationName(
                    plSet,
                    "DWG To PDF.pc3",
                    "ANSI_A_(8.50_x_11.00_Inches)");
                acDoc.Editor.WriteMessage("\nCanonical and Local media names: ");

                int cnt = 0;

                foreach (string mediaName in acPlSetVdr.GetCanonicalMediaNameList(plSet))
                {
                    // Output the names of the available media for the specified device
                    acDoc.Editor.WriteMessage(
                        "\n  " + mediaName + " | " +
                        acPlSetVdr.GetLocaleMediaName(plSet, cnt));

                    cnt = cnt + 1;
                }
            }
        }
    }
}
