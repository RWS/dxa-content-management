using System.Collections.Generic;

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

        public string SchemaId { get; set; }
        public Dictionary<string, object> Content { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public BinaryContentData BinaryContent { get; set; }
        public ExternalContentData ExternalContent { get; set; }
    }
}
