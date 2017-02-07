using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Sdl.Web.DataModel
{
    public class DataModelBinder : SerializationBinder
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Binder = new DataModelBinder(),
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        #region SerializationBinder Overrides
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;

            if (serializedType.IsGenericType)
            {
                Type genericType = serializedType.GetGenericTypeDefinition();
                if (typeof (List<>).IsAssignableFrom(genericType))
                {
                    typeName = serializedType.GenericTypeArguments[0].Name + "[]";
                    return;
                }
            }

            typeName = serializedType.Name;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType($"Sdl.Web.DataModel.{typeName}") ?? Type.GetType($"System.{typeName}", throwOnError: true);
        }
        #endregion
    }
}
