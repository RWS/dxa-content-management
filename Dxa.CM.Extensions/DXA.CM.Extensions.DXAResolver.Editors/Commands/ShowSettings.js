Tridion.Type.registerNamespace("DXA.CM.Extensions.DXAResolver.Editors.Commands");

DXA.CM.Extensions.DXAResolver.Editors.Commands.ShowSettings = function Commands$ShowSettings(name, action) {
    Tridion.OO.enableInterface(this, "DXA.CM.Extensions.DXAResolver.Editors.Commands.ShowSettings");
    this.addInterface("Tridion.Web.UI.Editors.CME.Commands.Navigation.Open", ["CRShowSettings", $const.AllowedActions.View]);

    this.properties.popup = null;
};

DXA.CM.Extensions.DXAResolver.Editors.Commands.ShowSettings.prototype._isAvailable = function ShowSettings$_isAvailable(selection) {
    // always enabled
    return true;
};

DXA.CM.Extensions.DXAResolver.Editors.Commands.ShowSettings.prototype._isEnabled = function ShowSettings$_isEnabled(selection) {
    // always enabled
    return true;
};

DXA.CM.Extensions.DXAResolver.Editors.Commands.ShowSettings.prototype._execute = function Delete$_execute(selection, pipeline) {
    var p = this.properties;

    // Popup management
    if (p.popup)
    {
        p.popup.focus();
    }
    else
    {
        p.popup = $popupManager.createExternalContentPopup(
            Tridion.Web.UI.Editors.CME.Constants.Popups.CUSTOM_RESOLVER_SETTINGS.URL,
            Tridion.Web.UI.Editors.CME.Constants.Popups.CUSTOM_RESOLVER_SETTINGS.FEATURES
        );

        $evt.addEventHandler(p.popup, "unload",
            function ShowSettings$_execute$_unload(event)
            {
                if (p.popup)
                {
                    p.popup.dispose();
                    p.popup = null;
                }
            });
        p.popup.open();
    }
};
