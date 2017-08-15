using System.Xml;
using DD4T.Templates.Base;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using System;

namespace DD4T.Templates
{
    /// <summary>
    /// Generates .NET resource (resx) XML 
    /// Example: <root><data name="keyname"><value>keyvalue</value></data></root>
    /// Schema should look like this: 1 embedded schema with 2 textfields: key and value (both NOT multivalue)
    ///                               1 normal schema with 1 field of type 'Embedded schema' and allow multiple values. 
    /// Field names are configurable in parameters
    /// </summary>
    /// <remarks>
    /// Commonly used in combination with DD4T.Web.ResourceManagement.DynamicResourceProviderFactory in the web application.
    /// This factory consumes the generated XML from the Tridion broker database.
    /// </remarks>
    [TcmTemplateTitle("Generate Resources")]
    [TcmTemplateParameterSchema("resource:DD4T.Templates.Resources.Schemas.Resources Parameters.xsd")]
    public class Resources : DefaultTemplate
    {
        private string _embeddedFieldName;
        public string EmbeddedFieldName
        {
            get
            {
                if (_embeddedFieldName == null)
                {
                    _embeddedFieldName = Package.GetValue("EmbeddedFieldName");
                }
                return _embeddedFieldName;
            }
        }
        private string _keyFieldName;
        public string KeyFieldName
        {
            get
            {
                if (_keyFieldName == null)
                {
                    _keyFieldName = Package.GetValue("KeyFieldName");
                }
                return _keyFieldName;
            }
        }
        private string _valueFieldName;
        public string ValueFieldName
        {
            get
            {
                if (_valueFieldName == null)
                {
                    _valueFieldName = Package.GetValue("ValueFieldName");
                }
                return _valueFieldName;
            }
        }

        public override void Transform(Engine engine, Package package)
        {
            Engine = engine;
            Package = package;

            if (!HasPackageValue(Package, "EmbeddedFieldName"))
                throw new Exception("Please specify an embedded field name in the template parameters");
            if (!HasPackageValue(Package, "KeyFieldName"))
                throw new Exception("Please specify a key field name in the template parameters");
            if (!HasPackageValue(Package, "ValueFieldName"))
                throw new Exception("Please specify a value field name in the template parameters");

            var c = IsPageTemplate() ? GetPage().ComponentPresentations[0].Component : GetComponent();

            XmlDocument resourceDoc = null;
            resourceDoc = new XmlDocument();
            resourceDoc.LoadXml("<root/>");

            var fields = new ItemFields(c.Content, c.Schema);
            var sourceField = fields[EmbeddedFieldName] as EmbeddedSchemaField;
            foreach (var innerField in sourceField.Values)
            {

                var key = innerField[KeyFieldName] as TextField;
                var value = innerField[ValueFieldName] as TextField;

                var data = resourceDoc.CreateElement("data");
                data.SetAttribute("name", key.Value);
                var v = resourceDoc.CreateElement("value");
                v.InnerText = value.Value;
                data.AppendChild(v);
                resourceDoc.DocumentElement.AppendChild(data);
            }
            
            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Xml, resourceDoc.OuterXml));
           
        }
    }
}
