using System.IO;
using Sdl.Web.Tridion.Templates.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Sdl.Web.DataModel;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.CommunicationManagement.Regions;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing;
using Tridion.ContentManager.Templating.Assembly;
using Tridion.ContentManager.Templating;
using TcmComponentPresentation = Tridion.ContentManager.CommunicationManagement.ComponentPresentation;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Generates Sitemap JSON based on Structure Groups (for Static Navigation). 
    /// </summary>
    /// <remarks>
    /// Should be used in a Component Template.
    /// </remarks>
    [TcmTemplateTitle("Generate Sitemap")]
    public class GenerateSiteMap : TemplateBase
    {
        private NavigationConfig _config;

        #region Nested Classes
        private enum NavigationType
        {
            Simple,
            Localizable
        }

        private class NavigationConfig
        {
            public List<string> NavTextFieldPaths { get; set; }
            public NavigationType NavType { get; set; }
            public string ExternalUrlTemplate { get; set; }
        }
        #endregion

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            _config = GetNavigationConfiguration(GetComponent());

            SitemapItemData sitemap = GenerateStructureGroupNavigation(Publication.RootStructureGroup);
            string sitemapJson = JsonSerialize(sitemap, IsPreview);

            package.PushItem(Package.OutputName, package.CreateStringItem(ContentType.Text, sitemapJson));
        }

        private static NavigationConfig GetNavigationConfiguration(Component navConfigComponent)
        {
            NavigationConfig result = new NavigationConfig { NavType = NavigationType.Simple };
            if (navConfigComponent.Metadata == null)
            {
                return result;
            }

            ItemFields navConfigComponentMetadataFields = new ItemFields(navConfigComponent.Metadata, navConfigComponent.MetadataSchema);
            Keyword type = navConfigComponentMetadataFields.GetKeywordValue("navigationType");
            switch (type.Key.ToLower())
            {
                case "localizable":
                    result.NavType = NavigationType.Localizable;
                    break;
            }
            string navTextFields = navConfigComponentMetadataFields.GetSingleFieldValue("navigationTextFieldPaths");
            if (!string.IsNullOrEmpty(navTextFields))
            {
                result.NavTextFieldPaths = navTextFields.Split(',').Select(s => s.Trim()).ToList();
            }
            result.ExternalUrlTemplate = navConfigComponentMetadataFields.GetSingleFieldValue("externalLinkTemplate");
            return result;
        }

        private SitemapItemData GenerateStructureGroupNavigation(StructureGroup structureGroup)
        {
            SitemapItemData result = new SitemapItemData
            {
                Id = structureGroup.Id,
                Title = GetNavigationTitle(structureGroup),
                Url = System.Web.HttpUtility.UrlDecode(structureGroup.PublishLocationUrl),
                Type = ItemType.StructureGroup.ToString(),
                Visible = IsVisible(structureGroup.Title)
            };

            foreach (RepositoryLocalObject item in structureGroup.GetItems().Where(i => !i.Title.StartsWith("_")).OrderBy(i => i.Title))
            {
                SitemapItemData childSitemapItem;
                Page page = item as Page;
                if (page != null)
                {
                    if (!IsPublished(page))
                    {
                        continue;
                    }

                    childSitemapItem = new SitemapItemData
                    {
                        Id = page.Id,
                        Title = GetNavigationTitle(page),
                        Url = GetUrl(page),
                        Type = ItemType.Page.ToString(),
                        PublishedDate = GetPublishedDate(page, Engine.PublishingContext?.TargetType),
                        Visible = IsVisible(page.Title)
                    };
                }
                else
                {
                    childSitemapItem = GenerateStructureGroupNavigation((StructureGroup) item);
                }

                result.Items.Add(childSitemapItem);
            }
            return result;
        }
      
        protected string StripPrefix(string title) => Regex.Replace(title, @"^\d{3}\s", string.Empty);

        private static DateTime? GetPublishedDate(Page page, TargetType targetType )
        {
            PublishInfo publishInfo = PublishEngine.GetPublishInfo(page).FirstOrDefault(pi => pi.TargetType == targetType);
            return publishInfo?.PublishedAt;
        }

        protected string GetNavigationTitle(StructureGroup sg)
        {
            string title = null;
            if (_config.NavType == NavigationType.Localizable)
            {
                title = GetNavTitleFromStructureGroup(sg);
            }
            return string.IsNullOrEmpty(title) ? StripPrefix(sg.Title) : title;
        }

        private string GetNavigationTitle(Page page)
        {
            string title = null;
            if (_config.NavType == NavigationType.Localizable)
            {
                title = GetNavTitleFromPageComponents(page);
            }
            return string.IsNullOrEmpty(title) ? StripPrefix(page.Title) : title;
        }

        private string GetNavTitleFromPageComponents(Page page)
        {
            string title = null;
            List<TcmComponentPresentation> cps = new List<TcmComponentPresentation>();
            GetComponentPresentationsFromRegion(ref cps, page);

            foreach (TcmComponentPresentation cp in cps)
            {
                title = GetNavTitleFromComponent(cp.Component);
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }
            return title;
        }

        private string GetNavTitleFromStructureGroup(StructureGroup sg)
        {
            string title = null;
            if (sg.Metadata != null)
            {
                title = GetNavTitleFromData(new List<XmlElement> { sg.Metadata });
            }
            return title;
        }

        private string GetNavTitleFromData(List<XmlElement> data)
        {
            foreach (string fieldname in _config.NavTextFieldPaths)
            {
                string title = GetNavTitleFromField(fieldname, data);
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }
            return null;
        }

        private void GetComponentPresentationsFromRegion(ref List<TcmComponentPresentation> cps, IRegion region)
        {
            cps.AddRange(region.ComponentPresentations.ToList());
            foreach (IRegion nestedRegion in region.Regions)
            {
                GetComponentPresentationsFromRegion(ref cps, nestedRegion);
            }
        }

        private string GetNavTitleFromComponent(Component component)
        {
            List<XmlElement> data = new List<XmlElement>();
            if (component.Content != null)
            {
                data.Add(component.Content);
            }
            if (component.Metadata != null)
            {
                data.Add(component.Metadata);
            }

            return GetNavTitleFromData(data);          
        }

        private static string GetNavTitleFromField(string fieldname, IEnumerable<XmlElement> data) 
            => data.Select(fieldData => fieldData.GetTextFieldValue(fieldname)).FirstOrDefault(title => !string.IsNullOrEmpty(title));      

        protected string GetUrl(Page page)
        {
            string url;
            if (page.PageTemplate.Title.Equals(_config.ExternalUrlTemplate, StringComparison.InvariantCultureIgnoreCase) && page.Metadata != null)
            {
                // The Page is a "Redirect Page"; obtain the URL from its metadata.
                ItemFields meta = new ItemFields(page.Metadata, page.MetadataSchema);
                ItemFields link = meta.GetEmbeddedField("redirect");
                url = link.GetExternalLink("externalLink");
                if (string.IsNullOrEmpty(url))
                {
                    url = link.GetSingleFieldValue("internalLink");
                }
            }
            else
            {
                url = GetExtensionlessUrl(page.PublishLocationUrl);
            }
            return url;
        }

        private static string GetExtensionlessUrl(string url)
        {
            string extension = Path.GetExtension(url);
            return string.IsNullOrEmpty(extension) ? url : url.Substring(0, url.Length - extension.Length);
        }

        private bool IsPublished(Page page)
        {
            if (Engine.PublishingContext?.PublicationTarget != null)
            {
                return PublishEngine.IsPublished(page, Engine.PublishingContext.PublicationTarget, true);
            }
            if (Engine.PublishingContext?.TargetType != null)
            {
                return PublishEngine.IsPublished(page, Engine.PublishingContext.TargetType, true);
            }
            //For preview we always return true - to help debugging
            return true;
        }

        private static bool IsVisible(string title) => Regex.Match(title, @"^\d{3}\s").Success;
    }
}

