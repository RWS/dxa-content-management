using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data for a Page Model.
    /// </summary>
    public class PageModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the identifier for the Page.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CM Uri namespace (Either 'tcm' or 'ish' but in future could be something else)
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the title of the Page which is typically rendered as HTML title tag.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the template used by this Page.
        /// </summary>
        public PageTemplateData PageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the structure group id for this page.
        /// </summary>
        public string StructureGroupId { get; set; }

        /// <summary>
        /// Gets or sets the canonical URL path (unencoded) of the Page.
        /// </summary>
        /// <remarks>
        /// The canonical URL path does not have a file extension, but does contain "index" for index Pages.
        /// </remarks>
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets the Page metadata which is typically rendered as HTML meta tags (name/value pairs).
        /// </summary>
        public Dictionary<string, string> Meta { get; set; }

        /// <summary>
        /// Gets or sets the Page Regions.
        /// </summary>
        public List<RegionModelData> Regions { get; set; }
    }
}
