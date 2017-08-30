using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Common;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class TemplateBaseTest : TestClass
    {
        private class TestTemplate : TemplateBase
        {
            internal TestTemplate(Publication publication)
            {
                Session = publication.Session;
                Publication = publication;
            }

            public override void Transform(Engine engine, Package package)
                => Console.WriteLine("DummyTemplate.Transform was called.");

            // Wrapper method to expose protected methods to test:
            internal string TestGetLocale() => GetLocale();
            internal string TestStripTcdlComponentPresentationTag(string input) => StripTcdlComponentPresentationTag(input);
        }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNativeRegionUniqueName_Success()
        {
            const string regionShemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_N";
            //init engine
            Component inputItem = (Component)TestSession.GetObject("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml");
            RenderedItem testRenderedItem = CreateTestRenderedItem(inputItem, null);
            var testEngine = new TestEngine(testRenderedItem);

            //init package
            var testPackage = new Package(testEngine);
            Type inputItemType = inputItem.GetType();
            string inputItemName = inputItemType.Name;
            ContentType inputItemContentType = new ContentType($"tridion/{inputItemType.Name.ToLower()}");
            testPackage.PushItem(inputItemName, testPackage.CreateTridionItem(inputItemContentType, inputItem));

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;

            Schema nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = nestedRegionSchemaTitle,
                Description = nestedRegionSchemaTitle
            };
            nestedRegionSchema.Save(true);

            Schema regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = regionShemaTitle,
                Description = regionShemaTitle,
                RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
            };
            regionSchema.Save(true);
            
            var publishMappings = new PublishMappings();

            // Region addition done as a part of Transform. No exception thown => Success
            publishMappings.Transform(testEngine, testPackage);

            //Cleanup
            regionSchema.Delete();
            nestedRegionSchema.Delete();
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNoNativeRegions_Success()
        {
            const string regionShemaTitle = "RegionSchema";
            
            //init engine
            Component inputItem = (Component)TestSession.GetObject("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml");
            RenderedItem testRenderedItem = CreateTestRenderedItem(inputItem, null);
            var testEngine = new TestEngine(testRenderedItem);
            
            //init package
            var testPackage = new Package(testEngine);
            Type inputItemType = inputItem.GetType();
            string inputItemName = inputItemType.Name;
            ContentType inputItemContentType = new ContentType($"tridion/{inputItemType.Name.ToLower()}");
            testPackage.PushItem(inputItemName, testPackage.CreateTridionItem(inputItemContentType, inputItem));

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;
            
            Schema regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = regionShemaTitle,
                Description = regionShemaTitle
            };
            regionSchema.Save(true);

            var publishMappings = new PublishMappings();

            // Region addition done as a part of Transform. No exception thown => Success
            publishMappings.Transform(testEngine, testPackage);

            //Cleanup
            regionSchema.Delete();
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNativeRegionNotUniqueName_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            
            //init engine
            Component inputItem = (Component)TestSession.GetObject("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml");
            RenderedItem testRenderedItem = CreateTestRenderedItem(inputItem, null);
            var testEngine = new TestEngine(testRenderedItem);

            //init package
            var testPackage = new Package(testEngine);
            Type inputItemType = inputItem.GetType();
            string inputItemName = inputItemType.Name;
            ContentType inputItemContentType = new ContentType($"tridion/{inputItemType.Name.ToLower()}");
            testPackage.PushItem(inputItemName, testPackage.CreateTridionItem(inputItemContentType, inputItem));

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;

            Schema nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = nestedRegionSchemaTitle,
                Description = nestedRegionSchemaTitle
            };
            nestedRegionSchema.Save(true);

            Schema regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = regionShemaTitle,
                Description = regionShemaTitle,
                RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
            };
            regionSchema.Save(true);

            var publishMappings = new PublishMappings();

            // Region addition done as a part of Transform. No exception thown => Success
            publishMappings.Transform(testEngine, testPackage);

            //Cleanup
            regionSchema.Delete();
            nestedRegionSchema.Delete();
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNestedRegionSchemas_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            const string superNestedRegionSchemaTitle = "superNestedRegionSchemaTitle";
            
            //init engine
            Component inputItem = (Component)TestSession.GetObject("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml");
            RenderedItem testRenderedItem = CreateTestRenderedItem(inputItem, null);
            var testEngine = new TestEngine(testRenderedItem);

            //init package
            var testPackage = new Package(testEngine);
            Type inputItemType = inputItem.GetType();
            string inputItemName = inputItemType.Name;
            ContentType inputItemContentType = new ContentType($"tridion/{inputItemType.Name.ToLower()}");
            testPackage.PushItem(inputItemName, testPackage.CreateTridionItem(inputItemContentType, inputItem));

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;

            Schema superNestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = superNestedRegionSchemaTitle,
                Description = superNestedRegionSchemaTitle
            };
            superNestedRegionSchema.Save(true);

            Schema nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = nestedRegionSchemaTitle,
                Description = nestedRegionSchemaTitle,
                RegionDefinition = { NestedRegions = { { regionShemaTitle, superNestedRegionSchema } } }
            };
            nestedRegionSchema.Save(true);

            Schema regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = SchemaPurpose.Region,
                Title = regionShemaTitle,
                Description = regionShemaTitle,
                RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
            };
            regionSchema.Save(true);

            var publishMappings = new PublishMappings();
           
            // Region addition done as a part of Transform. No exception thown => Success
            publishMappings.Transform(testEngine, testPackage);
            
            //Cleanup
            regionSchema.Delete();
            nestedRegionSchema.Delete();
            superNestedRegionSchema.Delete();
        }

        [TestMethod]
        public void GetCulture_WithLocalizationConfigComponent_Success()
        {
            Publication testPublication = (Publication) TestSession.GetObject(TestFixture.AutoTestParentWebDavUrl);
            TestTemplate testTemplate = new TestTemplate(testPublication);

            string locale = testTemplate.TestGetLocale();

            Assert.AreEqual("en-US", locale);
        }

        [TestMethod]
        public void StripTcdlComponentPresentationTag_Success()
        {
            Publication testPublication = (Publication) TestSession.GetObject(TestFixture.AutoTestParentWebDavUrl);
            TestTemplate testTemplate = new TestTemplate(testPublication);

            string input1 = "<TCDL:ComponentPresentation a=\"1\">Test <tcdl>.</TCDL:ComponentPresentation>";
            string result1 = testTemplate.TestStripTcdlComponentPresentationTag(input1);
            Console.WriteLine(result1);

            string input2 = "<tcdl:ComponentPresentation a=\"1\">Test <tcdl>.</tcdl:ComponentPresentation>";
            string result2 = testTemplate.TestStripTcdlComponentPresentationTag(input2);
            Console.WriteLine(result2);

            Assert.AreEqual(input1, result1);
            Assert.AreEqual("Test <tcdl>.", result2);
        }

    }
}
