<%@ Assembly Name="DXA.CM.Extensions.DXAResolver.Editors" %>
<%@ Control Language="C#" AutoEventWireup="true" Inherits="DXA.CM.Extensions.DXAResolver.Editors.Views.RibbonToolbar.RibbonGroup" %>
<%@ Register TagPrefix="c" Namespace="Tridion.Web.UI.Controls" Assembly="Tridion.Web.UI.Core, Version=8.1.0.194, Culture=neutral, PublicKeyToken=ddfc895746e5ee6b" %>

<c:RibbonButton
    runat="server" ID="CR_DXASettings"
    CommandName="CRShowSettings"
    Title="<%$ Resources: DXA.CM.Extensions.DXAResolver.Editors.Strings, CR_EditDXASettingsDialogTitle %>"
    Label="<%$ Resources: DXA.CM.Extensions.DXAResolver.Editors.Strings, CR_EditDXASettingsDialogTitle %>" />
