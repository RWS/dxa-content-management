using System;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Templates.Common;
using Sdl.Web.Tridion.Templates.R2.Data;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.R2.Templates
{
    /// <summary>
    /// Generates a DXA R2 Data Model based on the current Page
    /// </summary>
    [TcmTemplateTitle("Generate DXA R2 Page Model")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Templates.R2.Resources.GeneratePageModelParameters.xsd")]
    public class GeneratePageModel : TemplateR2Base
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

            string[] modelBuilderTypeNames = GetModelBuilderTypeNames();

            RenderedItem renderedItem = Engine.PublishingContext.RenderedItem;
            Page page = GetPage();

            try
            {
                DataModelBuilderSettings settings = new DataModelBuilderSettings
                {
                    ExpandLinkDepth = expandLinkDepth,
                    GenerateXpmMetadata = IsXpmEnabled || IsPreview,
                    Locale = GetLocale(),
                    SchemasForRichTextEmbed = GetSchemasForRichTextEmbed(),
                    SchemasForAsIsMultimediaUrls = GetSchemasForAsIsMultimediaUrls()
                };

                DataModelBuilderPipeline modelBuilderPipeline = new DataModelBuilderPipeline(renderedItem, settings, modelBuilderTypeNames);
                PageModelData pageModel = modelBuilderPipeline.CreatePageModel(page);
                OutputJson = JsonSerialize(pageModel, IsPreview, DataModelBinder.SerializerSettings);
                if (string.IsNullOrEmpty(OutputJson))
                    throw new DxaException("Output Json is empty!");
            }
            catch (Exception ex)
            {
                throw new DxaException($"An error occurred while rendering {page.FormatIdentifier()}", ex);
            }
        }
    }
}
