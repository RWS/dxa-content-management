using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    public class ExternalContentData : ModelData
    {
        public string Id { get; set; }
        public string DisplayTypeId { get; set; }
        public ContentModelData Metadata { get; set; }

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            Id = jObject.GetPropertyValueAsString("Id");
            DisplayTypeId = jObject.GetPropertyValueAsString("DisplayTypeId");
            JObject metadata = jObject.GetPropertyValueAsObject("Metadata");
            if (metadata != null)
            {
                Metadata = new ContentModelData(metadata);
            }
        }
        #endregion

    }
}
