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

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.enableControls = function Settings$enableControls() {
    var p = this.properties;

    p.buttons.save.enable();
};
DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.disableControls = function Settings$disableControls() {
    var p = this.properties;

    p.buttons.save.disable();
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.initializeControls = function Settings$initializeControls() {
    var p = this.properties;

    p.buttons.close = $controls.getControl($("#BtnClose"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.close, "click", this.getDelegate(this.onCloseClick));

    p.buttons.save = $controls.getControl($("#BtnSave"), "Tridion.Controls.Button");
    $evt.addEventHandler(p.buttons.save, "click", this.getDelegate(this.onSaveClick));

    var f = p.fields;
    f.recurseDepth = $("#cr-recurse-depth");
    $evt.addEventHandler(f.recurseDepth, "valuepropertychange", this.getDelegate(this.onFieldChange));

    f.recurseDepth && f.recurseDepth.focus();

};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.initialize = function Settings$initialize() {
    this.callBase("Tridion.Web.UI.Editors.Base.Views.ViewBase", "initialize");
    var item = this.getItem();

    if (item) {
        $evt.addEventHandler(item, "load", this.getDelegate(this.onItemLoaded));
        $evt.addEventHandler(item, "loading", this.getDelegate(this.onItemLoading));
        $evt.addEventHandler(item, "loadfailed", this.getDelegate(this.onItemLoadFailed));
        $evt.addEventHandler(item, "validate", this.getDelegate(this.onValidate));

        $evt.addEventHandler(item, "collectdata", this.getDelegate(this.onCollectData));
        $evt.addEventHandler(item, "save", this.getDelegate(this.onItemSaved));
        $evt.addEventHandler(item, "savefailed", this.getDelegate(this.onItemSaveFailed));

        item.load(true)
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemLoading = function Settings$onItemLoading() {
    this.disableControls();
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onValidate = function Settings$onValidate() {
    var p = this.properties;

    var fields = p.fields;
    var val = fields.recurseDepth.value;
    if(val){
        if(val.trim() === "") {
            $messages.registerError("Custom Resolver: Recurse Depth can not be empty.");
            return false;
        }

        if(isNaN(parseInt(val.trim()))) {
            $messages.registerError("Custom Resolver: Recurse Depth should be numeric.");
            return false;
        }
    }

    return true;

};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemLoaded = function Settings$onItemLoaded() {
    var item = this.getItem();
    var p = this.properties;

    if (item && item.isLoaded()) {
        var fields = p.fields;

        fields.recurseDepth.value = item.getRecurseDepth();
        this.disableControls();
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemSaved = function Settings$onItemSaved() {
    this.disableControls();
    $messages.registerNotification("Custom Resolver: Configuration has been saved.")
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemSaveFailed = function Settings$onItemSaveFailed() {
    this.enableControls();
    $messages.registerError("Custom Resolver: Couldn't save configuration.")
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onItemLoadFailed = function Settings$onItemLoadFailed() {
    this.enableControls();
    $messages.registerError("Custom Resolver: Couldn't load configuration.")
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
    var input = event && event.source;
    var val = input && input.value;
    var p = this.properties;
    if (val.trim() === "" || item.getRecurseDepth() == val.trim()) {
        p.buttons.save.disable()
    } else if (p.buttons.save.isDisabled()) {
        p.buttons.save.enable();
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onCollectData = function Settings$onFieldChange(event) {
    var item = this.getItem();
    var p = this.properties;

    if (p.fields.recurseDepth.value.trim() !== "") {
        item.setRecurseDepth(parseInt(p.fields.recurseDepth.value))
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.onSaveClick = function Settings$onSaveClick() {
    this.fireEvent("save");

    var item = this.getItem();
    if (item) {
        item.save(true);
    }
};

DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings.prototype.disposeInterface = Tridion.OO.nonInheritable(function Settings$disposeInterface() {
    var item = this.getItem();

    if (item) {
        $evt.removeEventHandler(item, "load", this.getDelegate(this.onItemLoaded));
        $evt.removeEventHandler(item, "loading", this.getDelegate(this.onItemLoading));
        $evt.removeEventHandler(item, "loadfailed", this.getDelegate(this.onItemLoadFailed));
        $evt.removeEventHandler(item, "validate", this.getDelegate(this.onValidate));

        $evt.removeEventHandler(item, "collectdata", this.getDelegate(this.onCollectData));
        $evt.removeEventHandler(item, "save", this.getDelegate(this.onItemSaved));
        $evt.removeEventHandler(item, "savefailed", this.getDelegate(this.onItemSaveFailed));
    }
});

$display.registerView(DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings);