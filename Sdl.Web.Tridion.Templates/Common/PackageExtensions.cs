using System;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Common
{
    public static class PackageExtensions
    {
        public static bool TryGetParameter<T>(this Package package, string name, out T value, TemplatingLogger logger = null)
        {
            string paramValue = package.GetValue(name);

            logger?.Debug($"{name}: '{paramValue}'");

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
