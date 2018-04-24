namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data of an Entity Model (Component Presentation or Component)
    /// </summary>
    /// <remarks>
    /// Entity Models for Component Presentations have <see cref="EntityModelData.MvcData"/> representing the MVC data obtained from the Component Template.
    /// Entity Models for (linked) Components do not have <see cref="EntityModelData.MvcData"/>.
    /// </remarks>
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

        /// <summary>
        /// Gets or sets the CM Uri namespace (Either 'tcm' or 'ish' but in future could be something else)
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the component template.
        /// </summary>
        public ComponentTemplateData ComponentTemplate { get; set; }

        /// <summary>
        /// Gets or sets the folder.
        /// </summary>
        public FolderData Folder { get; set; }

        /// <summary>
        /// Gets or sets the custom content.
        /// </summary>
        public ContentModelData Content { get; set; }

        /// <summary>
        /// Gets or sets the binary content of a Multimedia Component.
        /// </summary>
        public BinaryContentData BinaryContent { get; set; }

        /// <summary>
        /// Gets or sets the external content of an ECL Item.
        /// </summary>
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
