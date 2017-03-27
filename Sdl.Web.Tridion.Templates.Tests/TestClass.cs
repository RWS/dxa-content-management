using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Sdl.Web.DataModel;
using Tridion.ContentManager;

namespace Sdl.Web.Tridion.Templates.Tests
{
    /// <summary>
    /// Abstract base class for all Test Classes.
    /// </summary>
    public abstract class TestClass
    {
        protected static Session TestSession { get; private set; }

        protected static void DefaultInitialize(TestContext testContext)
        {
            Console.WriteLine("==== {0} ====", testContext.FullyQualifiedTestClassName);
            try
            {
                TestSession = new Session();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to initialize TOM.NET Session:\n{0}", ex);
                throw new ApplicationException($"Unable to initialize TOM.NET Session: {ex.Message}");
            }
        }

        protected void OutputJson(object objectToSerialize, JsonSerializerSettings serializerSettings = null)
        {
            if (serializerSettings == null)
            {
                serializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            Console.WriteLine($"---- JSON Representation of {objectToSerialize} ----");
            Console.WriteLine(JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented, serializerSettings));
        }

        protected T JsonSerializeDeserialize<T>(T objectToSerialize)
        {
            string json = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented, DataModelBinder.SerializerSettings);

            Console.WriteLine($"---- Serialized JSON for {objectToSerialize} ----");
            Console.WriteLine(json);

            T result = JsonConvert.DeserializeObject<T>(json, DataModelBinder.SerializerSettings);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }

        protected TException AssertThrowsException<TException>(Action action, string actionName = null)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Fail($"Action {actionName} did not throw an exception. Expected exception {typeof(TException).Name}.");
                return null; // Should never get here
            }
            catch (TException ex)
            {
                Console.WriteLine($"Expected exception was thrown by action {actionName}:");
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
