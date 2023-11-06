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
        /// Gets or sets the a list of schema identifiers used to determine if an Entity should be embedded 
        /// in Rich text fields. The identifiers come in three flavors:
        ///     1. Specify a schema title
        ///     2. Specify a schema namespace URI
        ///         E.g: http://www.sdl.com/web/schemas/core
        ///     3. Specify both a scheme namespace URI and root element name (separated by :)
        ///         E.g: http://www.sdl.com/web/schemas/core:Article
        /// </summary>
        public List<string> SchemasForRichTextEmbed { get; set; }

        /// <summary>
        /// Gets or sets the a list of schema names used to determine if multimedia link should use the url as is from the binary filename (no tcm-id)
        /// </summary>
        public List<string> SchemasForAsIsMultimediaUrls { get; set; }
    }
}
