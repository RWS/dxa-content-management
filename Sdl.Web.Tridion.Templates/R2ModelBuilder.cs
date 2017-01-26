using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion;
using Tridion.ContentManager;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.Logging;
using ComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Model Builder used to generate DXA R2 Data Models.
    /// </summary>
    /// <remarks>
    /// TODO: This should be made extensible (a "Model Builder Pipeline").
    /// It is currently already possible to extend the Data Model using the Modular Templating Pipeline,
    /// but that requires deserializing/serializing for each step in the pipeline.
    /// </remarks>
    public class R2ModelBuilder
    {
        private const string LegacyIncludePrefix = "system/include/";
        private const string EclMimeType = "application/externalcontentlibrary";

        private static readonly XmlNamespaceManager _xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
        private static readonly Regex _embeddedEntityRegex = new Regex(@"<\?EmbeddedEntity\s\?>", RegexOptions.Compiled);
        private readonly TemplatingLogger _logger = TemplatingLogger.GetLogger(typeof(R2ModelBuilder));

        public delegate string AddBinaryDelegate(Component mmComponent);
        public delegate string AddBinaryStreamDelegate(Stream inputStream, string fileName, Component relatedComponent, string mimeType);


        internal Session Session { get; }
        internal R2ModelBuilderSettings Settings { get; }
        internal AddBinaryDelegate AddBinaryFunction { get; }
        internal AddBinaryStreamDelegate AddBinaryStreamFunction { get; }

        /// <summary>
        /// Class constructor
        /// </summary>
        static R2ModelBuilder()
        {
            _xmlNamespaceManager.AddNamespace("xlink", Constants.XlinkNamespace);
            _xmlNamespaceManager.AddNamespace("xhtml", Constants.XhtmlNamespace);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public R2ModelBuilder(Session session, R2ModelBuilderSettings settings, AddBinaryDelegate addBinaryFunction, AddBinaryStreamDelegate addBinaryStreamFunction)
        {
            Session = session;
            Settings = settings;
            AddBinaryFunction = addBinaryFunction;
            AddBinaryStreamFunction = addBinaryStreamFunction;
        }

        /// <summary>
        /// Builds a Page Model for a given CM Page.
        /// </summary>
        /// <param name="page">The CM Page.</param>
        /// <returns>The Page Model.</returns>
        public PageModelData BuildPageModel(Page page)
        {
            _logger.Debug($"BuildPageModel({page})");

            if (page == null)
            {
                return null;
            }

            // We need Keyword XLinks for Keyword field expansion
            page.Load(LoadFlags.KeywordXlinks);

            IDictionary<string, RegionModelData> regionModels = new Dictionary<string, RegionModelData>();
            AddPredefinedRegions(regionModels, page.PageTemplate);
            AddComponentPresentationRegions(regionModels, page);
            AddIncludePageRegions(regionModels, page.PageTemplate);

            string title;
            return new PageModelData
            {
                Id = GetDxaIdentifier(page),
                Meta = BuildPageModelMeta(page, out title),
                Title = title,
                Regions = regionModels.Values.ToList(),
                Metadata = BuildContentModel(page.Metadata, Settings.ExpandLinkDepth),
                MvcData = GetPageMvcData(page.PageTemplate),
                XpmMetadata = GetXpmMetadata(page)
            };
        }

        private void AddPredefinedRegions(IDictionary<string, RegionModelData> regionModels, PageTemplate pageTemplate)
        {
            IEnumerable<XmlElement> regionsMetadata = pageTemplate.Metadata.GetEmbeddedFieldValues("regions");
            if (regionsMetadata == null)
            {
                return;
            }

            foreach (XmlElement regionMetadata in regionsMetadata)
            {
                string regionName = regionMetadata.GetTextFieldValue("name");
                string qualifiedRegionViewName = regionMetadata.GetTextFieldValue("view");
                string moduleName;
                string regionViewName = StripModuleName(qualifiedRegionViewName, out moduleName);

                if (string.IsNullOrEmpty(regionName))
                {
                    regionName = regionViewName;
                }

                RegionModelData regionModel = new RegionModelData
                {
                    Name = regionName,
                    MvcData = new MvcData
                    {
                        ViewName = regionViewName,
                        AreaName = moduleName
                    },
                    Entities = new List<EntityModelData>()
                };

                XmlElement additionalRegionMetadata = (XmlElement) regionMetadata.CloneNode(deep: true);
                XmlElement[] standardRegionMetadata = additionalRegionMetadata.SelectElements("*[local-name()='name' or local-name()='view']").ToArray();
                if (additionalRegionMetadata.SelectElements("*").Count() > standardRegionMetadata.Length)
                {
                    foreach (XmlElement xmlElement in standardRegionMetadata)
                    {
                        additionalRegionMetadata.RemoveChild(xmlElement);
                    }
                    regionModel.Metadata = BuildContentModel(additionalRegionMetadata, 0);
                }

                regionModels.Add(regionName, regionModel);
            }
        }

        private void AddIncludePageRegions(IDictionary<string, RegionModelData> regionModels, PageTemplate pageTemplate)
        {
            IEnumerable<string> includes = pageTemplate.Metadata.GetTextFieldValues("includes"); // TODO: use external link field (?)
            if (includes == null)
            {
                return;
            }

            foreach (string include in includes)
            {
                string includePageId;
                if (include.StartsWith(LegacyIncludePrefix))
                {
                    // Legacy include: publish path. Try to convert to WebDAV URL.
                    string relativeUrl = include.Substring(LegacyIncludePrefix.Length).Replace('-', ' ');
                    Publication contextPub = (Publication) pageTemplate.ContextRepository;
                    includePageId = $"/webdav/{contextPub.Title}/{contextPub.RootStructureGroup.Title}/_System/include/{relativeUrl}.tpg"; 
                }
                else
                {
                    includePageId = include;
                }

                Page includePage = (Page) Session.GetObject(includePageId);

                string moduleName;
                string regionViewName = StripModuleName(includePage.Title, out moduleName);
                string regionName = regionViewName;

                if (regionModels.ContainsKey(regionName))
                {
                    // TODO: log this? Throw exception? Promote Region to Include Page Region?
                    continue;
                }

                RegionModelData includePageRegion = new RegionModelData
                {
                    Name = regionName,
                    MvcData = new MvcData
                    {
                        ViewName = regionViewName,
                        AreaName = moduleName
                    },
                    IncludePageUrl = includePage.PublishLocationUrl,
                    Regions = ExpandIncludePage(includePage) // TODO TSI-24: We expand the include Page here for now (until we have a Model Service to do it).
                };

                if (Settings.GenerateXpmMetadata)
                {
                    includePageRegion.XpmMetadata = new Dictionary<string, object>
                    {
                        {"IncludedFromPageId", GetTcmIdentifier(includePage)},
                        {"IncludedFromPageTitle", includePage.Title},
                        {"IncludedFromPageFileName", includePage.FileName}
                    };
                }
                regionModels.Add(regionName, includePageRegion);
            }
        }

        private List<RegionModelData> ExpandIncludePage(Page includePage)
        {
            _logger.Debug($"Expanding Include Page '{includePage.Title}' for now (until we have a Model Service).");

            PageModelData includePageModel = BuildPageModel(includePage);
            return includePageModel.Regions;
        }

        private void AddComponentPresentationRegions(IDictionary<string, RegionModelData> regionModels, Page page)
        {
            foreach (ComponentPresentation cp in page.ComponentPresentations)
            {
                ComponentTemplate ct = cp.ComponentTemplate;
                // TODO TSI-24: For DCPs we should output only a minimal Entity Model containing the Component and Template ID, so it can be retrieved dynamically.
                //EntityModelData entityModel = cp.ComponentTemplate.IsRepositoryPublishable ?
                //    new EntityModelData { Id = GetDxaIdentifier(cp.Component, cp.ComponentTemplate) } :
                //    BuildEntityModel(cp.Component, cp.ComponentTemplate);
                if (ct.IsRepositoryPublishable)
                {
                    _logger.Debug($"Expanding DCP ({cp.Component}, {ct}) for now (until we have a Model Service).");
                }
                EntityModelData entityModel = BuildEntityModel(cp.Component, ct);

                string regionName;
                MvcData regionMvcData = GetRegionMvcData(cp.ComponentTemplate, out regionName);

                RegionModelData regionModel;
                if (regionModels.TryGetValue(regionName, out regionModel))
                {
                    if (!regionMvcData.Equals(regionModel.MvcData))
                    {
                        throw new DxaException($"Conflicting Region MVC data detected: [{regionMvcData}] versus [{regionModel.MvcData}]");
                    }
                }
                else
                {
                    regionModel = new RegionModelData
                    {
                        Name = regionName,
                        MvcData = regionMvcData,
                        Entities = new List<EntityModelData>()
                    };
                    regionModels.Add(regionName, regionModel);
                }
                regionModel.Entities.Add(entityModel);
            }
        }

        private Dictionary<string, string> BuildPageModelMeta(Page page, out string title)
        {
            // TODO: This logic reflects the DXA 1.x logic in DefaultModelBuilder, but should be revised.
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

            if (title == null || description == null)
            {
                // Try to get title/description/image from Component in Main Region.
                foreach (ComponentPresentation cp in page.ComponentPresentations)
                {
                    string regionName;
                    GetRegionMvcData(cp.ComponentTemplate, out regionName);
                    if (regionName != "Main")
                    {
                        continue;
                    }

                    XmlElement componentContent = cp.Component.Content;
                    XmlElement componentMetadata = cp.Component.Metadata;

                    XmlElement titleElement = null;
                    XmlElement descriptionElement = null;
                    XmlElement imageElement = null;
                    if (componentMetadata != null)
                    {
                        titleElement = componentMetadata.SelectSingleElement("standardMeta/name");
                        descriptionElement = componentMetadata.SelectSingleElement("standardMeta/description");
                    }
                    if (componentContent != null)
                    {
                        if (titleElement == null)
                        {
                            titleElement = componentContent.SelectSingleElement("headline");
                        }
                        imageElement = componentContent.SelectSingleElement("image");
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
                title = Regex.Replace(page.Title, @"^\d{3}\s", string.Empty);
            }

            result.Add("twitter:card", "summary");
            result.Add("og:title", title);
            result.Add("og:type", "article");
            // TODO: result.Add("og:locale", localization.Culture);
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
                if (childElement.SelectElements("*").Any())
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
                            result.Add(currentFieldName, currentFieldValue);
                        }
                        currentFieldName = childElement.Name;
                        currentFieldValue = fieldValue;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentFieldValue))
            {
                result.Add(currentFieldName, currentFieldValue);
            }
        }

        /// <summary>
        /// Builds an Entity Model for a given CM Component/Template.
        /// </summary>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The Component Template (optional; can be <c>null</c>)</param>
        /// <returns>The Entity Model.</returns>
        public EntityModelData BuildEntityModel(Component component, ComponentTemplate ct = null)
        {
            _logger.Debug($"BuildEntityModel({component}, {ct})");

            if (component == null)
            {
                return null;
            }

            EntityModelData result = BuildEntityModel(component, Settings.ExpandLinkDepth);

            if (ct != null)
            {
                result.MvcData = GetEntityMvcData(ct);
                result.XpmMetadata = GetXpmMetadata(component, ct);
            }

            return result;
        }

        private EntityModelData BuildEntityModel(Component component, int expandLinkLevels)
        {
            // We need Keyword XLinks for Keyword field expansion
            component.Load(LoadFlags.KeywordXlinks);

            EntityModelData result = new EntityModelData
            {
                Id = GetDxaIdentifier(component),
                SchemaId = GetDxaIdentifier(component.Schema),
                Content = BuildContentModel(component.Content, expandLinkLevels),
                Metadata = BuildContentModel(component.Metadata, expandLinkLevels),
            };

            if (IsEclItem(component))
            {
                using (EclModelBuilder eclModelBuilder = new EclModelBuilder(this))
                {
                    eclModelBuilder.BuildEclModel(result, component);
                }
            }
            else
            {
                result.BinaryContent = BuildBinaryContentData(component);
            }

            return result;
        }

        private BinaryContentData BuildBinaryContentData(Component component)
        {
            BinaryContent binaryContent = component.BinaryContent;
            if (binaryContent == null)
            {
                return null;
            }

            return new BinaryContentData
            {
                Url = AddBinaryFunction(component),
                FileName = binaryContent.Filename,
                FileSize = binaryContent.Size,
                MimeType = binaryContent.MultimediaType.MimeType
            };
        }

        private KeywordModelData BuildKeywordModel(Keyword keyword, int expandLinkLevels)
        {
            // We need Keyword XLinks for Keyword field expansion
            keyword.Load(LoadFlags.KeywordXlinks);

            return new KeywordModelData
            {
                Id = GetDxaIdentifier(keyword),
                Title = keyword.Title,
                Description = keyword.Description,
                Key = keyword.Key,
                TaxonomyId = GetDxaIdentifier(keyword.OrganizationalItem),
                SchemaId = GetDxaIdentifier(keyword.MetadataSchema),
                Metadata = BuildContentModel(keyword.Metadata, expandLinkLevels)
            };
        }

        private static string GetDxaIdentifier(IdentifiableObject tcmItem, ComponentTemplate ct = null)
        {
            if (tcmItem == null)
            {
                return null;
            }

            return ct == null ? tcmItem.Id.ItemId.ToString() : $"{tcmItem.Id.ItemId}-{ct.Id.ItemId}";
        }

        private static string GetTcmIdentifier(IdentifiableObject tcmItem)
        {
            return tcmItem?.Id.GetVersionlessUri().ToString();
        }

        private static string StripModuleName(string qualifiedName, out string moduleName)
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
                return parts[0];
            }
            moduleName = parts[0];
            return parts[1];
        }

        private static MvcData GetEntityMvcData(ComponentTemplate ct)
        {
            string qualifiedViewName = ct.Metadata.GetTextFieldValue("view");
            string qualifiedControllerName = ct.Metadata.GetTextFieldValue("controller");
            string controllerAction = ct.Metadata.GetTextFieldValue("action");

            string moduleName;
            string controllerModuleName;

            return new MvcData
            {
                ViewName = StripModuleName(qualifiedViewName, out moduleName),
                AreaName = moduleName,
                ControllerName = StripModuleName(qualifiedControllerName, out controllerModuleName),
                ControllerAreaName = controllerModuleName,
                ActionName = controllerAction
                // TODO: Parameters
            };
        }

        private static MvcData GetRegionMvcData(ComponentTemplate ct, out string regionName)
        {
            string qualifiedViewName = ct.Metadata.GetTextFieldValue("regionView");
            regionName = ct.Metadata.GetTextFieldValue("regionName");

            string moduleName;
            string viewName = StripModuleName(qualifiedViewName, out moduleName) ?? "Main";

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

        private static MvcData GetPageMvcData(PageTemplate pt)
        {
            string qualifiedViewName = pt.Metadata.GetTextFieldValue("view");

            string moduleName;

            return new MvcData
            {
                ViewName = StripModuleName(qualifiedViewName, out moduleName),
                AreaName = moduleName
            };
        }

        private Dictionary<string, object> GetXpmMetadata(Component component, ComponentTemplate ct)
        {
            if (!Settings.GenerateXpmMetadata)
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                { "ComponentID", GetTcmIdentifier(component) },
                { "ComponentModified", component.RevisionDate },
                { "ComponentTemplateID", GetTcmIdentifier(ct) },
                { "ComponentTemplateModified", ct.RevisionDate },
                { "IsRepositoryPublished" , ct.IsRepositoryPublishable }
            };
        }


        private Dictionary<string, object> GetXpmMetadata(Page page)
        {
            if (!Settings.GenerateXpmMetadata)
            {
                return null;
            }

            return new Dictionary<string, object>
            {
                { "PageID", GetTcmIdentifier(page) },
                { "PageModified", page.RevisionDate },
                { "PageTemplateID", GetTcmIdentifier(page.PageTemplate) },
                { "PageTemplateModified", page.PageTemplate.RevisionDate }
            };
        }

        internal ContentModelData BuildContentModel(XmlElement xmlElement, int expandLinkLevels)
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
                        result.Add(currentFieldName,  GetTypedFieldValue(currentFieldValues));
                    }
                    currentFieldName = childElement.Name;
                    currentFieldValues = new List<object>();
                }
                currentFieldValues.Add(GetFieldValue(childElement, expandLinkLevels));
            }

            if (currentFieldName != null)
            {
                result.Add(currentFieldName, GetTypedFieldValue(currentFieldValues));
            }

            return result.Count == 0 ? null : result;
        }

        private static object GetTypedFieldValue(List<object> fieldValues)
        {
            switch (fieldValues.Count)
            {
                case 0:
                    return null;
                case 1:
                    return fieldValues[0];
            }

            Array typedArray = Array.CreateInstance(fieldValues[0].GetType(), fieldValues.Count);
            int i = 0;
            foreach (object fieldValue in fieldValues)
            {
                typedArray.SetValue(fieldValue, i++);
            }
            return typedArray;
        }


        private object GetFieldValue(XmlElement xmlElement, int expandLinkLevels)
        {
            string xlinkHref = xmlElement.GetAttribute("href", Constants.XlinkNamespace);
            if (!string.IsNullOrEmpty(xlinkHref))
            {
                if (!TcmUri.IsValid(xlinkHref))
                {
                    // External link field
                    return xlinkHref;
                }

                TcmUri linkedItemUri = new TcmUri(xlinkHref);
                string path = xmlElement.GetPath();
                _logger.Debug($"Encountered XLink '{path}' -> {xlinkHref}");
                if (expandLinkLevels == 0)
                {
                    _logger.Debug($"Not expanding XLink because configured ExpandLinkDepth of {Settings.ExpandLinkDepth} has been reached.");
                    if (linkedItemUri.ItemType == ItemType.Component)
                    {
                        return new EntityModelData { Id = linkedItemUri.ItemId.ToString() };
                    }
                    if (linkedItemUri.ItemType == ItemType.Keyword)
                    {
                        Keyword keyword = (Keyword) Session.GetObject(linkedItemUri);
                        return new KeywordModelData
                        {
                            Id = GetDxaIdentifier(keyword),
                            TaxonomyId = GetDxaIdentifier(keyword.OrganizationalItem)
                        };
                    }
                }
                else
                {
                    // Expand Component and Keyword Links
                    _logger.Debug($"Expanding XLink. expandLinkLevels: {expandLinkLevels}");
                    if (linkedItemUri.ItemType == ItemType.Component)
                    {
                        Component linkedComponent = (Component) Session.GetObject(linkedItemUri);
                        return BuildEntityModel(linkedComponent, expandLinkLevels - 1);
                    }
                    if (linkedItemUri.ItemType == ItemType.Keyword)
                    {
                        Keyword keyword = (Keyword) Session.GetObject(linkedItemUri);
                        return BuildKeywordModel(keyword, expandLinkLevels - 1);
                    }
                }

                // Not a Component or Keyword link.
                _logger.Debug($"XLink is not a Component or Keyword link.");
                return xlinkHref;
            }

            if (xmlElement.SelectSingleNode("xhtml:*", _xmlNamespaceManager) != null)
            {
                // XHTML field
                return BuildRichTextModel(xmlElement);
            }

            if (xmlElement.SelectSingleNode("*") != null)
            {
                // Embedded field
                return BuildContentModel(xmlElement, expandLinkLevels);
            }

            // Text, number or date field
            return xmlElement.InnerText;
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

                TcmUri linkedItemId = new TcmUri(xlinkHref);
                if (linkedItemId.ItemType != ItemType.Keyword)
                {
                    // Component link field
                    return xlinkHref;
                }


                Keyword keyword = (Keyword) Session.GetObject(linkedItemId);
                return string.IsNullOrEmpty(keyword.Description) ? keyword.Title : keyword.Description;
            }

            if (xmlElement.SelectSingleNode("xhtml:*", _xmlNamespaceManager) != null)
            {
                // XHTML field
                RichTextData richText = BuildRichTextModel(xmlElement);
                return string.Join("", richText.Fragments.Select(f => f.ToString()));
            }

            // Text, number or date field
            return xmlElement.InnerText;
        }

        private RichTextData BuildRichTextModel(XmlElement xhtmlElement)
        {
            XmlDocument xmlDoc = xhtmlElement.OwnerDocument;
            IList<EntityModelData> embeddedEntities = new List<EntityModelData>();
            foreach (XmlElement xlinkElement in xhtmlElement.SelectElements(".//*[starts-with(@xlink:href, 'tcm:')]", _xmlNamespaceManager))
            {
                Component linkedComponent = Session.GetObject(xlinkElement) as Component;
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
                    embeddedEntities.Add(BuildEntityModel(linkedComponent, 0));

                    // Replace entire img element with marker XML processing instruction (see below). 
                    xlinkElement.ParentNode.ReplaceChild(
                        xmlDoc.CreateProcessingInstruction("EmbeddedEntity", string.Empty),
                        xlinkElement
                        );
                }
                else
                {
                    // Hyperlink to MM Component: add the Binary and set the URL as href
                    string binaryUrl = AddBinaryFunction(linkedComponent);
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
        private static bool IsEclItem(Component component)
        {
            return component.BinaryContent != null && component.BinaryContent.MultimediaType.MimeType == EclMimeType;
        }
    }
}
