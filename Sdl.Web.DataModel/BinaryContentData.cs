using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    public class BinaryContentData : ModelData
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            Url = jObject.GetPropertyValueAsString("Url");
            FileName = jObject.GetPropertyValueAsString("FileName");
            FileSize = jObject.Property("FileSize").Value.Value<long>();
            MimeType = jObject.GetPropertyValueAsString("MimeType");
        }
        #endregion
    }
}
