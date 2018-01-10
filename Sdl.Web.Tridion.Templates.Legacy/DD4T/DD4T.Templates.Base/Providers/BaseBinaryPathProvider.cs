using DD4T.Templates.Base.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamic = DD4T.ContentModel;
using Tridion.ContentManager.Templating;
using System.Text.RegularExpressions;
using Tridion.ContentManager.ContentManagement;
using System.IO;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using DD4T.Templates.Base.Utils;

namespace DD4T.Templates.Base.Providers
{
    /// <summary>
    /// Contains the default implementation of the IBinaryPathProvider. You can extend this class to replace some or all of the functionality with your own.
    /// </summary>
    public abstract class BaseBinaryPathProvider : IBinaryPathProvider
    {
        protected TemplatingLogger log = TemplatingLogger.GetLogger(typeof(BaseBinaryPathProvider));
        protected Engine Engine { get; private set; }
        protected Package Package { get; private set; }
        private TcmUri targetStructureGroupUri;
        private bool stripTcmUrisFromBinaryUrls;

        /// <summary>
        /// Constructor to create a BaseBinaryPathProvider 
        /// </summary>
        /// <param name="engine">The SDL Web publish engine</param>
        /// <param name="package">The SDL Web publish package</param>
        public BaseBinaryPathProvider(Engine engine, Package package)
        {
            Engine = engine;
            Package = package;

            String targetStructureGroupParam = package.GetValue("sg_PublishBinariesTargetStructureGroup");
            if (targetStructureGroupParam != null)
            {
                if (!TcmUri.IsValid(targetStructureGroupParam))
                {
                    log.Error(String.Format("TargetStructureGroup '{0}' is not a valid TCMURI. Exiting template.", targetStructureGroupParam));
                    return;
                }

                Publication publication = TridionUtils.GetPublicationFromContext(package, engine);
                TcmUri localTargetStructureGroupTcmUri = TridionUtils.GetLocalUri(new TcmUri(publication.Id), new TcmUri(targetStructureGroupParam));
                targetStructureGroupUri = new TcmUri(localTargetStructureGroupTcmUri);
                log.Debug($"targetStructureGroupUri = {targetStructureGroupUri.ToString()}");
            }

            String stripTcmUrisFromBinaryUrlsParam = package.GetValue("stripTcmUrisFromBinaryUrls");
            if (stripTcmUrisFromBinaryUrlsParam != null)
            {
                stripTcmUrisFromBinaryUrls = stripTcmUrisFromBinaryUrlsParam.ToLower() == "yes" || stripTcmUrisFromBinaryUrlsParam.ToLower() == "true";
            }
            log.Debug($"stripTcmUrisFromBinaryUrls = {stripTcmUrisFromBinaryUrls}");

        }

        /// <summary>
        /// Default implementation of GetFilename for the DD4T framework.
        /// Looks for a parameter in the template package, if that is not present it returns the magic value to indicate that the default SDL Web publishing logic must be used.
        /// </summary>
        /// <param name="mmComp"></param>
        /// <param name="variantId"></param>
        /// <returns></returns>
        public virtual string GetFilename(Component mmComp, string variantId)
        {
            log.Debug($"Called GetFilename for {mmComp.Title}");
            bool stripTcmUrisFromBinaryUrls = GetStripTcmUrisFromBinaryUrls(mmComp);
            TcmUri targetStructureGroupUri = GetTargetStructureGroupUri(mmComp.Id.ToString());
            log.Debug($"GetFilename found settings: stripTcmUrisFromBinaryUrls {stripTcmUrisFromBinaryUrls} and targetStructureGroupUri {targetStructureGroupUri}");

            // if no target SG is configured, and there is no requirement to strip the TCM uris from the path,
            // we will instruct the BinaryPublisher to use the default SDL Web logic
            if (targetStructureGroupUri == null && !stripTcmUrisFromBinaryUrls)
            {
                log.Debug("no special settings, returning magic value to use default binary path");
                return DefaultBinaryPathProvider.USE_DEFAULT_BINARY_PATH;
            }

            Regex re = new Regex(@"^(.*)\.([^\.]+)$");
            string fileName = mmComp.BinaryContent.Filename;
            if (!String.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileName(fileName);
            }
            if (stripTcmUrisFromBinaryUrls)
            {
                log.Debug("about to return " + fileName);
                return fileName;
            }
            return re.Replace(fileName, string.Format("$1_{0}_{1}.$2", mmComp.Id.ToString().Replace(":", ""), variantId.Replace(":", "")));

        }

        /// <summary>
        /// Default implementation of GetStripTcmUrisFromBinaryUrls for the DD4T framework.
        /// Looks for a parameter in the template package, if that is not present it returns false (TCM Uri is NOT stripped).
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public virtual bool GetStripTcmUrisFromBinaryUrls(Component component)
        {
            return stripTcmUrisFromBinaryUrls;
        }

        /// <summary>
        /// Default implementation of GetTargetStructureGroupUri for the DD4T framework.
        /// Looks for a parameter in the template package, if that is not present it returns null (do NOT use a special target structure group).
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public virtual TcmUri GetTargetStructureGroupUri(string componentUri)
        {
            log.Debug($"Called GetTargetStructureGroupUri with {componentUri}");
            return targetStructureGroupUri;
        }

    }
}
