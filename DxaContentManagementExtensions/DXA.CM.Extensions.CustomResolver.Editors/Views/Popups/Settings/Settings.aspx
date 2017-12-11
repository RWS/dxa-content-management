<%@ Page Language="C#" AutoEventWireup="true" Inherits="DXA.CM.Extensions.CustomResolver.Editors.Views.Popups.Settings" ClassName="Settings" %>
<%@ Import Namespace="Tridion.Web.UI" %>

<%@ Register TagPrefix="cc" Namespace="Tridion.Web.UI.Core.Controls" Assembly="Tridion.Web.UI.Core" %>
<%@ Register TagPrefix="ui" Namespace="Tridion.Web.UI.Editors.CME.Controls" %>
<%@ Register TagPrefix="c" Namespace="Tridion.Web.UI.Controls" Assembly="Tridion.Web.UI.Core" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html id="dxa-cr-settings-dialog" class="popup" xmlns="http://www.w3.org/1999/xhtml">
    <head>
		<title>
		    <asp:Literal runat="server" Text="<%$ Resources: DXA.CM.Extensions.CustomResolver.Editors.Strings, CR_EditDXASettingsDialogTitle %>" />
		</title>

		<link rel="shortcut icon" href="<%=ThemePath %>Images/Ico/favicon.ico" type="image/x-icon" />

	    <cc:TridionManager runat="server" Editor="CME">
        	<dependencies runat="server">
            	<dependency runat="server">SDL.Web.UI.Editors.CME</dependency>
        	    <dependency runat="server">SDL.Web.UI.Editors.CME.CommandSets.All</dependency>
        	</dependencies>
        </cc:TridionManager>

    </head>
    <body class="popupview dxa-settings">
        <div class="dxa-layout __full_screen">
            <div class="dxa-layout_element">
            </div>

            <div id="dxa-settings-form" class="dxa-layout_element __content dxa-form">
                    <div class="dxa-layout __horizontal">
                        <div class="dxa-layout_column __main">
                            <div class="dxa-layout_reducer">
                                <div class="dxa-field __horizontal">
                                    <div class="dxa-field_label">
                                        <label for="CR_RecurseDepth" class="__required">
                                            <asp:Literal runat="server" Text="<%$ Resources: DXA.CM.Extensions.CustomResolver.Editors.Strings, CR_RecurseDepth %>" />
                                        </label>
                                    </div>

                                    <div class="dxa-field_content">
                                        <input name="CR_RecurseDepth" type="text" id="cr-recurse-depth" class="dxa-input __type_text __numeric" value="">
                                    </div>

                                </div>
                            </div>
                        </div>
                        <div class="dxa-layout_column __side_right">
                            <div class="dxa-layout_reducer">
                                <div class="dxa-buttons">
                                    <c:Button ID="BtnSave" runat="server" Label="<%$ Resources: DXA.CM.Extensions.CustomResolver.Editors.Strings, CR_Save %>" />
                                </div>
                            </div>
                        </div>
                    </div>
            </div>

            <div id="BottomPanel" class="dxa-layout_element __footer">
                <div class="dxa-layout_reducer">
                    <div class="dxa-button_group __move_right">
                        <c:Button ID="BtnClose" runat="server" Label="<%$ Resources: DXA.CM.Extensions.CustomResolver.Editors.Strings, CR_Close %>" />
                    </div>

                </div>
            </div>
        </div>
    </body>
</html>