using System.Collections.Generic;

namespace Sdl.Web.Tridion.Templates.R2.Data
{
    /// <summary>
    /// Represents the settings for <see cref="DataModelBuilder"/>
    /// </summary>
    public class DataModelBuilderSettings
    {
        /// <summary>
        /// Gets or sets the depth that Component/Keyword links should be expanded (on CM-side)
        /// </summary>
        public int ExpandLinkDepth { get; set; }

        /// <summary>
        /// Gets or sets whether XPM metadata should be generated or not.
        /// </summary>
        public bool GenerateXpmMetadata { get; set; }


        /// <summary>
        /// Gets or sets the Locale which is output as <c>og:locale</c> PageModel Meta.
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the a list of schema names used to determine embed behaviour in Rich text fields
        /// </summary>
        public List<string> SchemasForRichTextEmbed { get; set; }
    }
}
