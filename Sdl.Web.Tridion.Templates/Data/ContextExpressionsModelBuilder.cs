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
        /// Builds an Entity Data Model from a given CM Component Presentation object.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="cp">The CM Component Presentation.</param>
        public void BuildEntityModel(ref EntityModelData entityModelData, ComponentPresentation cp)
        {
            // Add extension data for Context Expressions (if applicable)
            string[] includeContextExpressions = ContextExpressionUtils.GetContextExpressions(cp.Conditions.Where(c => !c.Negate).Select(c => c.TargetGroup));
            string[] excludeContextExpressions = ContextExpressionUtils.GetContextExpressions(cp.Conditions.Where(c => c.Negate).Select(c => c.TargetGroup));

            if (includeContextExpressions.Any())
            {
                Logger.Debug("Adding Context Expression Conditions (Include): " + string.Join(", ", includeContextExpressions));
                entityModelData.SetExtensionData("CX.Include", includeContextExpressions);
            }

            if (excludeContextExpressions.Any())
            {
                Logger.Debug("Adding Context Expression Conditions (Exclude): " + string.Join(", ", excludeContextExpressions));
                entityModelData.SetExtensionData("CX.Exclude", excludeContextExpressions);
            }
        }

        /// <summary>
        /// Builds an Entity Data Model from a given CM Component and Component Template.
        /// </summary>
        /// <param name="entityModelData">The Entity Data Model to build. Is <c>null</c> for the first Model Builder in the pipeline.</param>
        /// <param name="component">The CM Component.</param>
        /// <param name="ct">The CM Component Template. Can be <c>null</c>.</param>
        public void BuildEntityModel(ref EntityModelData entityModelData, Component component, ComponentTemplate ct)
        {
            // Nothing to do here
        }
    }
}
