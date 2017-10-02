using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tridion.ContentManager.Templating;

namespace DD4T.Templates.Base.Utils
{
    /// <summary>
    /// static helper methods to support SI4T rendered content within cp's
    /// </summary>
    public class Si4tUtils
    {
        private static Regex search_directive_pattern = new Regex(@"(?ims)<!--\s*INDEX-DATA-START:(.*?):INDEX-DATA-END\s*-->", RegexOptions.Compiled);
        private static TemplatingLogger log = TemplatingLogger.GetLogger(typeof(Si4tUtils));
        public static string RetrieveSearchData(string renderedContent)
        {
            var matches = search_directive_pattern.Match(renderedContent);

            if (!matches.Success)
                return string.Empty;


            log.Debug("found search data.");
            return matches.Value;
        }

        public static string RemoveSearchData(string renderedCotnent)
        {
            var matches = search_directive_pattern.Match(renderedCotnent);

            if (!matches.Success)
                return renderedCotnent;

            log.Debug("Found search data, about the strip it from the rendered content");
            return renderedCotnent.Replace(matches.Value, string.Empty);

        }
    } 
}
