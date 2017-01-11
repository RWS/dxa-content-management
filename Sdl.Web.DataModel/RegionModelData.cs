using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    public class RegionModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the name of the Region.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the Entities that the Region contains.
        /// </summary>
        public List<EntityModelData> Entities { get; set; }

        /// <summary>
        /// Gets the (nested) Regions within this Region.
        /// </summary>
        public List<RegionModelData> Regions { get; set; }

        public string IncludePageUrl { get; set; }
    }
}
