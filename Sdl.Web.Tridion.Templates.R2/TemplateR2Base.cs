using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Tridion.Templates.Common;
using Sdl.Web.Tridion.Templates.R2.Data;

namespace Sdl.Web.Tridion.Templates.R2
{
    public abstract class TemplateR2Base : TemplateBase
    {
        /// <summary>
        /// Return default model builder settings.
        /// </summary>
        protected DataModelBuilderSettings DefaultDataModelBuilderSettings
        {
            get
            {
                int expandLinkDepth;
                TryGetParameter("expandLinkDepth", out expandLinkDepth);
                return new DataModelBuilderSettings
                {
                    ExpandLinkDepth = expandLinkDepth,
                    GenerateXpmMetadata = IsXpmEnabled || IsPreview,
                    Locale = GetLocale(),
                    SchemaNamespacesForRichTextEmbed = GetSchemaNamespacesForRichTextEmbed()
                };
            }
        }

        /// <summary>
        /// Create data model pipeline with default settings.
        /// </summary>
        /// <returns>Data Model Pipeline</returns>
        protected DataModelBuilderPipeline CreatePipeline()
            => CreatePipeline(DefaultDataModelBuilderSettings);

        /// <summary>
        /// Create data model pipeline with specified settings
        /// </summary>
        /// <param name="settings">Settings to use when creating data model pipeline</param>
        /// <returns>Data Model Pipeline</returns>
        protected DataModelBuilderPipeline CreatePipeline(DataModelBuilderSettings settings)
            => new DataModelBuilderPipeline(RenderedItem, settings, GetModelBuilderTypeNames());

        /// <summary>
        /// Gets the configured Model Builder Type Names
        /// </summary>
        /// <returns>The configured Model Builder Type Names</returns>
        protected string[] GetModelBuilderTypeNames()
        {
            string modelBuilderTypeNamesParam;
            Package.TryGetParameter("modelBuilderTypeNames", out modelBuilderTypeNamesParam);
            if (string.IsNullOrEmpty(modelBuilderTypeNamesParam))
            {
                Logger.Warning("No Model Builder Type Names configured; using Default Model Builder only.");
                modelBuilderTypeNamesParam = typeof(DefaultModelBuilder).Name;
            }

            return modelBuilderTypeNamesParam.Split(';');
        }

        protected List<string> GetSchemaNamespacesForRichTextEmbed()
        {
            Logger.Info("Checking 'schemasToEmbedInRichText' template parameter for list of schema namespaces to determine what entities to embed in Rich Text fields.");
            string schemasForEmbed;
            List<string> schemasForEmbedList = new List<string>();
            TryGetParameter("schemasToEmbedInRichText", out schemasForEmbed);
            if (!string.IsNullOrEmpty(schemasForEmbed))
            {
                Logger.Info($"schemasToEmbedInRichText set to '{schemasForEmbed}'.");
                schemasForEmbedList = schemasForEmbed.Split(';').Select(s => s.Trim().ToLower()).ToList();
            }
            return schemasForEmbedList;
        }
    }
}
