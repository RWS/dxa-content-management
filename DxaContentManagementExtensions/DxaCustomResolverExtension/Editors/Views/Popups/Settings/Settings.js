Tridion.Type.registerNamespace("DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings");

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings = function Settings() {
    Tridion.OO.enableInterface(this, "DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings");

    this.addInterface("Tridion.Controls.ModalPopupView");
    this.addInterface("Tridion.Web.UI.Editors.Base.Views.ViewBase");

    var p = this.properties;

    p.buttons = {
        close: null,
        save: null
    };
    p.fields = {};

    this.initializeControls();
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.initializeControls = function Settings$initializeControls() {
    var p = this.properties;

    p.buttons.close = $controls.getControl($("#BtnClose"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.close, "click", this.getDelegate(this.onCloseClick));

    p.buttons.save = $controls.getControl($("#BtnSave"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.save, "click", this.getDelegate(this.onSaveClick));

    p.fields.recurseDepth = $("#cr-recurse-depth");
    $evt.addEventHandler(p.fields.recurseDepth, "valuepropertychange", this.getDelegate(this.onFieldChange));
    $evt.addEventHandler(p.fields.recurseDepth, "valuepropertychange", this.getDelegate(this.onFieldChange));

    p.fields.recurseDepth && p.fields.recurseDepth.focus();

};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.initialize = function Settings$initialize() {
    this.callBase("Tridion.Web.UI.Editors.Base.Views.ViewBase", "initialize");
    var item = this.getItem();

    if (item) {
        var onLoad = this.getDelegate(this.onItemLoaded);
        if(!item.isLoaded()) {
            $evt.addEventHandler(item, "load", onLoad);
            $evt.addEventHandler(item, "save", this.getDelegate(this.onItemSaved));
            $evt.addEventHandler(item, "savefailed", this.getDelegate(this.onItemSaveFailed));

            item.load()
        } else {
            onLoad()
        }

    }


};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemLoaded = function Settings$onItemLoaded() {
    var item = this.getItem();
    var p = this.properties;

    if (item && item.isLoaded()) {
        var fields = p.fields;
        fields.recurseDepth.value = item.getRecurseDepth();
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemSaved = function Settings$onItemLoaded() {
    $messages.registerNotification("Custom Resolver: Configuration has been saved.")
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemSaveFailed = function Settings$onItemSaveFailed() {
    $messages.registerError("Custom Resolver: Couldn't save configuration.")
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.getItem = function Settings$getItem() {
    return $models.$dcr.getItem(DXA.CM.Extensions.CustomResolver.Models.Constants.CUSTOM_RESOLVER_CONFIGURATION_ID);
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onCloseClick = function Settings$onCloseClick() {
    this.fireEvent("close");
    window.close();
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onFieldChange = function Settings$onFieldChange(event) {
    var item = this.getItem();
    var p = this.properties;

    if(item) {
        var targetField = event && event.source;
        if(targetField && (targetField.name === "CR_RecurseDepth")) {
            item.setRecurseDepth(p.fields.recurseDepth.value)
        }
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onSaveClick = function Settings$onSaveClick() {
    this.fireEvent("save");

    var item = this.getItem();
    if(item) {
        item.save();
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.disposeInterface = Tridion.OO.nonInheritable(function Settings$disposeInterface() {

});

$display.registerView(DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings);