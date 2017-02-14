using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Interface for DXA R2 Entity Data Model Builders
    /// </summary>
    /// <seealso cref="DataModelBuilderPipeline"/>
    public interface IEntityModelDataBuilder
    {
        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation object.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation.</param>
        void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp);

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct);
    }
}
