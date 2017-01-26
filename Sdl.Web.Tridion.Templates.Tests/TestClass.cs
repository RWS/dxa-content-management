using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.Templates.Tests
{
    /// <summary>
    /// Abstract base class for all Test Classes.
    /// </summary>
    public abstract class TestClass
    {
        protected static void DefaultInitialize(TestContext testContext)
        {
            // TODO: Log.Info("==== {0} ====", testContext.FullyQualifiedTestClassName);
        }

        protected void OutputJson(object objectToSerialize)
        {
            string json = JsonConvert.SerializeObject(
                objectToSerialize,
                Newtonsoft.Json.Formatting.Indented,
                DataModelBinder.SerializerSettings
                );
            Console.WriteLine("---- JSON Representation of {0} ----", objectToSerialize.GetType().FullName);
            Console.WriteLine(json);
        }

        protected T JsonSerializeDeserialize<T>(T objectToSerialize)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Binder = new DataModelBinder()
            };
            string json = JsonConvert.SerializeObject(objectToSerialize, jsonSerializerSettings);

            Console.WriteLine($"---- Serialized JSON for {objectToSerialize} ----");
            Console.WriteLine(json);

            T result = JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);

            OutputJson(result);

            return result;
        }

        protected TException AssertThrowsException<TException>(Action action, string actionName = null)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Fail("Action {0} did not throw an exception. Expected exception {1}.", actionName, typeof(TException).Name);
                return null; // Should never get here
            }
            catch (TException ex)
            {
                Console.WriteLine("Expected exception was thrown by action {0}:", actionName);
                Console.WriteLine(ex.ToString());
                return ex;
            }
        }

        protected static void AssertEqualCollections<T>(IEnumerable<T> expected, IEnumerable<T> actual, string subjectName)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, subjectName);
            }
            else
            {
                Assert.IsNotNull(actual, subjectName);
                Assert.AreNotSame(expected, actual, subjectName);
                Assert.AreEqual(expected.Count(), actual.Count(), subjectName + ".Count()");
            }
        }

    }
}
