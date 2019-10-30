using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Sdl.Web.DataModel.Configuration;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.Common
{
    /// <summary>
    /// Base class for common functionality used by DXA TBBs.
    /// </summary>
    public abstract class TemplateBase : ITemplate
    {       
        protected const string JsonMimetype = "application/json";
        protected const string JsonExtension = ".json";
        protected const string BootstrapFilename = "_all";
        protected const string DxaSchemaNamespaceUri = "http://www.sdl.com/web/schemas/core";
        protected const string ModuleConfigurationSchemaRootElementName = "ModuleConfiguration";

        private static readonly Regex _tcdlComponentPresentationRegex = new Regex(
            "</?tcdl:ComponentPresentation[^>]*>", RegexOptions.Compiled);

        private TemplatingLogger _logger;
        private Session _session;
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

        /// <summary>
        /// Returns the current Package
        /// </summary>
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
            set
            {
                // Allows dependency injection for unit test purposes.
                _package = value;
            }
        }

        /// <summary>
        /// Gets or sets the current Publication.
        /// </summary>
        protected Publication Publication
        {
            get { return _publication ?? (_publication = GetPublication()); }
            set
            {
                // Allows dependency injection for unit test purposes.
                _publication = value;
            }
        }

        /// <summary>
        /// Gets or sets the current Session.
        /// </summary>
        protected Session Session
        {
            get { return _session ?? (_session = Engine.GetSession()); }
            set
            {
                // Allows dependency injection for unit test purposes.
                _session = value;
            }
        }

        /// <summary>
        /// Returns the current Logger
        /// </summary>
        protected TemplatingLogger Logger => _logger ?? (_logger = TemplatingLogger.GetLogger(GetType()));

        /// <summary>
        /// Attempts to return value of a parameter
        /// </summary>
        /// <typeparam name="T">Type of Parameter</typeparam>
        /// <param name="parameterName">Parameter Name</param>
        /// <param name="value">Value of Parameter</param>
        /// <returns></returns>
        protected bool TryGetParameter<T>(string parameterName, out T value)
            => Package.TryGetParameter(parameterName, out value, Logger);

        /// <summary>
        /// Return item to be rendered.
        /// </summary>
        protected RenderedItem RenderedItem => Engine.PublishingContext.RenderedItem;
        
        /// <summary>
        /// Initializes the Engine and Package to use in this TemplateBase object.
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
        /// Output rendered json.
        /// </summary>
        /// <param name="json">Json to render</param>
        protected string OutputJson
        {
            get { return OutputText; }
            set { OutputText = value; }
        }

        /// <summary>
        /// Output text.
        /// </summary>
        protected string OutputText
        {
            get
            {
                Item outputItem = Package.GetByName(Package.OutputName);
                return outputItem?.GetAsString();
            }
            set
            {
                Item outputItem = Package.CreateStringItem(ContentType.Text, value);
                Package.PushItem(Package.OutputName, outputItem);
            }
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
                summaries = JsonConvert.DeserializeObject<List<SummaryData>>(outputItem.GetAsString());
                Package.Remove(outputItem);
            }

            SummaryData summary = new SummaryData {Name = name, Status = "Success", Files = files};
            summaries.Add(summary);

            string summariesJson = JsonSerialize(summaries, IsPreview);
            if (string.IsNullOrEmpty(summariesJson))
                throw new DxaException("Output Json should not be empty.");
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
                return (Component) Engine.GetObject(component.GetAsSource().GetValue("ID"));
            }

            return null;
        }

        /// <summary>
        /// Returns the Template from the resolved item if it's a Component Template
        /// </summary>
        /// <returns>A Component Template or <c>null</c></returns>
        protected ComponentTemplate GetComponentTemplate() 
            => Engine.PublishingContext.ResolvedItem.Template as ComponentTemplate;

        /// <summary>
        /// Returns the Page object that is defined in the package for this template.
        /// </summary>
        /// <returns>the page object that is defined in the package for this template.</returns>
        public Page GetPage()
        {
            //first try to get from the render context
            RenderContext renderContext = Engine.PublishingContext?.RenderContext;
            Page contextPage = renderContext?.ContextItem as Page;
            if (contextPage != null)
            {
                return contextPage;
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
        /// Gets whether the item is being rendered as part of CM Preview.
        /// </summary>
        protected bool IsPreview
            => (Engine.RenderMode == RenderMode.PreviewDynamic) || (Engine.RenderMode == RenderMode.PreviewStatic);

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

        protected List<KeyValuePair<TcmUri, string>> GetOrganizationalItemContents(OrganizationalItem orgItem,
            ItemType itemType, bool recursive)
        {
            OrganizationalItemItemsFilter filter = new OrganizationalItemItemsFilter(orgItem.Session)
            {
                ItemTypes = new List<ItemType> {itemType},
                Recursive = recursive
            };
            return XmlElementToTcmUriList(orgItem.GetListItems(filter));
        }

        protected OrganizationalItem GetChildOrganizationalItem(OrganizationalItem root, string title)
        {
            foreach (
                KeyValuePair<TcmUri, string> child in
                    GetOrganizationalItemContents(root, root is Folder ? ItemType.Folder : ItemType.StructureGroup,
                        false))
            {
                if (child.Value.ToLower() == title.ToLower())
                {
                    return (OrganizationalItem) Engine.GetObject(child.Key);
                }
            }
            return null;
        }

        protected List<KeyValuePair<TcmUri, string>> GetUsingItems(RepositoryLocalObject subject, ItemType itemType)
        {
            UsingItemsFilter filter = new UsingItemsFilter(Engine.GetSession())
            {
                ItemTypes = new List<ItemType> {itemType},
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

        /// <summary>
        /// Gets a cached value.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="cacheRegion">The cache region.</param>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="addFunction">The function used to provide a value if no value is cached yet.</param>
        /// <remarks>
        /// This uses the TOM.NET Session Cache if available (it typically is during rendering/publishing).
        /// </remarks>
        /// <returns>The cached value.</returns>
        protected T GetCachedValue<T>(string cacheRegion, string cacheKey, Func<T> addFunction)
        {
            ICache cache = Session.Cache;
            object cachedValue = cache?.Get(cacheRegion, cacheKey);

            T result;
            if (cachedValue == null)
            {
                result = addFunction();
                cache?.Add(cacheRegion, cacheKey, result);
            }
            else
            {
                result = (T) cachedValue;
            }

            return result;
        }

        /// <summary>
        /// Determines the locale/culture for the current Publication.
        /// </summary>
        /// <returns>The locale or <c>null</c> if it cannot be determined.</returns>
        protected string GetLocale()
            => GetCachedValue("DxaLocale", Publication.Id, DetermineLocale);

        private string DetermineLocale()
        {
            // TODO: use a less complicated (and more explicit) way to store locale/language. What about Publication metadata?
            Schema generalConfigSchema = GetSchema("GeneralConfiguration");

            UsingItemsFilter configComponentsFilter = new UsingItemsFilter(Session)
            {
                ItemTypes = new[] {ItemType.Component},
                BaseColumns = ListBaseColumns.IdAndTitle
            };

            IEnumerable<Component> configComponents =
                generalConfigSchema.GetUsingItems(configComponentsFilter).Cast<Component>();
            Component localizationConfigComponent =
                configComponents.FirstOrDefault(c => c.Title == "Localization Configuration");
            if (localizationConfigComponent == null)
            {
                Logger.Warning("No Localization Configuration Component found.");
                return null;
            }

            // Ensure we load the Component in the current context Publication
            localizationConfigComponent = (Component) Publication.GetObject(localizationConfigComponent.Id);

            Dictionary<string, string> settings = ExtractKeyValuePairs(localizationConfigComponent);

            string result;
            const string cultureSetting = "culture";
            if (!settings.TryGetValue(cultureSetting, out result))
            {
                Logger.Warning(
                    $"No '{cultureSetting}' setting found in Localization Configuration {localizationConfigComponent.FormatIdentifier()}.");
            }

            return result;
        }

        /// <summary>
        /// Strips tcdl:ComponentPresentation tag from rendered Component Presentation.
        /// </summary>
        /// <param name="renderedComponentPresentation">The rendered Component Presentation.</param>
        /// <returns>The rendered Component Presentation without tcdl:ComponentPresentation tag.</returns>
        protected static string StripTcdlComponentPresentationTag(string renderedComponentPresentation)
            => _tcdlComponentPresentationRegex.Replace(renderedComponentPresentation, string.Empty);

        #region Json Data Processing

        protected Dictionary<string, string> MergeData(Dictionary<string, string> source,
            Dictionary<string, string> mergeData)
        {
            foreach (string key in mergeData.Keys)
            {
                if (!source.ContainsKey(key))
                {
                    source.Add(key, mergeData[key]);
                }
                else
                {
                    Logger.Warning(
                        string.Format(
                            "Duplicate key ('{0}') found when merging data. The second value will be skipped.", key));
                }
            }
            return source;
        }

        protected void AddBootstrapJsonBinary(IList<Binary> binaries, Component relatedComponent, StructureGroup sg,
            string variantName)
        {
            BootstrapData bootstrapData = new BootstrapData
            {
                Files = binaries.Where(b => b != null).Select(b => b.Url).ToArray()
            };
            binaries.Add(AddJsonBinary(bootstrapData, relatedComponent, sg, BootstrapFilename,
                variantName + "-bootstrap"));
        }

        protected Binary AddJsonBinary(object objectToSerialize, Component relatedComponent,
            StructureGroup structureGroup, string name, string variantId = null)
        {
            //In unittest there may not be a PublishingContext
            if (Engine.PublishingContext == null) return null;

            if (string.IsNullOrEmpty(variantId))
            {
                variantId = name;
            }

            string json = JsonSerialize(objectToSerialize, IsPreview | IsXpmEnabled);
            Item jsonItem = Package.CreateStringItem(ContentType.Text, json);
            Binary jsonBinary = Engine.PublishingContext.RenderedItem.AddBinary(jsonItem.GetAsStream(),
                name + JsonExtension, structureGroup, variantId, relatedComponent, JsonMimetype);
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

        protected string JsonSerialize(object objectToSerialize, bool prettyPrint = false,
            JsonSerializerSettings settings = null)
        {
            if (settings == null)
            {
                settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            Newtonsoft.Json.Formatting jsonFormatting = prettyPrint
                ? Newtonsoft.Json.Formatting.Indented
                : Newtonsoft.Json.Formatting.None;

            return JsonConvert.SerializeObject(objectToSerialize, jsonFormatting, settings);         
        }

        #endregion

        #region Module Data Processing

        protected StructureGroup GetSystemStructureGroup(string subStructureGroupTitle = null)
        {
            string webDavUrl = $"{Publication.RootStructureGroup.WebDavUrl}/_System";
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

        protected Dictionary<string, Component> GetActiveModules()
        {
            Schema moduleConfigSchema = GetSchema(ModuleConfigurationSchemaRootElementName);
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


        protected Schema GetSchema(string rootElementName, string namespaceUri = DxaSchemaNamespaceUri)
        {
            Schema[] schemas = Publication.GetSchemasByNamespaceUri(namespaceUri, rootElementName).ToArray();

            if (schemas.Length == 0)
            {
                throw new DxaException(
                    $"Schema with namespace '{namespaceUri}' and root element name '{rootElementName}' not found.");
            }
            if (schemas.Length > 1)
            {
                throw new DxaException(
                    $"Found multiple Schemas with namespace '{namespaceUri}' and root element name '{rootElementName}'.");
            }

            return schemas.First();
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
