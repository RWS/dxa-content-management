using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Abstract base class for the data of View Models.
    /// </summary>
    public abstract class ViewModelData
    {
        /// <summary>
        /// Gets or sets MVC data used to determine which View (and Controller) to use.
        /// </summary>
        public MvcData MvcData { get; set; }

        /// <summary>
        /// Gets or sets HTML CSS classes for use in View top level HTML element.
        /// </summary>
        public string HtmlClasses { get; set; }

        /// <summary>
        /// Gets or sets metadata used to render XPM markup
        /// </summary>
        public Dictionary<string, object> XpmMetadata { get; set; }

        /// <summary>
        /// Gets or sets extension data (additional properties which can be used by custom Model Builders, Controllers and/or Views)
        /// </summary>
        /// <value>
        /// The value is <c>null</c> (i.e. not included in the serialized JSON) if no extension data has been set.
        /// </value>
        public Dictionary<string, object> ExtensionData { get; set; }

        public ContentModelData Metadata { get; set; }

        public string SchemaId { get; set; }

        /// <summary>
        ///  Sets an extension data key/value pair.
        /// </summary>
        /// <remarks>
        /// This convenience method ensures the <see cref="ExtensionData"/> dictionary is initialized before setting the key/value pair.
        /// </remarks>
        /// <param name="key">The key for the extension data.</param>
        /// <param name="value">The value.</param>
        public void SetExtensionData(string key, object value)
        {
            if (ExtensionData == null)
            {
                ExtensionData = new Dictionary<string, object>();
            }
            ExtensionData[key] = value;
        }

        [JsonIgnore]
        public uint SerializationHashCode { get; set; }
    }
}
