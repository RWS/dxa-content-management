namespace Sdl.Web.DataModel.Configuration
{
    public enum FieldType
    {
        Text,
        MultiLineText,
        Xhtml,
        Keyword,
        Embedded,
        MultiMediaLink,
        ComponentLink,
        ExternalLink,
        Number,
        Date
    }

    public class SemanticSchemaFieldData
    {
        public string Name;
        public string Path;
        public bool IsMultiValue;
        public SemanticPropertyData[] Semantics;
        public SemanticSchemaFieldData[] Fields;
        public FieldType FieldType;
    }

    public class EmbeddedSemanticSchemaFieldData : SemanticSchemaFieldData
    {
        public string RootElementName;
        public int Id;
        public string Title;
    }
}