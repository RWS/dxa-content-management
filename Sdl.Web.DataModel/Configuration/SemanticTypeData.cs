namespace Sdl.Web.DataModel.Configuration
{
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
}
