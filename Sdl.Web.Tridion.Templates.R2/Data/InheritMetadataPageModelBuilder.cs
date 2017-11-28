using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;

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
                if (structureGroup.MetadataSchema != null && structureGroup.Metadata != null)
                {
                    ContentModelData metaData = BuildContentModel(structureGroup.Metadata, Pipeline.Settings.ExpandLinkDepth);
                    string[] duplicateFieldNames;

                    ContentModelData pmdMetadata = pageModelData.Metadata ?? new ContentModelData();
                    pageModelData.Metadata = MergeFields(pmdMetadata, metaData, out duplicateFieldNames);
                }
                structureGroup = structureGroup.OrganizationalItem as StructureGroup;
            }
        }
    }
}
