using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Tridion.Configuration;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
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
                if (tcmSectionElem == null) return;
                var tcmConfigFilePath = tcmSectionElem.FilePath;
                var map = new ExeConfigurationFileMap {ExeConfigFilename = tcmConfigFilePath};
                var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                var resolverSettings =
                    ((AppSettingsSection) config.GetSection("Sdl.Web.DXAResolver")).Settings;
                _recurseDepth = Convert.ToInt32(resolverSettings["recurseDepth"].Value);
            }
            catch
            {
                // failed to load from config
            }
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
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (recurseLevel > _recurseDepth || resolved.Contains(component)) return toResolve;
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
            return toResolve;            
        }

        private List<ResolvedItem> Resolve(Page page, ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
            List<ResolvedItem> toResolve = new List<ResolvedItem>();
            if (resolved.Contains(page) || page.ComponentPresentations.Count <= 0) return toResolve;
            foreach (var cp in page.ComponentPresentations)
            {
                toResolve.AddRange(Resolve((IdentifiableObject)cp.Component, template, resolved, recurseLevel));
                resolved.Add(page);
            }
            return toResolve;
        }

        private List<ResolvedItem> Resolve(IdentifiableObject item,
            ComponentTemplate template, HashSet<IdentifiableObject> resolved, int recurseLevel)
        {
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
            var resolved = new HashSet<IdentifiableObject>();
            resolvedItems.Clear(); // remove items from default resolving process
            foreach (var x in Resolve(item, dataPresentationTemplate, resolved, 0))
            {
                resolvedItems.Add(x);
            }
        }
    }
}
