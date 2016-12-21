using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    internal static class JsonExtensions
    {
        /// <summary>
        /// Gets the value of a JObject property as a string
        /// </summary>
        /// <param name="jObject">The subject JObject.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property or <c>null</c> if the property is not present.</returns>
        internal static string GetPropertyValueAsString(this JObject jObject, string propertyName)
        {
            return jObject.Property(propertyName)?.Value.Value<string>();
        }

        /// <summary>
        /// Gets the value of a JObject property as a JObject
        /// </summary>
        /// <param name="jObject">The subject JObject.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property or <c>null</c> if the property is not present or not a complex type.</returns>
        internal static JObject GetPropertyValueAsObject(this JObject jObject, string propertyName)
        {
            return jObject.Property(propertyName)?.Value as JObject;
        }

        /// <summary>
        /// Gets the value of a JObject property as a Model object
        /// </summary>
        /// <typeparam name="T">The type of the Model object.</typeparam>
        /// <param name="jObject">The subject JObject.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The value of the property or <c>null</c> if the property is not present or not a complex type.</returns>
        internal static T GetPropertyValueAsModel<T>(this JObject jObject, string propertyName)
            where T : ModelData
        {
            JObject propertyValue = jObject.GetPropertyValueAsObject(propertyName);
            return (propertyValue == null) ? null : (T) ModelData.Create(propertyValue);
        }

        /// <summary>
        /// Gets a strongly typed value for a given JToken.
        /// </summary>
        /// <param name="jToken">The subject JToken.</param>
        /// <returns></returns>
        internal static object GetStronglyTypedValue(this JToken jToken)
        {
            switch (jToken.Type)
            {
                case JTokenType.String:
                    return jToken.Value<string>();

                case JTokenType.Date:
                    return jToken.Value<DateTime>();

                case JTokenType.Boolean:
                    return jToken.Value<bool>();

                case JTokenType.Integer:
                    return jToken.Value<int>();

                case JTokenType.Float:
                    return jToken.Value<double>();

                case JTokenType.Object:
                    JObject jObject = (JObject) jToken;
                    if (jObject.Property("@type") == null)
                    {
                        // No type identifier => embedded fields
                        return new ContentModelData(jObject);
                    }
                    return ModelData.Create(jObject);

                case JTokenType.Array:
                    JArray jArray = (JArray) jToken;
                    object[] typedElements = jArray.Select(element => element.GetStronglyTypedValue()).ToArray();
                    if (typedElements.Length == 0)
                    {
                        // Array has no elements; can't determine the element type.
                        return jArray;
                    }
                    // Create a strongly typed Array based on the type of the first element
                    Array typedArray = Array.CreateInstance(typedElements[0].GetType(), jArray.Count);
                    int i = 0;
                    foreach (object typedElement in typedElements)
                    {
                        typedArray.SetValue(typedElement, i++);
                    }
                    return  typedArray;

                default:
                    throw new ApplicationException($"Unexpected JToken type '{jToken.Type}' in Content Model: {jToken}");
            }
        }
    }
}
