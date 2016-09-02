using System.Collections.Specialized;
using AutoCADTest;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(Plotting))]
namespace AutoCADTest
{
    public class Plotting
    {
        [CommandMethod("getCanName")]
        public void getCanName()
        {
            var xxx = PlottingCommands.ChooseDeviceAndMedia();

            var strPlotSetName = "DWG To PDF";
            var PlotSettingExists = false;

            StringCollection strMediaNameList = null;

            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            var db = editor.Document.Database;


            PlotSettingsValidator psv2 = PlotSettingsValidator.Current;

            // Let's first select the device

            StringCollection devlist = psv2.GetPlotDeviceList();

            using (var t = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // get PlotSettings Dictionary
                    var psDicts = (DBDictionary)t.GetObject(db.PlotSettingsDictionaryId, OpenMode.ForRead);
                    foreach (System.Collections.DictionaryEntry psDict in psDicts)
                    {
                        var o = psDict.Value;
                        var key = (string)psDict.Key;
                        if (key == strPlotSetName)
                        {
                            // find the plot setting
                            PlotSettingExists = true;
                        }
                    }

                    if (PlotSettingExists)
                    {
                        var plotsettingdic = (DBDictionary)t.GetObject(db.PlotSettingsDictionaryId, OpenMode.ForRead);
                        var setting = (PlotSettings)t.GetObject(plotsettingdic.GetAt(strPlotSetName), OpenMode.ForWrite);
                        var psv = PlotSettingsValidator.Current;
                        psv.RefreshLists(setting);
                        // get the media name list
                        strMediaNameList = psv.GetCanonicalMediaNameList(setting);
                        editor.WriteMessage("Media Name List:\n");
                        foreach (var str in strMediaNameList)
                        {
                            editor.WriteMessage(str + "\n");
                        }
                    }
                    t.Commit();
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage(ex.Message);
                }
            }
        }
    }
}
