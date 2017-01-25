using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sdl.Web.DataModel.Configuration
{
    public class SummaryData
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("status")]
        public string Status;

        [JsonProperty("files")]
        public IEnumerable<string> Files;
    }
}
