using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel.Configuration;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Tests
{
    [TestClass]
    public class PublishMappingsTest : TemplateTest
    {

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNativeRegionNotUniqueName_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;
            try
            {
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

                nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = nestedRegionSchemaTitle,
                    Description = nestedRegionSchemaTitle
                };
                nestedRegionSchema.Save(true);

                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle,
                    RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
                };
                regionSchema.Save(true);

                SummaryData[] sitemapRoot = RunTemplate<SummaryData[]>(typeof(PublishMappings), inputItem);
                //TODO: Assertion on Regions.JSON
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                nestedRegionSchema?.Delete();
            }
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNativeRegionUniqueName_Success()
        {
            const string regionShemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_N";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;

            try
            {
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

                nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = nestedRegionSchemaTitle,
                    Description = nestedRegionSchemaTitle
                };
                nestedRegionSchema.Save(true);

                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle,
                    RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
                };
                regionSchema.Save(true);

                SummaryData[] sitemapRoot = RunTemplate<SummaryData[]>(typeof(PublishMappings), inputItem);
                //TODO: Assertion on Regions.JSON
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                nestedRegionSchema?.Delete();
            }
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNoNativeRegions_Success()
        {
            const string regionShemaTitle = "RegionSchema";
            Schema regionSchema = null;
            try
            {
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

                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle
                };
                regionSchema.Save(true);

                SummaryData[] sitemapRoot = RunTemplate<SummaryData[]>(typeof(PublishMappings), inputItem);
                //TODO: Assertion on Regions.JSON
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
            }
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNestedRegionSchemas_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueNameSuccess_R";
            const string superNestedRegionSchemaTitle = "superNestedRegionSchemaTitle";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;
            Schema superNestedRegionSchema = null;
            try
            {
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

                superNestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = superNestedRegionSchemaTitle,
                    Description = superNestedRegionSchemaTitle
                };
                superNestedRegionSchema.Save(true);

                nestedRegionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = nestedRegionSchemaTitle,
                    Description = nestedRegionSchemaTitle,
                    RegionDefinition = { NestedRegions = { { regionShemaTitle, superNestedRegionSchema } } }
                };
                nestedRegionSchema.Save(true);

                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle,
                    RegionDefinition = { NestedRegions = { { regionShemaTitle, nestedRegionSchema } } }
                };
                regionSchema.Save(true);

                SummaryData[] sitemapRoot = RunTemplate<SummaryData[]>(typeof(PublishMappings), inputItem);
                //TODO: Assertion on Regions.JSON
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                nestedRegionSchema?.Delete();
                superNestedRegionSchema?.Delete();
            }
        }
    }
}
