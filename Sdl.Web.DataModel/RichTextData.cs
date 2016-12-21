using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Sdl.Web.DataModel
{
    public class RichTextData : ModelData
    {
        public List<object> Fragments { get; set; }

        #region Overrides
        protected override void Initialize(JObject jObject)
        {
            IEnumerable<JToken> fragments = jObject.Property("Fragments")?.Values();
            if (fragments != null)
            {
                Fragments = fragments.Select(f => f.GetStronglyTypedValue()).ToList();
            }
        }
        #endregion

    }
}
