using System;
using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    public class PageTemplateData
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string FileExtension { get; set; }

        public DateTime RevisionDate { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
