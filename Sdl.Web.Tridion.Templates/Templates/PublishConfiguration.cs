using System.Globalization;
using Sdl.Web.Tridion.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sdl.Web.DataModel.Configuration;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Publishes site configuration as JSON files. Multiple configuration components can 
    /// be linked to a module configuration. The values in these are merged into a single
    /// configuration file per module. There are also JSON files published containing schema and template
    /// information (1 per module) and a general taxonomies configuration json file
    /// </summary>
    [TcmTemplateTitle("Publish Configuration")]
    public class PublishConfiguration : TemplateBase
    {
        private const string TemplateConfigName = "templates";
        private const string SchemasConfigName = "schemas";
        private const string LocalizationConfigComponentTitle = "Localization Configuration";
        private const string EnvironmentConfigComponentTitle = "Environment Configuration";
        private const string SearchConfigComponentTitle = "Search Configuration";
        private const string CmsUrlKey = "cmsurl";
        private const string SearchQueryUrlKey = "queryURL";
        private const string StagingSearchIndexKey = "stagingIndexConfig";
        private const string LiveSearchIndexKey = "liveIndexConfig";

        private StructureGroup _configStructureGroup;
        private Component _localizationConfigurationComponent;

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            // The input Component is used to relate some of the generated Binaries to (so they get unpublished if the Component is unpublished).
            Component inputComponent = GetComponent();
            _configStructureGroup = GetSystemStructureGroup("config");

            List<Binary> binaries = new List<Binary>()
            {
                PublishTaxonomyMappings(inputComponent)
            };

            //For each active module, publish the config and add the filename(s) to the bootstrap list
            foreach (KeyValuePair<string, Component> module in GetActiveModules())
            {
                string moduleName = module.Key;
                Component moduleConfigComponent = module.Value;
                Folder moduleFolder = GetModuleFolder(moduleConfigComponent);
                binaries.Add(PublishModuleConfig(moduleName, moduleConfigComponent));
                binaries.Add(PublishModuleSchemasConfig(moduleName, moduleFolder, moduleConfigComponent));
                binaries.Add(PublishModuleTemplatesConfig(moduleName, moduleFolder, moduleConfigComponent));
            }

            // Remove empty/null entries
            binaries = binaries.Where(b => b != null).ToList();

            //Publish the boostrap list, this is used by the web application to load in all other configuration files
            binaries.Add(PublishLocalizationData(binaries, inputComponent));

            OutputSummary("Publish Configuration", binaries.Select(b => b.Url));
        }

        private Binary PublishModuleConfig(string moduleName, Component moduleConfigComponent)
        {
            Dictionary<string, string> configSettings = new Dictionary<string, string>();
            ItemFields configComponentFields = new ItemFields(moduleConfigComponent.Content, moduleConfigComponent.Schema);
            foreach (Component configComp in configComponentFields.GetComponentValues("furtherConfiguration"))
            {
                configSettings = MergeData(configSettings, ExtractKeyValuePairs(configComp));
                switch (configComp.Title)
                {
                    case LocalizationConfigComponentTitle:
                        _localizationConfigurationComponent = configComp;
                        break;

                    case EnvironmentConfigComponentTitle:
                        string cmWebsiteUrl = TopologyManager.GetCmWebsiteUrl();
                        string cmsUrl;
                        if (configSettings.TryGetValue(CmsUrlKey, out cmsUrl) && !string.IsNullOrWhiteSpace(cmsUrl))
                        {
                            Logger.Warning(
                                string.Format("Overriding '{0}' specified in '{1}' Component ('{2}') with CM Website URL obtained from Topology Manager: '{3}'",
                                    CmsUrlKey, EnvironmentConfigComponentTitle, cmsUrl, cmWebsiteUrl)
                                );
                        }
                        else
                        {
                            Logger.Info(string.Format("Setting '{0}' to CM Website URL obtained from Topology Manager: '{1}'", CmsUrlKey, cmWebsiteUrl));
                        }
                        configSettings[CmsUrlKey] = cmWebsiteUrl;
                        break;

                    case SearchConfigComponentTitle:
                        string cdEnvironmentPurpose = Utility.GetCdEnvironmentPurpose(Engine.PublishingContext);
                        if (!string.IsNullOrEmpty(cdEnvironmentPurpose))
                        {
                            string searchQueryUrl = TopologyManager.GetSearchQueryUrl((Publication)configComp.ContextRepository, cdEnvironmentPurpose);
                            if (!string.IsNullOrEmpty(searchQueryUrl))
                            {
                                string legacyConfigKey = Utility.IsXpmEnabled(Engine.PublishingContext) ? StagingSearchIndexKey : LiveSearchIndexKey;
                                Logger.Info(string.Format("Setting '{0}' and '{1}' to Search Query URL obtained from Topology Manager: '{2}'", 
                                    SearchQueryUrlKey, legacyConfigKey, searchQueryUrl));
                                configSettings[legacyConfigKey] = searchQueryUrl;
                                configSettings[SearchQueryUrlKey] = searchQueryUrl;
                            }
                            else
                            {
                                Logger.Warning(string.Format("No Search Query URL defined in Topology Manager for Publication '{0}' and CD Environment Purpose '{1}'.", 
                                    configComp.ContextRepository.Id, cdEnvironmentPurpose));
                            }
                        }
                        break;

                }
            }

            return configSettings.Count == 0 ? null : AddJsonBinary(configSettings, moduleConfigComponent, _configStructureGroup, moduleName, "config");
        }
      
        private Binary PublishLocalizationData(IEnumerable<Binary> binaries, Component relatedComponent)
        {
            ComponentTemplatesFilter ctFilter = new ComponentTemplatesFilter(Session)
            {
                AllowedOnPage = false,
                BaseColumns = ListBaseColumns.IdAndTitle
            };
            const string dataPresentationTemplateTitle = "Generate Data Presentation";
            ComponentTemplate dataPresentationTemplate = Publication.GetComponentTemplates(ctFilter).FirstOrDefault(ct => ct.Title == dataPresentationTemplateTitle);
            string dataPresentationId = dataPresentationTemplate?.Id ?? string.Empty;

            string localizationId = Publication.Id.ItemId.ToString();
            
            List<SiteLocalizationData> siteLocalizations = DetermineSiteLocalizations(Publication);
 
            LocalizationData localizationData = new LocalizationData
            {
                IsDefaultLocalization = siteLocalizations.First(p => p.Id == localizationId).IsMaster,
                IsXpmEnabled = IsXpmEnabled,
                MediaRoot = Publication.MultimediaUrl,
                SiteLocalizations = siteLocalizations.ToArray(),
                ConfigStaticContentUrls = binaries.Select(b => b.Url).ToArray(),
                DataPresentationTemplateId = dataPresentationId
            };

            return AddJsonBinary(localizationData, relatedComponent, _configStructureGroup, "_all", variantId: "config-bootstrap");
        }

        private Publication GetMasterPublication(Publication contextPublication)
        {
            string siteId = GetSiteIdFromPublication(contextPublication);
            List<Publication> validParents = new List<Publication>();
            if (siteId != null && siteId!="multisite-master")
            {
                foreach (Repository item in contextPublication.Parents)
                {
                    Publication parent = (Publication)item;
                    if (IsCandidateMaster(parent, siteId))
                    {
                        validParents.Add(parent);
                    }
                }
            }
            if (validParents.Count > 1)
            {
                Logger.Error(String.Format("Publication {0} has more than one parent with the same (or empty) siteId {1}. Cannot determine site grouping, so picking the first parent: {2}.", contextPublication.Title, siteId, validParents[0].Title));
            }
            return validParents.Count==0 ? contextPublication : GetMasterPublication(validParents[0]);
        }

        private bool IsCandidateMaster(Publication pub, string childId)
        {
            //A publication is a valid master if:
            //a) Its siteId is "multisite-master" or
            //b) Its siteId matches the passed (child) siteId
            string siteId = GetSiteIdFromPublication(pub);
            return siteId == "multisite-master" || childId == siteId;
        }


        private List<SiteLocalizationData> DetermineSiteLocalizations(Publication contextPublication)
        {
            string siteId = GetSiteIdFromPublication(contextPublication);
            Publication master = GetMasterPublication(contextPublication);
            Logger.Debug(String.Format("Master publication is : {0}, siteId is {1}", master.Title, siteId));
            List<SiteLocalizationData> siteLocalizations = new List<SiteLocalizationData>();
            bool masterAdded = false;
            if (GetSiteIdFromPublication(master) == siteId)
            {
                masterAdded = IsMasterWebPublication(master);
                siteLocalizations.Add(GetPublicationDetails(master, masterAdded));
            }
            if (siteId!=null)
            {
                siteLocalizations.AddRange(GetChildPublicationDetails(master, siteId, masterAdded));
            }
            //It is possible that no publication has been set explicitly as the master
            //in which case we set the context publication as the master
            if (!siteLocalizations.Any(p => p.IsMaster))
            {
                string currentPubId = Publication.Id.ItemId.ToString(CultureInfo.InvariantCulture);
                foreach (SiteLocalizationData pub in siteLocalizations)
                {
                    if (pub.Id==currentPubId)
                    {
                        pub.IsMaster = true;
                    }
                }
            }
            return siteLocalizations;
        }

        private SiteLocalizationData GetPublicationDetails(Publication pub, bool isMaster = false)
        {
            SiteLocalizationData pubData = new SiteLocalizationData
            {
                Id = pub.Id.ItemId.ToString(CultureInfo.InvariantCulture),
                Path = pub.PublicationUrl,
                IsMaster = isMaster
            };

            if (_localizationConfigurationComponent != null)
            {
                TcmUri localUri = new TcmUri(_localizationConfigurationComponent.Id.ItemId,ItemType.Component,pub.Id.ItemId);
                Component locComp = (Component)Engine.GetObject(localUri);
                if (locComp != null)
                {
                    ItemFields fields = new ItemFields(locComp.Content, locComp.Schema);
                    foreach (ItemFields field in fields.GetEmbeddedFields("settings"))
                    {
                        if (field.GetTextValue("name") == "language")
                        {
                            pubData.Language = field.GetTextValue("value");
                            break;
                        }
                    }
                }
            }
            return pubData;
        }

        private IEnumerable<SiteLocalizationData> GetChildPublicationDetails(Publication master, string siteId, bool masterAdded)
        {
            List<SiteLocalizationData> pubs = new List<SiteLocalizationData>();
            UsingItemsFilter filter = new UsingItemsFilter(Session) { ItemTypes = new List<ItemType> { ItemType.Publication } };
            foreach (XmlElement item in master.GetListUsingItems(filter).ChildNodes)
            {
                string id = item.GetAttribute("ID");
                Publication child = (Publication)Engine.GetObject(id);
                string childSiteId = GetSiteIdFromPublication(child);
                if (childSiteId == siteId)
                {
                    Logger.Debug(String.Format("Found valid descendent {0} with site ID {1} ", child.Title, childSiteId));
                    bool isMaster = !masterAdded && IsMasterWebPublication(child);
                    pubs.Add(GetPublicationDetails(child, isMaster));
                    masterAdded = masterAdded || isMaster;
                }
                else
                {
                    Logger.Debug(String.Format("Descendent {0} has invalid site ID {1} - ignoring ",child.Title,childSiteId));
                }
            }
            return pubs;
        }

        private string GetSiteIdFromPublication(Publication startPublication)
        {
            if (startPublication.Metadata!=null)
            {
                ItemFields meta = new ItemFields(startPublication.Metadata, startPublication.MetadataSchema);
                return meta.GetTextValue("siteId");
            }
            return null;
        }

        private Binary PublishTaxonomyMappings(Component relatedComponent)
        {
            IEnumerable<Category> taxonomies = Publication.GetTaxonomies();
            IDictionary<string, int> taxonomyMappings = taxonomies.ToDictionary(Utility.GetKeyFromTaxonomy, cat => cat.Id.ItemId);

            return AddJsonBinary(taxonomyMappings, relatedComponent, _configStructureGroup, "core.taxonomies", variantId: "taxonomies");
        }


        private Folder GetModuleFolder(Component moduleConfigComponent)
        {
            IList<OrganizationalItem> moduleConfigAncestors = moduleConfigComponent.OrganizationalItem.GetAncestors().ToList();
            moduleConfigAncestors.Insert(0, moduleConfigComponent.OrganizationalItem);
            if (moduleConfigAncestors.Count < 3)
            {
                throw new ApplicationException(
                    String.Format("Unable to determine Module Folder for Module Configuration Component '{0}': too few parent Folders.", moduleConfigComponent.WebDavUrl)
                    );
            }

            // Module folder is always third level (under Root Folder and Modules folder).
            return (Folder) moduleConfigAncestors[moduleConfigAncestors.Count - 3];
        }

        private Binary PublishModuleSchemasConfig(string moduleName, Folder moduleFolder, Component moduleConfigComponent)
        {
            OrganizationalItemItemsFilter moduleSchemasFilter = new OrganizationalItemItemsFilter(Session)
            {
                ItemTypes =  new [] { ItemType.Schema },
                Recursive = true
            };

            Schema[] moduleSchemas = moduleFolder.GetItems(moduleSchemasFilter).Cast<Schema>().Where(s => s.Purpose == SchemaPurpose.Component).ToArray();
            if (!moduleSchemas.Any())
            {
                return null;
            }

            IDictionary <string, int> moduleSchemasConfig = new Dictionary<string, int>();
            foreach (Schema moduleSchema in moduleSchemas)
            {
                string schemaKey = Utility.GetKeyFromSchema(moduleSchema);
                int sameKeyAsSchema;
                if (moduleSchemasConfig.TryGetValue(schemaKey, out sameKeyAsSchema))
                {
                    Logger.Warning(string.Format("{0} has same key ('{1}') as Schema '{2}'; supressing from output.", moduleSchema, schemaKey, sameKeyAsSchema));
                    continue;
                }
                moduleSchemasConfig.Add(schemaKey, moduleSchema.Id.ItemId);
            }

            string fileName = string.Format("{0}.{1}", moduleName, SchemasConfigName);
            return AddJsonBinary(moduleSchemasConfig, moduleConfigComponent, _configStructureGroup, fileName, variantId: "schemas");
        }

        private Binary PublishModuleTemplatesConfig(string moduleName, Folder moduleFolder, Component moduleConfigComponent)
        {
            OrganizationalItemItemsFilter moduleTemplatesFilter = new OrganizationalItemItemsFilter(Session)
            {
                ItemTypes = new[] { ItemType.ComponentTemplate },
                Recursive = true
            };

            ComponentTemplate[] moduleComponentTemplates = moduleFolder.GetItems(moduleTemplatesFilter).Cast<ComponentTemplate>().Where(ct => ct.IsRepositoryPublishable).ToArray();
            if (!moduleComponentTemplates.Any())
            {
                return null;
            }

            IDictionary<string, int> moduleTemplatesConfig = new Dictionary<string, int>();
            foreach (ComponentTemplate moduleTemplate in moduleComponentTemplates)
            {
                string templateKey = Utility.GetKeyFromTemplate(moduleTemplate);
                int sameKeyAsTemplate;
                if (moduleTemplatesConfig.TryGetValue(templateKey, out sameKeyAsTemplate))
                {
                    Logger.Warning(string.Format("{0} has same key ('{1}') as CT '{2}'; supressing from output.", moduleTemplate, templateKey, sameKeyAsTemplate));
                    continue;
                }
                moduleTemplatesConfig.Add(templateKey, moduleTemplate.Id.ItemId);
            }

            string fileName = string.Format("{0}.{1}", moduleName, TemplateConfigName);
            return AddJsonBinary(moduleTemplatesConfig,moduleConfigComponent, _configStructureGroup, fileName, variantId: "templates");
        }
    }
}
