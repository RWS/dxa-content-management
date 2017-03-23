using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Data;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Publishing.Resolving;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class DataModelBuilderPipelineTest : TestClass
    {
        private static readonly DataModelBuilderSettings _defaultModelBuilderSettings =  new DataModelBuilderSettings
        {
            ExpandLinkDepth = 1,
            GenerateXpmMetadata = true,
            Locale = "en-US"
        };

        private static readonly string[] _defaultModelBuilderTypeNames =
        {
            typeof(DefaultModelBuilder).Name,
            typeof(DefaultPageMetaModelBuilder).Name,
            typeof(EclModelBuilder).Name,
            typeof(ContextExpressionsModelBuilder).FullName // Both unqualified and qualified type names should work.
        };

        private RenderedItem CreateTestRenderedItem(IdentifiableObject item, Template template)
        {
            RenderInstruction testRenderInstruction = new RenderInstruction(item.Session)
            {
                BinaryStoragePath = @"C:\Temp\DXA\Test",
                RenderMode = RenderMode.PreviewDynamic
            };
            return new RenderedItem(new ResolvedItem(item, template), testRenderInstruction);
        }

        private PageModelData CreatePageModel(Page page, out RenderedItem renderedItem, IEnumerable<string> modelBuilderTypeNames = null)
        {
            renderedItem = CreateTestRenderedItem(page, page.PageTemplate);

            if (modelBuilderTypeNames == null)
            {
                modelBuilderTypeNames = _defaultModelBuilderTypeNames;
            }

            DataModelBuilderPipeline testModelBuilderPipeline = new DataModelBuilderPipeline(
                renderedItem,
                _defaultModelBuilderSettings,
                modelBuilderTypeNames,
                new ConsoleLogger()
                );

            PageModelData result = testModelBuilderPipeline.CreatePageModel(page);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }

        private EntityModelData CreateEntityModel(Component component, ComponentTemplate ct, out RenderedItem renderedItem, IEnumerable<string> modelBuilderTypeNames = null)
        {
            renderedItem = CreateTestRenderedItem(component, ct);

            if (modelBuilderTypeNames == null)
            {
                modelBuilderTypeNames = _defaultModelBuilderTypeNames;
            }

            DataModelBuilderPipeline testModelBuilderPipeline = new DataModelBuilderPipeline(
                renderedItem,
                _defaultModelBuilderSettings,
                modelBuilderTypeNames,
                new ConsoleLogger()
                );

            EntityModelData result = testModelBuilderPipeline.CreateEntityModel(component, ct);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }


        [TestMethod]
        public void DataPresentationTemplate_Success()
        {
            Page dummyPage = (Page) TestSession.GetObject(TestFixture.AutoTestParentHomePageWebDavUrl);

            RenderedItem renderedItem = CreateTestRenderedItem(dummyPage, dummyPage.PageTemplate);

            DataModelBuilderPipeline testModelBuilderPipeline = new DataModelBuilderPipeline(
                renderedItem,
                _defaultModelBuilderSettings,
                _defaultModelBuilderTypeNames,
                new ConsoleLogger()
                );

            ComponentTemplate dataPresentationTemplate = testModelBuilderPipeline.DataPresentationTemplate;
            Assert.IsNotNull(dataPresentationTemplate, "dataPresentationTemplate");
        }

        [TestMethod]
        public void CreatePageModel_ExampleSiteHomePage_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ExampleSiteHomePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.AreEqual("Home", pageModel.Title, "pageModel.Title");
            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(5, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(5, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");

            Assert.IsNotNull(pageModel.Metadata, "pageModel.Metadata");
            KeywordModelData sitemapKeyword = (KeywordModelData) pageModel.Metadata["sitemapKeyword"];
            AssertNotExpanded(sitemapKeyword, false, "sitemapKeyword"); // Keyword Should not be expanded because Category is publishable

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreatePageModel_Article_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticlePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData article = mainRegion.Entities[0];

            AssertExpanded(article, true, "article");
            StringAssert.Matches(article.Id, new Regex(@"\d+"));

            ContentModelData articleBody = (ContentModelData) article.Content["articleBody"];
            RichTextData content = (RichTextData) articleBody["content"];

            Assert.IsNotNull(content, "content");
            Assert.IsNotNull(content.Fragments, "content.Fragments");

            // Embedded image in Rich Text field should be represented as an embedded EntityModelData
            Assert.AreEqual(3, content.Fragments.Count, "content.Fragments.Count");
            EntityModelData image = content.Fragments.OfType<EntityModelData>().FirstOrDefault();
            Assert.IsNotNull(image, "image");
            Assert.IsNotNull(image.BinaryContent, "image.BinaryContent");
            Assert.AreEqual("image/jpeg", image.BinaryContent.MimeType, "image.BinaryContent.MimeType");

            // Image should have "altText" metadata field obtained from the original XHTML; see TSI-2289.
            Assert.IsNotNull(image.Metadata, "image.Metadata");
            object altText;
            Assert.IsTrue(image.Metadata.TryGetValue("altText", out altText));
            Assert.AreEqual("calculator", altText, "altText");

            RegionModelData[] includePageRegions = pageModel.Regions.Where(r => r.IncludePageId != null).ToArray();
            Assert.AreEqual(3, includePageRegions.Length, "includePageRegions.Length");
            foreach (RegionModelData includePageRegion in includePageRegions)
            {
                StringAssert.Matches(includePageRegion.IncludePageId, new Regex(@"\d+"), "includePageRegion.IncludePageId");
            }
        }

        [TestMethod]
        public void CreatePageModel_ArticleDcp_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticleDcpPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData article = mainRegion.Entities[0];

            AssertNotExpanded(article, "article");
        }

        private static void AssertExpanded(EntityModelData entityModelData, bool hasContent, string subject)
        {
            Assert.IsNotNull(entityModelData, subject);
            Assert.IsNotNull(entityModelData.Id, subject + ".Id");
            Assert.IsNotNull(entityModelData.SchemaId, subject + ".SchemaId");
            if (hasContent)
            {
                Assert.IsNotNull(entityModelData.Content, subject + ".Content");
            }
            else
            {
                Assert.IsNull(entityModelData.Content, subject + ".Content");
            }
        }

        private static void AssertNotExpanded(EntityModelData entityModelData, string subject)
        {
            Assert.IsNotNull(entityModelData, subject);
            Assert.IsNotNull(entityModelData.Id, subject + ".Id");
            StringAssert.Matches(entityModelData.Id, new Regex(@"\d+-\d+"), subject + ".Id");
            Assert.IsNull(entityModelData.SchemaId, subject + ".SchemaId");
            Assert.IsNull(entityModelData.Content, subject + ".Content");
            Assert.IsNull(entityModelData.Metadata, subject + ".Metadata");
        }

        [TestMethod]
        public void CreatePageModel_ComponentLinkExpansion_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ComponentLinkTestPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData testEntity = mainRegion.Entities[0];

            Assert.IsNotNull(testEntity, "testEntity");
            Assert.IsNotNull(testEntity.Content, "testEntity.Content");
            EntityModelData[] compLinkField = (EntityModelData[]) testEntity.Content["compLink"];
            Assert.AreEqual(4, compLinkField.Length, "compLinkField.Length");

            EntityModelData notExpandedCompLink = compLinkField[0]; // Has Data Presentation
            Assert.IsNotNull(notExpandedCompLink, "notExpandedCompLink");
            Assert.AreEqual("9712-10247", notExpandedCompLink.Id, "notExpandedCompLink.Id");
            Assert.IsNull(notExpandedCompLink.SchemaId, "notExpandedCompLink.SchemaId");
            Assert.IsNull(notExpandedCompLink.Content, "notExpandedCompLink.Content");

            EntityModelData expandedCompLink = compLinkField[1]; // Has no Data Presentation
            Assert.IsNotNull(expandedCompLink, "expandedCompLink");
            Assert.AreEqual("9710", expandedCompLink.Id, "expandedCompLink.Id");
            Assert.AreEqual("9709", expandedCompLink.SchemaId, "9710");
            Assert.IsNotNull(expandedCompLink.Content, "expandedCompLink.Content");

        }

        [TestMethod]
        public void CreatePageModel_MediaManager_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.MediaManagerPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData mmItem = mainRegion.Entities[0];
            BinaryContentData binaryContent = mmItem.BinaryContent;
            ExternalContentData externalContent = mmItem.ExternalContent;

            Assert.IsNotNull(binaryContent, "binaryContent");
            Assert.AreEqual("https://mmecl.dist.sdlmedia.com/distributions/?o=51498399-31e9-4c89-98eb-0c4256c96f71", binaryContent.Url, "binaryContent.Url");
            Assert.AreEqual("application/externalcontentlibrary", binaryContent.MimeType, "binaryContent.MimeType");
            Assert.IsNotNull(externalContent, "externalContent");
            Assert.AreEqual("ecl:1065-mm-415-dist-file", externalContent.Id, "externalContent.Id");
            Assert.AreEqual("html5dist", externalContent.DisplayTypeId, "externalContent.DisplayTypeId");
            Assert.IsNotNull(externalContent.Metadata, "externalContent.Metadata");
            object globalId;
            Assert.IsTrue(externalContent.Metadata.TryGetValue("GlobalId", out globalId), "externalContent.Metadata['GlobalId']");
            Assert.AreEqual("51498399-31e9-4c89-98eb-0c4256c96f71", globalId, "globalId");
            StringAssert.Contains(externalContent.TemplateFragment, (string) globalId, "externalContent.TemplateFragment");
        }

        [TestMethod]
        public void CreatePageModel_Flickr_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.FlickrTestPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreatePageModel_SmartTarget_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.SmartTargetPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.IsNotNull(pageModel.Metadata, "pageModel.Metadata");
            object allowDups;
            Assert.IsTrue(pageModel.Metadata.TryGetValue("allowDuplicationOnSamePage", out allowDups), "pageModel.Metadata[allowDuplicationOnSamePage]");
            Assert.AreEqual("Use core configuration", allowDups, "allowDups");

            RegionModelData example1Region = pageModel.Regions.FirstOrDefault(r => r.Name == "Example1");
            Assert.IsNotNull(example1Region, "example1Region");
            Assert.IsNotNull(example1Region.Metadata, "example1Region.Metadata");
            object maxItems;
            Assert.IsTrue(example1Region.Metadata.TryGetValue("maxItems", out maxItems), "example1Region.Metadata[maxItems]");
            Assert.AreEqual("3", maxItems, "maxItems");
        }

        [TestMethod]
        public void CreatePageModel_ContextExpressions_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ContextExpressionsPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData[] entitiesWithExtensionData = mainRegion.Entities.Where(e => e.ExtensionData != null).ToArray();
            EntityModelData[] entitiesWithCxInclude = entitiesWithExtensionData.Where(e => e.ExtensionData.ContainsKey("CX.Include")).ToArray();
            EntityModelData[] entitiesWithCxExclude = entitiesWithExtensionData.Where(e => e.ExtensionData.ContainsKey("CX.Exclude")).ToArray();

            Assert.AreEqual(8, entitiesWithExtensionData.Length, "entitiesWithExtensionData.Length");
            Assert.AreEqual(6, entitiesWithCxInclude.Length, "entitiesWithCxInclude.Length");
            Assert.AreEqual(4, entitiesWithCxExclude.Length, "entitiesWithCxExclude.Length");
        }

        [TestMethod]
        public void CreatePageModel_DefaultModelBuilderOnly_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticlePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModelWithoutMeta = CreatePageModel(testPage, out testRenderedItem, new [] { typeof(DefaultModelBuilder).Name });
            PageModelData pageModelWithMeta = CreatePageModel(testPage, out testRenderedItem);

            Assert.AreEqual("Test Article Page", pageModelWithoutMeta.Title, "pageModelWithoutMeta.Title");
            Assert.IsNull(pageModelWithoutMeta.Meta, "pageModelWithoutMeta.Meta");

            Assert.AreEqual("Test Article used for Automated Testing (Sdl.Web.Tridion.Tests)", pageModelWithMeta.Title, "pageModelWithMeta.Title");
            Assert.IsNotNull(pageModelWithMeta.Meta, "pageModelWithMeta.Meta");
            string ogTitle;
            Assert.IsTrue(pageModelWithMeta.Meta.TryGetValue("og:title", out ogTitle));
            Assert.AreEqual(pageModelWithMeta.Title, ogTitle, "ogTite");
        }


        [TestMethod]
        public void CreatePageModel_Tsi811_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi811PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.IsNotNull(pageModel.SchemaId, "pageModel.SchemaId");

            ContentModelData pageMetadata = pageModel.Metadata;
            Assert.IsNotNull(pageMetadata, "pageMetadata");
            KeywordModelData pageKeyword = pageMetadata["pageKeyword"] as KeywordModelData;
            Assert.IsNotNull(pageKeyword, "pageKeyword");
            Assert.AreEqual("10120", pageKeyword.Id, "pageKeyword.Id");
            Assert.AreEqual("Test Keyword 2", pageKeyword.Title, "pageKeyword.Title");
            ContentModelData keywordMetadata = pageKeyword.Metadata;
            Assert.IsNotNull(keywordMetadata, "keywordMetadata");
            Assert.AreEqual("This is textField of Test Keyword 2", keywordMetadata["textField"], "keywordMetadata['textField']");
            Assert.AreEqual("999.99", keywordMetadata["numberField"], "keywordMetadata['numberField']");
            KeywordModelData keywordField = keywordMetadata["keywordField"] as KeywordModelData;
            Assert.IsNotNull(keywordField, "keywordField");
        }

        [TestMethod]
        public void CreatePageModel_UrlPath_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1278PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.AreEqual("/autotest-parent/tsi-1278_trådløst", pageModel.UrlPath, "pageModel.UrlPath");
        }

        [TestMethod]
        public void CreatePageModel_Tsi1614_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1614PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData article = mainRegion.Entities[0];
            Assert.AreEqual("article tsi1614", article.HtmlClasses, "article.HtmlClasses");

            ContentModelData articleBody = article.Content["articleBody"] as ContentModelData;
            Assert.IsNotNull(articleBody, "articleBody");
            RichTextData content = articleBody["content"] as RichTextData;
            Assert.IsNotNull(content, "content");
            EntityModelData embeddedEntity = content.Fragments.OfType<EntityModelData>().FirstOrDefault();
            Assert.IsNotNull(embeddedEntity, "embeddedEntity");
            Assert.AreEqual("test tsi1614", embeddedEntity.HtmlClasses, "embeddedEntity.HtmlClasses");
        }

        [TestMethod]
        public void CreatePageModel_Tsi1758_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1758PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreatePageModel_Tsi1946_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1946PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreatePageModel_Tsi2265_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticlePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData article = mainRegion.Entities[0];
            ContentModelData articleBody = (ContentModelData) article.Content["articleBody"];
            RichTextData content = (RichTextData) articleBody["content"];
            string firstHtmlFragment = (string) content.Fragments[0];
            StringAssert.Contains(firstHtmlFragment, "href=\"tcm:1065-9710\"");
            StringAssert.Contains(firstHtmlFragment, "<!--CompLink tcm:1065-9710-->");
        }

        [TestMethod]
        public void CreatePageModel_Tsi2277_Success()
        {
            Page testPage1 = (Page) TestSession.GetObject(TestFixture.Tsi2277Page1WebDavUrl);
            Page testPage2 = (Page) TestSession.GetObject(TestFixture.Tsi2277Page2WebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel1 = CreatePageModel(testPage1, out testRenderedItem);
            PageModelData pageModel2 = CreatePageModel(testPage2, out testRenderedItem);

            const string articleHeadline = "Article headline";
            const string articleStandardMetaName = "Article standardMeta name";
            const string articleStandardMetaDescription = "Article standardMeta description";

            Assert.AreEqual(articleHeadline, pageModel1.Title, "pageModel1.Title");
            Assert.AreEqual(articleHeadline, pageModel1.Meta["description"], "pageModel1.Meta['description']");
            Assert.IsFalse(pageModel1.Meta.ContainsKey("og:description"));

            Assert.AreEqual(articleStandardMetaName, pageModel2.Title, "pageModel2.Title");
            Assert.AreEqual(articleStandardMetaDescription, pageModel2.Meta["description"], "pageModel2.Meta['description']");
            string ogDescription;
            Assert.IsTrue(pageModel2.Meta.TryGetValue("og:description", out ogDescription), "pageModel2.Meta['og: description']");
            Assert.AreEqual(articleStandardMetaDescription, ogDescription, "ogDescription");
        }

        [TestMethod]
        public void CreatePageModel_Tsi2306_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi2306PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData article = mainRegion.Entities.LastOrDefault(e => e.MvcData.ViewName == "Article");
            Assert.IsNotNull(article, "article");

            ContentModelData[] articleBody = (ContentModelData[]) article.Content["articleBody"];
            RichTextData content = (RichTextData) articleBody[0]["content"];
            EntityModelData embeddedMediaManagerItem = content.Fragments.OfType<EntityModelData>().FirstOrDefault();
            Assert.IsNotNull(embeddedMediaManagerItem, "embeddedMediaManagerItem");

            OutputJson(embeddedMediaManagerItem);

            BinaryContentData binaryContent = embeddedMediaManagerItem.BinaryContent;
            ExternalContentData externalContent = embeddedMediaManagerItem.ExternalContent;
            Assert.IsNotNull(binaryContent, "binaryContent");
            Assert.IsNotNull(externalContent, "externalContent");
            Assert.AreEqual("https://mmecl.dist.sdlmedia.com/distributions/?o=3e5f81f2-c7b3-47f7-8ede-b84b447195b9", binaryContent.Url, "binaryContent.Url");
            Assert.AreEqual("1065-mm-204-dist-file.ecl", binaryContent.FileName, "binaryContent.FileName");
            Assert.AreEqual("application/externalcontentlibrary", binaryContent.MimeType, "binaryContent.MimeType");
            Assert.AreEqual("ecl:1065-mm-204-dist-file", externalContent.Id, "ecl:1065-mm-204-dist-file");
            Assert.AreEqual("html5dist", externalContent.DisplayTypeId, "externalContent.DisplayTypeId");
            Assert.IsNotNull(externalContent.Metadata, "externalContent.Metadata");
        }

        [TestMethod]
        public void CreatePageModel_Tsi1308_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1308PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreatePageModel_Tsi2316_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi2316PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData testEntity = mainRegion.Entities[0];
            KeywordModelData notPublishedKeyword = (KeywordModelData) testEntity.Content["notPublishedKeyword"];
            KeywordModelData publishedKeyword = (KeywordModelData) testEntity.Content["publishedKeyword"];

            AssertExpanded(notPublishedKeyword, false, "notPublishedKeyword");
            AssertNotExpanded(publishedKeyword, true, "publishedKeyword");
        }

        private static void AssertExpanded(KeywordModelData keywordModelData, bool hasMetadata, string subject)
        {
            Assert.IsNotNull(keywordModelData.Id, subject + ".Id");
            Assert.IsNotNull(keywordModelData.Title, subject + ".Title");
            Assert.IsNotNull(keywordModelData.TaxonomyId, subject + ".TaxonomyId");
            if (hasMetadata)
            {
                Assert.IsNotNull(keywordModelData.SchemaId, subject + ".SchemaId");
                Assert.IsNotNull(keywordModelData.Metadata, subject + ".Metadata");
            }
            else
            {
                Assert.IsNull(keywordModelData.SchemaId, subject + ".SchemaId");
                Assert.IsNull(keywordModelData.Metadata, subject + ".Metadata");
            }
        }

        private static void AssertNotExpanded(KeywordModelData keywordModelData, bool hasMetadata, string subject)
        {
            Assert.IsNotNull(keywordModelData.Id, subject + ".Id");
            Assert.IsNull(keywordModelData.Title, subject + ".Title");
            Assert.IsNull(keywordModelData.TaxonomyId, subject + ".TaxonomyId");
            if (hasMetadata)
            {
                Assert.IsNotNull(keywordModelData.SchemaId, subject + ".SchemaId");
            }
            else
            {
                Assert.IsNull(keywordModelData.SchemaId, subject + ".SchemaId");
            }
            Assert.IsNull(keywordModelData.Metadata, subject + ".Metadata");
        }


        [TestMethod]
        public void CreatePageModel_DuplicatePredefinedRegions_Exception()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.PredefinedRegionsTestPageWebDavUrl);

            RenderedItem testRenderedItem;
            AssertThrowsException<DxaException>(() => CreatePageModel(testPage, out testRenderedItem));
        }

        [TestMethod]
        public void CreateEntityModel_ArticleDcp_Success()
        {
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) TestSession.GetObject(articleDcpIds[0]);
            ComponentTemplate ct = (ComponentTemplate) TestSession.GetObject(articleDcpIds[1]);

            RenderedItem testRenderedItem;
            EntityModelData entityModel = CreateEntityModel(article, ct, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void CreateEntityModel_WithoutComponentTemplate_Success()
        {
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) TestSession.GetObject(articleDcpIds[0]);

            RenderedItem testRenderedItem;
            EntityModelData entityModel = CreateEntityModel(article, null, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        private static RegionModelData GetMainRegion(PageModelData pageModelData)
        {
            RegionModelData mainRegion = pageModelData.Regions.FirstOrDefault(r => r.Name == "Main");
            Assert.IsNotNull(mainRegion, "No 'Main' Region found in Page Model.");
            return mainRegion;
        }
    }
}
