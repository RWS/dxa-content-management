using Newtonsoft.Json.Linq;

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

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            base.Initialize(jObject);

            Id = jObject.GetPropertyValueAsString("Id");
            Title = jObject.GetPropertyValueAsString("Title");
            Description = jObject.GetPropertyValueAsString("Description");
            Key = jObject.GetPropertyValueAsString("Key");
            TaxonomyId = jObject.GetPropertyValueAsString("TaxonomyId");
        }
        #endregion
    }
}
