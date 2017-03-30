using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates.Tests
{
    /// <summary>
    /// Abstract base class for unit/integration tests for Templates.
    /// </summary>
    public abstract class TemplateTest : TestClass
    {
        protected string RunTemplate(Type templateType, IdentifiableObject inputItem, Template template = null)
        {
            RenderedItem testRenderedItem = CreateTestRenderedItem(inputItem, template);
            TestEngine testEngine = new TestEngine(testRenderedItem);
            Package testPackage = new Package(testEngine);

            Type inputItemType = inputItem.GetType();
            string inputItemName = inputItemType.Name;
            ContentType inputItemContentType = new ContentType($"tridion/{inputItemType.Name.ToLower()}");
            testPackage.PushItem(inputItemName, testPackage.CreateTridionItem(inputItemContentType, inputItem));

            ITemplate testTemplate = Activator.CreateInstance(templateType) as ITemplate;
            Assert.IsNotNull(testTemplate, "testTemplate");

            testTemplate.Transform(testEngine, testPackage);

            Item outputItem = testPackage.GetByName(Package.OutputName);
            Assert.IsNotNull(outputItem, "outputItem");

            string result = outputItem.GetAsString();
            Assert.IsNotNull(result, "result");

            Console.WriteLine("Output Item:");
            Console.WriteLine(result);

            return result;
        }

        protected TResult RunTemplate<TResult>(Type templateType, IdentifiableObject inputItem, Template template = null, JsonSerializerSettings serializerSettings = null)
        {
            if (serializerSettings == null)
            {
                serializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            string outputJson = RunTemplate(templateType, inputItem, template);
            return JsonConvert.DeserializeObject<TResult>(outputJson, serializerSettings);
        }
    }
}
