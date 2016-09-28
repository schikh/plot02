using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BatchPlot.Configuration;
using BatchPlot.Enumerations;
using BatchPlot.Extensions;

namespace BatchPlot
{
    public class PlotParameters
    {
        public PlotParameters(string[] args)
        {
            for (var i = 1; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case @"/r":
                        Resolution = int.Parse(args[++i]);
                        break;
                    case @"/c":
                        Category = args[++i];
                        break;
                    case @"/e":
                        Energies = args[++i].Split(',');
                        break;
                    case @"/p":
                        PlotterName = args[++i];
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
                    case @"/oce":
                        Oce = true;
                        break;
                    case @"/z":
                        Zone = (Zone) Enum.Parse(typeof(Zone), args[++i], true);
                        break;
                    case @"/d":
                        Debug = true;
                        break;
                    case @"/st":
                        StampId = args[++i];
                        break;
                    case @"/t":
                        PlanTotal = int.Parse(args[++i]);
                        break;
                    case @"/n":
                        PlanId = int.Parse(args[++i]);
                        break;
                    case @"/u":
                        UserId = args[++i];
                        break;
                    case @"/m":
                        ImagesFolder = args[++i];
                        break;
                    case @"/i":
                    case @"/l":
                    case @"/s":
                        ++i;
                        break;
                    case @"/isolate":
                        break;
                    default:
                        throw new ArgumentException("Unknown argument " + args[i]);
                }
            }
            Validate();
        }

        // PlotCardexEnerGISCommand => no stamp
        // l_id_stamp
        public string StampId { get; private set; }
        // n_tot_plan
        public int PlanTotal { get; private set; }
        // n_ord_plan
        public int PlanId { get; private set; }
        // userid
        public string UserId { get; private set; }
        // l_id_planchette
        public string PlanchetteId { get; private set; }
        // l_path_result_pdf
        public string OutputFilePath { get; private set; }
        // n_scale
        public int Resolution { get; private set; }
        // c_type_map
        public string Category { get; private set; }
        // list_energy
        public string[] Energies { get; private set; }

        public bool Impetrant { get; private set; }

        public bool Oce { get; private set; }

        public string PlotterName { get; private set; }

        public string PlanchetteLetter { get; private set; }

        public Point2d MapCoordinate { get; private set; }

        public Boolean Debug { get; private set; }

        public Zone Zone { get; set; }

        public string ImagesFolder { get; private set; }

        //public string StyleSheet
        //{
        //    get { return Path.GetFileNameWithoutExtension(PlotterName) + ".ctb"; }
        //}

        public Size ViewPortSize
        {
            get { return new Size(PlotConfiguration.Config.DrawingSize.Width * Scale 
                                  + 2 * PlotConfiguration.Config.InternalBorderWidth, 
                PlotConfiguration.Config.DrawingSize.Height * Scale 
                + 2 * PlotConfiguration.Config.InternalBorderWidth); }
        }

        public Point3d ViewPortCenterPoint
        {
            get
            {
                var s = ViewPortSize;
                return new Point3d(s.Width / 2 + PlotConfiguration.Config.ExternalBorderWidth,
                    s.Height / 2 + PlotConfiguration.Config.ExternalBorderWidth, 0);
            }
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
                    MapCoordinate.X + PlotConfiguration.Config.DrawingSize.Width,
                    MapCoordinate.Y + PlotConfiguration.Config.DrawingSize.Height);
            }
        }

        public Extents2d ExternalDrawingExtend
        {
            get
            {
                return DrawingExtend.Inflate(PlotConfiguration.Config.InternalBorderWidth);
            }
        }

        

        public Point2d DrawingCenter
        {
            get
            {
                var e = DrawingExtend;
                return new Point2d((e.MinPoint.X + e.MaxPoint.X) / 2,
                    (e.MinPoint.Y + e.MaxPoint.Y) / 2);
            }
        }

        public Point3d PlotCartridgePosition 
        {
            get
            {
                var position = PlotConfiguration.Config.DrawingSize.Width * Scale
                               + 2 * PlotConfiguration.Config.InternalBorderWidth
                               + 2 * PlotConfiguration.Config.ExternalBorderWidth
                               + PlotConfiguration.Config.CartridgeExternalBorderWidth;
                return new Point3d(position, 0, 0);
            }
        }

        //public Point3d StampPosition 
        //{
        //    get
        //    {
        //        var x = PageSize.Width - PlotConfiguration.Config.CartridgeExternalBorderWidth + 2;
        //        return new Point3d(x, 0, 0);
        //    }
        //}

        public Size PageSize 
        {
            get
            {
                var height = PlotConfiguration.Config.DrawingSize.Height * Scale 
                             + 2 * PlotConfiguration.Config.InternalBorderWidth
                             + 2 * PlotConfiguration.Config.ExternalBorderWidth
                             + 2 * PlotConfiguration.Config.PlotOrigin.X;
                var width = PlotConfiguration.Config.DrawingSize.Width * Scale 
                            + 2 * PlotConfiguration.Config.InternalBorderWidth
                            + 2 * PlotConfiguration.Config.ExternalBorderWidth
                            + PlotConfiguration.Config.PlotCartridgeWidth
                            + 2 * PlotConfiguration.Config.CartridgeExternalBorderWidth
                            + 2 * PlotConfiguration.Config.PlotOrigin.Y;
                return new Size(width, height);
            }
        }

        public string CartridgeTemplate
        {
            get { return Zone == Zone.E ? PlotConfiguration.Config.EstCartridgeTemplateFilePath : PlotConfiguration.Config.WestCartridgeTemplateFilePath; }
        }

        public string FileServerName
        {
            get { return Zone == Zone.E ? PlotConfiguration.Config.EstFileServerName : PlotConfiguration.Config.WestFileServerName; }
        }

        public string Stamp
        {
            get
            {
                return string.Format("ORES {0:dd.MM.yy}-{1}-{2}-{3}/{4}",
                    DateTime.Now, PlotConfiguration.Config.ServerName, StampId, PlanId, PlanTotal);
            }
        }

        public string PlotLayoutName
        {
            get
            {
                return !IsPlanchette ? "Model" : PlotConfiguration.Config.PlotLayoutName;
            }
        }

        public PaperFormat PaperFormat { get; set; }
        
        public IEnumerable<string> EnergyDescription { get; set; }

        public string Pc3Name 
        {
            get
            {
                var paperFormats = PlotConfiguration.Config.GetDefaultPaperFormats(PlotterName);
                if (paperFormats.Any())
                {
                    return paperFormats.First().Pc3Name;
                }
                return PlotterName + ".pc3";
            }
        }

        public bool IsPlanchette 
        {
            get
            {
                return !string.IsNullOrEmpty(PlanchetteId);
            }
        }

        public bool PlotToFile 
        {
            get
            {
                return !string.IsNullOrEmpty(OutputFilePath);
            }
        }

        public Point2d PlotOrigin 
        {
            get
            {
                return PlotConfiguration.Config.PlotOrigin;
            }
        }
        
        private void Validate()
        {
            if (IsPlanchette && string.IsNullOrEmpty(Category))
            {
                throw new ArgumentException("Category (/c) missing");
            }
            if (string.IsNullOrEmpty(PlotterName))
            {
                throw new ArgumentException("the plotter pc3 (/p) required for non pdf or dwf files");
            }
           
            //if (Impetrant && !string.IsNullOrEmpty(OutputFilePath) && Path.GetExtension(OutputFilePath) != ".pdf")
            //{
            //    throw new ArgumentException("Impetrant is for pdf file only");
            //}
            //if (Impetrant && !string.Equals(Pc3Name, "Impetrant.pc3"))
            //{
            //    throw new ArgumentException("Impetrant is for pdf file only");
            //}
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
            if (string.IsNullOrEmpty(PlotterName))
            {
                SetDiviceAndStyleSheet();
            }
        }

        private void SetImpetrant()
        {
            Impetrant = true;
            if (string.IsNullOrEmpty(PlotterName) || PlotterName == "PDF")
            {
                SetDiviceAndStyleSheet();
            }
        }

        private Point2d ParsePlanchetteId(string planchetteId, out string planchetteLetter)
        {
            var regex = new Regex(@"^(?<x>[0-9]{3})(?<y>[0-9]{3})(?<letter>[A-H])$", RegexOptions.IgnoreCase);
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
            if ("ABCD".Contains(planchetteLetter))
            {
                dx = 0;
                dy = "ABCD".IndexOf(planchetteLetter);
            }
            else
            {
                dx = 1;
                dy = "EFGH".IndexOf(planchetteLetter);
            }
            y = Convert.ToInt32(y + dy * PlotConfiguration.Config.DrawingSize.Height);
            x = Convert.ToInt32(x + dx * PlotConfiguration.Config.DrawingSize.Width);
            return new Point2d(x, y);
        }

        private void SetDiviceAndStyleSheet()
        {
            if (Impetrant)
            {
                PlotterName = "Impetrant";
            }
            else
            {
                switch (Path.GetExtension(OutputFilePath).ToLower())
                {
                    case ".pdf":
                        PlotterName = "Impetrant";
                        break;
                    case ".dwf":
                    case ".dwg":
                        PlotterName = "DWF6";
                        break;
                }
            }
        }
    }
}