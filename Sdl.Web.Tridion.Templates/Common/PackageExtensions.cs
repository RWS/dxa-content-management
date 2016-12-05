using System;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Common
{
    public static class PackageExtensions
    {
        public static bool TryGetParameter<T>(this Package package, string name, out T value, TemplatingLogger logger = null)
        {
            string paramValue = package.GetValue(name);

            if (logger != null)
            {
                logger.Debug(string.Format("{0}: '{1}'", name, paramValue));
            }

            if (string.IsNullOrEmpty(paramValue))
            {
                value = default(T);
                return false;
            }

            value = (T) Convert.ChangeType(paramValue, typeof (T));
            return true;
        }
    }
}
