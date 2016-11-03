using DD4T.Templates.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using Dynamic = DD4T.ContentModel;


namespace Sdl.Web.Tridion.Templates.DD4T
{
    /// <summary>
    /// Generates a DD4T data model based on the current component
    /// </summary>
    [TcmTemplateTitle("Generate Dynamic Component (DXA)")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.GenerateDynamicComponentParameters.xsd")]
    public class GenerateDynamicComponent : BaseComponentTemplate
    {

        public GenerateDynamicComponent()
            : base(TemplatingLogger.GetLogger(typeof (GenerateDynamicComponent)))
        {
            
        }

        /// <summary>
        /// Performs the Transform. Overridden here so we can inject our <see cref="DxaBuildManager"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="package"></param>
        public override void Transform(Engine engine, Package package)
        {
            Log.Debug("Transform");
            Manager = new DxaBuildManager(package, engine);
            base.Transform(engine, package);
        }

        #region DynamicDeliveryTransformer Members
        protected override void TransformComponent(Dynamic.Component component)
        {
        }
        #endregion
    }
}
