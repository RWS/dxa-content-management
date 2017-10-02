using System.Collections.Generic;
using DD4T.ContentModel;
using DD4T.Templates.Base;
using DD4T.Templates.Base.Builder;
using Tridion.ContentManager.AudienceManagement;
using Tridion.ContentManager.Templating.Assembly;
using Dynamic = DD4T.ContentModel;
using Tcm = Tridion.ContentManager.AudienceManagement;

namespace DD4T.Templates
{
    ///<summary>
    /// AddTargetGroups is responsible for adding the Target Group information to the DD4T Page Xml
    /// Author: Robert Stevenson-Leggett
    ///</summary>
    [TcmTemplateTitle("Add Target Groups")]
    public class AddTargetGroups : BasePageTemplate
    {
        protected override void TransformPage(Page page)
        {
            Tridion.ContentManager.CommunicationManagement.Page tcmPage = GetTcmPage();

            int count = 0;
            foreach (var componentPresentation in tcmPage.ComponentPresentations)
            {
                if(componentPresentation.Conditions != null && componentPresentation.Conditions.Count > 0)
                {
                    page.ComponentPresentations[count].Conditions = TargetGroupBuilder.MapTargetGroupConditions(componentPresentation.Conditions, new BuildManager(this.Package, this.Engine)); 
                }
                count += 1;
            }
        }
    }
}