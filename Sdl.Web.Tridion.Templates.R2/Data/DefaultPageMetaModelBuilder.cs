using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Default implementation for Model Builder which sets <see cref="PageModelData.Meta"/> and <see cref="PageModelData.Title"/>.
    /// </summary>
    /// <remarks>
    /// This implementation reflects the logic from DXA 1.x model mapping.
    /// </remarks>
    public class DefaultPageMetaModelBuilder : DataModelBuilder, IPageModelDataBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context <see cref="DataModelBuilderPipeline"/></param>
        public DefaultPageMetaModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
        }

        /// <summary>
        /// Builds a Page Data Model from a given CM Page object.
        /// </summary>
        /// <param name="pageModelData">The Page Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="page">The CM Page.</param>
        public void BuildPageModel(ref PageModelData pageModelData, Page page)
        {
            string pageModelTitle;
            pageModelData.Meta = BuildPageModelMeta(page, out pageModelTitle);
            pageModelData.Title = pageModelTitle;
        }

        private Dictionary<string, string> BuildPageModelMeta(Page page, out string title)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (page.Metadata != null)
            {
                ExtractKeyValuePairs(page.Metadata, result);
            }

            string description;
            string image;
            result.TryGetValue("description", out description);
            result.TryGetValue("title", out title);
            result.TryGetValue("image", out image);

            if ((title == null) || (description == null))
            {
                var pageCps = page.ComponentPresentations;
                var region = page.Regions.FirstOrDefault(r => r.RegionName == "Main" || r.RegionName == "Article");
                IEnumerable<ComponentPresentation> cps = (!pageCps.Any() && region != null) 
                    ? region.ComponentPresentations 
                    : pageCps;

                // Try to get title/description/image from Component in Main Region.
                foreach (ComponentPresentation cp in cps)
                {
                    string regionName;
                    GetRegionMvcData(cp.ComponentTemplate, out regionName);
                    if (regionName != "Main")
                    {
                        continue;
                    }

                    Component component = cp.Component;
                    XmlElement componentContent = component.Content;
                    XmlElement componentMetadata = component.Metadata;
                    string namespaceUri = componentContent?.NamespaceURI ?? componentMetadata?.NamespaceURI;
                    XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(new NameTable());
                    if (namespaceUri != null)
                    {
                        xmlNsManager.AddNamespace("c", namespaceUri);
                    }

                    XmlElement titleElement = null;
                    XmlElement descriptionElement = null;
                    XmlElement imageElement = null;
                    if (componentMetadata != null)
                    {
                        titleElement = componentMetadata.SelectSingleElement("c:standardMeta/c:name", xmlNsManager);
                        descriptionElement = componentMetadata.SelectSingleElement("c:standardMeta/c:description", xmlNsManager);
                    }
                    if (componentContent != null)
                    {
                        if (titleElement == null)
                        {
                            titleElement = componentContent.SelectSingleElement("c:headline", xmlNsManager);
                        }
                        imageElement = componentContent.SelectSingleElement("c:image", xmlNsManager);
                    }
                    if (title == null)
                    {
                        title = titleElement?.InnerText;
                    }
                    if (description == null)
                    {
                        description = descriptionElement?.InnerText;
                    }
                    if (image == null)
                    {
                        image = imageElement?.GetAttribute("href", Constants.XlinkNamespace);
                    }
                    break;
                }
            }

            if (title == null)
            {
                string sequencePrefix;
                title =  StripSequencePrefix(page.Title, out sequencePrefix);
            }

            result.Add("twitter:card", "summary");
            result.Add("og:title", title);
            result.Add("og:type", "article");

            if (!string.IsNullOrEmpty(Pipeline.Settings.Locale))
            {
                result.Add("og:locale", Pipeline.Settings.Locale);
            }
            if (description != null)
            {
                result.Add("og:description", description);
            }
            if (image != null)
            {
                result.Add("og:image", image);
            }
            if (!result.ContainsKey("description"))
            {
                result.Add("description", description ?? title);
            }

            return result;
        }

        private void ExtractKeyValuePairs(XmlElement xmlElement, IDictionary<string, string> result)
        {
            string currentFieldName = null;
            string currentFieldValue = string.Empty;
            foreach (XmlElement childElement in xmlElement.SelectElements("*"))
            {
                bool isRichText = childElement.SelectSingleElement("xhtml:*") != null;
                if (!isRichText && (childElement.SelectSingleElement("*") != null))
                {
                    // Embedded field: flatten
                    ExtractKeyValuePairs(childElement, result);
                }
                else
                {
                    string fieldValue = GetFieldValueAsString(childElement);
                    if (childElement.Name == currentFieldName)
                    {
                        // Multi-valued field: comma separate the values
                        currentFieldValue += ", " + fieldValue;
                    }
                    else
                    {
                        // New field
                        if (currentFieldName != null)
                        {
                            AddValue(result, currentFieldName, currentFieldValue);
                        }
                        currentFieldName = childElement.Name;
                        currentFieldValue = fieldValue;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentFieldValue))
            {
                AddValue(result, currentFieldName, currentFieldValue);
            }
        }

        private static void AddValue(IDictionary<string, string> result, string name, string value)
        {
            if (result.ContainsKey(name))
            {
                result[name] = $"{result[name]}, {value}";
            }
            else
            {
                result.Add(name, value);
            }
        }

        private string GetFieldValueAsString(XmlElement xmlElement)
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
                Keyword keyword = linkedItem as Keyword;
                if (keyword == null)
                {
                    // Component link field or some other linked item (except Keyword)
                    return xlinkHref;
                }

                // Keyword link field
                return string.IsNullOrEmpty(keyword.Description) ? keyword.Title : keyword.Description;
            }

            if (xmlElement.SelectSingleElement("xhtml:*") != null)
            {
                // XHTML field
                RichTextData richText = BuildRichTextModel(xmlElement);
                return string.Join("", richText.Fragments.Select(f => f.ToString()));
            }

            // Text, number or date field
            // Multi-line text field may use CR+LF to separate lines, but JSON.NET expects LF only.
            return xmlElement.InnerText.Replace("\r\n", "\n");
        }
    }
}
