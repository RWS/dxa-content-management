using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
        [Description("Ignore until DXA unit tests use at least 9.0 TCM version")]
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
                Assert.IsTrue(definedRegions.Count(x => x.Region == regionShemaTitle
                                                        && x.ComponentTypes.Any()
                                                        && x.ComponentTypes[0].Schema != "*"
                                                        && x.ComponentTypes[0].Template != "*"
                              ) == 1,
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
        [Description("Ignore until DXA unit tests use at least 9.0 TCM version")]
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
                SaveRegionSchemaWithRegionList(regionSchema, new object[] { regionShemaTitle, nestedRegionSchema, true });

                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
                var content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                Assert.IsTrue(definedRegions.Count(x => x.Region == regionSchema.Id.ItemId.ToString()
                                                        && x.ComponentTypes.Count == 1
                                                        && x.ComponentTypes[0].Schema == "*"
                                                        && x.ComponentTypes[0].Template == "*"
                              ) == 1,
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
        [Description("Ignore until DXA unit tests use at least 9.0 TCM version")]
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
        [Description("Ignore until DXA unit tests use at least 9.0 TCM version")]
        [TestMethod]
        public void NativeRegionDefaultOccurrenceConstraint_Success()
        {
            const string regionShemaTitle = "NativeRegionDefaultOccurrenceConstraint_Success";
            Schema regionSchema = null;
            try
            {
                int expectedMinOccurs = 0;
                int expectedMaxOccurs = -1;
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
                var content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                string regionId = regionSchema.Id.ItemId.ToString();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(expectedMinOccurs, region.OccurrenceConstraint.MinOccurs, $"MinOccurs should be {expectedMinOccurs}.");
                Assert.AreEqual(expectedMaxOccurs, region.OccurrenceConstraint.MaxOccurs, $"MaxOccurs should be {expectedMaxOccurs}.");
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
            }
        }

        [Ignore]
        [Description("Ignore until DXA unit tests use at least 9.0 TCM version")]
        [TestMethod]
        public void NativeRegionAddOccurrenceConstraint_Success()
        {
            const string regionShemaTitle = "NativeRegionAddOccurrenceConstraint_Success";
            Schema regionSchema = null;
            try
            {
                int expectedMinOccurs = 1;
                int expectedMaxOccurs = 7;
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
                dynamic regionDefinition = regionSchema.RegionDefinition;
                Type occurrenceConstraintType = GetType(
                    "Tridion.ContentManager.CommunicationManagement.Regions.OccurrenceConstraint");
                dynamic occurrenceConstraint = Activator.CreateInstance(occurrenceConstraintType, TestSession);
                occurrenceConstraint.MinOccurs = expectedMinOccurs;
                occurrenceConstraint.MaxOccurs = expectedMaxOccurs;
                regionDefinition.ComponentPresentationConstraints.Add(occurrenceConstraint);

                regionSchema.Save(true);

                RenderedItem testRenderedItem;
                Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
                Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
                var content = testItem.GetAsString();
                var definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
                string regionId = regionSchema.Id.ItemId.ToString();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(expectedMinOccurs, region.OccurrenceConstraint.MinOccurs, $"MinOccurs should be {expectedMinOccurs}.");
                Assert.AreEqual(expectedMaxOccurs, region.OccurrenceConstraint.MaxOccurs, $"MaxOccurs should be {expectedMaxOccurs}.");
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
            }
        }

        private Type GetType(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return type;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return type;
            }
            return null;
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

            [JsonProperty("OccurrenceConstraint")]
            public OccurrenceConstraintTest OccurrenceConstraint { get; set; }
        }

        private class ComponentTypesTest
        {
            [JsonProperty("Schema")]
            public string Schema { get; set; }
            [JsonProperty("Template")]
            public string Template { get; set; }
        }

        private class OccurrenceConstraintTest
        {
            [JsonProperty("MinOccurs")]
            public int MinOccurs { get; set; }

            [JsonProperty("MaxOccurs")]
            public int MaxOccurs { get; set; }
        }
    }
}
