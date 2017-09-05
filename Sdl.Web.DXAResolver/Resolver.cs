using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Tridion.Configuration;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Resolving;

namespace Sdl.Web.DXAResolver
{
    /// <summary>
    /// DXA Custom Resolver to publish components added to a page with the 'Generate Data Presentation' template.
    /// 
    /// Usage:
    ///   1) Add this assembly to the GAC on the machine hosting the CME
    ///   2) Modify the \config\Tridion.ContentManager.config file and add the following to the resolving/mappings section:
    ///         <section name="Sdl.Web.DXAResolver" type="System.Configuration.AppSettingsSection" />
    ///         ...
    ///         <Sdl.Web.Tridion.CustomResolver>
    ///             <add key = "recurseDepth" value="4" />
    ///         </Sdl.Web.Tridion.CustomResolver>
    ///         ...
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
        private readonly int _recurseDepth = 2;

        public Resolver()
        {
            try
            {
                var tcmConfigSections =
                    (ConfigurationSections) ConfigurationManager.GetSection(ConfigurationSections.SectionName);
                var tcmSectionElem =
                    tcmConfigSections.Sections.Cast<SectionElement>()
                        .FirstOrDefault(
                            s =>
                                !string.IsNullOrEmpty(s.FilePath) &&
                                s.FilePath.EndsWith("tridion.contentmanager.config",
                                    StringComparison.InvariantCultureIgnoreCase));
                if (tcmSectionElem != null)
                {
                    var tcmConfigFilePath = tcmSectionElem.FilePath;
                    var map = new ExeConfigurationFileMap {ExeConfigFilename = tcmConfigFilePath};
                    var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                    var resolverSettings =
                        ((AppSettingsSection) config.GetSection("Sdl.Web.DXAResolver")).Settings;
                    _recurseDepth = Convert.ToInt32(resolverSettings["recurseDepth"].Value);
                    //Log($"recurseDepth={_recurseDepth}");
                }
            }
            catch
            {
                // failed to load from config
                //Log($"Failed to load from config. Using default recurseDepth={_recurseDepth}");
            }
        }
      
        private List<ResolvedItem> Resolve(Component component, ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {   
            // if we already resolved this component then skip       
            if(resolved.Contains(component)) return new List<ResolvedItem>();

            //Log($"Looking at component {component.Title} to see if we should resolve it...");
            //Log($"Component Schema = {component.Schema.Id}");

            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            TcmUri schemaId = component.Schema.Id; // force load of schema!
            if (template.RelatedSchemas.Contains(component.Schema))
            {
                //Log($"Resolving Component: '{component.Title}'.");
                toResolve.Add(new ResolvedItem(component, template));
                resolved.Add(component);
            }

            var filter = new UsedItemsFilter(component.Session)
            {
                // only interested in linked components
                ItemTypes = new ItemType[] {ItemType.Component},
                BaseColumns = ListBaseColumns.Extended
            };

            var items = component.GetUsedItems(filter);
            foreach (var item in items)
            {
                toResolve.AddRange(Resolve(item, template, resolved, recurseLevel));
            }
            return toResolve;            
        }

        private List<ResolvedItem> Resolve(Page page, ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            List<ResolvedItem> toResolve = new List<ResolvedItem>();

            // if we already resolved this page then skip
            if (resolved.Contains(page)) return toResolve;
            if (page.ComponentPresentations.Count <= 0) return toResolve;
            foreach (var cp in page.ComponentPresentations)
            {
                toResolve.AddRange(Resolve((IdentifiableObject)cp.Component, template, resolved, recurseLevel));
            }
            return toResolve;
        }

        private List<ResolvedItem> Resolve(IdentifiableObject item,
            ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            //Log($"Current recurse level={recurseLevel}");
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (recurseLevel > _recurseDepth) return toResolve;
            if (item is Component)
                toResolve.AddRange(Resolve((Component) item, template, resolved, recurseLevel+1));
            if (item is Page)
                toResolve.AddRange(Resolve((Page) item, template, resolved, recurseLevel+1));
            return toResolve;
        }

        public void Resolve(IdentifiableObject item, ResolveInstruction instruction, PublishContext context, Tridion.Collections.ISet<ResolvedItem> resolvedItems)
        {
            //Log("Attempting to resolve items..");
            if (instruction.Purpose != ResolvePurpose.Publish && instruction.Purpose != ResolvePurpose.RePublish)
                return;
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
            if (dataPresentationTemplate == null) return;
            var resolvedItemList = resolvedItems.ToList();
            var resolved = new HashSet<IdentifiableObject>();
            foreach (var x in Resolve(item, dataPresentationTemplate, resolved, 0))
            {
                resolvedItems.Add(x);
            }
            foreach (var x in resolvedItemList.SelectMany(resolvedItem => Resolve(resolvedItem.Item, dataPresentationTemplate, resolved, 0)))
            {
                resolvedItems.Add(x);
            }
            //Log("Finished resolve items..");
        }
    }
}
