﻿using System.IO;
using Sdl.Web.Tridion.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
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

        private class SitemapItem
        {
            public SitemapItem()
            {
                Items = new List<SitemapItem>();
            }

            public string Title { get; set; }

            public string Url { get; set; }

            public string Id { get; set; }
            public string Type { get; set; }
            public List<SitemapItem> Items { get; set; }
            public DateTime? PublishedDate { get; set; }
            public bool Visible { get; set; }
        }

        #endregion Nested Classes

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            _config = GetNavigationConfiguration(GetComponent());

            SitemapItem sitemap = GenerateStructureGroupNavigation(Publication.RootStructureGroup);
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

        private SitemapItem GenerateStructureGroupNavigation(StructureGroup structureGroup)
        {
            SitemapItem result = new SitemapItem
            {
                Id = structureGroup.Id,
                Title = GetNavigationTitle(structureGroup),
                Url = System.Web.HttpUtility.UrlDecode(structureGroup.PublishLocationUrl),
                Type = ItemType.StructureGroup.ToString(),
                Visible = IsVisible(structureGroup.Title)
            };

            foreach (RepositoryLocalObject item in structureGroup.GetItems().Where(i => !i.Title.StartsWith("_")).OrderBy(i => i.Title))
            {
                SitemapItem childSitemapItem;
                Page page = item as Page;
                if (page != null)
                {
                    if (!IsPublished(page))
                    {
                        continue;
                    }

                    childSitemapItem = new SitemapItem
                    {
                        Id = page.Id,
                        Title = GetNavigationTitle(page),
                        Url = GetUrl(page),
                        Type = ItemType.Page.ToString(),
                        PublishedDate = GetPublishedDate(page, Engine.PublishingContext.TargetType),
                        Visible = IsVisible(page.Title)
                    };
                }
                else
                {
                    childSitemapItem = GenerateStructureGroupNavigation((StructureGroup)item);
                }

                result.Items.Add(childSitemapItem);
            }
            return result;
        }

        protected string GetNavigationTitle(StructureGroup sg)
        {
            return StripPrefix(sg.Title);
        }

        protected string StripPrefix(string title)
        {
            return Regex.Replace(title, @"^\d{3}\s", string.Empty);
        }

        private static DateTime? GetPublishedDate(Page page, TargetType targetType)
        {
            PublishInfo publishInfo = PublishEngine.GetPublishInfo(page)
                                                .FirstOrDefault(pi => pi.TargetType == targetType && pi.Publication.Id.ItemId == page.Id.PublicationId);
            if (publishInfo == null)
            {
                return null;
            }
            return publishInfo.PublishedAt;
        }

        private string GetNavigationTitle(Page page)
        {
            string title = null;
            if (_config.NavType == NavigationType.Localizable)
            {
                title = GetNavTextFromPageComponents(page);
            }
            return string.IsNullOrEmpty(title) ? StripPrefix(page.Title) : title;
        }

        private string GetNavTextFromPageComponents(Page page)
        {
            string title = null;
            foreach (TcmComponentPresentation cp in page.ComponentPresentations)
            {
                title = GetNavTitleFromComponent(cp.Component);
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }
            return title;
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

        private static string GetNavTitleFromField(string fieldname, IEnumerable<XmlElement> data)
        {
            string xpath = GetXPathFromFieldName(fieldname);
            foreach (XmlElement fieldData in data)
            {
                XmlNode field = fieldData.SelectSingleNode(xpath);
                if (field != null)
                {
                    return field.InnerText;
                }
            }
            return null;
        }

        private static string GetXPathFromFieldName(string fieldname)
        {
            string[] bits = fieldname.Split('/');
            return "//" + String.Join("/", bits.Select(f => String.Format("*[local-name()='{0}']", f)));
        }

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
            //For preview we always return true - to help debugging
            return Engine.PublishingContext.PublicationTarget == null || PublishEngine.IsPublished(page, Engine.PublishingContext.PublicationTarget);
        }

        private static bool IsVisible(string title)
        {
            Match match = Regex.Match(title, @"^\d{3}\s");
            return match.Success;
        }
    }
}