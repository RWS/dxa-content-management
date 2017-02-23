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
        /// Gets or sets the title of the Page which is typically rendered as HTML title tag.
        /// </summary>
        public string Title { get; set; }

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
