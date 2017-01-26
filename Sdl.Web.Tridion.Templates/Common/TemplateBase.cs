using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Sdl.Web.DataModel.Configuration;
using Sdl.Web.Tridion.Templates;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Common
{
    /// <summary>
    /// Base class for common functionality used by TBBs
    /// </summary>
    public abstract class TemplateBase : ITemplate
    {
        protected const string JsonMimetype = "application/json";
        protected const string JsonExtension = ".json";
        protected const string BootstrapFilename = "_all";
        protected const string DxaSchemaNamespaceUri = "http://www.sdl.com/web/schemas/core";
        protected const string ModuleConfigurationSchemaRootElementName = "ModuleConfiguration";

        private TemplatingLogger _logger;
        private Engine _engine;
        private Package _package;
        private Publication _publication;
        private bool? _isXpmEnabled;

        protected Engine Engine
        {
            get
            {
                if (_engine == null)
                {
                    throw new DxaException("Initialize has not been called.");
                }
                return _engine;
            }
        }
        protected Package Package
        {
            get
            {
                if (_package == null)
                {
                    throw new DxaException("Initialize has not been called.");
                }
                return _package;
            }
        }

        protected Publication Publication
        {
            get
            {
                if (_publication == null)
                {
                    _publication = GetPublication();
                }
                return _publication;
            }
        }

        protected Session Session
        {
            get
            {
                return Engine.GetSession();
            }
        }

        protected TemplatingLogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = TemplatingLogger.GetLogger(GetType());
                }

                return _logger;
            }
        }


        /// <summary>
        /// Initializes the engine and package to use in this TemplateBase object.
        /// </summary>
        /// <param name="engine">The engine to use in calls to the other methods of this TemplateBase object</param>
        /// <param name="package">The package to use in calls to the other methods of this TemplateBase object</param>
        protected void Initialize(Engine engine, Package package)
        {
            _engine = engine;
            _package = package;
        }

        public abstract void Transform(Engine engine, Package package);

        /// <summary>
        /// Checks whether the TemplateBase object has been initialized correctly.
        /// </summary>
        [Obsolete("Deprecated in DXA 1.7. No need to call this anymore.")]
        protected void CheckInitialized()
        {
        }

        /// <summary>
        /// Update the (JSON) summary in the Output item.
        /// </summary>
        /// <param name="name">The name of the Template.</param>
        /// <param name="files">The files/binaries created by the Template.</param>
        protected void OutputSummary(string name, IEnumerable<string> files)
        {
            List<SummaryData> summaries = new List<SummaryData>();

            Item outputItem = Package.GetByName(Package.OutputName);
            if (outputItem != null)
            {
                summaries = JsonConvert.DeserializeObject <List<SummaryData>>(outputItem.GetAsString());
                Package.Remove(outputItem);
            }

            SummaryData summary = new SummaryData { Name = name, Status = "Success", Files = files };
            summaries.Add(summary);

            string summariesJson = JsonSerialize(summaries);
            outputItem = Package.CreateStringItem(ContentType.Text, summariesJson);
            Package.PushItem(Package.OutputName, outputItem);
        }


        /// <summary>
        /// True if the rendering context is a page, rather than component
        /// </summary>
        public bool IsPageTemplate()
        {
            return Engine.PublishingContext.ResolvedItem.Item is Page;
        }

        /// <summary>
        /// Returns the component object that is defined in the package for this template.
        /// </summary>
        /// <remarks>
        /// This method should only be called when there is an actual Component item in the package. 
        /// It does not currently handle the situation where no such item is available.
        /// </remarks>
        /// <returns>the component object that is defined in the package for this template.</returns>
        public Component GetComponent()
        {
            Item component = Package.GetByName(Package.ComponentName);
            if (component != null)
            {
                return (Component)Engine.GetObject(component.GetAsSource().GetValue("ID"));
            }

            return null;
        }

        /// <summary>
        /// Returns the Template from the resolved item if it's a Component Template
        /// </summary>
        /// <returns>A Component Template or <c>null</c></returns>
        protected ComponentTemplate GetComponentTemplate()
        {
            return Engine.PublishingContext.ResolvedItem.Template as ComponentTemplate;
        }

        /// <summary>
        /// Returns the Page object that is defined in the package for this template.
        /// </summary>
        /// <returns>the page object that is defined in the package for this template.</returns>
        public Page GetPage()
        {
            //first try to get from the render context
            RenderContext renderContext = Engine.PublishingContext.RenderContext;
            if (renderContext != null)
            {
                Page contextPage = renderContext.ContextItem as Page;
                if (contextPage != null)
                {
                    return contextPage;
                }
            }

            Item pageItem = Package.GetByType(ContentType.Page);
            if (pageItem == null)
            {
                return null;
            }

            return (Page) Engine.GetObject(pageItem);
        }

        /// <summary>
        /// Gets the context Publication.
        /// </summary>
        /// <returns>The context Publication.</returns>
        protected Publication GetPublication()
        {
            RepositoryLocalObject inputItem = (RepositoryLocalObject) GetPage() ?? GetComponent();
            if (inputItem == null)
            {
                throw new DxaException("Unable to determine the context Publication.");
            }

            return (Publication) inputItem.ContextRepository;
        }

        /// <summary>
        /// Gets whether XPM is enabled on the publishing target.
        /// </summary>
        protected bool IsXpmEnabled
        {
            get
            {
                if (!_isXpmEnabled.HasValue)
                {
                    _isXpmEnabled = Utility.IsXpmEnabled(Engine.PublishingContext);
                }
                return _isXpmEnabled.Value;
            }
        }

        /// <summary>
        /// Determines if current action is publishing to a XPM enabled target.
        /// </summary>
        /// <returns>True if publishing to a target which is XPM enabled.</returns>
        [Obsolete("Deprecated in DXA 1.7. Use IsXpmEnabled instead.")]
        protected bool IsPublishingToStaging() => IsXpmEnabled;

        [Obsolete("Deprecated in DXA 2.0. Use IsPreview property instead.")]
        protected bool IsPreviewMode() => IsPreview;

        /// <summary>
        /// Gets whether the item is being rendered as part of CM Preview.
        /// </summary>
        protected bool IsPreview => (Engine.RenderMode == RenderMode.PreviewDynamic) || (Engine.RenderMode == RenderMode.PreviewStatic);

        protected bool IsMasterWebPublication(Publication publication)
        {
            if (publication.Metadata == null)
            {
                return false;
            }

            ItemFields publicationMetadataFields = new ItemFields(publication.Metadata, publication.MetadataSchema);
            return !string.IsNullOrEmpty(publicationMetadataFields.GetTextValue("isMaster"));
        }

        /// <summary>
        /// Put the context component back on top of the package stack
        /// As some TBBs (like SiteEdit ones) rely on this being the first
        /// Component in the stack
        /// </summary>
        protected void PutContextComponentOnTop()
        {
            Item mainComponent = Package.GetByName("Component");
            if (mainComponent != null)
            {
                Package.Remove(mainComponent);
                Package.PushItem("Component", mainComponent);
            }
        }

        protected List<KeyValuePair<TcmUri, string>> GetOrganizationalItemContents(OrganizationalItem orgItem, ItemType itemType, bool recursive)
        {
            OrganizationalItemItemsFilter filter = new OrganizationalItemItemsFilter(orgItem.Session)
                {
                    ItemTypes = new List<ItemType> { itemType },
                    Recursive = recursive
                };
            return XmlElementToTcmUriList(orgItem.GetListItems(filter));
        }

        protected OrganizationalItem GetChildOrganizationalItem(OrganizationalItem root, string title)
        {
            foreach (KeyValuePair<TcmUri, string> child in GetOrganizationalItemContents(root, root is Folder ? ItemType.Folder : ItemType.StructureGroup, false))
            {
                if (child.Value.ToLower() == title.ToLower())
                {
                    return (OrganizationalItem)Engine.GetObject(child.Key);
                }
            }
            return null;
        }

        protected List<KeyValuePair<TcmUri, string>> GetUsingItems(RepositoryLocalObject subject, ItemType itemType)
        {
            UsingItemsFilter filter = new UsingItemsFilter(Engine.GetSession())
                {
                    ItemTypes = new List<ItemType> { itemType },
                    BaseColumns = ListBaseColumns.IdAndTitle
                };
            return XmlElementToTcmUriList(subject.GetListUsingItems(filter));
        }

        protected List<KeyValuePair<TcmUri, string>> XmlElementToTcmUriList(XmlElement data)
        {
            List<KeyValuePair<TcmUri, string>> res = new List<KeyValuePair<TcmUri, string>>();
            foreach (XmlNode item in data.SelectNodes("/*/*"))
            {
                string title = item.Attributes["Title"].Value;
                TcmUri id = new TcmUri(item.Attributes["ID"].Value);
                res.Add(new KeyValuePair<TcmUri, string>(id, title));
            }
            return res;
        }

        #region Json Data Processing
        protected Dictionary<string, string> MergeData(Dictionary<string, string> source, Dictionary<string, string> mergeData)
        {
            foreach (string key in mergeData.Keys)
            {
                if (!source.ContainsKey(key))
                {
                    source.Add(key, mergeData[key]);
                }
                else
                {
                    Logger.Warning(String.Format("Duplicate key ('{0}') found when merging data. The second value will be skipped.", key));
                }
            }
            return source;
        }

        [Obsolete("Deprecated in DXA 1.7. Use AddJsonBinary instead.")]
        protected List<string> PublishJsonData(Dictionary<string, List<string>> settings, Component relatedComponent, string variantName, StructureGroup sg, bool isArray = false)
        {
            List<string> files = new List<string>();
            foreach (string key in settings.Keys)
            {
                files.Add(PublishJsonData(settings[key], relatedComponent, key, variantName+key, sg, isArray));
            }
            return files;
        }

        [Obsolete("Deprecated in DXA 1.7. Use AddJsonBinary instead.")]
        protected string PublishJsonData(Dictionary<string,string> data, Component relatedComponent, string filename, string variantName, StructureGroup sg, bool isArray = false)
        {
            return PublishJsonData(data.Select(i => String.Format("{0}:{1}", JsonEncode(i.Key), JsonEncode(i.Value))).ToList(), relatedComponent, filename, variantName, sg, isArray);
        }

        [Obsolete("Deprecated in DXA 1.7. Use AddJsonBinary instead.")]
        protected string PublishJsonData(List<string> settings, Component relatedComponent, string filename, string variantName, StructureGroup sg, bool isArray = false)
        {
            if (settings.Count > 0)
            {
                string json;
                if (isArray)
                {
                    json = String.Format("[{0}]", String.Join(",\n", settings));
                }
                else
                {
                    json = String.Format("{{{0}}}", String.Join(",\n", settings));
                }
                return PublishJson(json, relatedComponent, sg, filename, variantName);
            }
            return null;
        }

        [Obsolete("Deprecated in DXA 1.7. Use AddBootstrapJsonBinary instead.")]
        protected string PublishBootstrapJson(List<string> filesCreated, Component relatedComponent, StructureGroup sg, string variantName = null, List<string> additionalData = null)
        {
            string extras = additionalData != null && additionalData.Count > 0 ? String.Join(",", additionalData) + "," : "";
            return PublishJson(String.Format("{{{0}\"files\":[{1}]}}", extras, String.Join(",", filesCreated.Where(i=>!String.IsNullOrEmpty(i)).ToList())), relatedComponent, sg, BootstrapFilename, variantName + "bootstrap");
        }

        protected void AddBootstrapJsonBinary(IList<Binary> binaries, Component relatedComponent, StructureGroup sg, string variantName)
        {
            BootstrapData bootstrapData = new BootstrapData
            {
                Files = binaries.Where(b => b != null).Select(b => b.Url).ToArray()
            };
            binaries.Add(AddJsonBinary(bootstrapData, relatedComponent, sg, BootstrapFilename, variantName + "-bootstrap"));
        }

        [Obsolete("Deprecated in DXA 1.7. Use AddJsonBinary instead.")]
        protected string PublishJson(string json, Component relatedComponent, StructureGroup sg, string filename, string variantName)
        {
            Item jsonItem = Package.CreateStringItem(ContentType.Text, json);
            Binary binary = Engine.PublishingContext.RenderedItem.AddBinary(jsonItem.GetAsStream(), filename + JsonExtension, sg, variantName, relatedComponent, JsonMimetype);
            jsonItem.Properties[Item.ItemPropertyPublishedPath] = binary.Url;
            Package.PushItem(binary.Url, jsonItem);
            return JsonEncode(binary.Url);
        }

        protected Binary AddJsonBinary(object objectToSerialize, Component relatedComponent, StructureGroup structureGroup, string name, string variantId = null)
        {
            if (string.IsNullOrEmpty(variantId))
            {
                variantId = name;
            }

            string json = JsonSerialize(objectToSerialize);
            Item jsonItem = Package.CreateStringItem(ContentType.Text, json);
            Binary jsonBinary = Engine.PublishingContext.RenderedItem.AddBinary(jsonItem.GetAsStream(), name + JsonExtension, structureGroup, variantId, relatedComponent, JsonMimetype);
            jsonItem.Properties[Item.ItemPropertyPublishedPath] = jsonBinary.Url;
            Package.PushItem(jsonBinary.Url, jsonItem);

            Logger.Info(string.Format("Added JSON Binary '{0}' related to Component '{1}' ({2}) with variant ID '{3}'", 
                jsonBinary.Url, relatedComponent.Title, relatedComponent.Id, variantId));
            return jsonBinary;
        }

        protected Dictionary<string, string> ExtractKeyValuePairs(Component component)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            if (component.Content == null)
            {
                return keyValuePairs;
            }

            ItemFields configComponentFields = new ItemFields(component.Content, component.Schema);
            ItemFields[] settingsFieldValues = configComponentFields.GetEmbeddedFields("settings").ToArray();
            if (settingsFieldValues.Any())
            {
                //either schema is a generic multival embedded name/value
                foreach (ItemFields setting in settingsFieldValues)
                {
                    string key = setting.GetTextValue("name");
                    if (!string.IsNullOrEmpty(key) && !keyValuePairs.ContainsKey(key))
                    {
                        keyValuePairs.Add(key, setting.GetTextValue("value"));
                    }
                    else
                    {
                        Logger.Warning(string.Format("Empty or duplicate key found ('{0}') in Component '{1}' ({2})", 
                            key, component.Title, component.Id));
                    }
                }
            }
            else
            {
                //... or its a custom schema with individual fields
                foreach (ItemField field in configComponentFields)
                {
                    string key = field.Name;
                    keyValuePairs.Add(key, configComponentFields.GetSingleFieldValue(key));
                }
            }
            return keyValuePairs;
        }

        [Obsolete("Deprecated in DXA 1.7. Use JsonSerialize instead.")]
        protected string JsonEncode(object json)
        {
            return JsonSerialize(json);
        }

        protected string JsonSerialize(object objectToSerialize, JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            Newtonsoft.Json.Formatting jsonFormatting = (IsPreviewMode() || IsXpmEnabled) ?
                Newtonsoft.Json.Formatting.Indented :
                Newtonsoft.Json.Formatting.None;

            return JsonConvert.SerializeObject(objectToSerialize, jsonFormatting, settings);
        }

        #endregion

        #region Module Data Processing

        protected StructureGroup GetSystemStructureGroup(string subStructureGroupTitle = null)
        {
            string webDavUrl = string.Format("{0}/_System", Publication.RootStructureGroup.WebDavUrl);
            if (!string.IsNullOrEmpty(subStructureGroupTitle))
            {
                webDavUrl += "/" + subStructureGroupTitle;
            }
            StructureGroup result = Engine.GetObject(webDavUrl) as StructureGroup;
            if (result == null)
            {
                throw new DxaException(string.Format("Cannot find Structure Group with WebDAV URL '{0}'", webDavUrl));
            }
            return result;
        }

        [Obsolete("Deprecated in DXA 1.6. There is no need to pass in a coreConfigComponent; use the parameterless overload instead.")]
        protected Dictionary<string, Component> GetActiveModules(Component coreConfigComponent)
        {
            return GetActiveModules();
        }

        protected Dictionary<string, Component> GetActiveModules()
        {
            Schema moduleConfigSchema = GetModuleConfigSchema();
            Session session = moduleConfigSchema.Session;

            UsingItemsFilter moduleConfigComponentsFilter = new UsingItemsFilter(session)
            {
                ItemTypes = new[] {ItemType.Component},
                BaseColumns = ListBaseColumns.Id
            };

            Dictionary<string, Component> results = new Dictionary<string, Component>();
            foreach (Component comp in moduleConfigSchema.GetUsingItems(moduleConfigComponentsFilter).Cast<Component>())
            {
                // GetUsingItems returns the items in their Owning Publication, which could be lower in the BluePrint than were we are (so don't exist in our context Repository).
                Component moduleConfigComponent = (Component) Publication.GetObject(comp.Id);
                if (!session.IsExistingObject(moduleConfigComponent.Id))
                {
                    continue;
                }

                ItemFields fields = new ItemFields(moduleConfigComponent.Content, moduleConfigComponent.Schema);
                string moduleName = fields.GetTextValue("name").Trim().ToLower();
                if (fields.GetTextValue("isActive").ToLower() == "yes" && !results.ContainsKey(moduleName))
                {
                    results.Add(moduleName, moduleConfigComponent);
                }
            }

            return results;
        }


        private Schema GetModuleConfigSchema()
        {
            Schema[] moduleConfigSchemas = Publication.GetSchemasByNamespaceUri(DxaSchemaNamespaceUri, ModuleConfigurationSchemaRootElementName).ToArray();

            if (moduleConfigSchemas.Length == 0)
            {
                throw new DxaException(
                    string.Format("Schema with namespace '{0}' and root element name '{1}' not found.", DxaSchemaNamespaceUri, ModuleConfigurationSchemaRootElementName)
                    );
            }

            if (moduleConfigSchemas.Length > 1)
            {
                    throw new DxaException(
                        string.Format("Found multiple Schemas with namespace '{0}' and root element name '{1}'", DxaSchemaNamespaceUri, ModuleConfigurationSchemaRootElementName) 
                        );
            }

            return moduleConfigSchemas.First();
        }

        [Obsolete("Deprecated in DXA 1.7.")]
        protected string GetModuleNameFromItem(RepositoryLocalObject item, string moduleRoot)
        {
            //The module name is the name of the folder within the first level of the module root folder 
            //in which the item lives
            string fullItemWebdavUrl = item.WebDavUrl;
            if (fullItemWebdavUrl.StartsWith(moduleRoot))
            {
                Logger.Debug(fullItemWebdavUrl + ":" + moduleRoot);
                string res = fullItemWebdavUrl.Substring(moduleRoot.Length + 1);
                int pos = res.IndexOf("/", StringComparison.Ordinal);
                Logger.Debug(res);
                return res.Substring(0, pos).ToLower();
            }
            return null;
        }
        
        protected static string GetRegionName(ComponentTemplate template)
        {
            // check CT metadata
            if (template.MetadataSchema != null && template.Metadata != null)
            {
                ItemFields meta = new ItemFields(template.Metadata, template.MetadataSchema);

                string regionName = meta.GetTextValue("regionName");
                if (!string.IsNullOrEmpty(regionName))
                {
                    return regionName;
                }

                string regionViewName = meta.GetTextValue("regionView");
                if (!string.IsNullOrEmpty(regionViewName))
                {
                    // strip module from fully qualified name
                    // since we need just the region name here as the web application can't deal with fully qualified region names yet
                    return StripModuleFromName(regionViewName);
                }
            }

            // fallback use template title
            Match match = Regex.Match(template.Title, @".*?\[(.+?)\]");
            if (match.Success)
            {
                // strip module from fully qualified name
                // since we need just the region name here as the web application can't deal with fully qualified region names yet
                return StripModuleFromName(match.Groups[1].Value);
            }

            // default region name
            return "Main";
        }

        private static string StripModuleFromName(string name)
        {
            // split fully qualified view name on colon, use last part as unqualified view name
            string[] nameParts = name.Trim().Split(':');
            if (nameParts.Length > 1)
            {
                return nameParts[1];
            }

            return name;
        }
        #endregion

    }
}
