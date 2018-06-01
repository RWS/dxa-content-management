﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Templates.Common;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.CommunicationManagement.Regions;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Publishing.Resolving;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Default Page/Entity Model Builder implementation.
    /// </summary>
    public class DefaultModelBuilder : DataModelBuilder, IPageModelDataBuilder, IEntityModelDataBuilder, IKeywordModelDataBuilder
    {
        private const string LegacyIncludePrefix = "system/include/";

        private static readonly string[] _standardPageTemplateMetadataFields = { "includes", "view", "regions", "htmlClasses" };
        private static readonly string[] _standardRegionMetadataFields = { "name", "view" };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context <see cref="DataModelBuilderPipeline"/></param>
        public DefaultModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
        }

        /// <summary>
        /// Builds a Page Data Model from a given CM Page object.
        /// </summary>
        /// <param name="pageModelData">The Page Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="page">The CM Page.</param>
        /// <remarks>
        /// This Model Builder is designed to be the first in the pipeline and hence ignores the <paramref name="pageModelData"/> input value.
        /// </remarks>
        public void BuildPageModel(ref PageModelData pageModelData, Page page)
        {
            Logger.Debug($"BuildPageModel({page})");

            if (page == null)
            {
                pageModelData = null;
                return;
            }

            // We need Keyword XLinks for Keyword field expansion
            page.Load(LoadFlags.KeywordXlinks);

            StructureGroup structureGroup = (StructureGroup)page.OrganizationalItem;

            PageTemplate pt = page.PageTemplate;

            IDictionary<string, RegionModelData> regionModels = new Dictionary<string, RegionModelData>();
            AddPredefinedRegions(regionModels, pt);
            AddComponentPresentationRegions(regionModels, page);

            if (Utility.IsNativeRegionsAvailable(page))
            {
                var nativeRegionModels = GetNativeRegions(page.GetPropertyValue<IList<IRegion>>("Regions"));
                MergeNativeRegions(regionModels, nativeRegionModels);
            }

            AddIncludePageRegions(regionModels, pt);

            // Merge Page metadata and PT custom metadata
            ContentModelData ptCustomMetadata = ExtractCustomMetadata(pt.Metadata, excludeFields: _standardPageTemplateMetadataFields);
            ContentModelData pageMetadata = BuildContentModel(page.Metadata, Pipeline.Settings.ExpandLinkDepth);
            string[] duplicateFieldNames;
            ContentModelData pageModelMetadata = MergeFields(pageMetadata, ptCustomMetadata, out duplicateFieldNames);
            if (duplicateFieldNames.Length > 0)
            {
                string formattedDuplicateFieldNames = string.Join(", ", duplicateFieldNames);
                Logger.Debug($"Some custom metadata fields from {pt.FormatIdentifier()} are overridden by Page metadata: {formattedDuplicateFieldNames}");
            }

            string sequencePrefix;
            pageModelData = new PageModelData
            {
                Id = GetDxaIdentifier(page),
                PageTemplate = GetPageTemplateData(pt),
                StructureGroupId = GetDxaIdentifier(structureGroup),
                SchemaId = GetDxaIdentifier(page.MetadataSchema),
                Meta = null, // Default Model builder does not set PageModel.Meta; see DefaultPageMetaModelBuilder.
                Title = StripSequencePrefix(page.Title, out sequencePrefix) , // See DefaultPageMetaModelBuilder
                UrlPath = GetUrlPath(page),
                Regions = regionModels.Values.ToList(),                
                Metadata = pageModelMetadata,
                MvcData = GetPageMvcData(pt),
                HtmlClasses = GetHtmlClasses(pt),
                XpmMetadata = GetXpmMetadata(page)
            };
        }

        private static string GetUrlPath(Page page)
        {
            string pageUrl = page.PublishLocationUrl;

            // Ensure the URL starts with a slash
            if (!pageUrl.StartsWith("/", StringComparison.Ordinal))
            {
                pageUrl = "/" + pageUrl;
            }

            // Remove file extension
            pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf(".", StringComparison.Ordinal));

            return Uri.UnescapeDataString(pageUrl);
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation on a Page.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation (obtained from a Page).</param>
        /// <remarks>
        /// This Model Builder is designed to be the first in the pipeline and hence ignores the <paramref name="entityModelData"/> input value.
        /// </remarks>
        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
            => entityModelData = Pipeline.CreateEntityModel(cp.Component, cp.ComponentTemplate);

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        /// <param name="includeComponentTemplateDetails">Include component template details.</param>
        /// <param name="expandLinkDepth">The level of Component/Keyword links to expand.</param>
        /// <remarks>
        /// This method is called for Component Presentations on a Page, standalone DCPs and linked Components which are expanded.
        /// The <paramref name="expandLinkDepth"/> parameter starts at <see cref="DataModelBuilderSettings.ExpandLinkDepth"/>, 
        /// but is decremented for expanded Component links (recursively).
        /// This Model Builder is designed to be the first in the pipeline and hence ignores the <paramref name="entityModelData"/> input value.
        /// </remarks>
        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct, bool includeComponentTemplateDetails, int expandLinkDepth)
        {
            Logger.Debug($"BuildEntityModel({component}, {ct}, {expandLinkDepth})");

            if (component == null)
            {
                entityModelData = null;
                return;
            }

            // We need Keyword XLinks for Keyword field expansion
            component.Load(LoadFlags.KeywordXlinks);

            entityModelData = new EntityModelData
            {
                Id = GetDxaIdentifier(component),
                SchemaId = GetDxaIdentifier(component.Schema),
                Content = BuildContentModel(component.Content, expandLinkDepth),
                Metadata = BuildContentModel(component.Metadata, expandLinkDepth),
                BinaryContent = BuildBinaryContentData(component),
                Folder = GetFolderData(component)
            };

            if (ct == null) return;

            // We always want the component templaye id
            entityModelData.ComponentTemplate = GetComponentTemplateData(ct);

            if (includeComponentTemplateDetails)
            {
                entityModelData.MvcData = GetEntityMvcData(ct);
                entityModelData.HtmlClasses = GetHtmlClasses(ct);
                entityModelData.XpmMetadata = GetXpmMetadata(component, ct);
                if (ct.IsRepositoryPublishable)
                {
                    entityModelData.Id += "-" + GetDxaIdentifier(ct);
                }
            }
        }

        private BinaryContentData BuildBinaryContentData(Component component)
        {
            BinaryContent binaryContent = component.BinaryContent;
            if (binaryContent == null)
            {
                // Not a Multimedia Component
                return null;
            }

            if (IsEclItem(component))
            {
                // ECL Stub Component should be processed further on in the pipeline by EclModelBuilder.
                return null;
            }

            return new BinaryContentData
            {
                Url = Pipeline.RenderedItem.AddBinary(component).Url,
                FileName = binaryContent.Filename,
                FileSize = binaryContent.Size,
                MimeType = binaryContent.MultimediaType.MimeType
            };
        }

        /// <summary>
        /// Builds a Keyword Data Model from a given CM Keyword object.
        /// </summary>
        /// <param name="keywordModelData">The Keyword Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="keyword">The CM Page.</param>
        /// <param name="expandLinkDepth">The level of Component/Keyword links to expand.</param>
        /// <remarks>
        /// The <paramref name="expandLinkDepth"/> parameter starts at <see cref="DataModelBuilderSettings.ExpandLinkDepth"/>, 
        /// but is decremented for expanded Keyword/Component links (recursively).
        /// This Model Builder is designed to be the first in the pipeline and hence ignores the <paramref name="keywordModelData"/> input value.
        /// </remarks>
        public void BuildKeywordModel(ref KeywordModelData keywordModelData, Keyword keyword, int expandLinkDepth)
        {
            Logger.Debug($"BuildKeywordModel({keyword}, {expandLinkDepth})");

            // We need Keyword XLinks for Keyword field expansion
            keyword.Load(LoadFlags.KeywordXlinks);

            string sequencePrefix;
            keywordModelData = new KeywordModelData
            {
                Id = GetDxaIdentifier(keyword),
                Title = StripSequencePrefix(keyword.Title, out sequencePrefix),
                Description = keyword.Description.NullIfEmpty(),
                Key = keyword.Key.NullIfEmpty(),
                TaxonomyId = GetDxaIdentifier(keyword.OrganizationalItem),
                SchemaId = GetDxaIdentifier(keyword.MetadataSchema),
                Metadata = BuildContentModel(keyword.Metadata, expandLinkDepth)
            };
        }

        private static string GetHtmlClasses(Template t)
        {
            IEnumerable<string> htmlClasses = t?.Metadata?.GetTextFieldValues("htmlClasses");
            return (htmlClasses == null) ? null : string.Join(" ", htmlClasses);
        }

        private void AddPredefinedRegions(IDictionary<string, RegionModelData> regionModels, PageTemplate pageTemplate)
        {
            IEnumerable<XmlElement> regionsMetadata = pageTemplate.Metadata.GetEmbeddedFieldValues("regions");
            if (regionsMetadata == null)
            {
                Logger.Debug($"No predefined Regions found in {pageTemplate.FormatIdentifier()}");
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

                if (regionModels.ContainsKey(regionName))
                {
                    throw new DxaException($"Duplicate predefined Region name '{regionName}' encountered in {pageTemplate.FormatIdentifier()}.");
                }

                RegionModelData regionModel = new RegionModelData
                {
                    Name = regionName,
                    MvcData = new MvcData
                    {
                        ViewName = regionViewName,
                        AreaName = moduleName
                    },
                    Metadata = ExtractCustomMetadata(regionMetadata, excludeFields: _standardRegionMetadataFields),
                    Entities = new List<EntityModelData>()
                };

                regionModels.Add(regionName, regionModel);
            }
        }

        private void AddIncludePageRegions(IDictionary<string, RegionModelData> regionModels, PageTemplate pageTemplate)
        {
            IEnumerable<string> includes = pageTemplate.Metadata.GetTextFieldValues("includes"); // TODO: use external link field (?)
            if (includes == null)
            {
                Logger.Debug($"No include Pages found in {pageTemplate.FormatIdentifier()}");
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
                    Logger.Debug($"Converted legacy Page include '{include}' to WebDAV URL '{includePageId}'.");
                }
                else
                {
                    includePageId = include;
                }

                Page includePage = (Page) Pipeline.Session.GetObject(includePageId);

                string moduleName;
                string regionViewName = StripModuleName(includePage.Title, out moduleName);
                string regionName = regionViewName;

                RegionModelData includePageRegion;
                if (regionModels.TryGetValue(regionName, out includePageRegion))
                {
                    Logger.Debug($"Promoting Region '{regionName}' to Include Page Region.");
                }
                else
                {
                    includePageRegion = new RegionModelData
                    {
                        Name = regionName,
                        MvcData = new MvcData
                        {
                            ViewName = regionViewName,
                            AreaName = moduleName
                        }
                    };
                    regionModels.Add(regionName, includePageRegion);
                }
                includePageRegion.IncludePageId = GetDxaIdentifier(includePage);

                if (Pipeline.Settings.GenerateXpmMetadata)
                {
                    includePageRegion.XpmMetadata = new Dictionary<string, object>
                    {
                        {"IncludedFromPageID", GetTcmIdentifier(includePage)},
                        {"IncludedFromPageTitle", includePage.Title},
                        {"IncludedFromPageFileName", includePage.FileName}
                    };
                }
            }
        }

        private void AddComponentPresentationRegions(IDictionary<string, RegionModelData> regionModels, Page page)
        {
            foreach (ComponentPresentation cp in page.ComponentPresentations)
            {
                var entityModel = GetEntityModelData(cp);
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

        private List<RegionModelData> GetNativeRegions(IList<IRegion> regions)
        {
            List<RegionModelData> regionModelDatas = new List<RegionModelData>();
            foreach (IRegion region in regions)
            {
                string moduleName;
                string regionName = region.RegionName;
                string viewName = region.RegionSchema != null ? region.RegionSchema.Title : regionName;
                viewName = StripModuleName(viewName, out moduleName);
                ContentModelData metadata = BuildContentModel(region.Metadata, expandLinkDepth: 0);
                var regionModelData = new RegionModelData
                {
                    Name = regionName,
                    MvcData = new MvcData
                    {
                        ViewName = viewName,
                        AreaName = moduleName
                    },
                    Entities = new List<EntityModelData>(),
                    Metadata = metadata
                };

                foreach (var cp in region.ComponentPresentations)
                {
                    var entityModel = GetEntityModelData(cp);

                    string dxaRegionName;
                    GetRegionMvcData(cp.ComponentTemplate, out dxaRegionName, string.Empty);

                    if (!string.IsNullOrEmpty(dxaRegionName) && dxaRegionName != regionName)
                    {
                        Logger.Warning($"Component Template '{cp.ComponentTemplate.Title}' is placed inside Region '{regionName}', but Region name in Component Template Metadata is '{dxaRegionName}'.");
                    }

                    regionModelData.Entities.Add(entityModel);
                }

                IList<IRegion> nestedRegions = region.GetPropertyValue<IList<IRegion>>("Regions");
                if (nestedRegions != null)
                {
                    regionModelData.Regions = GetNativeRegions(nestedRegions);
                }

                regionModelDatas.Add(regionModelData);
            }

            return regionModelDatas;
        }

        private EntityModelData GetEntityModelData(ComponentPresentation cp)
        {
            ComponentTemplate ct = cp.ComponentTemplate;

            // Create a Child Rendered Item for the CP in order to make Component linking work.
            RenderedItem childRenderedItem = new RenderedItem(new ResolvedItem(cp.Component, ct),
                Pipeline.RenderedItem.RenderInstruction);
            Pipeline.RenderedItem.AddRenderedItem(childRenderedItem);

            EntityModelData entityModel;
            if (ct.IsRepositoryPublishable)
            {
                Logger.Debug($"Not expanding DCP ({cp.Component}, {ct})");
                entityModel = new EntityModelData
                {
                    Id = $"{GetDxaIdentifier(cp.Component)}-{GetDxaIdentifier(ct)}"
                };
            }
            else
            {
                entityModel = Pipeline.CreateEntityModel(cp);
            }
            return entityModel;
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
                ActionName = controllerAction,
                Parameters = GetMvcParameters(ct)
            };
        }

        private static Dictionary<string, string> GetMvcParameters(ComponentTemplate ct)
        {
            // TODO: support Key/Value Pair Schema (multi-valued embedded field)
            string routeValues = ct.Metadata.GetTextFieldValue("routeValues");
            if (string.IsNullOrEmpty(routeValues))
            {
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string[] keyValuePair in routeValues.Split(',').Select(routeValue => routeValue.Split(':')))
            {
                if (keyValuePair.Length != 2)
                {
                    throw new DxaException($"Invalid syntax for 'routeValues' field in {ct.FormatIdentifier()}: '{keyValuePair}'");
                }
                result.Add(keyValuePair[0].Trim(), keyValuePair[1].Trim());
            }

            return result;
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

        private void MergeNativeRegions(IDictionary<string, RegionModelData> dxaRegions, List<RegionModelData> nativeRegionModels)
        {
            foreach (var nativeRegion in nativeRegionModels)
            {
                // Add native Region if it is not present in DXA region collection
                string regionName = nativeRegion.Name;
                if (!dxaRegions.ContainsKey(regionName))
                {
                    dxaRegions.Add(regionName, nativeRegion);
                }
                else
                {
                    // Transform native Region members to fit into DXA Region model 
                    var dxaRegion = dxaRegions[regionName];
                    if (dxaRegion.Entities == null)
                    {
                        dxaRegion.Entities = new List<EntityModelData>();
                    }
                    dxaRegion.Entities.AddRange(nativeRegion.Entities);

                    // Override Metadata of DXA taken from native Region with the same name
                    if (nativeRegion.Metadata != null && nativeRegion.Metadata.Any())
                    {
                        string[] duplicateFieldNames;
                        dxaRegions[regionName].Metadata = MergeFields(nativeRegion.Metadata, dxaRegions[regionName].Metadata, out duplicateFieldNames);
                        if (duplicateFieldNames.Length > 0)
                        {
                            string formattedDuplicateFieldNames = string.Join(", ", duplicateFieldNames);
                            Logger.Debug($"Some custom metadata fields from DXA Region '{regionName}' are overridden by TCM Region: {formattedDuplicateFieldNames}");
                        }
                    }
                    dxaRegion.Regions = nativeRegion.Regions;
                }
            }
        }

        private Dictionary<string, object> GetXpmMetadata(Component component, ComponentTemplate ct)
        {
            if (!Pipeline.Settings.GenerateXpmMetadata)
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
            if (!Pipeline.Settings.GenerateXpmMetadata)
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

        private PageTemplateData GetPageTemplateData(PageTemplate pt)
        {
            var pageTemplateData = new PageTemplateData
            {
                Id = GetDxaIdentifier(pt),
                Title = pt.Title,
                FileExtension = pt.FileExtension,
                RevisionDate = pt.RevisionDate
            };

            if (pt.Metadata == null || pt.MetadataSchema == null) return pageTemplateData;
            pageTemplateData.Metadata = BuildContentModel(pt.Metadata, Pipeline.Settings.ExpandLinkDepth); ;
            return pageTemplateData;
        }

        private ComponentTemplateData GetComponentTemplateData(ComponentTemplate ct)
        {
            var componentTemplateData = new ComponentTemplateData
            {
                Id = GetDxaIdentifier(ct)
            };
           
            if (ct.Metadata == null || ct.MetadataSchema == null) return componentTemplateData;
            componentTemplateData.Title = ct.Title;
            componentTemplateData.RevisionDate = ct.RevisionDate;
            componentTemplateData.OutputFormat = ct.OutputFormat;
            componentTemplateData.Metadata = BuildContentModel(ct.Metadata, Pipeline.Settings.ExpandLinkDepth);
            return componentTemplateData;
        }

        private FolderData GetFolderData(Component component)
        {
            Folder folder = (Folder) component.OrganizationalItem;
            return new FolderData
            {
                Id = folder.Id?.ItemId.ToString(),
                Title = folder.Title
            };
        }
    }
}
