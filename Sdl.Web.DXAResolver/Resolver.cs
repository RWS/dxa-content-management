using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Resolving;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.DXAResolver
{  
    /// <summary>
    /// DXA Custom Resolver to publish components added to a page with the 'Generate Data Presentation' template.
    /// 
    /// Usage:
    ///   1) Add this assembly to the GAC on the machine hosting the CME
    ///   2) Modify the \config\Tridion.ContentManager.config file and add the following to the resolving/mappings section:
    ///         <resolving>
    ///             <mappings>
    ///                 ...
    ///                 <add itemType="Tridion.ContentManager.CommunicationManagement.Page">
    ///                     <resolvers>
    ///                         ...
    ///                         <add type="Sdl.Web.DXAResolver.Resolver" assembly="Sdl.Web.DXAResolver, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b9a30ebcbde732e4" />
    ///                     </resolvers>
    ///                 </add>
    ///             </mappings>
    ///         </resolving>
    /// </summary>
    public class Resolver : IResolver
    {
        private int _maxRecurseDepth = -1; // default = infinite recursion depth
        private readonly TemplatingLogger _log;
       
        public Resolver()
        {
            // init logging
            _log = TemplatingLogger.GetLogger(GetType());
            _log.Debug($"DXA Custom Resolver initialised with recurse depth {_maxRecurseDepth}");            
        }

        private List<ResolvedItem> Resolve(ItemField itemField, ComponentTemplate template,
            HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (itemField == null) return toResolve;
            if (itemField is ComponentLinkField)
            {
                ComponentLinkField linkField = (ComponentLinkField) itemField;
                if (linkField.Values != null)
                {
                    foreach (var linkedComponent in linkField.Values)
                    {
                        toResolve.AddRange(Resolve(linkedComponent, template, resolved, recurseLevel + 1));
                    }
                }
            }

            if (itemField is EmbeddedSchemaField)
            {
                EmbeddedSchemaField linkField = (EmbeddedSchemaField)itemField;
                EmbeddedSchemaFieldDefinition linksFieldDefinition =
                               linkField.Definition as EmbeddedSchemaFieldDefinition;

                if (linksFieldDefinition?.EmbeddedSchema != null)
                {
                    TcmUri id = linksFieldDefinition.EmbeddedSchema.Id; // force schema load
                    foreach (ItemFields embeddedFields in linkField.Values)
                    {
                        toResolve.AddRange(Resolve(embeddedFields, template, resolved, recurseLevel));
                    }
                }
            }

            return toResolve;
        }

        private List<ResolvedItem> Resolve(ItemFields itemFields, ComponentTemplate template,
            HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            foreach (var field in itemFields)
            {
                toResolve.AddRange(Resolve(field, template, resolved, recurseLevel));
            }
            return toResolve;
        }

        private List<ResolvedItem> Resolve(Component component, ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            _log.Debug($"Attempting to resolve component: title='{component.Title}' id='{component.Id}' recurseLevel={recurseLevel} maxRecurseDepth={_maxRecurseDepth}");
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (_maxRecurseDepth >= 0 && recurseLevel > _maxRecurseDepth)
            {
                _log.Debug($"Reached max recusion level when trying to resolve component {component.Id}, skipping.");
                return toResolve;
            }
            if (resolved.Contains(component))
            {
                _log.Debug($"Already resolved component {component.Id}, skipping.");
                return toResolve;
            }
            Stopwatch t = new Stopwatch();
            t.Start();
            TcmUri schemaId = component.Schema.Id; // force load of schema!
            if (template.RelatedSchemas.Contains(component.Schema))
            {
                toResolve.Add(new ResolvedItem(component, template));
                resolved.Add(component);
            }          
            if (component.Content != null)
            {
                ItemFields fields = new ItemFields(component.Content, component.Schema);
                toResolve.AddRange(Resolve(fields, template, resolved, recurseLevel));
            }
            if (component.Metadata != null)
            {
                ItemFields fields = new ItemFields(component.Metadata, component.MetadataSchema);
                toResolve.AddRange(Resolve(fields, template, resolved, recurseLevel));
            }
            t.Stop();
            _log.Debug($"Resolving component {component.Id} took {t.ElapsedMilliseconds}ms to complete and resulted in {toResolve.Count} items being resolved.");
            return toResolve;            
        }

        private List<ResolvedItem> Resolve(Page page, ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            _log.Debug($"Attempting to resolve component presentation(s) on page: title='{page.Title}' id='{page.Id}' recurseLevel={recurseLevel} maxRecurseDepth={_maxRecurseDepth}");
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (resolved.Contains(page))
            {
                _log.Debug($"Already resolved page {page.Id}, skipping.");
                return toResolve;
            }
            if (page.ComponentPresentations.Count <= 0)
            {
                _log.Debug($"No component presentations on page {page.Id}, skipping.");
                return toResolve;
            }
            if (_maxRecurseDepth >= 0 && recurseLevel > _maxRecurseDepth)
            {
                _log.Debug($"Reached max recusion level when trying to resolve page {page.Id}, skipping.");
                return toResolve;
            }
            Stopwatch t = new Stopwatch();
            t.Start();
            foreach (var cp in page.ComponentPresentations)
            {
                toResolve.AddRange(Resolve(cp.Component, template, resolved, 
                    recurseLevel == 0 ? recurseLevel : recurseLevel + 1));
            }
            t.Stop();
            _log.Debug($"Resolving page {page.Id} took {t.ElapsedMilliseconds}ms to complete and resulted in {toResolve.Count} items being resolved.");
            return toResolve;
        }

        private List<ResolvedItem> Resolve(IdentifiableObject item,
            ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (item is Component)
                toResolve.AddRange(Resolve((Component) item, template, resolved, recurseLevel));
            if (item is Page)
                toResolve.AddRange(Resolve((Page) item, template, resolved, recurseLevel));
            return toResolve;
        }

        /// <summary>
        /// Entry point for resolver.
        /// </summary>
        /// <param name="item">Item to resolve</param>
        /// <param name="instruction">Resolve instruction</param>
        /// <param name="context">Publish context</param>
        /// <param name="resolvedItems">Final items to resolve</param>
        public void Resolve(IdentifiableObject item, ResolveInstruction instruction, PublishContext context, Tridion.Collections.ISet<ResolvedItem> resolvedItems)
        {
            if (instruction.Purpose != ResolvePurpose.Publish && instruction.Purpose != ResolvePurpose.RePublish || _maxRecurseDepth == 0)
                return;
            _log.Debug("DXA Custom Resolver started...");
            _log.Debug("Loading global app data:");
            const string appId = "dxa:CustomResolver";
            try
            {
                var appData = context.Session.SystemManager.LoadGlobalApplicationData(appId);
                if (appData != null)
                {
                    _log.Debug("Found configuration for custom resolver in global app data. Attempting to parse.");
                    string xml = Encoding.Unicode.GetString(appData.Data);
                    XElement parsedXml = XElement.Parse(xml);
                    foreach (XElement xe in parsedXml.Elements())
                    {
                        if (xe.Name.LocalName.Equals("RecurseDepth"))
                        {
                            _maxRecurseDepth = int.Parse(xe.Value);
                            break; // only one setting at moment
                        }
                    }
                }
                else
                {
                    _log.Debug($"Custom resolver configuration not found in global app data for '{appId}'. Using default settings.");
                    _maxRecurseDepth = -1;
                }
            }
            catch (Exception e)
            {
                _log.Debug($"Exception occured when reading global app data for application id '{appId}'. Using default settings.");
                _log.Debug(e.Message);
                _maxRecurseDepth = -1;
            }         
          
            var sourceItem = (RepositoryLocalObject)item;
            var contextPublication = (Publication)sourceItem.ContextRepository;
            var filter = new ComponentTemplatesFilter(item.Session)
            {
                AllowedOnPage = false,
                BaseColumns = ListBaseColumns.IdAndTitle
            };
            const string dataPresentationTemplateTitle = "Generate Data Presentation";
            var dataPresentationTemplate = contextPublication.GetComponentTemplates(filter).FirstOrDefault(
                ct => ct.Title == dataPresentationTemplateTitle);
            if (dataPresentationTemplate == null)
            {
                _log.Debug("No 'Generate Data Presentation' component template found. Skipping custom resolver.");
                return;
            }
            var resolved = new HashSet<IdentifiableObject>();
            Stopwatch t = new Stopwatch();
            t.Start();
            foreach (var x in Resolve(item, dataPresentationTemplate, resolved, 0))
            {
                resolvedItems.Add(x);
            }
            t.Stop();
            _log.Debug($"DXA Custom Resolver took {t.ElapsedMilliseconds}ms to complete.");
        }
    }
}
