using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sdl.Web.Tridion.Common;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Pre-processes Rich Text fields so they contain sufficient information for Rich Text Processing in the Web Application.
    /// </summary>
    [TcmTemplateTitle("Resolve Rich Text")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.ResolveRichTextParameters.xsd")]
    public class ResolveRichText : TemplateBase
    {
        private const string SchemaUriAttribute = "data-schemaUri";
        private const string FileNameAttribute = "data-multimediaFileName";
        private const string FileSizeAttribute = "data-multimediaFileSize";
        private const string MimeTypeAttribute = "data-multimediaMimeType";
        private const string TcmXLinkPattern = @"xlink:href=\\""(tcm\:\d+\-\d+)\\""(?!\sdata-schemaUri)";
        private const string XhtmlNamespaceDeclaration = " xmlns=\\\"http://www.w3.org/1999/xhtml\\\"";

        private List<string> _dataFieldNames;
            
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            Item outputItem = package.GetByName(Package.OutputName);
            if (outputItem == null)
            {
                Logger.Error("No Output item found in package. Ensure this TBB is executed at the end of the modular templating pipeline.");
                return;
            }

            string multimediaLinkAttributesParam = package.GetValue("multimediaLinkAttributes") ?? string.Empty;
            Logger.Debug("Using multimediaLinkAttributes: " + multimediaLinkAttributesParam);
            _dataFieldNames = multimediaLinkAttributesParam.Split(',').Select(s => s.Trim()).ToList();

            string output = outputItem.GetAsString();
            package.Remove(outputItem);
            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Text, PreProcessRichTextContent(output)));
        }

        private string PreProcessRichTextContent(string content)
        {
            // Strip off XHTML namespace declarations
            content = content.Replace(XhtmlNamespaceDeclaration, string.Empty);

            // Add data attributes to MM component links
            content = Regex.Replace(
                content, 
                TcmXLinkPattern, 
                match =>
                {
                    string originalLink = match.Value;
                    string linkedItemId = match.Groups[1].Value;
                    Component linkedComponent = Engine.GetObject(linkedItemId) as Component;

                    if (linkedComponent == null || linkedComponent.BinaryContent == null)
                    {
                        // Linked item is not a MM Component.
                        return originalLink;
                    }

                    Logger.Debug("Found Multimedia Component Link in Rich Text: " + linkedComponent.ToString());

                    StringBuilder dataAttributesBuilder = new StringBuilder();
                    BinaryContent binaryContent = linkedComponent.BinaryContent;
                    dataAttributesBuilder.AppendFormat(" {0}=\"{1}\"", SchemaUriAttribute, linkedComponent.Schema.Id);
                    dataAttributesBuilder.AppendFormat(" {0}=\"{1}\"", FileNameAttribute, binaryContent.Filename);
                    dataAttributesBuilder.AppendFormat(" {0}=\"{1}\"", FileSizeAttribute, binaryContent.Size);
                    dataAttributesBuilder.AppendFormat(" {0}=\"{1}\"", MimeTypeAttribute, binaryContent.MultimediaType.MimeType);

                    if (linkedComponent.Metadata != null)
                    {
                        ItemFields metadataFields = new ItemFields(linkedComponent.Metadata, linkedComponent.MetadataSchema);
                        ExtractDataAttributes(metadataFields, dataAttributesBuilder);
                    }

                    string dataAttributes;
                    if (dataAttributesBuilder.Length > 0)
                    {
                        // encode and strip first and last character (quotes added by encode)
                        dataAttributes = JsonSerialize(dataAttributesBuilder.ToString()).Substring(1);
                        dataAttributes = dataAttributes.Substring(0, dataAttributes.Length - 1);
                        Logger.Debug("Added data attributes: " + dataAttributes);
                    }
                    else
                    {
                        dataAttributes = string.Empty;
                    }

                    return originalLink + dataAttributes;
                }
                );

            return content;
        }

        private void ExtractDataAttributes(ItemFields fields, StringBuilder dataAttributesBuilder)
        {
            if (fields == null)
            {
                return;
            }

            foreach (string fieldname in _dataFieldNames.Where(fn => fields.Contains(fn)))
            {
                string dataAttribute = string.Format(" data-{0}=\"{1}\"", fieldname, System.Net.WebUtility.HtmlEncode(fields.GetSingleFieldValue(fieldname)));
                dataAttributesBuilder.Append(dataAttribute);
            }

            // Flatten embedded fields
            foreach (EmbeddedSchemaField embeddedSchemaField in fields.OfType<EmbeddedSchemaField>())
            {
                ExtractDataAttributes(embeddedSchemaField.Value, dataAttributesBuilder);
            }
        }
    }
}
