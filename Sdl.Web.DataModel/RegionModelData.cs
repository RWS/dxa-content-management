using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data for a Region Model.
    /// </summary>
    public class RegionModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the name of the Region.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Entities that the Region contains.
        /// </summary>
        public List<EntityModelData> Entities { get; set; }

        /// <summary>
        /// Gets or sets the (nested) Regions within this Region.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) for "regular" Regions; it's currently only used for Include Page Regions.
        /// </value>
        public List<RegionModelData> Regions { get; set; }

        /// <summary>
        /// Gets or sets the Identifier of the Include Page which this Region represents (if any).
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) for "regular" Regions.
        /// </value>
        public string IncludePageId { get; set; }
    }
}
