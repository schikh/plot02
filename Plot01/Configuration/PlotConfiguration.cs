using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.Geometry;

namespace BatchPlot.Configuration
{
    public class PlotConfiguration : ConfigurationSection
    {
        public static readonly PlotConfiguration Config = (PlotConfiguration)ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).Sections["PlotConfiguration"];

        [ConfigurationProperty("PlotLayoutName", IsRequired = true)]
        public string PlotLayoutName
        {
            get { return (string)this["PlotLayoutName"]; }
        }

        [ConfigurationProperty("DrawingSize", IsRequired = true)]
        public Size DrawingSize
        {
            get { return (Size) this["DrawingSize"]; }
        }

        [ConfigurationProperty("ExternalBorderWidth", IsRequired = true)]
        public int ExternalBorderWidth
        {
            get { return (int)this["ExternalBorderWidth"]; }
        }

        [ConfigurationProperty("InternalBorderWidth", IsRequired = true)]
        public int InternalBorderWidth
        {
            get { return (int)this["InternalBorderWidth"]; }
        }

        [ConfigurationProperty("CartridgeExternalBorderWidth", IsRequired = true)]
        public double CartridgeExternalBorderWidth
        {
            get { return (double)this["CartridgeExternalBorderWidth"]; }
        }

        [ConfigurationProperty("PlotCartridgeWidth", IsRequired = true)]
        public int PlotCartridgeWidth
        {
            get { return (int)this["PlotCartridgeWidth"]; }
        }

        [ConfigurationProperty("PlotOrigin", IsRequired = true)]
        private Size PlotOriginHidden
        {
            get { return (Size)this["PlotOrigin"]; }
        }

        public Point2d PlotOrigin
        {
            get { return new Point2d(PlotOriginHidden.Width, PlotOriginHidden.Height); }
        }

        [ConfigurationProperty("EstCartridgeTemplateFilePath", IsRequired = true)]
        public string EstCartridgeTemplateFilePath
        {
            get { return (string)this["EstCartridgeTemplateFilePath"]; }
        }

        [ConfigurationProperty("WestCartridgeTemplateFilePath", IsRequired = true)]
        public string WestCartridgeTemplateFilePath
        {
            get { return (string)this["WestCartridgeTemplateFilePath"]; }
        }

        [ConfigurationProperty("DefaultStyleSheet", IsRequired = true)]
        public string DefaultStyleSheet
        {
            get { return (string)this["DefaultStyleSheet"]; }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return (string)this["ConnectionString"]; }
        }

        [ConfigurationProperty("WestFileServerName", IsRequired = true)]
        public string WestFileServerName
        {
            get { return (string)this["WestFileServerName"]; }
        }

        [ConfigurationProperty("EstFileServerName", IsRequired = true)]
        public string EstFileServerName
        {
            get { return (string)this["EstFileServerName"]; }
        }

        [ConfigurationProperty("WestFileServerUncName", IsRequired = true)]
        public string WestFileServerUncName
        {
            get { return (string)this["WestFileServerUncName"]; }
        }

        [ConfigurationProperty("EstFileServerUncName", IsRequired = true)]
        public string EstFileServerUncName
        {
            get { return (string)this["EstFileServerUncName"]; }
        }

        [ConfigurationProperty("ServerName", IsRequired = true)]
        public string ServerName
        {
            get { return (string)this["ServerName"]; }
        }

        [ConfigurationProperty("TopoLayersRegexFilter", IsRequired = true)]
        public string TopoLayersRegexFilter
        {
            get { return (string)this["TopoLayersRegexFilter"]; }
        }

        [ConfigurationProperty("LayersToDeleteRegexFilter", IsRequired = true)]
        public string LayersToDeleteRegexFilter
        {
            get { return (string)this["LayersToDeleteRegexFilter"]; }
        }

        [ConfigurationProperty("PaperFormats", IsRequired = false, IsDefaultCollection = true)]
        public PlotConfigurationCollection PaperFormats
        {
            get { return (PlotConfigurationCollection)base["PaperFormats"]; }
        }

        public IEnumerable<PaperFormat> GetDefaultPaperFormats(string plotterName)
        {
            return PaperFormats.Cast<PaperFormat>().Where(x => x.PlotterName == plotterName);
        }
    }

    public class PlotConfigurationCollection : ConfigurationElementCollection
    {
        public PaperFormat this[int index]
        {
            get { return (PaperFormat)BaseGet(index); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PaperFormat();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var e = (PaperFormat)element;
            return e.PlotterName + e.CanonicalMediaName;
        }
    }
    
    public class PaperFormat : ConfigurationElement
    {
        [ConfigurationProperty("PlotterName", IsRequired = true)]
        public string PlotterName
        {
            get { return (string)base["PlotterName"]; }
            set { base["PlotterName"] = value; }
        }

        [ConfigurationProperty("Pc3Name")]
        public string Pc3Name
        {
            get
            {
                var value = (string) base["Pc3Name"];
                return string.IsNullOrEmpty(value) ? PlotterName + ".pc3" : value;
            }
            set { base["Pc3Name"] = value; }
        }

        [ConfigurationProperty("CtbName")]
        public string CtbName
        {
            get
            {
                var value = (string) base["CtbName"];
                return string.IsNullOrEmpty(value) ? PlotterName + ".ctb" : value;
            }
            set { base["CtbName"] = value; }
        }

        [ConfigurationProperty("CanonicalMediaName", IsRequired = true)]
        public string CanonicalMediaName
        {
            get { return (string)base["CanonicalMediaName"]; }
            set { base["CanonicalMediaName"] = value; }
        }

        //[ConfigurationProperty("FeederType", IsRequired = true)]
        //public string FeederType
        //{
        //    get { return (string)base["FeederType"]; }
        //    set { base["FeederType"] = value; }
        //}

        //[ConfigurationProperty("IsDefaultPaperSize")]
        //public bool IsDefaultPaperSize
        //{
        //    get { return (bool)base["IsDefaultPaperSize"]; }
        //    set { base["IsDefaultPaperSize"] = value; }
        //}

        [ConfigurationProperty("PlotPaperSize", IsRequired = true)]
        public Size PlotPaperSize
        {
            get { return (Size)this["PlotPaperSize"]; }
            set { base["PlotPaperSize"] = value; }
        }

        public bool ShrinkDrawing { get; set; }

        public Point2d PlotOrigin { get; set; }
    }
}
