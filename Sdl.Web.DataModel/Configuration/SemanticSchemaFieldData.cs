namespace Sdl.Web.DataModel.Configuration
{
    public class SemanticSchemaFieldData
    {
        public string Name;
        public string Path;
        public bool IsMultiValue;
        public SemanticPropertyData[] Semantics;
        public SemanticSchemaFieldData[] Fields;
    }
}
