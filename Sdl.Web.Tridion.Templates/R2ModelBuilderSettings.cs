namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Represents the settings for <see cref="R2ModelBuilder"/>
    /// </summary>
    public class R2ModelBuilderSettings
    {
        /// <summary>
        /// Gets or sets the depth that Component/Keyword links should be expanded (on CM-side)
        /// </summary>
        public int ExpandLinkDepth { get; set; }

        /// <summary>
        /// Gets or sets whether XPM metadata should be generated or not.
        /// </summary>
        public bool GenerateXpmMetadata { get; set; }
    }
}
