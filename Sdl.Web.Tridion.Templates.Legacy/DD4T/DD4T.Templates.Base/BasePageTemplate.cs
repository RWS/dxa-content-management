using DD4T.Serialization;
using DD4T.Templates.Base.Utils;
using System;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Dynamic = DD4T.ContentModel;

namespace DD4T.Templates.Base
{
    public abstract class BasePageTemplate : BaseTemplate
    {
        public BasePageTemplate() : base(TemplatingLogger.GetLogger(typeof(BasePageTemplate))) { }
        public BasePageTemplate(TemplatingLogger log) : base(log) { }

      
        public static string VariableNameCalledFromDynamicDelivery = "CalledFromDynamicDelivery";
        public static string VariableValueCalledFromDynamicDelivery = "true";

        /// <summary>
        /// Abstract method to be implemented by a subclass. The method takes a DynamicDelivery page and can add information to it (e.g. by searching in folders / structure groups / linked components, etc
        /// </summary>
        /// <param name="page">DynamicDelivery page</param>
        protected abstract void TransformPage(Dynamic.Page page);

        public override void Transform(Engine engine, Package package)
        {
            GeneralUtils.TimedLog("started Transform");
            Package = package;
            Engine = engine;
            Dynamic.Page page;
            bool hasOutput = HasPackageValue(package, "Output");
            if (hasOutput)
            {
                String inputValue = package.GetValue("Output");
                page = (Dynamic.Page)SerializerService.Deserialize<Dynamic.Page>(inputValue);
            }
            else
            {
                page = GetDynamicPage();
            }

            try
            {
                TransformPage(page);
            }
            catch (StopChainException)
            {
                Log.Debug("caught stopchainexception, will not write current page back to the package");
                return;
            }

            string outputValue = SerializerService.Serialize<Dynamic.Page>(page);

            if (hasOutput)
            {
                Item outputItem = package.GetByName("Output");
                outputItem.SetAsString(outputValue);
                //package.Remove(outputItem);
                //package.PushItem(Package.OutputName, package.CreateStringItem(SerializerService is XmlSerializerService ? ContentType.Xml : ContentType.Text, outputValue));
            }
            else
            {
                package.PushItem(Package.OutputName, package.CreateStringItem(SerializerService is XmlSerializerService ? ContentType.Xml : ContentType.Text, outputValue));
            }

            GeneralUtils.TimedLog("finished Transform");
        }

        public Dynamic.Page GetDynamicPage()
        {
            Item item = Package.GetByName(Package.PageName);
            if (item == null)
            {
                Log.Error("no page found (is this a component template?)");
                return null;
            }

            Page tcmPage = (Page)Engine.GetObject(item);
         
            Dynamic.Page page = Manager.BuildPage(tcmPage, Engine);
            EnsureExtraProperties(page, tcmPage);
            return page;
        }

        protected Page GetTcmPage()
        {
            Item item = Package.GetByName(Package.PageName);
            if (item == null)
            {
                Log.Error("no page found (is this a component template?)");
                return null;
            }

            return (Page)Engine.GetObject(item.GetAsSource().GetValue("ID"));
        }
        private void EnsureExtraProperties(Dynamic.Page page, Page tcmPage)
        {
            // make sure that the Publication, Folder and OwningPublication are always available on the top level
            if (page.Publication == null)
            {
                page.Publication = Manager.BuildPublication(tcmPage.ContextRepository);
            }
            if (page.OwningPublication == null)
            {
                page.OwningPublication = Manager.BuildPublication(tcmPage.OwningRepository);
            }
            if (page.StructureGroup == null)
            {
                page.StructureGroup = Manager.BuildOrganizationalItem((StructureGroup)tcmPage.OrganizationalItem);
            }
        }

    }
}