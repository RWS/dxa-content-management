using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.Common;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Templates.Tests
{

    [TestClass]
    public class TemplateBaseTest : TestClass
    {
        private class TestTemplate : TemplateBase
        {
            internal TestTemplate(Publication publication)
            {
                Session = publication.Session;
                Publication = publication;
            }

            public override void Transform(Engine engine, Package package)
                => Console.WriteLine("DummyTemplate.Transform was called.");

            internal string TestGetLocale() => GetLocale();
        }

        [TestMethod]
        public void GetCulture_WithLocalizationConfigComponent_Success()
        {
            Publication testPublication = (Publication) TestSession.GetObject(TestFixture.AutoTestParentWebDavUrl);
            TestTemplate testTemplate = new TestTemplate(testPublication);

            string locale = testTemplate.TestGetLocale();

            Assert.AreEqual("en-US", locale);
        }
    }
}
