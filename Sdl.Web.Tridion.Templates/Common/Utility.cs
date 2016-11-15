using System;
using System.Text.RegularExpressions;
using System.Xml;
using Tridion;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.Templating;

namespace Sdl.Web.Tridion.Common
{
    /// <summary>
    /// Static Helper methods
    /// </summary>
    public static class Utility
    {
        public const string SiteEditApplicationId = "SiteEdit";

        public static string GetKeyFromTaxonomy(Category taxonomy)
        {
            string key = taxonomy.XmlName;
            return key.Substring(0, 1).ToLower() + key.Substring(1);
        }

        public static string GetKeyFromTemplate(ComponentTemplate template)
        {
            string key = Regex.Replace(template.Title, @"[\[\]\s\.]", "");
            return key.Substring(0, 1).ToLower() + key.Substring(1);
        }

        public static string GetKeyFromSchema(Schema schema)
        {
            string key = schema.RootElementName;
            if (String.IsNullOrEmpty(key))
            {
                key = Regex.Replace(schema.Title.Trim(), @"[^A-Za-z0-9.]+", "");
            }
            return key.Substring(0, 1).ToLower() + key.Substring(1);
        }


        public static bool IsXpmEnabled(PublishingContext publishingContext)
        {
            if (publishingContext == null || publishingContext.PublicationTarget == null)
            {
                return false;
            }

            TargetType targetType = publishingContext.TargetType;
            if (targetType != null && targetType.BusinessProcessType != null)
            {
                // New-style publishing
                Publication contextPublication = (Publication) ((RepositoryLocalObject) publishingContext.ResolvedItem.Item).ContextRepository;
                return targetType.IsPreviewCapable(contextPublication);
            }

            return IsPublicationTargetXpmEnabled(publishingContext.PublicationTarget);
        }

        public static bool IsPublicationTargetXpmEnabled(PublicationTarget publicationTarget)
        {
            if (publicationTarget == null)
            {
                return false;
            }

            ApplicationData appData = publicationTarget.LoadApplicationData(SiteEditApplicationId);
            if (appData == null)
            {
                return false;
            }

            ApplicationDataAdapter ada = new ApplicationDataAdapter(appData);
            XmlElement appDataXml = ada.GetAs<XmlElement>();
            if (appDataXml == null)
            {
                return false;
            }

            return (appDataXml.SelectSingleNode("self::se:configuration/se:PublicationTarget[se:EnableSiteEdit = 'true']", GetSeNamespaceManager()) != null);
        }

        public static string GetCdEnvironmentPurpose(PublishingContext publishingContext)
        {
            TargetType targetType = publishingContext.TargetType;
            if (targetType == null || targetType.BusinessProcessType == null)
            {
                // Template Debugger, CM Preview or old-style publishing
                return null;
            }

            // New-style publishing
            return targetType.Purpose;
        }

        private static XmlNamespaceManager _ns;
        private static XmlNamespaceManager GetSeNamespaceManager()
        {
            if (_ns == null)
            {
                _ns = new XmlNamespaceManager(new NameTable());
                _ns.AddNamespace("se", "http://www.sdltridion.com/2011/SiteEdit");
                _ns.AddNamespace(Constants.XlinkPrefix, Constants.XlinkNamespace);
            }
            return _ns;
        }
    }
}
