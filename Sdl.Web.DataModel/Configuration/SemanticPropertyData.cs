using Newtonsoft.Json;

namespace Sdl.Web.DataModel.Configuration
{
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
}
