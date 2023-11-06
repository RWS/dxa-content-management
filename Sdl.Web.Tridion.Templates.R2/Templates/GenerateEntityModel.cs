using System;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Templates.Common;
using Sdl.Web.Tridion.Templates.R2.Data;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.R2.Templates
{
    /// <summary>
    /// Generates a DXA R2 Data Model based on the current Component (Presentation)
    /// </summary>
    [TcmTemplateTitle("Generate DXA R2 Entity Model")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Templates.R2.Resources.GenerateEntityModelParameters.xsd")]
    public class GenerateEntityModel : TemplateR2Base
    {
        /// <summary>
        /// Performs the Transform.
        /// </summary>
        public override void Transform(Engine engine, Package package)
        {
            Logger.Debug("Transform");

            Initialize(engine, package);

            bool includeComponentTemplateData;
            if (!package.TryGetParameter("includeComponentTemplateData", out includeComponentTemplateData, Logger))
            {
                includeComponentTemplateData = true; // Default
            }

            int expandLinkDepth;
            package.TryGetParameter("expandLinkDepth", out expandLinkDepth, Logger);

            string[] modelBuilderTypeNames = GetModelBuilderTypeNames();

            RenderedItem renderedItem = Engine.PublishingContext.RenderedItem;
            Component component = GetComponent();
            ComponentTemplate ct = GetComponentTemplate();

            try
            {
                DataModelBuilderSettings settings = new DataModelBuilderSettings
                {
                    ExpandLinkDepth = expandLinkDepth,
                    GenerateXpmMetadata = IsXpmEnabled || IsPreview,
                    SchemasForRichTextEmbed = GetSchemasForRichTextEmbed(),
                    SchemasForAsIsMultimediaUrls = GetSchemasForAsIsMultimediaUrls()
                };

                DataModelBuilderPipeline modelBuilderPipeline = new DataModelBuilderPipeline(renderedItem, settings, modelBuilderTypeNames);
                EntityModelData entityModel = modelBuilderPipeline.CreateEntityModel(component, ct, includeComponentTemplateData);
                OutputJson = JsonSerialize(entityModel, IsPreview, DataModelBinder.SerializerSettings);

                if (string.IsNullOrEmpty(OutputJson))
                    throw new DxaException("Output Json is empty!");
            }
            catch (Exception ex)
            {
                throw new DxaException($"An error occurred while rendering {component.FormatIdentifier()} with {ct.FormatIdentifier()}", ex);
            }
        }
    }
}
