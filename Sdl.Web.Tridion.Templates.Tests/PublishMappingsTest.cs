using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Tests
{
    [TestClass]
    public class PublishMappingsTest : TemplateTest
    {
        private static readonly string CoreCoponentWebDavUrl = "/webdav/100 Master/Building Blocks/Modules/Core/Admin/Core.xml";
        private static readonly string RegionJsonFile = "/Preview/system/mappings/regions.json";

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 8.7 TCM version")]
        [TestMethod]
        public void AddNativeRegionNotUniqueName_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionNotUniqueName_Success";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;
             
            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreCoponentWebDavUrl);
               
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
                };
                SaveRegionSchemaWithRegionList(regionSchema, new object[] { regionShemaTitle, nestedRegionSchema, true });

                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
                string content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                Assert.IsTrue(definedRegions.Count(x => x.Region == regionShemaTitle && x.ComponentTypes.Any()) == 1,
                    "DXA 'Main' Region and its ComponentTypes not overridden by TCM Region");

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
            const string regionShemaTitle = "AddNativeRegionUniqueName_Success1";
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueName_Success2";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;
            
            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreCoponentWebDavUrl);
               
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
                    Description = regionShemaTitle
                };
                SaveRegionSchemaWithRegionList(regionSchema, new object[] { regionShemaTitle, nestedRegionSchema, true});

                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
                var content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                Assert.IsTrue(definedRegions.Count(x => x.Region==regionShemaTitle && !x.ComponentTypes.Any()) == 1, 
                    "TCM Region without any Component Presentations added to Regions.json");

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
            const string regionShemaTitle = "AddNoNativeRegions_Success";
            Schema regionSchema = null;
            
            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreCoponentWebDavUrl);
               
                // Create TestData Regions
                Publication testPublication = (Publication)inputItem.ContextRepository;

                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle
                };
                regionSchema.Save(true);
                
                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
                string content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                Assert.IsFalse(definedRegions.Any(x => x.Region == regionShemaTitle),
                    "No TCM Region added to Regions.json");
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
        public void AddNotUniqueRegions_Success()
        {
            const string regionShemaTitle = "AddNotUniqueRegions_Success1"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNotUniqueRegions_Success2";
            const string superNestedRegionSchemaTitle = "AddNotUniqueRegions_Success3";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;
            Schema superNestedRegionSchema = null;
           
            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreCoponentWebDavUrl);
                
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
                    Description = nestedRegionSchemaTitle
                };
                SaveRegionSchemaWithRegionList(nestedRegionSchema, new object[] { regionShemaTitle, superNestedRegionSchema, true});
                
                regionSchema = new Schema(TestSession, testPublication.RootFolder.Id)
                {
                    Purpose = SchemaPurpose.Region,
                    Title = regionShemaTitle,
                    Description = regionShemaTitle
                };
                SaveRegionSchemaWithRegionList(regionSchema, new object[] { nestedRegionSchemaTitle, nestedRegionSchema, true });
                
                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem =  testPackage.GetByName(RegionJsonFile);
                string content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                Assert.IsTrue(definedRegions.Count(x => x.Region == regionShemaTitle) == 1,
                    "Despite Different Schemas has Regions with the same name only one of those added to Regions.json");

            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                nestedRegionSchema?.Delete();
                superNestedRegionSchema?.Delete();
            }
        }

        private void SaveRegionSchemaWithRegionList(Schema schema, params object[] args)
        {
            dynamic regionDefinition = schema.RegionDefinition;
            dynamic nestedRegions = regionDefinition.NestedRegions;
            dynamic nestedRegionDefinitionType = nestedRegions.GetType().GenericTypeArguments[0];
            dynamic nestedRegion = Activator.CreateInstance(nestedRegionDefinitionType, args);
            nestedRegions.Add(nestedRegion);
            schema.Save(true);
        }

        private class RegionDefinitionTest
        {
            [JsonProperty("Region")]
            public string Region { get; set; }
            [JsonProperty("ComponentTypes")]
            public List<ComponentTypesTest> ComponentTypes { get; set; }
        }

        private class ComponentTypesTest
        {
            [JsonProperty("Schema")]
            public string Schema { get; set; }
            [JsonProperty("Template")]
            public string Template { get; set; }
        }
    }
}
