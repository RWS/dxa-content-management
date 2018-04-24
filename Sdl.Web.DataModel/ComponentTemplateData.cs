using System;

namespace Sdl.Web.DataModel
{
    public class ComponentTemplateData
    {
        /// <summary>
        /// Gets or sets the identifier for the Template.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CM Uri namespace (Either 'tcm' or 'ish' but in future could be something else)
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the title for the Template
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the revision date for the Template.
        /// </summary>
        public DateTime RevisionDate { get; set; }

        /// <summary>
        /// Gets or sets the output format for the Template.
        /// </summary>
        public string OutputFormat { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the Page Template.
        /// </summary>
        public ContentModelData Metadata { get; set; }
    }
}
