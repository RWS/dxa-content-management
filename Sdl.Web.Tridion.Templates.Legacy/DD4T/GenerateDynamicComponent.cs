using DD4T.Templates.Base;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using Dynamic = DD4T.ContentModel;


namespace Sdl.Web.Tridion.Templates.Legacy.DD4T
{
    /// <summary>
    /// Generates a DD4T data model based on the current component
    /// </summary>
    [TcmTemplateTitle("Generate Dynamic Component (DXA)")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Templates.Legacy.Resources.GenerateDynamicComponentParameters.xsd")]
    public class GenerateDynamicComponent : BaseComponentTemplate
    {

        public GenerateDynamicComponent()
            : base(TemplatingLogger.GetLogger(typeof (GenerateDynamicComponent)))
        {
            ComponentPresentationRenderStyle = ComponentPresentationRenderStyle.ComponentPresentation;
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
            Log.Debug(string.Format("Started TransformComponent for component {0}", component.Id));
            // persist the ComponentPresentationRenderStyle in the package so that the next TBB in the chain is able to read it
            if (Package != null)
            {
                Item renderStyle = Package.CreateStringItem(ContentType.Text, ComponentPresentationRenderStyle.ToString());
                Package.PushItem("render-style", renderStyle);
            }
        }
        #endregion
    }
}
