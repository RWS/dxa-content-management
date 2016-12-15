using System.Collections.Generic;
using Newtonsoft.Json;

// TODO: Split out into separate files
namespace Sdl.Web.DataModel
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

    public class BootstrapData
    {
        [JsonProperty("files")]
        public string[] Files;
    }

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
    }

    public class SiteLocalizationData
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Language { get; set; }
        public bool IsMaster { get; set; }
    }


    public class VocabularyData
    {
        public string Prefix;
        public string Vocab;
    }

    public class SemanticSchemaData
    {
        public int Id;
        public string RootElement;
        public SemanticSchemaFieldData[] Fields;
        public SemanticTypeData[] Semantics;
    }

    public class SemanticTypeData
    {
        public string Prefix;
        public string Entity;

        #region Overrides
        public override bool Equals(object obj)
        {
            SemanticTypeData other = obj as SemanticTypeData;
            return (other != null) && (other.Prefix == Prefix) && (other.Entity == Entity);
        }

        public override int GetHashCode()
        {
            return Prefix.GetHashCode() ^ Entity.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Prefix}:{Entity}";
        }
        #endregion
    }

    public class SemanticPropertyData : SemanticTypeData
    {
        [JsonProperty(Order = 1)]
        public string Property;

        #region Overrides
        public override bool Equals(object obj)
        {
            SemanticPropertyData other = obj as SemanticPropertyData;
            return (other != null) && base.Equals(other) && (other.Property == Property);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Property.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Prefix}:{Entity}:{Property}";
        }
        #endregion
    }

    public class SemanticSchemaFieldData
    {
        public string Name;
        public string Path;
        public bool IsMultiValue;
        public SemanticPropertyData[] Semantics;
        public SemanticSchemaFieldData[] Fields;
    }

    public class XpmRegionData
    {
        public string Region;
        public List<XpmComponentTypeData> ComponentTypes;
    }

    public class XpmComponentTypeData
    {
        public string Schema;
        public string Template;
    }
}
