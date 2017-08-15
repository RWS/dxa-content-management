using DD4T.Templates.Base;
using DD4T.Templates.Base.Utils;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;
using Dynamic = DD4T.ContentModel;

namespace DD4T.Templates
{
    /// <summary>
    /// Generates a DD4T data model based on the current component
    /// </summary>
    [TcmTemplateTitle("Generate dynamic component")]
    [TcmTemplateParameterSchema("resource:DD4T.Templates.Resources.Schemas.Dynamic Delivery Parameters.xsd")]
    public partial class DynamicComponent : BaseComponentTemplate
    {

        public DynamicComponent() : base(TemplatingLogger.GetLogger(typeof(DynamicComponent))) { }

        #region DynamicDeliveryTransformer Members
        protected override void TransformComponent(Dynamic.Component component)
        {
        }
        #endregion
    }
}
