Tridion.Type.registerNamespace("DXA.CM.Extensions.CustomResolver.Views.Popups.Settings");

DXA.CM.Extensions.CustomResolver.Views.Popups.Settings = function Settings() {
    Tridion.OO.enableInterface(this, "DXA.CM.Extensions.CustomResolver.Views.Popups.Settings");

    this.addInterface("Tridion.Controls.ModalPopupView");
    this.addInterface("Tridion.Web.UI.Editors.Base.Views.ViewBase");

    var p = this.properties;

    p.buttons = {
        close: null,
        save: null
    }
};

DXA.CM.Extensions.CustomResolver.Views.Popups.Settings.prototype.initialize = function Settings$initialize()
{
    this.callBase("Tridion.Web.UI.Editors.Base.Views.ViewBase", "initialize");

    var p = this.properties;

    p.buttons.close = $controls.getControl($("#BtnSave"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.close, "click", this.getDelegate(this.onCloseClick));

    p.buttons.save = $controls.getControl($("#BtnSave"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.save, "click", this.getDelegate(this.onSaveClick));
};

DXA.CM.Extensions.CustomResolver.Views.Popups.Settings.prototype.disposeInterface = Tridion.OO.nonInheritable(function Settings$disposeInterface(){

});

DXA.CM.Extensions.CustomResolver.Views.Popups.Settings.prototype.onCloseClick = function Settings$onCloseClick()
{
    this.fireEvent("close");
    window.close();
};

DXA.CM.Extensions.CustomResolver.Views.Popups.Settings.prototype.onSaveClick = function Settings$onSaveClick()
{
    this.fireEvent("save");

    // Save logic
};

$display.registerView(DXA.CM.Extensions.CustomResolver.Views.Popups.Settings);