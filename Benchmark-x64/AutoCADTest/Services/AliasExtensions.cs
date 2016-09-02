using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Gis.Map;

namespace AutoCADTest.Extension
{
    public static class AliasExtensions
    {
        public static void AddNewAlias(this Aliases aliases, string name, string path)
        {
            var found = false;
            for (var j = 0; j < aliases.AliasesCount & found == false; j++)
            {
                found = string.Equals(aliases[j].Path, path, StringComparison.InvariantCultureIgnoreCase);
            }
            if (!found)
            {
                aliases.AddAlias(name, path);
            }
        }
    }
}
