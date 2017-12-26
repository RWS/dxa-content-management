using Sdl.Web.DataModel;
using System.Collections.Generic;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    public class InheritMetadataEntityModelBuilder : DataModelBuilder, IEntityModelDataBuilder
    {
        public InheritMetadataEntityModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
            Logger.Debug("InheritMetadataEntityModelBuilder initialized.");
        }

        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
        {
        }

        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct,
            bool includeComponentTemplateDetails, int expandLinkDepth)
        {
            Logger.Debug("Adding folder metadata to entity model metadata.");

            Folder folder = (Folder)component.OrganizationalItem;
            List<string> schemaIdList = new List<string>();

            // Checking for Schema Metadata is very important because we need to stop adding metadata as soon as we found folder without it
            while (folder != null && folder.MetadataSchema != null)
            {
                if (folder.Metadata != null)
                {
                    schemaIdList.Insert(0, folder.MetadataSchema.Id);

                    ContentModelData metaData = BuildContentModel(folder.Metadata, expandLinkDepth);
                    string[] duplicateFieldNames;
                    ContentModelData emdMetadata = entityModelData.Metadata ?? new ContentModelData();
                    entityModelData.Metadata = MergeFields(emdMetadata, metaData, out duplicateFieldNames);
                }

                folder = (Folder)folder.OrganizationalItem;
            }

            CreateSchemaIdListExtensionData(entityModelData, schemaIdList);
        }
    }
}
