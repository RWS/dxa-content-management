using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tridion.ContentManager;
using Tridion.ContentManager.ContentManagement;
using Dynamic = DD4T.ContentModel;
using Tridion.ContentManager.Templating;

namespace DD4T.Templates.Base.Contracts
{
    /// <summary>
    /// Defines logic for retrieving a path to which a binary file must be published
    /// </summary>
    public interface IBinaryPathProvider
    {

        /// <summary>
        /// Returns the target structure group URI where the binary should be published to
        /// </summary>
        /// <param name="componentUri">String representing the URI of the multimedia component being published</param>
        /// <returns></returns>
        TcmUri GetTargetStructureGroupUri(string componentUri);
        //string ConstructFileName(Component mmComp, string variantId, bool stripTcmUrisFromBinaryUrls, string targetStructureGroupUri);


        /// <summary>
        /// Returns a filename for an item.
        /// </summary>
        /// <remarks>Based on the properties of the item. The filename property must be set, but
        /// there can be other aspects set on the item that are taken into account in the filename</remarks>
        /// <param name="item"></param>
        /// <returns></returns>

        string GetFilename(Component mmComp, string variantId);
    }
}
