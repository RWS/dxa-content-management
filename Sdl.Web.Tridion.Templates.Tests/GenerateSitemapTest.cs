using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;

namespace Sdl.Web.Tridion.Templates.Tests
{
    [TestClass]
    public class GenerateSitemapTest : TemplateTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext testContext)
            => DefaultInitialize(testContext);

        [TestMethod]
        public void Transform_GenerateSitemap_Success()
        {
            Component navConfigComponent = (Component) TestSession.GetObject(TestFixture.NavConfigurationComponentWebDavUrl);
            Publication testPublication = (Publication) navConfigComponent.ContextRepository;
            StructureGroup rootStructureGroup = testPublication.RootStructureGroup;

            SitemapItemData sitemapRoot = RunTemplate<SitemapItemData>(typeof(GenerateSiteMap), navConfigComponent);

            Assert.AreEqual(rootStructureGroup.Id.ToString(), sitemapRoot.Id, "sitemapRoot.Id");
            Assert.AreEqual(rootStructureGroup.PublishLocationUrl, sitemapRoot.Url, "sitemapRoot.Url");
            Assert.AreEqual("StructureGroup", sitemapRoot.Type, "sitemapRoot.Type");
            Assert.AreEqual(rootStructureGroup.Title, sitemapRoot.Title, "sitemapRoot.Title");
            Assert.IsNotNull(sitemapRoot.Items, "sitemapRoot.Items");

            // TODO: further assertions
        }
    }
}
