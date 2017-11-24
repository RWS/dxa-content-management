<%@ Page Language="C#" AutoEventWireup="true" Inherits="DXA.CM.Extensions.CustomResolver.Views.Popups.Settings" ClassName="Settings" %>
<%@ Import Namespace="Tridion.Web.UI" %>

<%@ Register TagPrefix="cc" Namespace="Tridion.Web.UI.Core.Controls" Assembly="Tridion.Web.UI.Core" %>
<%@ Register TagPrefix="ui" Namespace="Tridion.Web.UI.Editors.CME.Controls" %>
<%@ Register TagPrefix="c" Namespace="Tridion.Web.UI.Controls" Assembly="Tridion.Web.UI.Core" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html id="SettingsView" class="popup" xmlns="http://www.w3.org/1999/xhtml">
    <head>
		<title>
		    <asp:Literal runat="server" Text="<%$ Resources: DXA.CM.Extensions.CustomResolver.Strings, CR_Settings %>" />
		</title>

		<link rel="shortcut icon" href="<%=ThemePath%>Images/Ico/favicon.ico" type="image/x-icon" />

	    <cc:TridionManager runat="server" Editor="CME">
        	<dependencies runat="server">
            	<dependency runat="server">SDL.Web.UI.Editors.CME</dependency>
        	    <dependency runat="server">SDL.Web.UI.Editors.CME.CommandSets.All</dependency>
        	</dependencies>
        </cc:TridionManager>

	</head>
	<body class="dxa-settings">
	    <div id="LayoutWrapper">
	        <div class="dialogtitle">
	            <asp:Label runat="server" Text="<%$ Resources: DXA.CM.Extensions.CustomResolver.Strings, CR_EditDXASettingsDialogTitle %>" />
	        </div>
    		<div id="dxa-settings-form" class="content stack-elem">

            </div>
			<div id="Footer" class="footer stack-elem">
				<div class="BtnWrapper">
				    <div class="rightbuttons">
                        <c:Button ID="BtnSave" runat="server" Label="<%$ Resources: DXA.CM.Extensions.CustomResolver.Strings, CR_Save %>" />
                        <c:Button ID="BtnClose" runat="server" Label="<%$ Resources: DXA.CM.Extensions.CustomResolver.Strings, CR_Close %>" />
                    </div>
				</div>
			</div>
	    </div>
    </body>
</html>