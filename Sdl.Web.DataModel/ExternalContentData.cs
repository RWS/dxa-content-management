namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the External Content of an (ECL Stub) Component.
    /// </summary>
    /// <seealso cref="EntityModelData.ExternalContent"/>
    public class ExternalContentData
    {
        /// <summary>
        /// Gets or sets the external identifier (ECL URI).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ECL Display Type Identifier.
        /// </summary>
        public string DisplayTypeId { get; set; }

        /// <summary>
        /// Gets or sets the template fragment provided by the ECL Provider.
        /// </summary>
        public string TemplateFragment { get; set; }

        /// <summary>
        /// Gets or sets the metadata retrieved from the external system.
        /// </summary>
        public ContentModelData Metadata { get; set; }
    }
}
