using Tridion.Web.UI.Core.Controls;
using Tridion.Web.UI.Editors.Base.Views.Popups;
using Tridion.Web.UI.Core;



namespace DXA.CM.Extensions.DXAResolver.Editors.Views.Popups
{
    [ControlResources("DXA.CM.Extensions.DXAResolver.Editors.Views.Popups.Settings")]

    public class Settings : PopupView
    {
        public string ThemePath
        {
            get
            {
                return Utils.getThemePathFromEditor("DXAResolver");
            }
        }
    }
}