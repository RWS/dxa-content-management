using System;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    public class InheritMetadataPageModelBuilder : DataModelBuilder, IPageModelDataBuilder
    {
        public InheritMetadataPageModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
            Logger.Debug("InheritMetadataPageModelBuilder initialized.");
        }

        public void BuildPageModel(ref PageModelData pageModelData, Page page)
        {
            Logger.Debug("Adding structure group metadata to page model metadata.");
            StructureGroup structureGroup = (StructureGroup)page.OrganizationalItem;
            while (structureGroup != null)
            {
                if (structureGroup.MetadataSchema != null)
                {
                    ContentModelData metaData = BuildContentModel(structureGroup.Metadata, Pipeline.Settings.ExpandLinkDepth);
                    string[] duplicateFieldNames;
                    pageModelData.Metadata = MergeFields(pageModelData.Metadata, metaData, out duplicateFieldNames);
                }
                structureGroup = structureGroup.OrganizationalItem as StructureGroup;
            }
        }
    }
}
