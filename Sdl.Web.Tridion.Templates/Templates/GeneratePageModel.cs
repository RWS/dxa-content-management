using System;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Common;
using Sdl.Web.Tridion.Data;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Generates a DXA R2 Data Model based on the current Page
    /// </summary>
    [TcmTemplateTitle("Generate DXA R2 Page Model")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.GenerateDynamicPageParameters.xsd")]
    public class GeneratePageModel : TemplateBase
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
            Page page = GetPage();

            try
            {
                DataModelBuilderSettings settings = new DataModelBuilderSettings
                {
                    ExpandLinkDepth = expandLinkDepth,
                    GenerateXpmMetadata = IsXpmEnabled || IsPreview,
                    Locale = GetLocale()
                };

                DataModelBuilder modelBuilder = new DataModelBuilder(renderedItem, settings);
                PageModelData pageModel = modelBuilder.BuildPageModel(page);

                string pageModelJson = JsonSerialize(pageModel, DataModelBinder.SerializerSettings);
                Item outputItem = Package.CreateStringItem(ContentType.Text, pageModelJson);
                Package.PushItem(Package.OutputName, outputItem);
            }
            catch (Exception ex)
            {
                throw new DxaException($"An error occurred while rendering {page.FormatIdentifier()}", ex);
            }
        }
    }
}
