using System;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data for the Page Template.
    /// </summary>
    public class PageTemplateData
    {
        /// <summary>
        /// Gets or sets the identifier for the Page Template.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CM Uri namespace (Either 'tcm' or 'ish' but in future could be something else)
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the title for the Page Template.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the file extension for the Page.
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Gets or sets the revision date for the Page Template.
        /// </summary>
        public DateTime RevisionDate { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the Page Template.
        /// </summary>
        public ContentModelData Metadata { get; set; }
    }
}
