namespace Sdl.Web.Tridion.Templates.Tests
{
    internal static class TestFixture
    {
        internal const string ExampleSiteWebDavUrl = "/webdav/400 Example Site";
        internal const string AutoTestParentWebDavUrl = "/webdav/401 Automated Test Parent";
        internal const string AutoTestChildWebDavUrl = "/webdav/500 Automated Test Child";

        internal const string ExampleSiteHomePageWebDavUrl = ExampleSiteWebDavUrl + "/Home/000 Home.tpg";
        internal const string AutoTestParentHomePageWebDavUrl = AutoTestParentWebDavUrl + "/Home/000 Home.tpg";
        internal const string AutoTestChildHomePageWebDavUrl = AutoTestChildWebDavUrl + "/Home/000 Home.tpg";
        internal const string ArticlePageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Test Article Page.tpg";
        internal const string ArticleDcpPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Test Article (Dynamic) Page.tpg";
        internal const string ComponentLinkTestPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Component Link Test Page.tpg";
        internal const string MediaManagerPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/MediaManager.tpg";
        internal const string FlickrTestPageWebDavUrl = "/webdav/401 adcevora.com/Home/Test/Flickr Test.tpg";
        internal const string SmartTargetPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Smoke/Smart Target Smoke Test.tpg";
        internal const string SmartTargetMetadataOverridePageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Smoke/Smart Target PT metadata override.tpg";
        internal const string ContextExpressionsPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Smoke/Context Expression Smoke Test.tpg";
        internal const string Tsi811PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-811 Test Page.tpg";
        internal const string Tsi1278PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/TSI-1278 Internationalized URLs.tpg";
        internal const string Tsi1278ChildPageWebDavUrl = AutoTestChildWebDavUrl + "/Home/TSI-1278 Internationalized URLs.tpg";
        internal const string Tsi1308PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-1308 Test Page.tpg";
        internal const string Tsi1614PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/TSI-1614 Rich Text Image with HTML class.tpg";
        internal const string Tsi1758PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-1758 Test Page.tpg";
        internal const string Tsi2277Page1WebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-2277 Test Page 1.tpg";
        internal const string Tsi2277Page2WebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-2277 Test Page 2.tpg";
        internal const string Tsi2306PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Smoke/Media Manager Smoke Test.tpg";
        internal const string Tsi2316PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-2316 Test Page.tpg";
        internal const string PredefinedRegionsTestPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Acceptance/RegionsTest/Predefined Regions - Empty.tpg";
        internal const string R2PageIncludesPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/R2 Page Includes.tpg";

        internal const string Tsi811TestKeyword2WebDavUrl = "/webdav/401 Automated Test Parent/TSI-811 Test Category/Test Keyword 2.tkw";
        internal const string HomePageMetadataSchemaWebDavUrl = "/webdav/100 Master/Building Blocks/Modules/Core/Editor/Schemas/Page Schemas/Home Page.xsd";
        internal const string Tsi811TestCategoryWebDavUrl = "/webdav/401 Automated Test Parent/TSI-811 Test Category";
        internal const string ArticleDcpComponentWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Content/Test/Test Article.xml";
        internal const string ArticleDcpComponentTemplateWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Modules/Test/Test Article %28Dynamic%29.tctcmp";
        internal const string CompanyNewMediaManagerComponentWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Content/Video/Company News Media Manager Video.png";
        internal const string GenerateDataPresentationCPWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Framework/Developer/Templates/Generate Data Presentation.tctcmp";
        internal const string TestSchemaWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Modules/Test/Test Schema.xsd";
        internal const string EclComponentWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Modules/MediaManager/Editor/Schemas/SDL Media Manager/566/BDB/ecl:0-mm-415-dist-file.ecl";
        internal const string EclMMComponentWebDavUrl = "/webdav/401 Automated Test Parent/Building Blocks/Modules/MediaManager/Editor/Schemas/SDL Media Manager/BEF/AF8/ecl%3A0-mm-204-dist-file.ecl";

        //Native Region Items
        internal const string NestedRegionsPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Acceptance/NativeRegionsTests/Page with Nested Regions.tpg";
        internal const string MergedRegionsPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Acceptance/NativeRegionsTests/Page with CP in Native and DXA Regions.tpg";
        internal const string MergedRegionWithMetadataPageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Acceptance/NativeRegionsTests/Page with Regions Metadata.tpg";
        internal const string EmbeddedMetadataSchemaWebDavUrl = AutoTestParentWebDavUrl + "/401 Automated Test Parent/Building Blocks/Modules/Test/Native Regions/Embedded Metadata Schema (Native Regions).xsd";
        internal const string RegionMetadataSchemaWebDavUrl = AutoTestParentWebDavUrl + "/401 Automated Test Parent/Building Blocks/Modules/Test/Native Regions/Region Schema Test [Main].xsd";


        internal const string BullsEyeComponentWebDavUrl = AutoTestParentWebDavUrl + "/Building Blocks/Content/Images/Large/bulls-eye.jpg";
        internal const string TestComponentWebDavUrl = AutoTestParentWebDavUrl + "/Building Blocks/Content/Test/Test Component.xml";
        internal const string NavConfigurationComponentWebDavUrl = AutoTestParentWebDavUrl + "/Building Blocks/Settings/Core/Site Manager/Navigation Configuration.xml";


        internal const string Tsi2844WebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-2844.tpg";
        internal const string Tsi2844PageWebDavUrl = AutoTestParentWebDavUrl + "/Home/Regression/TSI-2844/TSI-2844 Inherited Page Metadata.tpg";
    }
}
