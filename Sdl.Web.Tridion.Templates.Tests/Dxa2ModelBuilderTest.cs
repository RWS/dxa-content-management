using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            ComponentPresentation articleDcp = new ComponentPresentation(article, ct);

            Dxa2ModelBuilder testModelBuilder = new Dxa2ModelBuilder(
                testSession,
                _defaultModelBuilderSettings,
                mockBinaryPublisher.AddBinary,
                mockBinaryPublisher.AddBinaryStream
                );
            EntityModelData entityModel = testModelBuilder.BuildEntityModel(articleDcp);

            Assert.IsNotNull(entityModel);
            OutputJson(entityModel);

            // TODO: further assertions
        }
    }
}
