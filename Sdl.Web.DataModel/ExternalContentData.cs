using System.Collections.Generic;

namespace Sdl.Web.DataModel
{
    public class ExternalContentData
    {
        public string Id { get; set; }
        public string DisplayTypeId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } 
    }
}
