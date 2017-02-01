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
    public class DataModelBuilderTest : TestClass
    {
        private static readonly DataModelBuilderSettings _defaultModelBuilderSettings =  new DataModelBuilderSettings
        {
            ExpandLinkDepth = 1,
            GenerateXpmMetadata = true,
            Locale = "en-US"
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

        private PageModelData BuildPageModel(Page page, out RenderedItem renderedItem)
        {
            renderedItem = CreateTestRenderedItem(page, page.PageTemplate);

            DataModelBuilder testModelBuilder = new DataModelBuilder(renderedItem, _defaultModelBuilderSettings, new ConsoleLogger());

            PageModelData result = testModelBuilder.BuildPageModel(page);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }

        private EntityModelData BuildEntityModel(Component component, ComponentTemplate ct, out RenderedItem renderedItem)
        {
            renderedItem = CreateTestRenderedItem(component, ct);

            DataModelBuilder testModelBuilder = new DataModelBuilder(renderedItem, _defaultModelBuilderSettings, new ConsoleLogger());

            EntityModelData result = testModelBuilder.BuildEntityModel(component, ct);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }


        [TestMethod]
        public void BuildPageModel_ExampleSiteHomePage_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ExampleSiteHomePageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            Assert.AreEqual("Home", pageModel.Title, "pageModel.Title");
            Assert.IsNotNull(testRenderedItem, "testRenderedItem");
            Assert.AreEqual(6, testRenderedItem.Binaries.Count, "testRenderedItem.Binaries.Count");
            Assert.AreEqual(16, testRenderedItem.ChildRenderedItems.Count, "testRenderedItem.ChildRenderedItems.Count");
            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_ArticleDcp_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.ArticleDcpPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_MediaManager_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.MediaManagerPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Flickr_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.FlickrTestPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_SmartTarget_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.SmartTargetPageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi811_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi811PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            PageModelData deserializedPageModel = JsonSerializeDeserialize(pageModel);

            ContentModelData pageMetadata = deserializedPageModel.Metadata;
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
        public void BuildPageModel_Tsi1758_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1758PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi1946_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1946PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi1308_Success()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.Tsi1308PageWebDavUrl);

            RenderedItem testRenderedItem;
            PageModelData pageModel = BuildPageModel(testPage, out testRenderedItem);

            // TODO TSI-132: further assertions
        }


        [TestMethod]
        public void BuildPageModel_DuplicatePredefinedRegions_Exception()
        {
            Page testPage = (Page) TestSession.GetObject(TestFixture.PredefinedRegionsTestPageWebDavUrl);

            RenderedItem testRenderedItem;
            AssertThrowsException<DxaException>(() => BuildPageModel(testPage, out testRenderedItem));
        }

        [TestMethod]
        public void BuildEntityModel_ArticleDcp_Success()
        {
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) TestSession.GetObject(articleDcpIds[0]);
            ComponentTemplate ct = (ComponentTemplate) TestSession.GetObject(articleDcpIds[1]);

            RenderedItem testRenderedItem;
            EntityModelData entityModel = BuildEntityModel(article, ct, out testRenderedItem);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildEntityModel_WithoutComponentTemplate_Success()
        {
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) TestSession.GetObject(articleDcpIds[0]);

            RenderedItem testRenderedItem;
            EntityModelData entityModel = BuildEntityModel(article, null, out testRenderedItem);

            // TODO TSI-132: further assertions
        }
    }
}
