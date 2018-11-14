using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager;

namespace Sdl.Web.Tridion.Templates.Tests
{
    [TestClass]
    public class PublishMappingsTest : TemplateTest
    {
        private const string CoreComponentWebDavUrl = "/webdav/400 Example Site/Building Blocks/Modules/Core/Admin/Core.xml";
        private const string ContentPageTemplateWebDavUrl = "/webdav/400 Example Site/Building Blocks/Modules/Core/Editor/Templates/Content Page.tptcmp";

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);


        [TestMethod]
        public void Transform_PageIncludes_Success()
        {
            Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);
            RenderedItem testRenderedItem;
            Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);

            Item includesItem = testPackage.GetByName("/Preview/system/mappings/includes.json");
            Assert.IsNotNull(includesItem, "includesItem");
            string includesJson = includesItem.GetAsString();
            Console.WriteLine(includesJson);

            Dictionary<string, string[]> includes = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(includesJson);

            PageTemplate testPageTemplate = (PageTemplate)TestSession.GetObject(ContentPageTemplateWebDavUrl);
            ItemFields ptMetadataFields = new ItemFields(testPageTemplate.Metadata, testPageTemplate.MetadataSchema);
            TextField includesField = (TextField)ptMetadataFields["includes"];
            string testKey = testPageTemplate.Id.ItemId.ToString();
            string[] includePaths;
            Assert.IsTrue(includes.TryGetValue(testKey, out includePaths), $"includes.TryGetValue['{testKey}']");
            Assert.AreEqual(includesField.Values.Count, includePaths.Length, "includePaths.Length");
            int i = 0;
            foreach (string include in includesField.Values)
            {
                string expectedIncludePath = include;
                if (TcmUri.IsValid(include) || include.StartsWith("/webdav/"))
                {
                    Page includePage = (Page) TestSession.GetObject(include);
                    expectedIncludePath = includePage.PublishLocationUrl.Substring(1);
                }
                Assert.AreEqual(expectedIncludePath, includePaths[i], $"includePaths[{i}]");
                i++;
            }
        }

        [TestMethod]
        public void Transform_AddNativeRegionNotUniqueName_Success()
        {
            const string regionShemaTitle = "Main"; // Region with this name already exists in DXA Templates 
            const string nestedRegionSchemaTitle = "AddNativeRegionNotUniqueName_Success";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;

            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);

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

                var definedRegions = GetRenderedRegionDefinitions();
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
        
        [TestMethod]
        public void Transform_AddNativeRegionUniqueName_Success()
        {
            const string regionShemaTitle = "AddNativeRegionUniqueName_Success1";
            const string nestedRegionSchemaTitle = "AddNativeRegionUniqueName_Success2";
            Schema regionSchema = null;
            Schema nestedRegionSchema = null;

            try
            {
                //init engine
                Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);

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

                var definedRegions = GetRenderedRegionDefinitions();
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
        
        [TestMethod]
        public void Transform_NativeRegionDefaultOccurrenceConstraint_Success()
        {
            int expectedMinOccurs = 0;
            int expectedMaxOccurs = -1;
            Schema regionSchema = null;
            try
            {
                regionSchema = GetNewSchema(SchemaPurpose.Region);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
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
        
        [TestMethod]
        public void Transform_NativeRegionAddOccurrenceConstraint_Success()
        {
            int expectedMinOccurs = 1;
            int expectedMaxOccurs = 7;
            Schema regionSchema = null;
            try
            {
                regionSchema = GetNewSchema(SchemaPurpose.Region);
                AddOccurrenceConstraint(regionSchema.RegionDefinition, expectedMinOccurs, expectedMaxOccurs);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
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
        
        [TestMethod]
        public void Transform_NativeRegionDefaultTypeConstraint_Success()
        {
            Schema regionSchema = null;
            try
            {
                regionSchema = GetNewSchema(SchemaPurpose.Region);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(1, region.ComponentTypes.Count);
                Assert.AreEqual("*", region.ComponentTypes[0].Schema);
                Assert.AreEqual("*", region.ComponentTypes[0].Template);
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
            }
        }
        
        [TestMethod]
        public void Transform_NativeRegionAddSchemaConstraint_Success()
        {
            Schema regionSchema = null;
            Schema constraintSchema = null;
            try
            {
                constraintSchema = GetNewSchema(SchemaPurpose.Component);
                constraintSchema.Save(true);

                regionSchema = GetNewSchema(SchemaPurpose.Region);
                AddTypeConstraint(regionSchema.RegionDefinition, constraintSchema, null);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(1, region.ComponentTypes.Count);
                Assert.AreEqual(constraintSchema.Id, region.ComponentTypes[0].Schema);
                Assert.AreEqual("*", region.ComponentTypes[0].Template);
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                constraintSchema?.Delete();
            }
        }
        
        [TestMethod]
        public void Transform_NativeRegionAddComponentTemplateConstraint_Success()
        {
            Schema regionSchema = null;
            Schema constraintSchema = null;
            ComponentTemplate constraintComponentTemplate = null;
            try
            {
                constraintSchema = GetNewSchema(SchemaPurpose.Component);
                constraintSchema.Save(true);
                constraintComponentTemplate = GetNewComponentTemaplate(constraintSchema);
                constraintComponentTemplate.Save(true);

                regionSchema = GetNewSchema(SchemaPurpose.Region);
                AddTypeConstraint(regionSchema.RegionDefinition, null, constraintComponentTemplate);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(1, region.ComponentTypes.Count);
                Assert.AreEqual("*", region.ComponentTypes[0].Schema);
                Assert.AreEqual(constraintComponentTemplate.Id, region.ComponentTypes[0].Template);
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                constraintComponentTemplate?.Delete();
                constraintSchema?.Delete();
            }
        }
        
        [TestMethod]
        public void Transform_NativeRegionAddComponentTemplateAndSchemaConstraint_Success()
        {
            Schema regionSchema = null;
            Schema constraintSchema = null;
            ComponentTemplate constraintComponentTemplate = null;
            try
            {
                constraintSchema = GetNewSchema(SchemaPurpose.Component);
                constraintSchema.Save(true);
                constraintComponentTemplate = GetNewComponentTemaplate(constraintSchema);
                constraintComponentTemplate.Save(true);

                regionSchema = GetNewSchema(SchemaPurpose.Region);
                AddTypeConstraint(regionSchema.RegionDefinition, constraintSchema, constraintComponentTemplate);
                regionSchema.Save(true);
                string regionId = regionSchema.Id.ItemId.ToString();

                var definedRegions = GetRenderedRegionDefinitions();
                var region = definedRegions.FirstOrDefault(r => r.Region == regionId);

                Assert.IsNotNull(region, $"Region with Id {regionId} was not found");
                Assert.AreEqual(1, region.ComponentTypes.Count);
                Assert.AreEqual(constraintSchema.Id, region.ComponentTypes[0].Schema);
                Assert.AreEqual(constraintComponentTemplate.Id, region.ComponentTypes[0].Template);
            }
            finally
            {
                //Cleanup
                regionSchema?.Delete();
                constraintComponentTemplate?.Delete();
                constraintSchema?.Delete();
            }
        }

        private void AddTypeConstraint(dynamic regionDefinition, Schema schema, ComponentTemplate ct)
        {
            Type typeConstraintType = GetType(
                "Tridion.ContentManager.CommunicationManagement.Regions.TypeConstraint");
            dynamic typeConstraint = Activator.CreateInstance(typeConstraintType, TestSession);
            typeConstraint.BasedOnComponentTemplate = ct;
            typeConstraint.BasedOnSchema = schema;
            regionDefinition.ComponentPresentationConstraints.Add(typeConstraint);
        }

        private void AddOccurrenceConstraint(dynamic regionDefinition, int minOccurs, int maxOccurs)
        {
            Type occurrenceConstraintType = GetType(
                "Tridion.ContentManager.CommunicationManagement.Regions.OccurrenceConstraint");
            dynamic occurrenceConstraint = Activator.CreateInstance(occurrenceConstraintType, TestSession);
            occurrenceConstraint.MinOccurs = minOccurs;
            occurrenceConstraint.MaxOccurs = maxOccurs;
            regionDefinition.ComponentPresentationConstraints.Add(occurrenceConstraint);
        }

        private Schema GetNewSchema(SchemaPurpose purpose)
        {
            //init engine
            Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;
            var title = Guid.NewGuid().ToString("N");
            var schema = new Schema(TestSession, testPublication.RootFolder.Id)
            {
                Purpose = purpose,
                Title = title,
                Description = title
            };
            return schema;
        }

        private ComponentTemplate GetNewComponentTemaplate(Schema relatedSchema)
        {
            //init engine
            Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);

            // Create TestData Regions
            Publication testPublication = (Publication)inputItem.ContextRepository;
            var title = Guid.NewGuid().ToString("N");
            var componentTemplate = new ComponentTemplate(TestSession, testPublication.RootFolder.Id)
            {
                Title = title,
                Description = title,
                RelatedSchemas = { relatedSchema }
            };
            return componentTemplate;
        }

        private List<RegionDefinitionTest> GetRenderedRegionDefinitions()
        {
            Component inputItem = (Component)TestSession.GetObject(CoreComponentWebDavUrl);
            RenderedItem testRenderedItem;
            Package testPackage = RunTemplate(typeof(PublishMappings), inputItem, out testRenderedItem);
            Item testItem = testPackage.GetByName("/Preview/system/mappings/regions.json");
            var content = testItem.GetAsString();
            List<RegionDefinitionTest> definedRegions = JsonConvert.DeserializeObject<List<RegionDefinitionTest>>(content);
            return definedRegions;
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
