using System.Collections.Generic;
using Dynamic = DD4T.ContentModel;
using Tridion.ContentManager.ContentManagement;
using System;

namespace DD4T.Templates.Base.Builder
{
    public class KeywordBuilder
    {
        public static Dynamic.Keyword BuildKeyword(Keyword keyword, BuildManager buildManager)
        {
            return BuildKeyword(keyword, 2, buildManager);
        }

        public static Dynamic.Keyword BuildKeyword(Keyword keyword, int currentLinkLevel, BuildManager buildManager)
        {
            Dynamic.Keyword dk = new Dynamic.Keyword();
            dk.Id = keyword.Id;
            dk.Title = keyword.Title;
            dk.Path = FindKeywordPath(keyword);
            dk.Description = keyword.Description;
            dk.Key = keyword.Key;
            dk.TaxonomyId = keyword.OrganizationalItem.Id;

            if (currentLinkLevel > 0)
            {
                if (keyword.Metadata != null && keyword.MetadataSchema != null)
                {
                    var tcmMetadataFields = new Tridion.ContentManager.ContentManagement.Fields.ItemFields(keyword.Metadata, keyword.MetadataSchema);
                    dk.MetadataFields = buildManager.BuildFields(tcmMetadataFields, currentLinkLevel);
                }
            }
            return dk;
        }

        private static string FindKeywordPath(Keyword keyword)
        {
            IList<Keyword> parentKeywords = keyword.ParentKeywords;
            string path = @"\" + keyword.Title;
            while (parentKeywords.Count > 0)
            {
                path = @"\" + parentKeywords[0].Title + path;
                parentKeywords = parentKeywords[0].ParentKeywords;
            }
            return @"\" + keyword.OrganizationalItem.Title + path;
        }
    }
}
