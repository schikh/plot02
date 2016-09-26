using System.Configuration;
using System.Linq;

namespace PlotService2.Configuration
{
    public class QueryTemplates : ConfigurationSection
    {
        internal static readonly QueryTemplates Templates = ConfigurationManager.GetSection("QueryTemplates") as QueryTemplates;

        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public QueryTemplateCollection Instances
        {
            get { return (QueryTemplateCollection)base[""]; }
        }

        public QueryTemplateElement GetTemplate(string name)
        {
            return Instances.Cast<QueryTemplateElement>().FirstOrDefault(x => x.Name == name);
        }
    }

    public class QueryTemplateCollection : ConfigurationElementCollection
    {
        public QueryTemplateElement this[int index]
        {
            get
            {
                return (QueryTemplateElement)BaseGet(index);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new QueryTemplateElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((QueryTemplateElement)element).Name;
        }
    }

    public class QueryTemplateElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
        }

        [ConfigurationProperty("template", IsRequired = true)]
        public string Template
        {
            get { return (string)base["template"]; }
        }
    }
}
