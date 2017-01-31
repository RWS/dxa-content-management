using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Data;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class DataModelBuilderTest : TestClass
    {
        private static readonly DataModelBuilderSettings _defaultModelBuilderSettings =  new DataModelBuilderSettings
        {
            ExpandLinkDepth = 1,
            GenerateXpmMetadata = true
        };

        private PageModelData BuildPageModel(Page page, MockBinaryPublisher mockBinaryPublisher)
        {
            DataModelBuilder testModelBuilder = new DataModelBuilder(
                page.Session,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream,
                new ConsoleLogger()
                );

            PageModelData result = testModelBuilder.BuildPageModel(page);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }

        private EntityModelData BuildEntityModel(Component component, ComponentTemplate ct, MockBinaryPublisher mockBinaryPublisher)
        {
            DataModelBuilder testModelBuilder = new DataModelBuilder(
                component.Session,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream,
                new ConsoleLogger()
                );

            EntityModelData result = testModelBuilder.BuildEntityModel(component, ct);

            Assert.IsNotNull(result);
            OutputJson(result, DataModelBinder.SerializerSettings);

            return result;
        }


        [TestMethod]
        public void BuildPageModel_ExampleSiteHomePage_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.ExampleSiteHomePageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_ArticleDcp_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.ArticleDcpPageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_MediaManager_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.MediaManagerPageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Flickr_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.FlickrTestPageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_SmartTarget_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.SmartTargetPageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi811_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi811PageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

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
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi1758PageWebDavUrl);

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi1946_Success()
        {
            Session testSession = new Session();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi1946PageWebDavUrl);
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi1308_Success()
        {
            Session testSession = new Session();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi1308PageWebDavUrl);
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();

            PageModelData pageModel = BuildPageModel(testPage, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }


        [TestMethod]
        public void BuildPageModel_DuplicatePredefinedRegions_Exception()
        {
            Session testSession = new Session();
            Page testPage = (Page) testSession.GetObject(TestFixture.PredefinedRegionsTestPageWebDavUrl);
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();

            AssertThrowsException<DxaException>(() => BuildPageModel(testPage, mockBinaryPublisher));
        }

        [TestMethod]
        public void BuildEntityModel_ArticleDcp_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) testSession.GetObject(articleDcpIds[0]);
            ComponentTemplate ct = (ComponentTemplate) testSession.GetObject(articleDcpIds[1]);

            EntityModelData entityModel = BuildEntityModel(article, ct, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }

        [TestMethod]
        public void BuildEntityModel_WithoutComponentTemplate_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) testSession.GetObject(articleDcpIds[0]);

            EntityModelData entityModel = BuildEntityModel(article, null, mockBinaryPublisher);

            // TODO TSI-132: further assertions
        }
    }
}
