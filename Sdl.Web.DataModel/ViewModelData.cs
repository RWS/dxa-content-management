﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    public abstract class ViewModelData : ModelData
    {
        /// <summary>
        /// Gets or sets MVC data used to determine which View to use.
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
        /// The value is <c>null</c> if no extension data has been set.
        /// </value>
        public Dictionary<string, object> ExtensionData { get; set; }

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

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            HtmlClasses = jObject.GetPropertyValueAsString("HtmlClasses");
            // TODO: MvcData, XpmMetadata and ExtensionData (not likely to have those on embedded models, though)
        }
        #endregion
    }
}
