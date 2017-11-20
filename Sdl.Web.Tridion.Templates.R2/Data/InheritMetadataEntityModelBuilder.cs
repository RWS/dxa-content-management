using Sdl.Web.DataModel;
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

            while (folder.OrganizationalItem != null)
            {
                if (folder.MetadataSchema != null)
                {
                    ContentModelData metaData = BuildContentModel(folder.Metadata, expandLinkDepth);
                    string[] duplicateFieldNames;
                    entityModelData.Metadata = MergeFields(entityModelData.Metadata, metaData, out duplicateFieldNames);
                }
                folder = (Folder)folder.OrganizationalItem;
            }
        }
    }
}
