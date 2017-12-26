using Sdl.Web.DataModel;
using System.Collections.Generic;
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
            List<string> schemaIdList = new List<string>();

            // Checking for Schema Metadata is very important because we need to stop adding metadata as soon as we found page without it
            while (structureGroup != null && structureGroup.MetadataSchema != null)
            {
                schemaIdList.Insert(0, structureGroup.MetadataSchema.Id);

                if (structureGroup.Metadata != null)
                {
                    ContentModelData metaData = BuildContentModel(structureGroup.Metadata, Pipeline.Settings.ExpandLinkDepth);
                    string[] duplicateFieldNames;

                    ContentModelData pmdMetadata = pageModelData.Metadata ?? new ContentModelData();
                    pageModelData.Metadata = MergeFields(pmdMetadata, metaData, out duplicateFieldNames);
                }

                structureGroup = structureGroup.OrganizationalItem as StructureGroup;
            }

            ViewModelData vm = pageModelData as ViewModelData;
            CreateSchemaIdListExtensionData(ref vm, schemaIdList);
        }
    }
}
