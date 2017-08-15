using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tridion.ContentManager.Templating.Assembly;
using Tridion.ContentManager.CommunicationManagement;
using TCM = Tridion.ContentManager.ContentManagement;
using Dynamic = DD4T.ContentModel;
using DD4T.Templates.Base;
using DD4T.Templates.Base.Builder;
using DD4T.Templates.Base.Utils;

namespace DD4T.Templates
{
    /// <summary>
    /// Adds metadata of the structrue group containing the page, or one of its parents, to the current page
    /// </summary>
    [TcmTemplateTitle("Add inherited metadata to page")]
    [TcmTemplateParameterSchema("resource:DD4T.Templates.Resources.Schemas.Dynamic Delivery Parameters.xsd")]
    public partial class InheritMetadataPage : BasePageTemplate
    {

        protected override void TransformPage(Dynamic.Page page)
        {
            GeneralUtils.TimedLog("start TransformPage with id " + page.Id);

            Page tcmPage = this.GetTcmPage();
            StructureGroup tcmSG = (StructureGroup)tcmPage.OrganizationalItem;
            String mergeActionStr = Package.GetValue("MergeAction");

            while (tcmSG != null)
            {

                if (tcmSG.MetadataSchema != null)
                {
                    TCM.Fields.ItemFields tcmFields = new TCM.Fields.ItemFields(tcmSG.Metadata, tcmSG.MetadataSchema);
                    FieldsBuilder.AddFields(page.MetadataFields, tcmFields, Manager);
                }
                tcmSG = tcmSG.OrganizationalItem as StructureGroup;
            }
            GeneralUtils.TimedLog("finished TransformPage");
        }
    }
}
