using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents structured content (CM fields) modeled as key/value pairs.
    /// </summary>
    public class ContentModelData : Dictionary<string, object>
    {
        #region Constructors
        /// <summary>
        /// Initializes an empty Content Model.
        /// </summary>
        public ContentModelData()
        {
        }

        /// <summary>
        /// Initializes a Content Model based on a (loosely-typed) JObject.
        /// </summary>
        /// <param name="jObject">The JObject.</param>
        internal ContentModelData(JObject jObject)
        {
            foreach (JProperty jProperty in jObject.Properties())
            {
                Add(jProperty.Name, jProperty.Value.GetStronglyTypedValue());
            }
        }
        #endregion

        /// <summary>
        /// Post-process a deserialized Content Model
        /// </summary>
        /// <remarks>
        /// Deserialization results in loosely typed values (JArray or JObject).
        /// This post-processing transforms it to strongly typed values.
        /// </remarks>
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            // Extract strongly typed values from loosely typed properties
            IDictionary<string, object> typedValues = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in this)
            {
                JToken jToken = kvp.Value as JToken;
                if (jToken != null)
                {
                    typedValues.Add(kvp.Key, jToken.GetStronglyTypedValue());
                }
            }

            // Substitute loosely typed values with strongly typed ones.
            foreach (KeyValuePair<string, object> kvp in typedValues)
            {
                this[kvp.Key] = kvp.Value;
            }
        }
    }
}
