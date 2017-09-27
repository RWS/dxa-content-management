using System.Linq;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Data
{
    /// <summary>
    /// Entity Model Builder implementation for Context Expressions.
    /// </summary>
    public class ContextExpressionsModelBuilder : DataModelBuilder, IEntityModelDataBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipeline">The context Model Builder Pipeline.</param>
        public ContextExpressionsModelBuilder(DataModelBuilderPipeline pipeline) : base(pipeline)
        {
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component Presentation on a Page.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation (obtained from a Page).</param>
        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
        {
            // Add extension data for Context Expressions (if applicable)
            ContentModelData contextExpressions = new ContentModelData();
            object includeContextExpressions =
                GetContextExpressions(
                    ContextExpressionUtils.GetContextExpressions(
                        cp.Conditions.Where(c => !c.Negate).Select(c => c.TargetGroup)));
            object excludeContextExpressions =
                GetContextExpressions(
                    ContextExpressionUtils.GetContextExpressions(
                        cp.Conditions.Where(c => c.Negate).Select(c => c.TargetGroup)));

            if (includeContextExpressions != null)
            {
                Logger.Debug("Adding Context Expression Conditions (Include): " +
                             string.Join(", ", includeContextExpressions));
                contextExpressions.Add("Include", includeContextExpressions);
            }

            if (excludeContextExpressions != null)
            {
                Logger.Debug("Adding Context Expression Conditions (Exclude): " +
                             string.Join(", ", excludeContextExpressions));
                contextExpressions.Add("Exclude", excludeContextExpressions);
            }

            if (contextExpressions.Count > 0)
            {
                entityModelData.SetExtensionData("ContextExpressions", contextExpressions);
            }
        }

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
        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct, bool includeComponentTemplateDetails, int expandLinkDepth)
        {
            // Nothing to do here
        }

        private static object GetContextExpressions(string[] expressions)
        {
            if (expressions == null) return expressions;
            if (expressions.Length == 0)
            {
                return null;
            }
            if (expressions.Length == 1)
            {
                return expressions[0];
            }
            return expressions;
        }
    }
}
