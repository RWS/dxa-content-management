using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    public class KeywordModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the identifier for the Keyword.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the Keyword
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description of the Keyword
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the key of the Keyword
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the Taxonomy identifier
        /// </summary>
        public string TaxonomyId { get; set; }

        public string MetadataSchemaId { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
