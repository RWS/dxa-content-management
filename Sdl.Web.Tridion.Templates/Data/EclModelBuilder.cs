using System.Xml;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Entity Model Builder implementation for ECL Stub Components.
    /// </summary>
    public class EclModelBuilder : DataModelBuilder, IEntityModelDataBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context <see cref="DataModelBuilderPipeline"/></param>
        public EclModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation object.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation.</param>
        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
        {
            // Nothing to do here
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct)
        {
            if (!IsEclItem(component))
            {
                return;
            }

            Logger.Debug($"Processing ECL Stub Component {component.FormatIdentifier()}");
            using (ExternalContentLibrary externalContentLibrary = new ExternalContentLibrary(Pipeline))
            {
                XmlElement externalMetadata = externalContentLibrary.BuildEntityModel(entityModelData, component);
                entityModelData.ExternalContent.Metadata = BuildContentModel(externalMetadata, expandLinkLevels:0);
            }
        }
    }
}
