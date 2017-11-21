using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Interface for DXA R2 Entity Data Model Builders
    /// </summary>
    /// <seealso cref="DataModelBuilderPipeline"/>
    public interface IEntityModelDataBuilder
    {
        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation on a Page.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation (obtained from a Page).</param>
        void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp);

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        /// <param name="includeComponentTemplateDetails">Include component template details.</param>
        /// <param name="expandLinkDepth">The level of Component/Keyword links to expand.</param>
        /// <remarks>
        /// This method is called for Component Presentations on a Page, standalone DCPs and linked Components which are expanded.
        /// The <paramref name="expandLinkDepth"/> parameter starts at <see cref="DataModelBuilderSettings.ExpandLinkDepth"/>, 
        /// but is decremented for expanded Component links (recursively).
        /// </remarks>
        void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct, bool includeComponentTemplateDetails, int expandLinkDepth);
    }
}
