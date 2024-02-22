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
                    SchemasForRichTextEmbed = GetSchemasForRichTextEmbed()
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

        /// <summary>
        /// Gets a list of schema identifiers to determine what Entities to embed in Rich Text fields.
        /// </summary>
        /// <returns>List of schema identifiers</returns>
        protected List<string> GetSchemasForRichTextEmbed()
        {
            return GetListFromStringParameter("schemasToEmbedInRichText");
        }
        /// <summary>
        /// Gets a list of schema identifiers to determine what binaries can be published with 'as is' Urls (without tcmUri being appended).
        /// </summary>
        /// <returns>List of schema identifiers</returns>

        protected List<string> GetSchemasForAsIsMultimediaUrls()
        {
            return GetListFromStringParameter("schemasForAsIsMultimediaUrls");
        }
        protected List<string> GetListFromStringParameter(string parameterName)
        {
            Logger.Info($"Checking '{parameterName}' template parameter");
            string parameterValue;
            List<string> parameterList = new List<string>();
            TryGetParameter(parameterName, out parameterValue);
            if (!string.IsNullOrEmpty(parameterValue))
            {
                Logger.Info($"parameter value set to '{parameterValue}'.");
                parameterList = parameterValue.Split(';').Select(s => s.Trim().ToLower()).ToList();
            }
            return parameterList;
        }
    }
}
