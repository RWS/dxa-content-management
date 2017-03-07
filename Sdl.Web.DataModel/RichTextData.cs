using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the data for a Rich Text field
    /// </summary>
    /// <remarks>
    /// Rich Text can contain embedded Media Items (Images, Videos), in which case the data becomes a mix of HTML fragments and Entity Models.
    /// </remarks>
    public class RichTextData
    {
        /// <summary>
        /// Gets or sets the rich text fragments.
        /// </summary>
        /// <value>
        /// Each fragment can be either an HTML fragment (String) or an embedded Media Item (<see cref="EntityModelData"/>).
        /// </value>
        public List<object> Fragments { get; set; }
    }
}
