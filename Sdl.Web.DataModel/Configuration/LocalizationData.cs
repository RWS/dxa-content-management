using Newtonsoft.Json;

namespace Sdl.Web.DataModel.Configuration
{
    /// <summary>
    /// Represents the (JSON) data for a Localization as stored in /system/config/_all.json ("config bootstrap")
    /// </summary>
    public class LocalizationData
    {
        [JsonProperty("defaultLocalization")]
        public bool IsDefaultLocalization { get; set; }

        [JsonProperty("staging")]
        public bool IsXpmEnabled { get; set; }

        [JsonProperty("mediaRoot")]
        public string MediaRoot { get; set; }

        [JsonProperty("siteLocalizations")]
        public SiteLocalizationData[] SiteLocalizations { get; set; }

        [JsonProperty("files")]
        public string[] ConfigStaticContentUrls { get; set; }

        [JsonProperty("dataPresentationTemplateUri")]
        public string DataPresentationTemplateUri { get; set; }
    }

    public class SiteLocalizationData
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Language { get; set; }
        public bool IsMaster { get; set; }
    }
}
