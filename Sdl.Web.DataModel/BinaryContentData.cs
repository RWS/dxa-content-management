namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the metadata for the binary content of a Multimedia Component
    /// </summary>
    /// <seealso cref="EntityModelData.BinaryContent"/>
    public class BinaryContentData
    {
        /// <summary>
        /// Gets or sets the URL path of the published Binary.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or set the file name of the binary content.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size of the binary content.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the MIME type.
        /// </summary>
        public string MimeType { get; set; }
    }
}
