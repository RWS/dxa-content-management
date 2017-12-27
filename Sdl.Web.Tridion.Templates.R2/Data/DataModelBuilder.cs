using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Abstract base class for DXA R2 Data Model Builders.
    /// </summary>
    public class DataModelBuilder
    {
        private const string EclMimeType = "application/externalcontentlibrary";

        private static readonly Regex _embeddedEntityRegex = new Regex(@"<\?EmbeddedEntity\s\?>", RegexOptions.Compiled);
        private static readonly Regex _cmTitleRegex = new Regex(@"(?<sequence>\d\d\d)?\s*(?<title>.*)", RegexOptions.Compiled);

        /// <summary>
        /// Gets the context <see cref="DataModelBuilderPipeline"/>.
        /// </summary>
        protected DataModelBuilderPipeline Pipeline { get; }

        /// <summary>
        /// Gets the Logger used by this Model Builder.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context Model Builder Pipeline.</param>
        protected DataModelBuilder(DataModelBuilderPipeline pipeline)
        {
            Pipeline = pipeline;

            if (pipeline.Logger is TemplatingLoggerAdapter)
            {
                // If the pipeline uses a TemplatingLogger (default), we also use our own.
                Logger = new TemplatingLoggerAdapter(TemplatingLogger.GetLogger(GetType()));
            }
            else
            {
                // If the pipeline uses another logger (unit/integration tests), we use that one.
                Logger = pipeline.Logger;
            }
        }

        protected static bool IsEclItem(Component component) =>
            (component.BinaryContent != null) && (component.BinaryContent.MultimediaType.MimeType == EclMimeType);

        public static string GetDxaIdentifier(IdentifiableObject tcmItem)
            => tcmItem?.Id.ItemId.ToString();

        protected static string GetTcmIdentifier(IdentifiableObject tcmItem)
            => tcmItem?.Id.GetVersionlessUri().ToString();

        protected static string StripModuleName(string qualifiedName, out string moduleName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                moduleName = null;
                return null;
            }

            string[] parts = qualifiedName.Split(':');
            if (parts.Length > 2)
            {
                throw new DxaException($"Invalid qualified name format: '{qualifiedName}'");
            }
            if (parts.Length == 1)
            {
                moduleName = null;
                return parts[0];    // qualifiedName == viewName
            }

            moduleName = parts[0].Trim();
            string viewName = parts[1].Trim();

            if (string.IsNullOrEmpty(moduleName))
            {
                throw new DxaException($"Invalid Area name: '{parts[0]}' in the qualified name '{qualifiedName}'");
            }
            if (string.IsNullOrEmpty(viewName))
            {
                throw new DxaException($"Invalid View name: '{parts[1]}' in the qualified name '{qualifiedName}'");
            }

            return viewName;
        }

        /// <summary>
        /// Strips off the "sequence prefix" (3 digits used for ordering purposes) from the title of a CM Item.
        /// </summary>
        /// <param name="title">The title which may contain a sequence prefix.</param>
        /// <param name="sequencePrefix">The sequence prefix (if any).</param>
        /// <returns>The title without sequence prefix.</returns>
        protected static string StripSequencePrefix(string title, out string sequencePrefix)
        {
            Match titleMatch = _cmTitleRegex.Match(title);
            sequencePrefix = titleMatch.Groups["sequence"].Value;
            return titleMatch.Groups["title"].Value;
        }

        protected ContentModelData BuildContentModel(XmlElement xmlElement, int expandLinkDepth)
        {
            if (xmlElement == null)
            {
                return null;
            }

            ContentModelData result = new ContentModelData();

            string currentFieldName = null;
            List<object> currentFieldValues = new List<object>();
            foreach (XmlElement childElement in xmlElement.SelectElements("*"))
            {
                if (childElement.Name != currentFieldName)
                {
                    // New field
                    if (currentFieldName != null)
                    {
                        result.Add(currentFieldName, GetTypedFieldValue(currentFieldValues));
                    }
                    currentFieldName = childElement.Name;
                    currentFieldValues = new List<object>();
                }
                currentFieldValues.Add(GetFieldValue(childElement, expandLinkDepth));
            }

            if (currentFieldName != null)
            {
                result.Add(currentFieldName, GetTypedFieldValue(currentFieldValues));
            }

            return result.Count == 0 ? null : result;
        }

        private static object GetTypedFieldValue<T>(List<T> fieldValues)
        {
            switch (fieldValues.Count)
            {
                case 0:
                    return null;
                case 1:
                    return fieldValues[0];
            }

            return GetTypedArrayOfValues(fieldValues);
        }

        private static object GetTypedArrayOfValues<T>(List<T> fieldValues)
        {
            Array typedArray = Array.CreateInstance(fieldValues[0].GetType(), fieldValues.Count);
            int i = 0;
            foreach (T fieldValue in fieldValues)
            {
                typedArray.SetValue(fieldValue, i++);
            }

            return typedArray;
        }

        private object GetFieldValue(XmlElement xmlElement, int expandLinkDepth)
        {
            string xlinkHref = xmlElement.GetAttribute("href", Constants.XlinkNamespace);
            if (!string.IsNullOrEmpty(xlinkHref))
            {
                if (!TcmUri.IsValid(xlinkHref))
                {
                    // External link field
                    return xlinkHref;
                }

                IdentifiableObject linkedItem = Pipeline.Session.GetObject(xmlElement);
                Logger.Debug($"Encountered XLink '{xmlElement.GetPath()}' -> {linkedItem}");

                ViewModelData embeddedViewModel = GetLinkFieldValue(linkedItem, expandLinkDepth);
                if (embeddedViewModel == null)
                {
                    // XLink is not a Component or Keyword link; return the raw URI.
                    return xlinkHref;
                }
                return embeddedViewModel;
            }

            if (xmlElement.SelectSingleElement("xhtml:*") != null)
            {
                // XHTML field
                return BuildRichTextModel(xmlElement);
            }

            if (xmlElement.SelectSingleElement("*") != null)
            {
                // Embedded field
                return BuildContentModel(xmlElement, expandLinkDepth);
            }

            // Text, number or date field
            return xmlElement.InnerText;
        }

        private ViewModelData GetLinkFieldValue(IdentifiableObject linkedItem, int expandLinkDepth)
        {
            Component linkedComponent = linkedItem as Component;
            Keyword linkedKeyword = linkedItem as Keyword;
            if ((linkedComponent == null) && (linkedKeyword == null))
            {
                Logger.Debug("XLink is not a Component or Keyword link.");
                return null;
            }

            if (expandLinkDepth == 0)
            {
                Logger.Debug($"Not expanding link because configured ExpandLinkDepth of {Pipeline.Settings.ExpandLinkDepth} has been reached.");
                if (linkedComponent != null)
                {
                    return new EntityModelData
                    {
                        Id = GetDxaIdentifier(linkedComponent)
                    };
                }
                return new KeywordModelData
                {
                    Id = GetDxaIdentifier(linkedKeyword),
                    SchemaId = GetDxaIdentifier(linkedKeyword.MetadataSchema)
                };
            }

            if (linkedComponent != null)
            {
                ComponentTemplate dataPresentationTemplate = Pipeline.DataPresentationTemplate;
                if ((dataPresentationTemplate != null) && dataPresentationTemplate.RelatedSchemas.Contains(linkedComponent.Schema))
                {
                    Logger.Debug($"Not expanding Component link because a Data Presentation exists: {linkedComponent.Schema.FormatIdentifier()}");
                    return new EntityModelData
                    {
                        Id = $"{GetDxaIdentifier(linkedComponent)}-{GetDxaIdentifier(dataPresentationTemplate)}"
                    };
                }

                Logger.Debug($"Expanding Component link. expandLinkDepth: {expandLinkDepth}");
                return Pipeline.CreateEntityModel(linkedComponent, dataPresentationTemplate, false, expandLinkDepth - 1);
            }

            Category category = (Category) linkedKeyword.OrganizationalItem;
            if (category.UseForNavigation)
            {
                Logger.Debug($"Not expanding Keyword link because its Category is publishable: {category.FormatIdentifier()}");
                return new KeywordModelData
                {
                    Id = GetDxaIdentifier(linkedKeyword),
                    SchemaId = GetDxaIdentifier(linkedKeyword.MetadataSchema)
                };
            }

            Logger.Debug($"Expanding Keyword link. expandLinkDepth: {expandLinkDepth}");
            return Pipeline.CreateKeywordModel(linkedKeyword, expandLinkDepth - 1);
        }

        protected RichTextData BuildRichTextModel(XmlElement xhtmlElement)
        {
            XmlDocument xmlDoc = xhtmlElement.OwnerDocument;
            IList<EntityModelData> embeddedEntities = new List<EntityModelData>();
            foreach (XmlElement xlinkElement in xhtmlElement.SelectElements(".//*[starts-with(@xlink:href, 'tcm:')]"))
            {
                Component linkedComponent = Pipeline.Session.GetObject(xlinkElement) as Component;

                if ((xlinkElement.LocalName == "a") && (linkedComponent != null))
                {
                    // Hyperlink to Component; put a Component Link marker just after the hyperlink to facilitate link suppression.
                    XmlComment compLinkEndMarker = xmlDoc.CreateComment($"CompLink {linkedComponent.Id.GetVersionlessUri()}");
                    xlinkElement.ParentNode.InsertAfter(compLinkEndMarker, xlinkElement);
                }

                if (linkedComponent?.BinaryContent == null)
                {
                    // Not a MM Component link; put TCM URI in href and remove XLink attributes.
                    xlinkElement.SetAttribute("href", xlinkElement.GetAttribute("href", Constants.XlinkNamespace));
                    xlinkElement.RemoveXlinkAttributes();
                    continue;
                }

                if (xlinkElement.LocalName == "img")
                {
                    // img element pointing to MM Component is expanded to an embedded Entity Model
                    EntityModelData embeddedEntity = Pipeline.CreateEntityModel(linkedComponent, ct: null, expandLinkDepth: 0);
                    string htmlClasses = xlinkElement.GetAttribute("class");
                    if (!string.IsNullOrEmpty(htmlClasses))
                    {
                        embeddedEntity.HtmlClasses = htmlClasses;
                    }
                    string altText = xlinkElement.GetAttribute("alt");
                    if (!string.IsNullOrEmpty(altText))
                    {
                        // The XHTML img element has an alt attribute; use this as "altText" metadata field (possibly overwriting the actual metadata field).
                        if (embeddedEntity.Metadata == null)
                        {
                            embeddedEntity.Metadata = new ContentModelData();
                        }
                        embeddedEntity.Metadata["altText"] = altText;
                    }
                    embeddedEntities.Add(embeddedEntity);

                    // Replace entire img element with marker XML processing instruction (see below). 
                    xlinkElement.ParentNode.ReplaceChild(
                        xmlDoc.CreateProcessingInstruction("EmbeddedEntity", string.Empty),
                        xlinkElement
                        );
                }
                else
                {
                    // Hyperlink to MM Component: add the Binary and set the URL as href
                    string binaryUrl = Pipeline.RenderedItem.AddBinary(linkedComponent).Url;
                    xlinkElement.SetAttribute("href", binaryUrl);
                    xlinkElement.RemoveXlinkAttributes();
                }
            }

            // Remove XHTML namespace declarations
            string html = xhtmlElement.InnerXml.Replace(" xmlns=\"http://www.w3.org/1999/xhtml\"", string.Empty);

            // Split the HTML into fragments based on EmbeddedEntity XML processing instructions (see above).
            List<object> richTextFragments = new List<object>();
            int lastFragmentIndex = 0;
            int i = 0;
            foreach (Match embeddedEntityMatch in _embeddedEntityRegex.Matches(html))
            {
                int embeddedEntityIndex = embeddedEntityMatch.Index;
                if (embeddedEntityIndex > lastFragmentIndex)
                {
                    richTextFragments.Add(html.Substring(lastFragmentIndex, embeddedEntityIndex - lastFragmentIndex));
                }
                richTextFragments.Add(embeddedEntities[i++]);
                lastFragmentIndex = embeddedEntityIndex + embeddedEntityMatch.Length;
            }
            if (lastFragmentIndex < html.Length)
            {
                // Final text fragment
                richTextFragments.Add(html.Substring(lastFragmentIndex));
            }

            return new RichTextData { Fragments = richTextFragments };
        }

        protected ContentModelData ExtractCustomMetadata(XmlElement metadata, IEnumerable<string> excludeFields)
        {
            if (metadata == null)
            {
                return null;
            }

            XmlElement customMetadata = (XmlElement) metadata.CloneNode(deep: true);
            string excludeXPathPredicate = string.Join(" or ", excludeFields.Select(name => $"local-name()='{name}'"));
            XmlElement[] excludeElements = customMetadata.SelectElements($"*[{excludeXPathPredicate}]").ToArray();

            if (customMetadata.SelectElements("*").Count() <= excludeElements.Length)
            {
                // No custom metadata found.
                return null;
            }

            foreach (XmlElement excludeElement in excludeElements)
            {
                customMetadata.RemoveChild(excludeElement);
            }

            return BuildContentModel(customMetadata, expandLinkDepth: 0);
        }

        protected static ContentModelData MergeFields(ContentModelData primaryFields, ContentModelData secondaryFields, out string[] duplicateFieldNames)
        {
            List<string> duplicates = new List<string>();

            ContentModelData result;
            if (secondaryFields == null)
            {
                result = primaryFields;
            }
            else if (primaryFields == null)
            {
                result = secondaryFields;
            }
            else
            {
                result = primaryFields;
                foreach (KeyValuePair<string, object> field in secondaryFields)
                {
                    if (result.ContainsKey(field.Key))
                    {
                        duplicates.Add(field.Key);
                    }
                    else
                    {
                        result.Add(field.Key, field.Value);
                    }
                }
            }

            duplicateFieldNames = duplicates.ToArray();
            return result;
        }

        public static MvcData GetRegionMvcData(ComponentTemplate ct, out string regionName, string defaultViewName = "Main")
        {
            string qualifiedViewName = ct.Metadata.GetTextFieldValue("regionView");
            regionName = ct.Metadata.GetTextFieldValue("regionName");

            string moduleName;
            string viewName = StripModuleName(qualifiedViewName, out moduleName) ?? defaultViewName;

            if (string.IsNullOrEmpty(regionName))
            {
                regionName = viewName;
            }

            return new MvcData
            {
                ViewName = viewName,
                AreaName = moduleName
            };
        }

        public void CreateSchemaIdListExtensionData(ref ViewModelData modelData, List<string> ids) {
            if (modelData != null && modelData.ExtensionData == null)
            {
                modelData.ExtensionData = new Dictionary<string, object>();
            }

            object typedValue = GetTypedArrayOfValues(ids);
            if (!modelData.ExtensionData.ContainsKey("Schemas"))
            {
                modelData.ExtensionData.Add("Schemas", typedValue);
            }
            else {
                modelData.ExtensionData["Schemas"] = typedValue;
            }
        }
    }
}
