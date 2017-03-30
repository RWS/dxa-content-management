using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class DataModelTest : TestClass
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);

        [TestMethod]
        public void PageModelData_SerializeDeserialize_Success()
        {
            PageModelData testPageModel = CreateTestPageModel("PageModelData_SerializeDeserialize_Success");

            PageModelData deserializedPageModel = JsonSerializeDeserialize(testPageModel);

            AssertEqualPageModels(testPageModel, deserializedPageModel, "deserializedPageModel");
        }

        [TestMethod]
        public void RegionModelData_SerializeDeserialize_Success()
        {
            RegionModelData testRegionModel = CreateTestRegionModel("RegionModelData_SerializeDeserialize_Success");

            RegionModelData deserializedRegionModel = JsonSerializeDeserialize(testRegionModel);

            AssertEqualRegionModels(testRegionModel, deserializedRegionModel, "deserializedRegionModel");
        }

        [TestMethod]
        public void EntityModelData_SerializeDeserialize_Success()
        {
            EntityModelData testEntityModel = CreateTestEntityModel("EntityModelData_SerializeDeserialize_Success");

            EntityModelData deserializedEntityModel = JsonSerializeDeserialize(testEntityModel);

            AssertEqualEntityModels(testEntityModel, deserializedEntityModel, "deserializedEntityModel");
        }

        [TestMethod]
        public void KeywordModelData_SerializeDeserialize_Success()
        {
            KeywordModelData testKeywordModel = CreateTestKeywordModel("KeywordModelData_SerializeDeserialize_Success");

            KeywordModelData deserializedKeywordModel = JsonSerializeDeserialize(testKeywordModel);

            AssertEqualKeywordModels(testKeywordModel, deserializedKeywordModel, "deserializedKeywordModel");
        }

        [TestMethod]
        public void ContentModelData_SerializeDeserialize_Success()
        {
            ContentModelData testContentModel = CreateTestContentModel(
                "ContentModelData_SerializeDeserialize_Success",
                includeEntityModels: true,
                includeKeywordModels: true
                );
            string testTextField = (string) testContentModel["textField"];
            string[] testMultiValueTextField = (string[]) testContentModel["multiValueTextField"];
            RichTextData testRichTextField = (RichTextData) testContentModel["richTextField"];
            RichTextData[] testMultiValueRichTextField = (RichTextData[]) testContentModel["multiValueRichTextField"];
            EntityModelData testCompLinkField = (EntityModelData) testContentModel["compLinkField"];
            EntityModelData[] testMultiValueCompLinkField = (EntityModelData[]) testContentModel["multiValueCompLinkField"];
            KeywordModelData testKeywordField = (KeywordModelData) testContentModel["keywordField"];
            KeywordModelData[] testMultiValueKeywordField = (KeywordModelData[]) testContentModel["multiValueKeywordField"];

            ContentModelData deserializedContentModel = JsonSerializeDeserialize(testContentModel);

            RichTextData deserializedRichTextField = deserializedContentModel["richTextField"] as RichTextData;
            RichTextData[] deserializedMultiValueRichTextField = deserializedContentModel["multiValueRichTextField"] as RichTextData[];
            EntityModelData deserializedCompLink = deserializedContentModel["compLinkField"] as EntityModelData;
            EntityModelData[] deserializedMultiValueCompLink = deserializedContentModel["multiValueCompLinkField"] as EntityModelData[];
            KeywordModelData deserializedKeyword = deserializedContentModel["keywordField"] as KeywordModelData;
            KeywordModelData[] deserializedMultiValueKeywordField = (KeywordModelData[]) deserializedContentModel["multiValueKeywordField"];

            Assert.IsNotNull(deserializedRichTextField, "deserializedRichTextField");
            Assert.IsNotNull(deserializedMultiValueRichTextField, "deserializedMultiValueRichTextField");
            Assert.IsNotNull(deserializedCompLink, "deserializedCompLink");
            Assert.IsNotNull(deserializedMultiValueCompLink, "deserializedMultiValueCompLink");
            Assert.IsNotNull(deserializedKeyword, "deserializedKeyword");
            Assert.IsNotNull(deserializedMultiValueKeywordField, "deserializedMultiValueKeywordField");

            Assert.AreEqual(testTextField, deserializedContentModel["textField"], "textField");
            AssertEqualCollections(testMultiValueTextField, deserializedContentModel["multiValueTextField"] as string[], "multiValueTextField");
            Assert.AreEqual(testRichTextField.Fragments.Count, deserializedRichTextField.Fragments.Count, "deserializedRichTextField.Fragments.Count");
            AssertEqualCollections(testMultiValueRichTextField, deserializedMultiValueRichTextField, "deserializedMultiValueRichTextField");
            Assert.AreEqual(testCompLinkField.Id, deserializedCompLink.Id, "deserializedCompLink.Id");
            AssertEqualCollections(testMultiValueCompLinkField, deserializedMultiValueCompLink, "deserializedMultiValueCompLink");
            Assert.AreEqual(testKeywordField.Id, deserializedKeyword.Id, "deserializedKeyword.Id");
            AssertEqualCollections(testMultiValueKeywordField, deserializedMultiValueKeywordField, "deserializedMultiValueKeywordField");
        }

        private static void AssertEqualViewModels(ViewModelData expected, ViewModelData actual, string subject)
        {
            Assert.AreEqual(expected.MvcData, actual.MvcData, subject + ".MvcData");
            Assert.AreEqual(expected.HtmlClasses, actual.HtmlClasses, subject + ".HtmlClasses");
            AssertEqualDictionaries(expected.XpmMetadata, actual.XpmMetadata, subject + ".XpmMetadata");
            AssertEqualDictionaries(expected.ExtensionData, actual.ExtensionData, subject + ".ExtensionData");
            Assert.AreEqual(expected.SchemaId, actual.SchemaId, subject + ".SchemaId");
            // TODO: AssertEqualDictionaries(expected.Metadata, actual.Metadata, subject + ".Metadata");
        }

        private static void AssertEqualPageModels(PageModelData expected, PageModelData actual, string subject)
        {
            AssertEqualViewModels(expected, actual, subject);
            Assert.AreEqual(expected.Id, actual.Id, subject + ".Id");
            Assert.AreEqual(expected.Title, actual.Title, subject + ".Title");
            Assert.AreEqual(expected.UrlPath, actual.UrlPath, subject + ".UrlPath");
            AssertEqualDictionaries(expected.Meta, actual.Meta, subject + ".Meta");
            AssertEqualCollections(expected.Regions, actual.Regions, subject + ".Regions");
        }

        private static void AssertEqualRegionModels(RegionModelData expected, RegionModelData actual, string subject)
        {
            AssertEqualViewModels(expected, actual, subject);
            Assert.AreEqual(expected.Name, actual.Name, subject + ".Name");
            AssertEqualCollections(expected.Entities, actual.Entities, subject + ".Entities");
            AssertEqualCollections(expected.Regions, actual.Regions, subject + ".Regions");
            Assert.AreEqual(expected.IncludePageId, actual.IncludePageId, subject + ".IncludePageId");
        }

        private void AssertEqualEntityModels(EntityModelData expected, EntityModelData actual, string subject)
        {
            AssertEqualViewModels(expected, actual, subject);
            Assert.AreEqual(expected.Id, actual.Id, subject + ".Id");
            // TODO: AssertEqualDictionaries(expected.Content, actual.Content, subject + ".Content");
            AssertEqualBinaryContent(expected.BinaryContent, actual.BinaryContent, subject + ".BinaryContent");
            AssertEqualExternalContent(expected.ExternalContent, actual.ExternalContent, subject + ".ExternalContent");
            Assert.AreEqual(expected.LinkUrl, actual.LinkUrl, subject + ".LinkUrl");
        }
        private void AssertEqualKeywordModels(KeywordModelData expected, KeywordModelData actual, string subject)
        {
            AssertEqualViewModels(expected, actual, subject);
            Assert.AreEqual(expected.Id, actual.Id, subject + ".Id");
            Assert.AreEqual(expected.Title, actual.Title, subject + ".Title");
            Assert.AreEqual(expected.Description, actual.Description, subject + ".Description");
            Assert.AreEqual(expected.Key, actual.Key, subject + ".Key");
            Assert.AreEqual(expected.TaxonomyId, actual.TaxonomyId, subject + ".TaxonomyId");
        }

        private static void AssertEqualBinaryContent(BinaryContentData expected, BinaryContentData actual, string subject)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, subject);
            }
            else
            {
                Assert.IsNotNull(actual, subject);
                Assert.AreEqual(expected.Url, actual.Url, subject + ".Url");
                Assert.AreEqual(expected.FileName, actual.FileName, subject + ".FileName");
                Assert.AreEqual(expected.FileSize, actual.FileSize, subject + ".FileSize");
                Assert.AreEqual(expected.MimeType, actual.MimeType, subject + ".MimeType");
            }
        }

        private void AssertEqualExternalContent(ExternalContentData expected, ExternalContentData actual, string subject)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, subject);
            }
            else
            {
                Assert.IsNotNull(actual, subject);
                Assert.AreEqual(expected.Id, actual.Id, subject + ".Id");
                Assert.AreEqual(expected.DisplayTypeId, actual.DisplayTypeId, subject + ".DisplayTypeId");
                Assert.AreEqual(expected.TemplateFragment, actual.TemplateFragment, subject + ".TemplateFragment");
                // TODO: AssertEqualDictionaries(expected.Metadata, actual.Metadata, subject + ".Metadata");
            }
        }

        private static PageModelData CreateTestPageModel(string testId)
        {
            return new PageModelData
            {
                MvcData = CreateTestMvcData(testId),
                HtmlClasses = "class1 class2",
                XpmMetadata =  CreateTestXpmMetadata(testId),
                ExtensionData = CreateTestExtensionData(testId),
                Id = testId,
                Title = "Test Page Model for " + testId,
                UrlPath = "/test/url/path",
                Meta = new Dictionary<string, string>
                {
                    { "og:type", "test" }
                },
                Regions = new List<RegionModelData> { CreateTestRegionModel(testId + "_Region") },
                Metadata = CreateTestContentModel(testId, true, true)
            };
        }

        private static RegionModelData CreateTestRegionModel(string testId, string name = "TestRegion")
        {
            return new RegionModelData
            {
                MvcData = CreateTestMvcData(testId),
                XpmMetadata = CreateTestXpmMetadata(testId),
                ExtensionData = CreateTestExtensionData(testId),
                Name = name,
                Entities = new List<EntityModelData> { CreateTestEntityModel(testId + "_Entity1") },
                Metadata = CreateTestContentModel(testId, false, false),
                IncludePageId = "666"
            };
        }

        private static EntityModelData CreateTestEntityModel(string testId)
        {
            return new EntityModelData
            {
                MvcData = CreateTestMvcData(testId),
                XpmMetadata = CreateTestXpmMetadata(testId),
                ExtensionData = CreateTestExtensionData(testId),
                Id = testId,
                SchemaId = "666",
                Content = CreateTestContentModel(testId + "_Content", false, true),
                Metadata = CreateTestContentModel(testId + "_Metadata", false, true),
                BinaryContent = new BinaryContentData
                {
                    FileSize = 123456789,
                    FileName = testId + ".png",
                    MimeType = "image/png",
                    Url = $"/iamges/{testId}.png"
                },
                ExternalContent = new ExternalContentData
                {
                    DisplayTypeId = "image",
                    Metadata = CreateTestContentModel(testId + "_ExternalMetadata", false, false),
                    TemplateFragment = "test template fragment",
                    Id = "123456"
                }
            };
        }

        private static KeywordModelData CreateTestKeywordModel(string testId)
        {
            return new KeywordModelData
            {
                ExtensionData = CreateTestExtensionData(testId),
                Id = testId,
                Title = "Test Keyword for " + testId,
                Key = "TestKW",
                Description = "Need I say more?",
                SchemaId = "999",
                Metadata = CreateTestContentModel(testId + "_Metadata", false, false)
            };
        }

        private static ContentModelData CreateTestContentModel(string testId, bool includeEntityModels, bool includeKeywordModels)
        {
            ContentModelData result = new ContentModelData
            {
                { "textField", "Test content for " + testId },
                { "multiValueTextField", new[] { "value1", "value2" } },
                { "richTextField", CreateTestRichText(testId + "_richTextField", includeEntityModels) },
                { "multiValueRichTextField", new[] { CreateTestRichText("multiValueRichTextField", includeEntityModels) } }
            };

            if (includeEntityModels)
            {
                result.Add("compLinkField", CreateTestEntityModel(testId + "_compLinkField"));
                result.Add("multiValueCompLinkField", new[] { CreateTestEntityModel(testId + "_multiValueCompLinkField") });
            }

            if (includeKeywordModels)
            {
                result.Add("keywordField", CreateTestKeywordModel(testId + "_keywordField"));
                result.Add("multiValueKeywordField", new [] { CreateTestKeywordModel(testId + "_multiValueKeywordField") });
            }

            return result;
        }

        private static RichTextData CreateTestRichText(string testId, bool includeEntityModels)
        {
            RichTextData result = new RichTextData
            {
                Fragments = new List<object> { "Test Rich Text for " + testId }
            };

            if (includeEntityModels)
            {
                result.Fragments.Add(CreateTestEntityModel(testId + "_EmbeddedEntity"));
            }

            return result;
        }

        private static MvcData CreateTestMvcData(string testId)
        {
            return new MvcData
            {
                ViewName = $"{testId}_View",
                AreaName = "Test"
            };
        }

        private static Dictionary<string, object> CreateTestExtensionData(string testId)
        {
            return new Dictionary<string, object>
            {
                { "ext1", testId },
                { "ext2", 666.666 },
                { "ext3", (long) 666 },
                { "ext4", new DateTime(1970, 12, 16, 12, 34, 56) },
                { "ext5", true }
            };
        }

        private static Dictionary<string, object> CreateTestXpmMetadata(string testId)
        {
            return new Dictionary<string, object>
            {
                { "TestID", testId },
                { "IsTest", true },
                { "Modified", DateTime.Now }
            };
        }
    }
}
