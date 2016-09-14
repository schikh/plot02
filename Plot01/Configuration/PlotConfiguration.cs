using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.Geometry;

namespace BatchPlot.Configuration
{
    internal class PlotConfiguration : ConfigurationSection
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

        [ConfigurationProperty("PageFormats", IsRequired = false, IsDefaultCollection = true)]
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
        public Size PlotPaperSize
        {
            get { return (Size)this["PlotPaperSize"]; }
            set { base["PlotPaperSize"] = value; }
        }
    }
}
