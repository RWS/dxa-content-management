using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Publishing.Resolving;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Default Page/Entity Model Builder implementation.
    /// </summary>
    public class DefaultModelBuilder : DataModelBuilder, IPageModelDataBuilder, IEntityModelDataBuilder
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

            PageTemplate pt = page.PageTemplate;

            IDictionary<string, RegionModelData> regionModels = new Dictionary<string, RegionModelData>();
            AddPredefinedRegions(regionModels, pt);
            AddComponentPresentationRegions(regionModels, page);
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

            pageModelData = new PageModelData
            {
                Id = GetDxaIdentifier(page),
                SchemaId = GetDxaIdentifier(page.MetadataSchema),
                Meta = null, // Default Model builder does not set PageModel.Meta; see DefaultPageMetaBuilder.
                Title = page.Title, // See DefaultPageMetaBuilder
                Regions = regionModels.Values.ToList(),
                Metadata = pageModelMetadata,
                MvcData = GetPageMvcData(pt),
                XpmMetadata = GetXpmMetadata(page)
            };
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation object.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation.</param>
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
        /// <remarks>
        /// This Model Builder is designed to be the first in the pipeline and hence ignores the <paramref name="entityModelData"/> input value.
        /// </remarks>
        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct)
        {
            Logger.Debug($"BuildEntityModel({component}, {ct})");

            if (component == null)
            {
                entityModelData = null;
                return;
            }

            entityModelData = BuildEntityModel(component, Pipeline.Settings.ExpandLinkDepth);

            if (ct == null)
            {
                return;
            }

            entityModelData.MvcData = GetEntityMvcData(ct);
            entityModelData.HtmlClasses = GetHtmlClasses(ct);
            entityModelData.XpmMetadata = GetXpmMetadata(component, ct);
            if (ct.IsRepositoryPublishable)
            {
                entityModelData.Id += "-" + GetDxaIdentifier(ct);
            }
        }

        private static string GetHtmlClasses(ComponentTemplate ct)
        {
            IEnumerable<string> htmlClasses = ct?.Metadata?.GetTextFieldValues("htmlClasses");
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

                if (regionModels.ContainsKey(regionName))
                {
                    // TODO: log this? Throw exception? Promote Region to Include Page Region?
                    Logger.Debug("TODO: merge include Page Region '{regionName}'");
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

                if (Pipeline.Settings.GenerateXpmMetadata)
                {
                    includePageRegion.XpmMetadata = new Dictionary<string, object>
                    {
                        {"IncludedFromPageID", GetTcmIdentifier(includePage)},
                        {"IncludedFromPageTitle", includePage.Title},
                        {"IncludedFromPageFileName", includePage.FileName}
                    };
                }
                regionModels.Add(regionName, includePageRegion);
            }
        }

        private List<RegionModelData> ExpandIncludePage(Page includePage)
        {
            Logger.Debug($"Expanding Include Page '{includePage.Title}' for now (until we have a Model Service).");

            PageModelData includePageModel = null;
            BuildPageModel(ref includePageModel, includePage); // NOTE: Not using the entire Model Builder Pipeline here.
            return includePageModel.Regions;
        }

        private void AddComponentPresentationRegions(IDictionary<string, RegionModelData> regionModels, Page page)
        {
            foreach (ComponentPresentation cp in page.ComponentPresentations)
            {
                ComponentTemplate ct = cp.ComponentTemplate;

                // Create a Child Rendered Item for the CP in order to make Component linking work.
                RenderedItem childRenderedItem = new RenderedItem(new ResolvedItem(cp.Component, ct), Pipeline.RenderedItem.RenderInstruction);
                Pipeline.RenderedItem.AddRenderedItem(childRenderedItem);

                // TODO TSI-24: For DCPs we should output only a minimal Entity Model containing the Component and Template ID, so it can be retrieved dynamically.
                //EntityModelData entityModel = cp.ComponentTemplate.IsRepositoryPublishable ?
                //    new EntityModelData { Id = GetDxaIdentifier(cp.Component, cp.ComponentTemplate) } :
                //    BuildEntityModel(cp.Component, cp.ComponentTemplate);
                if (ct.IsRepositoryPublishable)
                {
                    Logger.Debug($"Expanding DCP ({cp.Component}, {ct}) for now (until we have a Model Service).");
                }
                EntityModelData entityModel = Pipeline.CreateEntityModel(cp);

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
    }
}
