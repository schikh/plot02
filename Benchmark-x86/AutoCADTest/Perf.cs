using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using AutoCADTest;
using AutoCADTest.Service;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Gis.Map;
using Autodesk.Gis.Map.Utilities;
using acApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;
using AutoCADTest.Extension;
using Autodesk.AutoCAD.EditorInput;

[assembly: CommandClass(typeof(Perf))]
namespace AutoCADTest
{
    public class Perf : IExtensionApplication
    {
        private string connectionString =
            "DATA SOURCE=WALLP1_UNWALL.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
        //private string connectionString =
        //    "DATA SOURCE=WALLA1.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
        private List<MapId> mapIds = new List<MapId>();
        private string _fileServerName;
        private Point2d _mapCenterCoordinate;
        private List<object> _results;

        public void Initialize()
        {
            Logger.Setup();
        }

        public void Terminate()
        {
        }

        [CommandMethod("TestAll")]
        public void AllTests()
        {
            GetParameters();
            RunTests();
        }

        [CommandMethod("ContinuousTestAll")]
        public void ContinuousTestAll()
        {
            GetParameters();
            while (true)
            {
                RunTests();
                Thread.Sleep(new TimeSpan(0,2,0));
            }
        }

        public void RunTests()
        {
            GetMachineInfo("###");
            DataConnectionTests();
            DataAccessTests();
            FileCopyTests();
            OpenAndCloseFiles();
            LogResults();
        }

        private void LogResults()
        {
            Helper.Log(string.Join(",", _results.Select(x => x.ToString()).ToArray()));
            Helper.Trace("DONE");
        }

        private void GetMachineInfo(string testName)
        {
            string clockSpeed = "";
            string procName = "";

            //using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"),
            //    win32CompSys = new ManagementObjectSearcher("select * from Win32_ComputerSystem"),
            //    win32Memory = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
            //{
            //    foreach (ManagementObject obj in win32Proc.Get())
            //    {
            //        clockSpeed = obj["CurrentClockSpeed"].ToString();
            //        procName = obj["Name"].ToString();
            //    }
            //}

            _results = new List<object>();
            _results.AddRange(
                new object[] {
                    testName,
                    Environment.MachineName,
                    Environment.OSVersion.ToString(),
                    Environment.Is64BitOperatingSystem,
                    procName,
                    Environment.ProcessorCount,
                    clockSpeed,
                });
        }

        public void DataConnectionTests()
        {
            _results.Add(DataAccessTest(1, 10));
            _results.Add(DataAccessTest(1, 100));
        }

        public void DataAccessTests()
        {
            _results.Add(DataAccessTest(10));
            _results.Add(DataAccessTest(100));
            _results.Add(DataAccessTest(1000));
            _results.Add(DataAccessTest(10000));
        }


        public void FileCopyTests()
        {
            _results.Add(FileCopyTest(10));
            _results.Add(FileCopyTest(100));
            DeleteImportedFiles();
        }

        public void CloseAndOpenFiles2()
        {
            _fileServerName = "RWA004";
            //_fileServerName = "RWA002AEST";
            _mapCenterCoordinate = new Point2d(184000, 127000);
            OpenAndCloseFiles();
        }

        [CommandMethod("TestOpenCloseDwg")]
        public void TestOpenCloseDwg()
        {
            GetParameters();
            GetMachineInfo(">>>");
            try
            {
                HostMapApplicationServices.Application.Aliases.AddNewAlias("C", "C:");
                HostMapApplicationServices.Application.Aliases.AddNewAlias("D", "D:");
                HostMapApplicationServices.Application.Aliases.AddNewAlias("W", "W:");

                var filePaths = ImportFiles(250).ToArray();
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(10), true), CloseFiles(true) });
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(50), true), CloseFiles(true) });
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(100), true), CloseFiles(true) });

                DeleteImportedFiles();
            }
            catch (Exception ex)
            {
                Helper.Log(ex.ToString());
            }
            LogResults();
        }

        public void OpenAndCloseFiles()
        {
            try
            {
                CloseFiles();
                HostMapApplicationServices.Application.Aliases.AddNewAlias("C", "C:");
                HostMapApplicationServices.Application.Aliases.AddNewAlias("D", "D:");
                HostMapApplicationServices.Application.Aliases.AddNewAlias("W", "W:");

                var filePaths = ImportFiles(250).ToArray();
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(10)), CloseFiles() });
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(50)), CloseFiles() });
                _results.AddRange(new object[] { OpenFiles(filePaths.Take(100)), CloseFiles() });

                DeleteImportedFiles();
            }
            catch (Exception ex)
            {
                Helper.Log(ex.ToString());
            }
        }

        public void GetParameters()
        {
            _fileServerName = null;
            _mapCenterCoordinate = new Point2d(0, 0);
            var options = new PromptStringOptions("\nZone (Est|West): ") {DefaultValue = "West"};
            var ed = acApp.DocumentManager.MdiActiveDocument.Editor;
            var pr = ed.GetString(options);
            if (pr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("Command canceled\n");
                return;
            }
            switch (pr.StringResult.ToUpper())
            {
                case "E":
                case "EST":
                    _fileServerName = "RWA004";
                    _mapCenterCoordinate = new Point2d(184000, 127000);
                    break;
                case "W":
                case "WEST":
                    _fileServerName = "RWA005";
                    _mapCenterCoordinate = new Point2d(155500, 122000);
                    break;
                default:
                    ed.WriteMessage("Command canceled\n");
                    break;
            }
        }

        private double FileCopyTest(int top)
        {
            var serverFilePaths = GetServerFilePaths(top);
            var tempFolder = Helper.GetTempFolder();
            var time = Helper.GetExecutionTime(() =>
            {
                foreach (var serverFilePath in serverFilePaths)
                {
                    if (File.Exists(serverFilePath))
                    {
                        var localFilePath = Helper.GetLocalFilePath(serverFilePath, tempFolder);
                        File.Copy(serverFilePath, localFilePath, true);
                    }
                    else
                    {
                        Helper.Trace("### FILE NOT FOUND {0}", serverFilePath);
                    }
                }
            });
            Helper.DeleteTempFolder(tempFolder);
            return time;
        }

        private double DataAccessTest(int top, int iterations = 1)
        {
            var da = new DataAccessService(connectionString);
            //da.ClearDatabaseCaches();
            var query = string.Format("SELECT * "
                            + "FROM DESSIN "
                            + "WHERE serveur='{0}' "
                            + "ORDER BY serveur, path, fileName",
                            _fileServerName, _mapCenterCoordinate.X, _mapCenterCoordinate.Y);
            var time = Helper.GetExecutionTime(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var list = da.IterateOverReader(query, x => new {f = x.GetString(0)}, top);
                }
            });
            return time;
        }

        private List<string> GetServerFilePaths(int top)
        {
            var da = new DataAccessService(connectionString);
            var query = string.Format("SELECT path, fileName "
                + "FROM DESSIN "
                + "WHERE serveur='{0}' "
                + "AND path not like '%#%' "
                + "ORDER BY sqrt(power((xmin+xmax)/2-{1},2) + power((ymin+ymax)/2-{2},2)), path, fileName",
                _fileServerName, _mapCenterCoordinate.X, _mapCenterCoordinate.Y);
            var list = da.IterateOverReader(query,
                    x => Path.Combine(x.GetString(0), x.GetString(1)), top).ToList();
            return list;
        }

        private double OpenFiles(IEnumerable<string> filePaths, bool logTimming = false)
        {
            var count = filePaths.Count();
            var i = 0;
            var time = Helper.GetExecutionTime(() =>
            {
                foreach (var filePath in filePaths)
                {
                    i++;
                    var fileTime = Helper.GetExecutionTime(() =>
                    {
                        var id = OpenTile(filePath);
                        mapIds.Add(id);
                        Zoom();
                        MessagePump.DoEvents();
                    });
                    if (logTimming)
                    {
                        Helper.Log(">>> OPEN FILE {1}/{2}   {3:0.00}   {0}", filePath, i, count, fileTime);
                    }
                    else
                    {
                        Helper.Trace(">>> OPEN FILE {1}/{2}   {3:0.00}   {0}", filePath, i, count, fileTime);
                    }
                }
            });

            return time;
        }

        private double CloseFiles(bool logTimming = false)
        {
            var count = mapIds.Count();
            var i = 0;
            var time = Helper.GetExecutionTime(() =>
            {
                foreach (var id in mapIds.ToArray())
                {
                    i++;
                    var fileTime = Helper.GetExecutionTime(() =>
                    {
                        CloseTile(id);
                        mapIds.Remove(id);
                        Zoom();
                        MessagePump.DoEvents();
                    });
                    if (logTimming)
                    {
                        Helper.Log(">>> CLOSE FILE {0}/{1}   {2:0.00}", i, count, fileTime);
                    }
                    else
                    {
                        Helper.Trace(">>> CLOSE FILE {0}/{1}   {2:0.00}", i, count, fileTime);
                    }                }
            });
            return time;
        }

        private IEnumerable<string> ImportFiles(int top)
        {
            var serverFilePaths = GetServerFilePaths(top);
            var tempFolder = Helper.GetTempFolder();
            foreach (var serverFilePath in serverFilePaths)
            {
                if (File.Exists(serverFilePath) && new FileInfo(serverFilePath).Length <= 1E6)
                {
                    var localFilePath = Helper.GetLocalFilePath(serverFilePath, tempFolder);
                    File.Copy(serverFilePath, localFilePath, true);
                    yield return localFilePath;
                }
                else
                {
                    Helper.Trace("### FILE NOT FOUND {0}", serverFilePath);
                }
            }
        }

        private void DeleteImportedFiles()
        {
            try
            {
                var tempFolder = Helper.GetTempFolder();
                Helper.DeleteTempFolder(tempFolder);
            }
            catch (Exception ex)
            {
                Helper.Log(ex.ToString());
            }
        }

        private void CloseTile(MapId dwgHandle)
        {
            var document = acApp.DocumentManager.MdiActiveDocument;
            using (document.LockDocument())
            {
                using (var attachedDrawing = HostMapApplicationServices.Application.ActiveProject.DrawingSet.AllAttachedDrawings[dwgHandle])
                {
                    //attachedDrawing.LockForRead();
                    attachedDrawing.Preview(new ObjectIdCollection());
                    //attachedDrawing.UnlockForRead();
                    //attachedDrawing.Deactivate();
                }
                HostMapApplicationServices.Application.ActiveProject.DrawingSet.DetachDrawing(dwgHandle);
            }
        }

        private MapId OpenTile(string filename)
        {
            var document = acApp.DocumentManager.MdiActiveDocument;
            using (document.LockDocument())
            {
                var database = document.Database;
                var currentLineType = database.Celtype;
                var currentLayer = database.Clayer;
                var currentColor = database.Cecolor;

                try
                {
                    database.Celtype = database.ContinuousLinetype;
                    database.Clayer = database.LayerZero;

                    using (var attachDrawing = HostMapApplicationServices.Application.ActiveProject.DrawingSet.AttachDrawing(filename))
                    {
                        var dwgHandle = attachDrawing.ObjectId;
                        if (!HostMapApplicationServices.Application.ActiveProject.Options.ActivateDwgsOnAttach)
                        {
                            attachDrawing.Activate();
                        }

                        //attachDrawing.LockForRead();

                        //if (attachDrawing.AllNestedDrawingsCount > 0)
                        //{
                        //    for (int i = 0; i < attachDrawing.AllNestedDrawingsCount; i++)
                        //    {
                        //        using (var nestedDrawing = attachDrawing.AllNestedDrawings[i])
                        //        {
                        //            if (!String.IsNullOrWhiteSpace(nestedDrawing.ActualPath))
                        //            {
                        //                HostMapApplicationServices.Application.Aliases.AddAlias(nestedDrawing.ActualPath.Substring(0, 1), nestedDrawing.ActualPath);
                        //            }
                        //        }
                        //    }
                        //}

                        attachDrawing.Preview();
                        return dwgHandle;
                    }
                }
                finally
                {
                    database.Celtype = currentLineType;
                    database.Clayer = currentLayer;
                    database.Cecolor = currentColor;
                }
            }
        }

        private void Zoom()
        {
            Document acDoc = acApp.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            var pMin = acCurDb.Extmin;
            var pMax = acCurDb.Extmax;
            Zoom(pMin, pMax);
        }

        //private void Zoom()
        //{
        //    Zoom(new Point3d(_mapCenterCoordinate.X - 250, _mapCenterCoordinate.Y - 250, 0), new Point3d(_mapCenterCoordinate.X + 250, _mapCenterCoordinate.Y + 250, 0));
        //}

        private void Zoom(Point3d pMin, Point3d pMax)
        {
            Document acDoc = acApp.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                using (ViewTableRecord vtr = acDoc.Editor.GetCurrentView())
                {
                    vtr.CenterPoint = new Point2d(pMin.X + ((pMax.X - pMin.X) / 2.0), pMin.Y + ((pMax.Y - pMin.Y) / 2.0));
                    vtr.Height = pMax.Y - pMin.Y;
                    vtr.Width = pMax.X - pMin.X;
                    acDoc.Editor.SetCurrentView(vtr);
                    acDoc.Window.Focus();
                }
                acTrans.Commit();
            }
        }
    }
}