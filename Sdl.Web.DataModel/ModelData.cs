using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Abstract base class for all Data Models.
    /// </summary>
    public abstract class ModelData
    {
        /// <summary>
        /// Gets or sets the Type Identifier.
        /// </summary>
        /// <remarks>
        /// The Type Identifier is included in the JSON to assist with deserializing strongly typed Models.
        /// </remarks>
        [JsonProperty("@type")]
        public string TypeId { get; set; }

        /// <summary>
        /// Initializes a new Model.
        /// </summary>
        protected ModelData()
        {
            TypeId = GetType().Name;
        }

        /// <summary>
        /// Creates a new strongly-typed Model object from a (loosely-typed) JObject.
        /// </summary>
        /// <remarks>
        /// This happens when the Model is part of a Content Model (see <see cref="ContentModelData"/>).
        /// Such Content Models are initially deserialized loosely typed and then transformed into strongly typed (see <see cref="ContentModelData.OnDeserialized"/>).
        /// </remarks>
        /// <param name="jObject">The JObject.</param>
        /// <returns>The strongly-typed Model object.</returns>
        internal static ModelData Create(JObject jObject)
        {
            string typeId = jObject.GetPropertyValueAsString("@type");
            if (typeId == null)
            {
                throw new ApplicationException($"No type indentifier found on complex type: {jObject}");
            }

            Type modelType = Type.GetType($"Sdl.Web.DataModel.{typeId}", throwOnError: true);
            ModelData model = (ModelData) Activator.CreateInstance(modelType);
            model.Initialize(jObject);

            return model;
        }

        /// <summary>
        /// Initializes the (strongly-typed) Model from a (loosely-typed) JObject.
        /// </summary>
        /// <remarks>
        /// This happens when the Model is part of a Content Model (see <see cref="ContentModelData"/>).
        /// Such Content Models are initially deserialized loosely typed and then transformed into strongly typed (see <see cref="ContentModelData.OnDeserialized"/>).
        /// </remarks>
        /// <param name="jObject">The JObject.</param>
        protected abstract void Initialize(JObject jObject);
    }
}
