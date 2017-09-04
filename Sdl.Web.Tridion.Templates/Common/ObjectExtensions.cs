using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sdl.Web.Tridion.Common
{
    public static class ObjectExtensions
    {
        public static IEnumerable<TOutput> IfNotNull<TInput, TOutput>(this TInput value, Func<TInput, IEnumerable<TOutput>> getResult)
        {
            // TODO possible compare of value type with null (http://confluence.jetbrains.com/display/ReSharper/Possible+compare+of+value+type+with+null)
            return null != value ? getResult(value) : Enumerable.Empty<TOutput>();
        }

        public static TOutput IfNotNull<TInput, TOutput>(this TInput value, Func<TInput, TOutput> getResult)
        {
            // TODO possible compare of value type with null
            return null != value ? getResult(value) : default(TOutput);
        }

        public static void IfNotNull<TInput>(this TInput value, Action<TInput> action)
        {
            // TODO possible compare of value type with null
            if (null != value)
            {
                action(value);
            }
        }

        public static object GetPropertyValue(this object obj, string name)
        {
            foreach (string part in name.Split('.'))
            {
                if (obj == null)
                {
                    return null;
                }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null)
                {
                    return null;
                }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }

        public static T GetPropertyValue<T>(this object obj, string name)
        {
            object retval = GetPropertyValue(obj, name);
            if (retval == null)
            {
                return default(T);
            }
            return (T)retval;
        }
    }
}
