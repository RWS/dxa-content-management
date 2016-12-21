using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    public class PageModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the identifier for the Page.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Title of the Page which is typically rendered as HTML title tag.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the Page metadata which is typically rendered as HTML meta tags (name/value pairs).
        /// </summary>
        public Dictionary<string, string> Meta { get; set; }

        /// <summary>
        /// Gets the Page Regions.
        /// </summary>
        public List<RegionModelData> Regions { get; set; }

        public ContentModelData Metadata { get; set; }
    }
}
