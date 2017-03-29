using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Data;
using Tridion.ContentManager;
using Tridion.ContentManager.Caching;
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

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            DefaultInitialize(testContext);
        }

        [TestMethod]
        public void DataPresentationTemplate_FoundAndCached_Success()
        {
            using (Session testSessionWithCache = new Session())
            {
                SimpleCache testCache = new SimpleCache();
                testSessionWithCache.Cache = testCache;

                Page dummyPage = (Page) testSessionWithCache.GetObject(TestFixture.AutoTestParentHomePageWebDavUrl);

                RenderedItem renderedItem = CreateTestRenderedItem(dummyPage, dummyPage.PageTemplate);
                TestLogger testLogger = new TestLogger();

                DataModelBuilderPipeline testModelBuilderPipeline = new DataModelBuilderPipeline(
                    renderedItem,
                    _defaultModelBuilderSettings,
                    _defaultModelBuilderTypeNames,
                    testLogger
                    );

                ComponentTemplate dataPresentationTemplate = testModelBuilderPipeline.DataPresentationTemplate;
                Assert.IsNotNull(dataPresentationTemplate, "dataPresentationTemplate");

                SimpleCacheRegion dxaCacheRegion;
                Assert.IsTrue(testCache.Regions.TryGetValue("DXA", out dxaCacheRegion), "DXA Cache Region not found.");
                ComponentTemplate cachedComponentTemplate = dxaCacheRegion.Get("DataPresentationTemplate") as ComponentTemplate;
                Assert.AreEqual(dataPresentationTemplate, cachedComponentTemplate, "cachedComponentTemplate");

                // Create new DataModelBuilderPipeline for same Session; DataPresentationTemplate should be obtained from cache.
                testModelBuilderPipeline = new DataModelBuilderPipeline(
                    renderedItem,
                    _defaultModelBuilderSettings,
                    _defaultModelBuilderTypeNames,
                    testLogger
                    );

                ComponentTemplate dataPresentationTemplate2 = testModelBuilderPipeline.DataPresentationTemplate;
                Assert.AreEqual(dataPresentationTemplate, dataPresentationTemplate2, "dataPresentationTemplate2");

                Assert.IsTrue(
                    testLogger.LoggedMessages.Contains(new LogMessage(LogLevel.Debug, "Obtained Data Presentation Template from cache.")),
                    "Expected Log message not found."
                    );
            }
        }

        [TestMethod]
        public void DataPresentationTemplate_NotFound_Success()
        {
            Page dummyPage = (Page) TestSession.GetObject(TestFixture.AutoTestChildHomePageWebDavUrl);

            RenderedItem renderedItem = CreateTestRenderedItem(dummyPage, dummyPage.PageTemplate);
            TestLogger testLogger = new TestLogger();

            DataModelBuilderPipeline testModelBuilderPipeline = new DataModelBuilderPipeline(
                renderedItem,
                _defaultModelBuilderSettings,
                _defaultModelBuilderTypeNames,
                testLogger
                );

            ComponentTemplate dataPresentationTemplate = testModelBuilderPipeline.DataPresentationTemplate;
            Assert.IsNull(dataPresentationTemplate, "dataPresentationTemplate");
            Assert.IsTrue(
                testLogger.LoggedMessages.Contains(new LogMessage(LogLevel.Warning, "Component Template 'Generate Data Presentation' not found.")),
                "Expected Log message not found."
                );
        }

        [TestMethod]
        public void CreatePageModel_ExampleSiteHomePage_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ExampleSiteHomePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.IsNotNull(pageModel.MvcData, "pageModel.MvcData");
            Assert.IsNull(pageModel.HtmlClasses, "pageModel.HtmlClasses");
            Assert.IsNotNull(pageModel.XpmMetadata, "pageModel.XpmMetadata");
            Assert.IsNull(pageModel.ExtensionData, "pageModel.ExtensionData");
            Assert.AreEqual("10015", pageModel.SchemaId, "pageModel.SchemaId");
            Assert.IsNotNull(pageModel.Metadata, "pageModel.Metadata");
            Assert.AreEqual("640", pageModel.Id, "pageModel.Id");
            Assert.AreEqual("Home", pageModel.Title, "pageModel.Title");
            Assert.AreEqual("/index", pageModel.UrlPath, "pageModel.UrlPath");
            Assert.IsNotNull(pageModel.Meta, "pageModel.Meta");
            Assert.AreEqual(7, pageModel.Meta.Count, "pageModel.Meta.Count");
            Assert.IsNotNull(pageModel.Regions, "pageModel.Regions");
            Assert.AreEqual(5, pageModel.Regions.Count, "pageModel.Regions.Count");
            AssertExpectedIncludePageRegions(pageModel.Regions, new [] { "Header", "Footer" });

            KeywordModelData sitemapKeyword = (KeywordModelData) pageModel.Metadata["sitemapKeyword"];
            AssertNotExpanded(sitemapKeyword, false, "sitemapKeyword"); // Keyword Should not be expanded because Category is publishable

            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(5, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(5, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");
        }

        [TestMethod]
        public void CreatePageModel_Article_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticlePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            // First assert the Page Model itself looks OK
            Assert.IsNotNull(pageModel.MvcData, "pageModel.MvcData");
            Assert.IsNull(pageModel.HtmlClasses, "pageModel.HtmlClasses");
            Assert.IsNotNull(pageModel.XpmMetadata, "pageModel.XpmMetadata");
            Assert.IsNull(pageModel.ExtensionData, "pageModel.ExtensionData");
            Assert.IsNull(pageModel.SchemaId, "pageModel.SchemaId");
            Assert.IsNull(pageModel.Metadata, "pageModel.Metadata");
            Assert.AreEqual("9786", pageModel.Id, "pageModel.Id");
            Assert.AreEqual("Test Article used for Automated Testing (Sdl.Web.Tridion.Tests)", pageModel.Title, "pageModel.Title");
            Assert.AreEqual("/autotest-parent/test_article_page", pageModel.UrlPath, "pageModel.UrlPath");
            Assert.IsNotNull(pageModel.Meta, "pageModel.Meta");
            Assert.AreEqual(5, pageModel.Meta.Count, "pageModel.Meta.Count");
            Assert.IsNotNull(pageModel.Regions, "pageModel.Regions");
            Assert.AreEqual(4, pageModel.Regions.Count, "pageModel.Regions.Count");
            AssertExpectedIncludePageRegions(pageModel.Regions, new[] { "Header", "Footer", "Content Tools" });

            // Assert the output RenderedItem looks OK
            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(2, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(1, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");

            // Assert the Article EntityModelData looks OK
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

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData flickrImage = mainRegion.Entities[0];
            BinaryContentData binaryContent = flickrImage.BinaryContent;
            ExternalContentData externalContent = flickrImage.ExternalContent;

            Assert.IsNotNull(binaryContent, "binaryContent");
            StringAssert.Matches(binaryContent.Url, new Regex(@"/Preview/Images/.*\.jpg"), "binaryContent.Url");
            Assert.AreEqual("image/jpeg", binaryContent.MimeType, "binaryContent.MimeType");
            Assert.IsNotNull(externalContent, "externalContent");
            Assert.AreEqual("ecl:1065-flickr-5606989559_6b62b3c3fc_72157626470204584-img-file", externalContent.Id, "externalContent.Id");
            Assert.AreEqual("img", externalContent.DisplayTypeId, "externalContent.DisplayTypeId");
            Assert.IsNotNull(externalContent.Metadata, "externalContent.Metadata");
            object width;
            Assert.IsTrue(externalContent.Metadata.TryGetValue("Width", out width), "externalContent.Metadata['Width']");
            Assert.AreEqual("1024", width, "width");
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
        public void CreatePageModel_MetadataMerge_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.SmartTargetMetadataOverridePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            ContentModelData pageModelMetadata = pageModel.Metadata;
            Assert.IsNotNull(pageModelMetadata, "pageModelMetadata");
            object allowDups;
            Assert.IsTrue(pageModelMetadata.TryGetValue("allowDuplicationOnSamePage", out allowDups), "pageModelMetadata['allowDuplicationOnSamePage']");
            Assert.AreEqual("True", allowDups, "allowDups"); // PT metadata overridden by Page metadata

            Assert.AreEqual("metadata merge test", pageModelMetadata["htmlClasses"], "pageModelMetadata['htmlClasses']");
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
        public void CreatePageModel_KeywordModel_Success()
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
        public void CreatePageModel_InternationalizedUrlPath_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1278PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Assert.AreEqual("/autotest-parent/tsi-1278_trådløst", pageModel.UrlPath, "pageModel.UrlPath");
        }

        [TestMethod]
        public void CreatePageModel_HtmlClasses_Success()
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
        public void CreatePageModel_EmbeddedFields_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1758PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            RegionModelData mainRegion = GetMainRegion(pageModel);
            EntityModelData testEntity = mainRegion.Entities[0];
            ContentModelData[] topLevelEmbedField1 = (ContentModelData[]) testEntity.Content["embedField1"];
            ContentModelData nestedEmbedField1 = (ContentModelData) topLevelEmbedField1[0]["embedField1"];

            Assert.AreEqual("This is the textField of the first embedField1", topLevelEmbedField1[0]["textField"], "topLevelEmbedField1['textField']");
            Assert.AreEqual("This is the link text of embedField1 within the first embedField1", nestedEmbedField1["linkText"], "nestedEmbedField1['linkText']");
        }

        [TestMethod]
        public void CreatePageModel_RichTextComponentLinks_Success()
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
        public void CreatePageModel_PageMetaModelBuilder_Success()
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
        public void CreatePageModel_RichTextEmbeddedMediaManagerItems_Success()
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
        public void CreatePageModel_PageMetaForCustomFields_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1308PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = CreatePageModel(testPage, out testRenderedItem);

            Dictionary<string, string> pageMeta = pageModel.Meta;
            Assert.IsNotNull(pageMeta, "pageMeta");
            Assert.AreEqual("This is single line text", pageMeta["singleLineText"], "pageMeta['singleLineText']");
            Assert.AreEqual("This is multi line text line 1\nAnd line 2\n", pageMeta["multiLineText"], "pageMeta['multiLineText']");
            Assert.AreEqual("This is <strong>rich</strong> text with a <a title=\"Test Article\" href=\"tcm:1065-9712\">Component Link</a><!--CompLink tcm:1065-9712-->", pageMeta["richText"], "pageMeta['richText']");
            Assert.AreEqual("News Article", pageMeta["keyword"], "pageMeta['keyword']");
            Assert.AreEqual("tcm:1065-9712", pageMeta["componentLink"], "pageMeta['componentLink']");
            Assert.AreEqual("tcm:1065-4480", pageMeta["mmComponentLink"], "pageMeta['mmComponentLink']");
            Assert.AreEqual("1970-12-16T12:34:56.000", pageMeta["date"], "pageMeta['date']");
            Assert.AreEqual("2016-11-23T13:11:40.000", pageMeta["dateCreated"], "pageMeta['dateCreated']");
            Assert.AreEqual("Rick Pannekoek", pageMeta["author"], "pageMeta['author']");
            Assert.AreEqual("666.666", pageMeta["number"], "pageMeta['number']");
        }

        [TestMethod]
        public void CreatePageModel_KeywordModelExpansion_Success()
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
            Component testComponent = (Component) TestSession.GetObject(articleDcpIds[0]);
            ComponentTemplate ct = (ComponentTemplate) TestSession.GetObject(articleDcpIds[1]);

            RenderedItem testRenderedItem;
            EntityModelData article = CreateEntityModel(testComponent, ct, out testRenderedItem);

            AssertExpanded(article, true, "article");
            StringAssert.Matches(article.Id, new Regex(@"\d+-\d+"), "article.Id");
            Assert.IsNotNull(article.MvcData, "article.MvcData");
            Assert.AreEqual("Article", article.MvcData.ViewName, "article.MvcData.ViewName");
            Assert.IsNotNull(article.XpmMetadata, "article.XpmMetadata");
            Assert.AreEqual(testComponent.Id.ToString(), article.XpmMetadata["ComponentID"], "article.XpmMetadata['ComponentID']");
            Assert.AreEqual(ct.Id.ToString(), article.XpmMetadata["ComponentTemplateID"], "article.XpmMetadata['ComponentTemplateID']");

            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(2, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(0, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");
        }

        [TestMethod]
        public void CreateEntityModel_WithoutComponentTemplate_Success()
        {
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component testComponent = (Component) TestSession.GetObject(articleDcpIds[0]);

            RenderedItem testRenderedItem;
            EntityModelData article = CreateEntityModel(testComponent, null, out testRenderedItem);

            AssertExpanded(article, true, "article");
            StringAssert.Matches(article.Id, new Regex(@"\d+"), "article.Id");
            Assert.IsNull(article.MvcData, "article.MvcData");
            Assert.IsNull(article.XpmMetadata, "article.XpmMetadata");

            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(2, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(0, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");
        }

        [TestMethod]
        public void CreateEntityModel_WithCategoryLink_Success()
        {
            Component testComponent = (Component) TestSession.GetObject(TestFixture.TestComponentWebDavUrl);

            RenderedItem testRenderedItem;
            EntityModelData testEntity = CreateEntityModel(testComponent, null, out testRenderedItem);

            string[] externalLinkField = (string[]) testEntity.Content["ExternalLink"];
            Assert.AreEqual(2, externalLinkField.Length, "externalLinkField.Length");
            Assert.AreEqual("http://www.sdl.com", externalLinkField[0], "externalLinkField[0]");
            Assert.AreEqual("tcm:1065-2702-512", externalLinkField[1], "externalLinkField[1]"); // NOTE: This is a (managed) Category link (!)
        }


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
                new TestLogger()
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
                new TestLogger()
                );

            EntityModelData result = testModelBuilderPipeline.CreateEntityModel(component, ct);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }

        private static RegionModelData GetMainRegion(PageModelData pageModelData)
        {
            RegionModelData mainRegion = pageModelData.Regions.FirstOrDefault(r => r.Name == "Main");
            Assert.IsNotNull(mainRegion, "No 'Main' Region found in Page Model.");
            return mainRegion;
        }

        private static void AssertExpectedIncludePageRegions(IEnumerable<RegionModelData> regions, string[] expectedRegionNames)
        {
            RegionModelData[] includePageRegions = regions.Where(r => r.IncludePageId != null).ToArray();
            Assert.AreEqual(expectedRegionNames.Length, includePageRegions.Length, "includePageRegions.Length");

            foreach (RegionModelData includePageRegion in includePageRegions)
            {
                StringAssert.Matches(includePageRegion.IncludePageId, new Regex(@"\d+"), "includePageRegion.IncludePageId");
                Assert.IsNotNull(includePageRegion.Name, "includePageRegion.Name");
                Assert.IsTrue(expectedRegionNames.Contains(includePageRegion.Name), "Unexpected Include Page Region name: " + includePageRegion.Name);
                Assert.IsNull(includePageRegion.Regions, "includePageRegion.Regions");
                Assert.IsNull(includePageRegion.Entities, "includePageRegion.Entities");
                Assert.IsNotNull(includePageRegion.XpmMetadata, "includePageRegion.XpmMetadata");
                object includedFromPageId;
                Assert.IsTrue(includePageRegion.XpmMetadata.TryGetValue("IncludedFromPageID", out includedFromPageId), "includePageRegion.XpmMetadata['IncludedFromPageID']");
                StringAssert.Contains((string) includedFromPageId, includePageRegion.IncludePageId, "includedFromPageId");
            }
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

    }
}
