using System;
using System.IO;
using System.Xml;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using DD4T.Templates.Base.Xml;
using System.Text.RegularExpressions;
using DD4T.Templates.Base.Builder;
using Dynamic = DD4T.ContentModel;
using DD4T.Templates.Base.Contracts;
using System.Reflection;
using System.Linq;
using DD4T.Templates.Base.Providers;

namespace DD4T.Templates.Base.Utils
{
    public class BinaryPublisher
    {
        protected static TemplatingLogger log = TemplatingLogger.GetLogger(typeof(BinaryPublisher));
        protected Package package;
        protected Engine engine;
        Template currentTemplate;
        private IBinaryPathProvider binaryPathProvider;

        private const string EclMimeType = "application/externalcontentlibrary";

        public BinaryPublisher(Package package, Engine engine)
        {
            this.package = package;
            this.engine = engine;

            Init();
        }

        [Obsolete("Please use the constructor BinaryPublisher(Package,Engine)")]
        public BinaryPublisher(Package package, Engine engine, string targetStructureGroup)
        {

            this.package = package;
            this.engine = engine;

            Init();
        }

        private void Init()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            currentTemplate = engine.PublishingContext.ResolvedItem.Template;
            BuildProperties buildProperties = new BuildProperties(package);

            if (!string.IsNullOrWhiteSpace(buildProperties.BinaryPathProvider))
            {
                log.Debug($"Found class to override the binary path provider: {buildProperties.BinaryPathProvider}");

                if (buildProperties.BinaryPathProvider.Contains("|"))
                {
                    var t = buildProperties.BinaryPathProvider.Split('|').Select(a => a.Trim()).ToArray<string>();
                    log.Debug("Assembly:" + t[0]);
                    log.Debug("Class:" + t[1]);
                    object[] parameters = new object[2] { engine, package };
                    binaryPathProvider = (IBinaryPathProvider)Activator.CreateInstance(t[0], t[1], parameters).Unwrap();
                }
                else
                {
                    var type = Type.GetType(buildProperties.BinaryPathProvider);
                    if (type == null)
                    {
                        log.Warning($"Could not find class {buildProperties.BinaryPathProvider}");
                        binaryPathProvider = null;
                    }
                    else
                    {
                        log.Debug($"Found type {type.FullName}");
                        binaryPathProvider = (IBinaryPathProvider)Activator.CreateInstance(type, engine, package);
                        log.Debug($"Instantiated class {binaryPathProvider.GetType().FullName}");
                    }
                }
            }
            if (binaryPathProvider == null)
            {
                binaryPathProvider = new DefaultBinaryPathProvider(engine, package);
            }



        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            log.Debug($"called CurrentDomain_AssemblyResolve for {sender} and {args.Name}");
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                log.Debug($"Found assembly {ass.FullName}");
                if (ass.FullName.Contains("DD4T.Templates.Merged"))
                {
                    log.Debug("FOUND!");
                    return ass;
                }
            }
            return null;
        }


        #region Protected Members

        public virtual string PublishBinariesInRichTextField(string xhtml, BuildProperties buildProperties)
        {
            // rich text field is well-formed XML (XHTML), except that it does not always have a root element
            // to be sure it can be parsed, we will add a root and remove it afterwards
            TridionXml xml = new TridionXml();
            xml.LoadXml("<tmproot>" + xhtml + "</tmproot>");

            foreach (XmlElement xlinkElement in xml.SelectNodes("//*[@xlink:href[starts-with(string(.),'tcm:')]]", xml.NamespaceManager))
            {
                log.Debug("Found XLink in Rich Text: " + xlinkElement.OuterXml);
                ProcessRichTextXlink(xlinkElement, buildProperties);
            } 

            return xml.DocumentElement.InnerXml;
        }

        public virtual string PublishMultimediaComponent(string uri, BuildProperties buildProperties)
        {
            string itemName = "PublishMultimedia_" + uri;
            TcmUri tcmuri = new TcmUri(uri);
            Item mmItem = package.GetByName(itemName);
            if (mmItem == null)
            {
                mmItem = package.CreateMultimediaItem(tcmuri);
                package.PushItem(itemName, mmItem);
                log.Debug(string.Format("Image {0} ({1}) unique, adding to package", itemName, uri));
                if (!mmItem.Properties.ContainsKey(Item.ItemPropertyPublishedPath))
                {
                    log.Debug(string.Format("Publish Image {0} ({1}).", itemName, uri));
                    PublishItem(mmItem, tcmuri);
                }
            }
            else
            {
                log.Debug(string.Format("Image {0} ({1}) already present in package, not adding again", itemName, tcmuri));
            }
            return GetReferencePath(mmItem, uri);
        }


        /// <summary>
        /// Publishes the Binary Data of a Multimedia Component and sets it Multimedia URL (and ExtensionData for ECL).
        /// </summary>
        /// <param name="mmComponent">The (DD4T) Multimedia Component to Publish.</param>
        /// <param name="buildProperties">The DD4T Build Properties</param>
        public void PublishMultimediaComponent(Dynamic.Component mmComponent, BuildProperties buildProperties)
        {
            Dynamic.Multimedia multimedia = mmComponent.Multimedia;
            if (multimedia == null)
            {
                log.Warning("PublishMultimediaComponent called with a non-Multimedia Component: " + mmComponent.Id);
                return;
            }
            if (multimedia.MimeType == EclMimeType && buildProperties.ECLEnabled && mmComponent.EclId == null)
            {
                using (EclProcessor eclProcessor = new EclProcessor(engine, binaryPathProvider.GetTargetStructureGroupUri(mmComponent.Id)))
                {
                    eclProcessor.ProcessEclStubComponent(mmComponent);
                }
            }
            else if (mmComponent.EclId != null)
            {
                log.Debug(string.Format("ECL Stub Component '{0}' has already been processed (ECL ID: '{1}') ", mmComponent.Id, mmComponent.EclId));
            }
            else
            {
                multimedia.Url = PublishMultimediaComponent(mmComponent.Id, buildProperties);
            }
        }

        private void ProcessRichTextXlink(XmlElement xlinkElement, BuildProperties buildProperties)
        {
            const string xlinkNamespaceUri = "http://www.w3.org/1999/xlink";

            string xlinkHref = xlinkElement.GetAttribute("href", xlinkNamespaceUri);
            if (string.IsNullOrEmpty(xlinkHref))
            {
                log.Warning("No xlink:href found: " + xlinkElement.OuterXml);
                return;
            }

            Component component = engine.GetObject(xlinkHref) as Component;
            if (component == null || component.BinaryContent == null)
            {
                // XLink doesn't refer to MM Component; do nothing.
                return;
            }
            log.Debug("Processing XLink to Multimedia Component: " + component.Id);

            BinaryContent binaryContent = component.BinaryContent;
            MultimediaType multimediaType = binaryContent.MultimediaType;

            string url;
            if (multimediaType.MimeType == EclMimeType && buildProperties.ECLEnabled)
            {
                using (EclProcessor eclProcessor = new EclProcessor(engine, binaryPathProvider.GetTargetStructureGroupUri(component.Id)))
                {
                    url = eclProcessor.ProcessEclXlink(xlinkElement);
                }
            }
            else
            {
                url = PublishMultimediaComponent(component.Id, buildProperties);
            }

            // Put the resolved URL (path) in an appropriate XHTML attribute
            string attrName = (xlinkElement.LocalName == "img") ? "src" : "href"; // Note that XHTML is case-sensitive, so case-sensitive comparison is OK.
            xlinkElement.SetAttribute(attrName, url);

            // NOTE: intentionally not removing xlink:href attribute to keep the MM Component ID available for post-processing purposes (e.g. DXA).

            log.Debug(string.Format("XLink to Multimedia Component '{0}' resolved to: {1}", component.Id, xlinkElement.OuterXml));
        }


        /// <summary>
        /// Return the reference path for the binary which has just been published. This path is stored in the XML which is published to the broker, and may be used in 
        /// the presentation engine to retrieve the binary. In this implementation, the reference path is the same as the publish path, but the method can be overridden 
        /// to implement other logic. It could (for example) return the path to the binary through a CDN.
        /// </summary>
        /// <param name="item">The templating Item containing the multimedia component (including the publish path)</param>
        /// <param name="uri">The uri of the multimedia component</param>
        /// <returns>The reference path that will be stored in the XML</returns>
        protected virtual string GetReferencePath(Item item, string uri)
        {
            return item.Properties[Item.ItemPropertyPublishedPath];
        }

        protected virtual void PublishItem(Item item, TcmUri itemUri)
        {

            log.Debug($"PublishItem called on {itemUri}");

            Stream itemStream = null;
            // See if some template set itself as the applied template on this item
            TcmUri appliedTemplateUri = null;
            if (item.Properties.ContainsKey(Item.ItemPropertyTemplateUri))
            {
                appliedTemplateUri = new TcmUri(item.Properties[Item.ItemPropertyTemplateUri]);
            }
            Component mmComp = (Component)engine.GetObject(item.Properties[Item.ItemPropertyTcmUri]);
            string fileName = binaryPathProvider.GetFilename(mmComp, currentTemplate.Id);
            
            try
            {
                string publishedPath;
                if (fileName == DefaultBinaryPathProvider.USE_DEFAULT_BINARY_PATH)
                {
                    log.Debug("no structure group defined, publishing binary with default settings");
                    // Note: it is dangerous to specify the CT URI as variant ID without a structure group, because it will fail if you publish the same MMC from two or more CTs!
                    // So I removed the variant ID altogether (QS, 20-10-2011)
                    log.Debug(string.Format("publishing mm component {0} without variant id", mmComp.Id));
                    Binary binary = engine.PublishingContext.RenderedItem.AddBinary(mmComp);
                    publishedPath = binary.Url;
                    log.Debug(string.Format("binary is published to url {0}", publishedPath));
                }
                else
                {
                    string targetSGuri = binaryPathProvider.GetTargetStructureGroupUri(mmComp.Id.ToString());
                    StructureGroup targetSG = null;
                    if (targetSGuri!= null)
                    {
                        targetSG = (StructureGroup)engine.GetObject(targetSGuri);
                    }

                    itemStream = item.GetAsStream();
                    if (itemStream == null)
                    {
                        // All items can be converted to a stream?
                        log.Error(String.Format("Cannot get item '{0}' as stream", itemUri.ToString()));
                    }
                    Binary b;
                    if (targetSG == null)
                    {
                        log.Debug(string.Format("publishing mm component {0} with variant id {1} and filename {2}", mmComp.Id, currentTemplate.Id, fileName));
                        b = engine.PublishingContext.RenderedItem.AddBinary(itemStream, fileName, currentTemplate.Id, mmComp, mmComp.BinaryContent.MultimediaType.MimeType);
                    }
                    else
                    {
                        log.Debug(string.Format("publishing mm component {0} to structure group {1} with variant id {2} and filename {3}", mmComp.Id, targetSGuri, currentTemplate.Id, fileName));
                        b = engine.PublishingContext.RenderedItem.AddBinary(itemStream, fileName, targetSG, currentTemplate.Id, mmComp, mmComp.BinaryContent.MultimediaType.MimeType);
                    }
                    publishedPath = b.Url;
                    log.Debug(string.Format("binary is published to url {0}", publishedPath));
                }
                log.Debug("binary published, published path = " + publishedPath);
                item.Properties[Item.ItemPropertyPublishedPath] = publishedPath;
            }
            finally
            {
                if (itemStream != null) itemStream.Close();
            }
        }

        #endregion

    }

}