using Newtonsoft.Json;

namespace Sdl.Web.DataModel.Configuration
{
    public class BootstrapData
    {
        [JsonProperty("files")]
        public string[] Files;
    }
}
