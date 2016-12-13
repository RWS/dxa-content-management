using System;
using System.Text;
using Sdl.Web.Tridion.Common;
using System.Collections.Generic;
using System.Linq;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Publish resource JSON files (one per module). A module configuration can link to 
    /// multiple resource components - these are merged into a single JSON file
    /// </summary>
    [TcmTemplateTitle("Publish Resources")]
    public class PublishResources : TemplateBase
    {
        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            // The input Component is used to relate some of the generated Binaries to (so they get unpublished if the Component is unpublished).
            Component inputComponent = GetComponent();
            StructureGroup sg = GetSystemStructureGroup("resources");

            //For each active module, publish the config and add the filename(s) to the bootstrap list
            List<Binary> binaries = GetActiveModules().Select(module => PublishModuleResources(module.Key, module.Value, sg)).Where(b => b != null).ToList();

            AddBootstrapJsonBinary(binaries, inputComponent, sg, "resource");

            OutputSummary("Publish Resources", binaries.Select(b => b.Url));
        }

        private Binary PublishModuleResources(string moduleName, Component moduleConfigComponent, StructureGroup structureGroup)
        {
            ItemFields moduleConfigComponentFields = new ItemFields(moduleConfigComponent.Content, moduleConfigComponent.Schema);

            Dictionary<string, string> resources = new Dictionary<string, string>();
            foreach (Component resourcesComponent in moduleConfigComponentFields.GetComponentValues("resource"))
            {
                resources = MergeData(resources, ExtractKeyValuePairs(resourcesComponent));
            }

            return resources.Count == 0 ? null : AddJsonBinary(resources, moduleConfigComponent, structureGroup, moduleName, variantId: "resources");
        }
    }
}
