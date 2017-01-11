using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sdl.Web.DataModel;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class Dxa2ModelBuilderTest : TestClass
    {
        private static readonly Dxa2ModelBuilderSettings _defaultModelBuilderSettings =  new Dxa2ModelBuilderSettings
        {
            ExpandLinkDepth = 2,
            IsXpmEnabled = true
        };

        [TestMethod]
        public void BuildPageModel_ExampleSiteHomePage_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.ExampleSiteHomePageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession, 
                _defaultModelBuilderSettings, 
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_ArticleDcp_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.ArticleDcpPageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_MediaManager_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.MediaManagerPageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Flickr_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.FlickrTestPageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_SmartTarget_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.SmartTargetPageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi811_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi811PageWebDavUrl);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions

            PageModelData deserializedPageModel = JsonSerializeDeserialize(pageModel);
            Assert.IsNotNull(deserializedPageModel, "deserializedPageModel");
            OutputJson(deserializedPageModel);

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

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildPageModel_Tsi1946_Success()
        {
            Session testSession = new Session();
            Page testPage = (Page) testSession.GetObject(TestFixture.Tsi1946PageWebDavUrl);
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            PageModelData pageModel = testModelBuilder.BuildPageModel(testPage);

            Assert.IsNotNull(pageModel);
            OutputJson(pageModel);

            // TODO: further assertions
        }

        [TestMethod]
        public void BuildEntityModel_ArticleDcp_Success()
        {
            Session testSession = new Session();
            MockBinaryPublisher mockBinaryPublisher = new MockBinaryPublisher();
            string[] articleDcpIds = TestFixture.ArticleDcpId.Split('/');
            Component article = (Component) testSession.GetObject(articleDcpIds[0]);
            ComponentTemplate ct = (ComponentTemplate) testSession.GetObject(articleDcpIds[1]);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            EntityModelData entityModel = testModelBuilder.BuildEntityModel(article, ct);

            Assert.IsNotNull(entityModel);
            OutputJson(entityModel);

            // TODO: further assertions
        }
    }
}
