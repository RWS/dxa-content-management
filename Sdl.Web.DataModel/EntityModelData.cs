namespace Sdl.Web.DataModel
{
    public class EntityModelData : ViewModelData
    {
        /// <summary>
        /// Gets or sets the identifier for the Entity.
        /// </summary>
        /// <remarks>
        /// Note that class <see cref="EntityModelData"/> is also used for complex types which are not really Entities and thus don't have an Identifier.
        /// Therefore, <see cref="Id"/> can be <c>null</c>.
        /// </remarks>
        public string Id { get; set; }

        public ContentModelData Content { get; set; }
        public BinaryContentData BinaryContent { get; set; }
        public ExternalContentData ExternalContent { get; set; }

        /// <summary>
        /// Gets or sets the resolved URL for the Component this Entity Model represents.
        /// </summary>
        /// <remarks>
        /// This property is not set on CM-side, but may be set in the DXA Model Service.
        /// </remarks>
        public string LinkUrl { get; set; }
    }
}
