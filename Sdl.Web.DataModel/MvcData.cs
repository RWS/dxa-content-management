using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.DataModel
{
    /// <summary>
    /// Represents the metadata needed to render a View Model in an MVC Web Application.
    /// </summary>
    /// <seealso cref="ViewModelData.MvcData"/>
    public class MvcData
    {
        /// <summary>
        /// Gets or sets the name of the (custom) Controller to be used to process/render the View Model.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) if the default Controller is to be used.
        /// </value>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets or sets the area/module name of the (custom) Controller.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) if the default Controller and/or Area is used.
        /// </value>
        public string ControllerAreaName { get; set; }

        /// <summary>
        /// Gets or sets the (custom) Controller action name.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) if the default Controller and/or Action is used.
        /// </value>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the (logical) name of the View to be used to render the View Model.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) for embedded Entity Models (linked Components) and Keyword Models.
        /// </value>
        public string ViewName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Area/Module where the View resides.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) if the View is in the default Area/Module.
        /// </value>
        public string AreaName { get; set; }

        /// <summary>
        /// Gets or sets the parameters to be passed to the (custom) Controller.
        /// </summary>
        /// <value>
        /// Is <c>null</c> (i.e. not included in the serialized JSON) if no parameters have to be passed.
        /// </value>
        public Dictionary<string, string> Parameters { get; set; }

        #region Overrides
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            MvcData other = obj as MvcData;
            if (other == null)
            {
                return false;
            }

            if ((ControllerName != other.ControllerName) ||
                (ControllerAreaName != other.ControllerAreaName) ||
                (ActionName != other.ActionName) ||
                (ViewName != other.ViewName) ||
                (AreaName != other.AreaName))
            {
                return false;
            }

            if (Parameters == null)
            {
                return other.Parameters == null;
            }
            return Parameters.SequenceEqual(other.Parameters);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return SafeHashCode(ControllerName) ^
                   SafeHashCode(ControllerAreaName) ^
                   SafeHashCode(ActionName) ^
                   SafeHashCode(ViewName) ^
                   SafeHashCode(AreaName);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the AreaName, ControllerName and ViewName.
        /// </returns>
        public override string ToString()
            => $"{AreaName}:{ControllerName}:{ViewName}";

        #endregion

        private static int SafeHashCode(object obj)
            => obj?.GetHashCode() ?? 0;
    }
}
