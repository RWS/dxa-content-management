Tridion.Type.registerNamespace("DXA.CM.Extensions.CustomResolver.Models");

DXA.CM.Extensions.CustomResolver.Models.Configuration = function Configuration(id) {
    Tridion.OO.enableInterface(this, "DXA.CM.Extensions.CustomResolver.Models.Configuration");
    this.addInterface("Tridion.ContentManager.Item", [id]);

    var p = this.properties;

    p.recurseDepth = undefined;
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.getItemType = function Configuration$getItemType() {
    return DXA.CM.Extensions.CustomResolver.Models.Configuration.ItemType;
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.getDeltaXmlDefinition = function Configuration$getDeltaXmlDefinition() {
    var p = this.properties;
    if (!p.deltaXmlDefinition) {
        p.deltaXmlDefinition = {
            xml: Tridion.Utils.String.format("<dcr:Configuration xmlns:sg=\"{0}\"></dcr:Configuration>", Tridion.Constants.Namespaces.dcr),
            sections: ["/"]
        };
    }
    return p.deltaXmlDefinition;
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.getRecurseDepth = function Configuration$getRecurseDepth() {
    var xmlDoc, p = this.properties;
    if (p.recurseDepth === undefined && (xmlDoc = this.getStateXmlDocument())) {
        p.recurseDepth = $xml.getInnerText(xmlDoc, "/dcr:Configuration/dcr:RecurseDepth") || null;
    }
    return p.recurseDepth;
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.setRecurseDepth = function Configuration$setRecurseDepth(value, context) {
    var p = this.properties;
    if (this.getRecurseDepth() != value) {
        p.recurseDepth = value;

        this.updateInnerXml("/dcr:Configuration", "", context);
        this.updateValue("/dcr:Configuration/dcr:RecurseDepth", value, context);
    }
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.invalidateReadOnlyProperties = function Configuration$invalidateReadOnlyProperties() {
    var p = this.properties;
    p.config = undefined;
    p.recurseDepth = undefined;

    this.callBase("Tridion.ContentManager.Item", "invalidateReadOnlyProperties", arguments);
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.invalidateEditableProperties = function Configuration$invalidateEditableProperties() {
    var p = this.properties;
    p.config = undefined;
    p.recurseDepth = undefined;

    this.callBase("Tridion.ContentManager.Item", "invalidateEditableProperties", arguments);
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.executeLoadItem = function Configuration$executeLoadItem(id, openMode, success, failure) {
    tridion.web.ui.models.safeguard.services.LoadConfiguration(success, failure);
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.executeSaveItem = function Configuration$executeSaveItem(id, xml, doneEditing, success, failure) {
    tridion.web.ui.models.safeguard.services.SaveConfiguration(xml, success, failure);
};

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.pack = Tridion.OO.nonInheritable(function Configuration$pack() {
    var p = this.properties;
    return {
        config: p.config,
        recurseDepth: p.recurseDepth
    };
});

DXA.CM.Extensions.CustomResolver.Models.Configuration.prototype.unpack = Tridion.OO.nonInheritable(function Configuration$unpack(data) {
    if (data) {
        var p = this.properties;
        p.config = data.config;
        p.recurseDepth = data.recurseDepth;
    }
});