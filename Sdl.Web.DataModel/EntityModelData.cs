using Newtonsoft.Json.Linq;

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
        public ContentModelData Content { get; set; }
        public ContentModelData Metadata { get; set; }
        public BinaryContentData BinaryContent { get; set; }
        public ExternalContentData ExternalContent { get; set; }

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            base.Initialize(jObject);

            Id = jObject.GetPropertyValueAsString("Id");
            SchemaId = jObject.GetPropertyValueAsString("SchemaId");
            JObject content = jObject.GetPropertyValueAsObject("Content");
            if (content != null)
            {
                Content = new ContentModelData(content);
            }
            JObject metadata = jObject.GetPropertyValueAsObject("Metadata");
            if (metadata != null)
            {
                Metadata = new ContentModelData(metadata);
            }
            BinaryContent = jObject.GetPropertyValueAsModel<BinaryContentData>("BinaryContent");
            ExternalContent = jObject.GetPropertyValueAsModel<ExternalContentData>("ExternalContent");
        }
        #endregion

    }
}
