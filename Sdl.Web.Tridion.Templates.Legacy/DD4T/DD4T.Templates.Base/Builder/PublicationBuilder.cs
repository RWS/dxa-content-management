using System;
using System.Collections.Generic;
using System.Text;
using Dynamic = DD4T.ContentModel;
using TComm = Tridion.ContentManager.CommunicationManagement;
using TCM = Tridion.ContentManager.ContentManagement;
using DD4T.Templates.Base.Utils;

namespace DD4T.Templates.Base.Builder
{
    public class PublicationBuilder
    {
        public static Dynamic.Publication BuildPublication(TCM.Repository tcmPublication)
        {
            Dynamic.Publication pub = new Dynamic.Publication();
            pub.Title = tcmPublication.Title;
            pub.Id = tcmPublication.Id.ToString();
            return pub;
        }
    }
}
