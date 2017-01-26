using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.DataModel
{
    public class MvcData
    {
        public string ControllerName { get; set; }
        public string ControllerAreaName { get; set; }
        public string ActionName { get; set; }
        public string ViewName { get; set; }
        public string AreaName { get; set; }
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
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{AreaName}:{ControllerName}:{ViewName}";
        } 

        #endregion

        private static int SafeHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }

    }
}
