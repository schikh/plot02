using System;
using System.Collections.Generic;
using System.Linq;

namespace BatchPlot.Extensions
{
    public static class ListExtension
    {
        public static IEnumerable<string> GroupListItems(this IEnumerable<string> list, double numberOfItemsPerLine)
        {
            var s = Convert.ToInt32(Math.Ceiling(list.Count() / numberOfItemsPerLine));
            for (var j = 0; j < list.Count(); j += s)
            {
                yield return string.Join(", ", list.Skip(j).Take(s));
            }
        }
    }
}