<#
.SYNOPSIS
   Prepares CMS for upgrading from an earlier DXA version to DXA 1.6
.EXAMPLE
   .\cms-upgrade.ps1 -cmsUrl http://localhost -masterPublication 
#>

[CmdletBinding( SupportsShouldProcess=$false, PositionalBinding=$true)]
Param (
    # The URL of the CMS
    [Parameter(Mandatory=$true, Position=0, HelpMessage="The URL of the CMS")]
    [string]$cmsUrl,

    # Title of the Publication containing the DXA Master items. Defaults to "100 Master".
    [Parameter(Mandatory=$false, HelpMessage="Title of the Publication containing the DXA Master items")]
    [string]$masterPublication = "100 Master",

    # Title of the Root Folder. Defaults to "Building Blocks".
    [Parameter(Mandatory=$false, HelpMessage="Title of the Root Folder")]
    [string]$rootFolder = "Building Blocks",

    # Specify this switch to force the upgrade even though the Framework subtree already exists.
    [Parameter(Mandatory=$false, HelpMessage="Specify this switch to force the upgrade even though the Framework subtree already exists.")]
    [switch]$force,

    # By default, the current Windows user's credentials are used, but it is possible to specify alternative credentials:
    [Parameter(Mandatory=$false, HelpMessage="CMS User name")]
    [string]$cmsUserName,
    [Parameter(Mandatory=$false, HelpMessage="CMS User password")]
    [string]$cmsUserPassword,
    [Parameter(Mandatory=$false, HelpMessage="CMS Authentication type")]
    [ValidateSet("Windows", "Basic")]
    [string]$cmsAuth = "Windows"
)

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

$cmsUrl = $cmsUrl.TrimEnd("/") + "/"

#Include functions from ContentManagerUtils.ps1
$PSScriptDir = Split-Path $MyInvocation.MyCommand.Path
$importExportFolder = Join-Path $PSScriptDir "..\ImportExport"
. (Join-Path $importExportFolder "ContentManagerUtils.ps1")

Initialize-CoreServiceClient $importExportFolder | Out-Null
$defaultReadOptions = New-Object Tridion.ContentManager.CoreService.Client.ReadOptions


function Create-Folder([string] $title, [string] $parentFolderWebDavUrl)
{
    Write-Host "Creating Folder '$title' in '$parentFolderWebDavUrl' ..."
    $folderWebDavUrl = "$parentFolderWebDavUrl/$title"

    $folderData = $coreServiceClient.TryRead($folderWebDavUrl, $defaultReadOptions)
    if ($folderData)
    {
        Write-Warning "Folder '$folderWebDavUrl' already exists."
        return $folderData
    }

    $folderData = $coreServiceClient.GetDefaultData("Folder", $parentFolderWebDavUrl, $defaultReadOptions)
    $folderData.Title = $title
    return $coreServiceClient.Create($folderData, $defaultReadOptions)
}

function Move-Item([string] $itemId, [Tridion.ContentManager.CoreService.Client.RepositoryLocalObjectData] $destinationFolder, [string] $newTitle = $null)
{
    $destinationFolderWebDavUrl = $destinationFolder.LocationInfo.WebDavUrl
    Write-Host "Moving item '$itemId' to '$destinationFolderWebDavUrl' ..."
    
    if (!$coreServiceClient.IsExistingObject($itemId))
    {
        Write-Warning "Item '$itemId' does not exist; has it been moved already?"
        return
    }

    $movedItem = $coreServiceClient.Move($itemId, $destinationFolderWebDavUrl, $defaultReadOptions)

    if ($newTitle)
    {
        Write-Host "Renaming Item '$($movedItem.LocationInfo.WebDavUrl)' to '$newTitle' ..."
        $movedItem.Title = $newTitle
        $coreServiceClient.Update($movedItem, $null)
    }
}

function Remove-Item([string] $itemId)
{
    Write-Host "Removing item '$itemId' ..."
    
    if (!$coreServiceClient.IsExistingObject($itemId))
    {
        Write-Warning "Item '$itemId' does not exist; has it been removed already?"
        return
    }

    $coreServiceClient.Delete($itemId)
}


function Create-DxaFrameworkSubtree()
{
    $frameworkFolder = "Framework"
    $rootFolderWebDavUrl = Encode-WebDav "$masterPublication/$rootFolder"
    $frameworkFolderWebDavUrl = "$rootFolderWebDavUrl/$frameworkFolder"
    $coreModuleFolderWebDavUrl = "$rootFolderWebDavUrl/Modules/Core"

    if (!$coreServiceClient.IsExistingObject($coreModuleFolderWebDavUrl))
    {
        throw "Folder '$coreModuleFolderWebDavUrl' does not exist. Check if parameter -masterPublication and -rootFolder are properly specified."
    }
    if ($coreServiceClient.IsExistingObject($frameworkFolderWebDavUrl))
    {
        Write-Host "Folder '$frameworkFolderWebDavUrl' already exists; it seems that your CMS already has DXA 1.6 structure."
        if (!$force)
        {
            Write-Host "Specify the -force flag if you want the run the upgrade anyways."
            return
        }
    }

    # Create the new Framework subtree. We don't bother with security here; cms-import.ps1 should take care of that.
    $frameworkFolderData = Create-Folder $frameworkFolder $rootFolderWebDavUrl
    $fwDeveloperFolderData = Create-Folder "Developer" $frameworkFolderWebDavUrl
    $fwDeveloperSchemasFolderData = Create-Folder "Schemas" $fwDeveloperFolderData.LocationInfo.WebDavUrl
    $fwDeveloperTemplatesFolderData = Create-Folder "Templates" $fwDeveloperFolderData.LocationInfo.WebDavUrl
    $fwSiteManagerFolderData = Create-Folder "Site Manager" $frameworkFolderWebDavUrl
    $fwSiteManagerSchemasFolderData = Create-Folder "Schemas" $fwSiteManagerFolderData.LocationInfo.WebDavUrl

    Move-Item "$coreModuleFolderWebDavUrl/Admin/Schemas/Module Configuration.xsd" $fwDeveloperSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Admin/Schemas/Module Dependency.xsd" $fwDeveloperSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Schemas/Metadata/Component Template Metadata.xsd" $fwDeveloperSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Schemas/Metadata/Page Template Metadata.xsd" $fwDeveloperSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Schemas/Embedded/Region Metadata.xsd" $fwDeveloperSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Schemas/ZIP file.xsd" $fwDeveloperSchemasFolderData

    Move-Item "$coreModuleFolderWebDavUrl/Developer/Core Template Building Blocks/DD4T" $fwDeveloperTemplatesFolderData "DD4T.Templates"
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Core Template Building Blocks" $fwDeveloperTemplatesFolderData "Sdl.Web.Tridion.Templates"
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Templates/Generate Navigation.tctcmp" $fwDeveloperTemplatesFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Templates/Publish Settings.tctcmp" $fwDeveloperTemplatesFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Developer/Templates/JSON.tptcmp" $fwDeveloperTemplatesFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Templates/Publish HTML Design.tctcmp" $fwDeveloperTemplatesFolderData # TODO

    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/Bootstrap Configuration.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/HTML Design Configuration.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/Module Design Configuration.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/General Configuration.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/Favicon.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/Metadata/Publication Metadata.xsd" $fwSiteManagerSchemasFolderData
    Move-Item "$coreModuleFolderWebDavUrl/Editor/Schemas/Embedded/Name Value Pair.xsd" $fwSiteManagerSchemasFolderData

    Remove-Item "$coreModuleFolderWebDavUrl/Admin/Schemas"
    Remove-Item "$coreModuleFolderWebDavUrl/Developer"
    Remove-Item "$coreModuleFolderWebDavUrl/Site Manager/Schemas/Metadata"
}


$coreServiceClient = Get-CoreServiceClient "Service"
Create-DxaFrameworkSubtree
$coreServiceClient.Dispose()