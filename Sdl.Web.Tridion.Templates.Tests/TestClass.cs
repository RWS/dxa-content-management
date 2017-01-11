using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
                );
            Console.WriteLine("---- JSON Representation of {0} ----", objectToSerialize.GetType().FullName);
            Console.WriteLine(json);
        }
        protected T JsonSerializeDeserialize<T>(T objectToSerialize)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            string json = JsonConvert.SerializeObject(objectToSerialize, jsonSerializerSettings);
            T result = JsonConvert.DeserializeObject<T>(json);

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
