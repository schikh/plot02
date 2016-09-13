using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.Geometry;

namespace BatchPlot.Configuration2
{
    internal class PlotConfiguration : ConfigurationSection
    {
        public static readonly PlotConfiguration Config = (PlotConfiguration)ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).Sections["PlotConfiguration"];

        [ConfigurationProperty("PlotLayoutName")]
        public string PlotLayoutName
        {
            get { return (string)this["PlotLayoutName"]; }
        }

        [ConfigurationProperty("DrawingSize")]
        public Size DrawingSize
        {
            get
            {
                return (Size) this["DrawingSize"];
                var s = ((string)this["DrawingSize"]).Split(',');
                return new Size(int.Parse(s[0]), int.Parse(s[0]));
            }
        }

        [ConfigurationProperty("ExternalBorderWidth")]
        public int ExternalBorderWidth
        {
            get { return (int)this["ExternalBorderWidth"]; }
        }

        [ConfigurationProperty("InternalBorderWidth")]
        public int InternalBorderWidth
        {
            get { return (int)this["InternalBorderWidth"]; }
        }

        [ConfigurationProperty("CartridgeExternalBorderWidth")]
        public double CartridgeExternalBorderWidth
        {
            get { return (double)this["CartridgeExternalBorderWidth"]; }
        }

        [ConfigurationProperty("PlotCartridgeWidth")]
        public int PlotCartridgeWidth
        {
            get { return (int)this["PlotCartridgeWidth"]; }
        }

        [ConfigurationProperty("PlotOrigin")]
        private string PlotOriginHidden
        {
            get
            {
                return (string)this["PlotOrigin"];
            }
        }

        public Point2d PlotOrigin
        {
            get
            {
                var s = PlotOriginHidden.Split(',');
                return new Point2d(int.Parse(s[0]), int.Parse(s[1]));
            }
        }

        [ConfigurationProperty("EstCartridgeTemplateFilePath")]
        public string EstCartridgeTemplateFilePath
        {
            get { return (string)this["EstCartridgeTemplateFilePath"]; }
        }

        [ConfigurationProperty("WestCartridgeTemplateFilePath")]
        public string WestCartridgeTemplateFilePath
        {
            get { return (string)this["WestCartridgeTemplateFilePath"]; }
        }

        [ConfigurationProperty("DefaultStyleSheet")]
        public string DefaultStyleSheet
        {
            get { return (string)this["DefaultStyleSheet"]; }
        }

        [ConfigurationProperty("ConnectionString")]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
        }

        [ConfigurationProperty("WestFileServerName")]
        public string WestFileServerName
        {
            get { return (string)this["WestFileServerName"]; }
        }

        [ConfigurationProperty("EstFileServerName")]
        public string EstFileServerName
        {
            get { return (string)this["EstFileServerName"]; }
        }

        [ConfigurationProperty("ServerName")]
        public string ServerName
        {
            get { return (string)this["ServerName"]; }
        }

        [ConfigurationProperty("TopoLayersRegexFilter")]
        public string TopoLayersRegexFilter
        {
            get { return (string)this["TopoLayersRegexFilter"]; }
        }

        [ConfigurationProperty("LayersToDeleteRegexFilter")]
        public string LayersToDeleteRegexFilter
        {
            get { return (string)this["LayersToDeleteRegexFilter"]; }
        }

        [ConfigurationProperty("PageFormats", IsRequired = true, IsDefaultCollection = true)]
        public PlotConfigurationCollection PageFormats
        {
            get { return (PlotConfigurationCollection)base["PageFormats"]; }
        }

        public IEnumerable<PageFormat> GetDefaultPageFormat(string pc3Name)
        {
            return PageFormats.Cast<PageFormat>().Where(x => x.Pc3Name == pc3Name);
        }
    }

    internal class PlotConfigurationCollection : ConfigurationElementCollection
    {
        public PageFormat this[int index]
        {
            get { return (PageFormat)BaseGet(index); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PageFormat();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var e = (PageFormat)element;
            return e.Pc3Name + e.CanonicalMediaName;
        }
    }

    internal class PageFormat : ConfigurationElement
    {
        [ConfigurationProperty("Pc3Name", IsRequired = true)]
        public string Pc3Name
        {
            get { return (string)base["Pc3Name"]; }
            set { base["Pc3Name"] = value; }
        }

        [ConfigurationProperty("CanonicalMediaName", IsRequired = true)]
        public string CanonicalMediaName
        {
            get { return (string)base["CanonicalMediaName"]; }
            set { base["CanonicalMediaName"] = value; }
        }

        [ConfigurationProperty("PlotPaperSize", IsRequired = true)]
        private Size PlotPaperSizeHidden
        {
            get { return (Size)this["PlotPaperSize"]; }
            set { base["PlotPaperSize"] = value; }
        }

        public Point2d PlotPaperSize
        {
            get { return new Point2d(PlotPaperSizeHidden.Width, PlotPaperSizeHidden.Height); }
            set { PlotPaperSizeHidden = new Size(value.X, value.Y); }
        }
    }


    // [ConfigurationProperty("PlotLayoutName")] public string  TextSize { get { return (string )this["PlotLayoutName"]; } set { this["PlotLayoutName"] = value; } }
    // [ConfigurationProperty("DrawingSize")] public Size    TextSize { get { return (Size   )this["DrawingSize"]; } set { this["DrawingSize"] = value; } }
    // [ConfigurationProperty("ExternalBorderWidth")] public int     TextSize { get { return (int    )this["ExternalBorderWidth"]; } set { this["ExternalBorderWidth"] = value; } }
    // [ConfigurationProperty("InternalBorderWidth")] public int     TextSize { get { return (int    )this["InternalBorderWidth"]; } set { this["InternalBorderWidth"] = value; } }
    // [ConfigurationProperty("CartridgeExternalBorderWidth")] public double  TextSize { get { return (double )this["CartridgeExternalBorderWidth"]; } set { this["CartridgeExternalBorderWidth"] = value; } }
    // [ConfigurationProperty("PlotCartridgeWidth")] public int     TextSize { get { return (int    )this["PlotCartridgeWidth"]; } set { this["PlotCartridgeWidth"] = value; } }
    // [ConfigurationProperty("PlotOrigin")] public Point2d TextSize { get { return (Point2d)this["PlotOrigin"]; } set { this["PlotOrigin"] = value; } }
    // [ConfigurationProperty("EstCartridgeTemplateFilePath")] public string  TextSize { get { return (string )this["EstCartridgeTemplateFilePath"]; } set { this["EstCartridgeTemplateFilePath"] = value; } }
    // [ConfigurationProperty("WestCartridgeTemplateFilePath")] public string  TextSize { get { return (string )this["WestCartridgeTemplateFilePath"]; } set { this["WestCartridgeTemplateFilePath"] = value; } }
    // [ConfigurationProperty("DefaultStyleSheet")] public string  TextSize { get { return (string )this["DefaultStyleSheet"]; } set { this["DefaultStyleSheet"] = value; } }
    // [ConfigurationProperty("ConnectionString")] public string  TextSize { get { return (string )this["ConnectionString"]; } set { this["ConnectionString"] = value; } }
    // [ConfigurationProperty("WestFileServerName")] public string  TextSize { get { return (string )this["WestFileServerName"]; } set { this["WestFileServerName"] = value; } }
    // [ConfigurationProperty("EstFileServerName")] public string  TextSize { get { return (string )this["EstFileServerName"]; } set { this["EstFileServerName"] = value; } }
    // [ConfigurationProperty("ServerName")] public string  TextSize { get { return (string )this["ServerName"]; } set { this["ServerName"] = value; } }
    // [ConfigurationProperty("TopoLayersRegexFilter")] public string  TextSize { get { return (string )this["TopoLayersRegexFilter"]; } set { this["TopoLayersRegexFilter"] = value; } }
    // [ConfigurationProperty("LayersToDeleteRegexFilter")] public string  TextSize { get { return (string )this["LayersToDeleteRegexFilter"]; } set { this["LayersToDeleteRegexFilter"] = value; } }


    //public static string  PlotLayoutName                = "Energis_plot_layout_name";
    //public static Size    DrawingSize                   = new Size(500, 250);
    //public static int     ExternalBorderWidth           = 5;
    //public static int     InternalBorderWidth           = 10;
    //public static double  CartridgeExternalBorderWidth  = 10;
    //public static int     PlotCartridgeWidth            = 190;
    //public static Point2d PlotOrigin                    = new Point2d(5, 5);
    //public static string  EstCartridgeTemplateFilePath  = @"C:\\Test\\Plot\\Plot01\\Scripts\\Gis_cstd_est.dwg";
    //public static string  WestCartridgeTemplateFilePath = @"C:\\Test\\Plot\\Plot01\\Scripts\\Gis_cstd_ouest.dwg";
    //public static string  DefaultStyleSheet             = "Default.ctb";
    //public static string  ConnectionString              = "DATA SOURCE=WALLP1_UNWALL.WORLD;USER ID=GENERGIS;PASSWORD=GENERGIS87;PERSIST SECURITY INFO=True;Pooling=false;";
    //public static string  WestFileServerName            = "RWA005";
    //public static string  EstFileServerName             = "RWA004";
    //public static string  ServerName                    = "XXXXXXXXXX";
    //public static string  TopoLayersRegexFilter         = @"^BR\d\d|BB\d\d|BF\d\d|BH\d\d|BG\d\d|BL\d\d|BP\d\d|BT\d\d|BW\d\d|W\d{4}$";
    //public static string  LayersToDeleteRegexFilter     = @"^BR05|niv_voirie|w0001|W0060|W0070$";

}
