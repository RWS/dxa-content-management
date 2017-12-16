Tridion.Type.registerNamespace("DXA.CM.Extensions.DXAResolver.Models");

DXA.CM.Extensions.DXAResolver.Models.Configuration = function Configuration(id) {
    Tridion.OO.enableInterface(this, "DXA.CM.Extensions.DXAResolver.Models.Configuration");
    this.addInterface("Tridion.ContentManager.Item", [id]);

    var p = this.properties;

    p.recurseDepth = undefined;
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.RECURSE_DEPTH_XPATH = "/Configuration/dcr:RecurseDepth";

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.getItemType = function Configuration$getItemType() {
    return DXA.CM.Extensions.DXAResolver.Models.Configuration.ItemType;
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.getDeltaXmlDefinition = function Configuration$getDeltaXmlDefinition() {
    var p = this.properties;
    if (!p.deltaXmlDefinition) {
        p.deltaXmlDefinition = {
            xml: Tridion.Utils.String.format("<Configuration xmlns:dcr=\"{0}\"></Configuration>", Tridion.Constants.Namespaces.dcr),
            sections: ["/"]
        };
    }
    return p.deltaXmlDefinition;
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.getRecurseDepth = function Configuration$getRecurseDepth() {
    var xmlDoc, p = this.properties;
    if (p.recurseDepth === undefined && (xmlDoc = this.getStateXmlDocument())) {
        p.recurseDepth = $xml.getInnerText(xmlDoc, this.RECURSE_DEPTH_XPATH) || null;
    }
    return p.recurseDepth;
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.setRecurseDepth = function Configuration$setRecurseDepth(value, context) {
    var p = this.properties;
    if (this.getRecurseDepth() != value) {
        p.recurseDepth = value;

        this.updateInnerXml("/Configuration", "", context);
        this.updateValue(this.RECURSE_DEPTH_XPATH, value, context);
    }
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.invalidateReadOnlyProperties = function Configuration$invalidateReadOnlyProperties() {
    var p = this.properties;
    p.config = undefined;
    p.recurseDepth = undefined;

    this.callBase("Tridion.ContentManager.Item", "invalidateReadOnlyProperties", arguments);
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.invalidateEditableProperties = function Configuration$invalidateEditableProperties() {
    var p = this.properties;
    p.config = undefined;
    p.recurseDepth = undefined;

    this.callBase("Tridion.ContentManager.Item", "invalidateEditableProperties", arguments);
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.executeLoadItem = function Configuration$executeLoadItem(id, openMode, success, failure) {
    dxa.cm.extensions.dxaresolver.models.services.LoadConfiguration(success, failure);
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.executeSaveItem = function Configuration$executeSaveItem(id, xml, doneEditing, success, failure) {
    dxa.cm.extensions.dxaresolver.models.services.SaveConfiguration(xml, success, failure);
};

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.pack = Tridion.OO.nonInheritable(function Configuration$pack() {
    var p = this.properties;
    return {
        config: p.config,
        recurseDepth: p.recurseDepth
    };
});

DXA.CM.Extensions.DXAResolver.Models.Configuration.prototype.unpack = Tridion.OO.nonInheritable(function Configuration$unpack(data) {
    if (data) {
        var p = this.properties;
        p.config = data.config;
        p.recurseDepth = data.recurseDepth;
    }
});