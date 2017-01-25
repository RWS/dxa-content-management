using System.IO;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Common;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Generates a DXA 2 data model based on the current Page
    /// </summary>
    [TcmTemplateTitle("Generate DXA 2 Entity Model")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.GenerateDynamicComponentParameters.xsd")]
    public class GenerateEntityModel : TemplateBase
    {
        /// <summary>
        /// Performs the Transform.
        /// </summary>
        public override void Transform(Engine engine, Package package)
        {
            Logger.Debug("Transform");

            Initialize(engine, package);

            int expandLinkDepth;
            package.TryGetParameter("expandLinkDepth", out expandLinkDepth, Logger);

            RenderedItem renderedItem = Engine.PublishingContext.RenderedItem;

            Dxa2ModelBuilderSettings settings = new Dxa2ModelBuilderSettings
            {
                ExpandLinkDepth = expandLinkDepth,
                IsXpmEnabled = Utility.IsXpmEnabled(Engine.PublishingContext)
            };

            Dxa2ModelBuilder modelBuilder = new Dxa2ModelBuilder(
                Session,
                settings,
                mmc => renderedItem.AddBinary(mmc).Url,
                (stream, fileName, relatedComponent, mimeType) => renderedItem.AddBinary(stream, fileName, string.Empty, relatedComponent, mimeType).Url
                );
            EntityModelData entityModel = modelBuilder.BuildEntityModel(GetComponent(), GetComponentTemplate());

            string entityModelJson = JsonSerialize(entityModel, DataModelBinder.SerializerSettings);
            Item outputItem = Package.CreateStringItem(ContentType.Text, entityModelJson);
            Package.PushItem(Package.OutputName, outputItem);
        }
    }
}
