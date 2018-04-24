namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data of a Keyword Model
    /// </summary>
    public class KeywordModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the identifier for the Keyword.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CM Uri namespace (Either 'tcm' or 'ish' but in future could be something else)
        /// </summary>
        public string Namespace { get; set; }

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
        /// Gets or sets the Taxonomy/Category identifier
        /// </summary>
        public string TaxonomyId { get; set; }
    }
}
