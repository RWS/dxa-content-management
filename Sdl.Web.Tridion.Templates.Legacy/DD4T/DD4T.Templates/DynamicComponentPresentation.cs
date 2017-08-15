using DD4T.Templates.Base;
using DD4T.Templates.Base.Utils;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using Dynamic = DD4T.ContentModel;

namespace DD4T.Templates
{
    /// <summary>
    /// Generates a DD4T data model based for the current component presentation
    /// </summary>
    /// <remarks>
    /// This is a new feature in DD4T 2.0. If your web application is running on earlier versions 
    /// of DD4T, do NOT use this template, but use Generate dynamic component instead.
    /// </remarks>
    [TcmTemplateTitle("Generate dynamic component presentation")]
    [TcmTemplateParameterSchema("resource:DD4T.Templates.Resources.Schemas.Dynamic Delivery Parameters.xsd")]
    public partial class DynamicComponentPresentation : BaseComponentTemplate
    {

        public DynamicComponentPresentation() : base(TemplatingLogger.GetLogger(typeof(DynamicComponentPresentation))) 
        { 
               ComponentPresentationRenderStyle = ComponentPresentationRenderStyle.ComponentPresentation;
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
