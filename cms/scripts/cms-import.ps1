<#
.SYNOPSIS
   Import Digital Experience Accelerator Core items into the Content Manager System.
.DESCRIPTION
   This script imports DXA Core items into the CMS using the CM Import/Export service and Core Service.
   It also creates Mappings in Topology Manager for the DXA Example Site Publication. It uses the Topology Manager cmdlets for that purpose.
   The ttm-prepare.ps1 PowerShell script should be run before running this script to preconfigure Topology Manager.
.EXAMPLE
   & .\cms-import.ps1 -cmsUrl "http://localhost:81/" -importType all-publications
.NOTES
   Importing into existing publication by means of mapping a target publication title is currently not supported for rights and permissions.
   Because the script uses Topology Manager cmdlets, it is easiest to run it on the SDL Web 8 CM Server.
   It is possible to run it on another machine if it has Topology Manager cmdlets installed and TRIDION_TTM_SERVICEURL environment variable set.
   Alternatively, the noTopologyManager switch can be used to suppress creation of mappings in Topology Manager.
#>

[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding=$false)]
param (
    # Use "all-publications" if you want to import DXA into a new Publication Blueprint tree in your CMS.
    # Use "master-only" if you want to only import DXA Master items into an existing Publication. See also -masterPublication parameter.
    # Use "example-publication" if you want to import DXA Example Publications (110 DXA Site Type, 200 Example Content and 400 Example Site) into an existing BluePrint. See also -masterPublication and -rootPublication parameters.
    # Use "rights-permissions" if you want to import DXA rights and permissions in your CMS (do this after importing all Publications).
    [ValidateSet("all-publications", "master-only", "example-publications", "rights-permissions")]
    [string]$importType = "all-publications",

    # Enter your cms url
    [Parameter(Mandatory=$true, HelpMessage="URL of the CMS you want to import in")]
    [string]$cmsUrl,

    # Title of the BluePrint Root Publication (containing the CM default items). Use in conjunction with -importType example-publications.
    [Parameter(Mandatory=$false, HelpMessage="Title of the BluePrint Root Publication (containing the CM default items)")]
    [string]$rootPublication = "000 Empty",

    # Title of the Publication containing the DXA Master items. Use in conjunction with -importType master-only or example-publications.
    [Parameter(Mandatory=$false, HelpMessage="Title of the Publication containing the DXA Master items")]
    [string]$masterPublication = "100 Master",

    # Title of the Root Structure Group
    [Parameter(Mandatory=$false, HelpMessage="Title of the Root Structure Group")]
    [string]$rootStructureGroup = "Home",

    # Title of the Root Folder
    [Parameter(Mandatory=$false, HelpMessage="Title of the Root Folder")]
    [string]$rootFolder = "Building Blocks",

    # Specify this switch to suppress creation of mappings in SDL Web 8 Topology Manager (which requires Topology Manager cmdlets).
    [Parameter(Mandatory=$false, HelpMessage="Specify this switch to suppress creation of mappings in SDL Web 8 Topology Manager.")]
    [switch]$noTopologyManager,

    #The type of Model Mapping to use. Can be 'Legacy' (compatible with DXA 1.x) or 'R2'.
    [Parameter(Mandatory=$false)]
    [ValidateSet("Legacy", "R2")]
    [string]$modelMapping = "R2",

    # By default, the current Windows user's credentials are used, but it is possible to specify alternative credentials:
    [Parameter(Mandatory=$false, HelpMessage="CMS User name")]
    [string]$cmsUserName,
    [Parameter(Mandatory=$false, HelpMessage="CMS User password")]
    [string]$cmsUserPassword,
    [Parameter(Mandatory=$false, HelpMessage="CMS Authentication type")]
    [ValidateSet("Windows", "Basic")]
    [string]$cmsAuth = "Windows",

    #Destination for DXA UI Extension
    [Parameter(Mandatory=$false, HelpMessage="DXA UI Extension installation path")]
    [string]$DxaExtensionPath = "C:\DXA\UIExtensions",

    [Parameter(Mandatory=$false, HelpMessage="CM Website name")]
    [string]$cmSiteName = "SDL Web"
)

$cmsVersionRegKeyPath = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Tridion"

#Check CMS version by using registry key. If this key is not found or is empty we assume
#that we could still work so dont terminate the script
Try {
    if(Test-Path $cmsVersionRegKeyPath)
    {
        $cmsVersion = (Get-ItemProperty $cmsVersionRegKeyPath).SuiteVersion -as [string]
    }
} Catch {
}

if($cmsVersion)
{
    Write-Host "Installed CMS version reported to be $cmsVersion"
    $majorVersion = $cmsVersion.Split(".")[0] -as [int];
    if ($majorVersion -lt 8)
    {
        throw "Unexpected CMS version: $cmsVersion. DXA 2.0 requires SDL Web 8 or higher."
    }
}
else
{
    Write-Host "WARNING: Unable to find CME version from registry. Continuing anyway..."
}

#Include functions from ContentManagerUtils.ps1
$PSScriptDir = Split-Path $MyInvocation.MyCommand.Path
$importExportFolder = Join-Path $PSScriptDir "..\ImportExport"
. (Join-Path $importExportFolder "ContentManagerUtils.ps1")

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

#Process 'WhatIf' and 'Confirm' options
if (!($pscmdlet.ShouldProcess($cmsUrl, "Import DXA Core items"))) { return }



function Set-EnvironmentConfig()
{
    $environmentConfigComponentWebDavUrl = "/webdav/$masterPublication/$rootFolder/Settings/Core/Admin/Environment Configuration.xml"    
    $environmentConfigComponent = $coreServiceClient.TryRead($environmentConfigComponentWebDavUrl, $defaultReadOptions)
    if (!$environmentConfigComponent)
    {
        Write-Warning "Environment Configuration Component not found: '$environmentConfigComponentWebDavUrl'" 
        return
    }

    # Remove the entire cmsurl setting (if any); the CMS URL will be obtained from Topology Manager.
    $environmentConfigComponent.Content = $environmentConfigComponent.Content -replace "<settings>\s*<name>cmsurl</name>\s*<value>.*</value>\s*</settings>", ''
    

    Write-Host "Updating Environment Configuration Component '$environmentConfigComponentWebDavUrl' ($($environmentConfigComponent.Id))"
    Write-Verbose "Updated Content: $($environmentConfigComponent.Content)" 
    
    $coreServiceClient.Update($environmentConfigComponent, $null)
}

function Prepare-Upgrade()
{
    $rootFolderWebDavUrl = Encode-WebDav "$masterPublication/$rootFolder"
    $frameworkFolderWebDavUrl = "$rootFolderWebDavUrl/Framework"
    $coreModuleFolderWebDavUrl = "$rootFolderWebDavUrl/Modules/Core"

    if (!$coreServiceClient.IsExistingObject($coreModuleFolderWebDavUrl))
    {
        Write-Host "Folder '$coreModuleFolderWebDavUrl' does not exist which implies this is an initial import. If you are upgrading, check whether the -masterPublication and -rootFolder parameters are specified correctly."
        return
    }  

    Write-Host "Preparing upgrade to DXA 1.6 structure..."

    .\cms-upgrade.ps1 -cmsUrl $cmsUrl -cmsAuth $cmsAuth -cmsUserName $cmsUserName -cmsUserPassword $cmsUserPassword -masterPublication $masterPublication -rootFolder $rootFolder
}

function Set-ModelMappingType()
{
    if ($modelMapping -eq "R2")
    {
        # DXA CM Packages already have R2 model mapping
        return
    }

    Write-Host "Configuring Legacy model mapping..."
    
    $dxaTemplatesFolderWebDavUrl = Encode-WebDav "$masterPublication/$rootFolder/Framework/Developer/Templates/DXA.R2"
    $renderPageContentTbbWebDavUrl = "$dxaTemplatesFolderWebDavUrl/Render Page Content.tbbcmp"
    $renderComponentContentTbbWebDavUrl = "$dxaTemplatesFolderWebDavUrl/Render Component Content.tbbcmp"
    $defaultCTFinishActionsTbbWebDavUrl = "$dxaTemplatesFolderWebDavUrl/Default Component Template Finish Actions.tbbcmp"
    $defaultPTFinishActionsTbbWebDavUrl = "$dxaTemplatesFolderWebDavUrl/Default Page Template Finish Actions.tbbcmp"

    $legacyTemplatesFolderWebDavUrl = Encode-WebDav "$masterPublication/$rootFolder/Framework/Developer/Templates/DXA.Legacy"
    $legacyRenderPageContentTbbWebDavUrl = "$legacyTemplatesFolderWebDavUrl/Render Page Content (Legacy).tbbcmp"
    $legacyRenderComponentContentTbbWebDavUrl = "$legacyTemplatesFolderWebDavUrl/Render Component Content (Legacy).tbbcmp"
    $legacyDefaultCTFinishActionsTbbWebDavUrl = "$legacyTemplatesFolderWebDavUrl/Default Component Template Finish Actions (Legacy).tbbcmp"
    $legacyDefaultPTFinishActionsTbbWebDavUrl = "$legacyTemplatesFolderWebDavUrl/Default Page Template Finish Actions (Legacy).tbbcmp"

    Copy-TemplateContent $legacyRenderPageContentTbbWebDavUrl $renderPageContentTbbWebDavUrl
    Copy-TemplateContent $legacyRenderComponentContentTbbWebDavUrl $renderComponentContentTbbWebDavUrl
    Copy-TemplateContent $legacyDefaultCTFinishActionsTbbWebDavUrl $defaultCTFinishActionsTbbWebDavUrl
    Copy-TemplateContent $legacyDefaultPTFinishActionsTbbWebDavUrl $defaultPTFinishActionsTbbWebDavUrl
}

function Copy-TemplateContent($sourceTemplateId, $destTemplateId)
{
    Write-Host "Copying Template content from '$sourceTemplateId' to '$destTemplateId' ..."

    $sourceTemplate = $coreServiceClient.TryRead($sourceTemplateId, $defaultReadOptions)
    if (!$sourceTemplate)
    {
        Write-Error "Source Template '$sourceTemplate' not found."
        return
    }

    $destTemplate = $coreServiceClient.TryRead($destTemplateId, $defaultReadOptions)
    if (!$destTemplate)
    {
        Write-Error "Destination Template '$destTemplate' not found."
        return
    }

    $destTemplate.Content = $sourceTemplate.Content

    $destTemplate = $coreServiceClient.Update($destTemplate, $defaultReadOptions) 
}

# Deploy the DXA Application Data Definition if we're running on a CMS server.
$dxaAppDataDef = "$PSScriptDir\DXA Application Data Definition.xml"
$cmsHomeDir = $env:TRIDION_CM_HOME
if ($cmsHomeDir)
{
    # Seems we're running on the CM server; deploy the DXA App Data Definition file
    $cmsAppDataDir =  Join-Path $cmsHomeDir "config\ImportExport\ApplicationData"
    Copy-Item $dxaAppDataDef $cmsAppDataDir
    Write-Host "Copied '$dxaAppDataDef' to '$cmsAppDataDir'"    
}
else
{
    Write-Host "It seems that you are not running the script on a CMS Server. Please ensure that the DXA Application Data Definition ('$dxaAppDataDef') is deployed on the CMS server." 
}

$tempFolder = Get-TempFolder "DXA"
Initialize-ImportExport $importExportFolder $tempFolder

# Create Core Service client and default read options
$cmsUrl = $cmsUrl.TrimEnd("/") + "/"
$coreServiceClient = Get-CoreServiceClient "Service"
$defaultReadOptions = New-Object Tridion.ContentManager.CoreService.Client.ReadOptions

$importPackageFullPath = Join-Path $PSScriptDir ($importType + ".zip")
Write-Verbose "Import Package location: '$importPackageFullPath'"

$permissionsFullPath = Join-Path $PSScriptDir "permissions.xml"
Write-Verbose "Permissions file location: '$permissionsFullPath'"

# Mappings - only edit these if you really know what you are doing!
$detailedMapping = (
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("Publication", "/webdav/000 Empty",(Encode-WebDav($rootPublication)))),
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("Folder", "/webdav/000 Empty/Building Blocks", (Encode-WebDav "$rootPublication/$rootFolder"))),
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("StructureGroup", "/webdav/000 Empty/Home",(Encode-WebDav($rootPublication + "/" + $rootStructureGroup)))),
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("Publication", "/webdav/100 Master",(Encode-WebDav($masterPublication)))),
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("Folder", "/webdav/100 Master/Building Blocks", (Encode-WebDav "$masterPublication/$rootFolder"))),
    (New-Object Tridion.ContentManager.ImportExport.Packaging.V2013.Mapping2013("StructureGroup", "/webdav/100 Master/Home",(Encode-WebDav($masterPublication + "/" + $rootStructureGroup))))
)

switch ($importType)
{
    "all-publications"
    {
        # Case where you have an empty CM database, or want to create a new, separate Blueprint tree
        # For full import we do not map publications (this doesn't work - we can only map when working with existing publications)
        Prepare-Upgrade
        Import-CmPackage $importPackageFullPath $tempFolder
        Set-EnvironmentConfig
        Set-ModelMappingType
    }

    "master-only"
    {
        # Case where you want to import/update (and map) just the 100 Master publication elements (so no content/pages)
        # NOTE - this will not create/update the master publication, this should already have happened
        Prepare-Upgrade
        Import-CmPackage $importPackageFullPath $tempFolder $detailedMapping
        Set-EnvironmentConfig
        Set-ModelMappingType
    }

    "example-publications"
    {
        # NOTE: only the $rootPublication and $masterPublication can be mapped; mapping the example Publication themselves won't work.
        Import-CmPackage $importPackageFullPath $tempFolder $detailedMapping
    }

    "rights-permissions"
    {
        # NOTE - this should be executed last after importing all Publications and does not work for mapped Publications
        Import-Security $permissionsFullPath $coreServiceClient
    }
}

# Create Topology Mananager Mappings for the "Example Site" Publication (if applicable).
if (($importType -in "all-publications", "example-publications") -and !$noTopologyManager)
{
    $dxaSiteTypeKey = "DxaSiteType"
    $sitePublication = "400 Example Site"
    $sitePublicationId = $coreServiceClient.GetTcmUri("/webdav/$sitePublication", '', $null)
    try
    {
        $dxaWebAppIds = Get-TtmWebApplication | Where { $_.ScopedRepositoryKeys -contains $dxaSiteTypeKey } | Select -ExpandProperty Id
        if ($dxaWebAppIds)
        {
            Write-Host "Creating Topology Manager Mappings for Publication '$sitePublication' ($sitePublicationId)."
            Write-Host ("DXA Web Application ID(s): " + ($dxaWebAppIds -join ", "))
            $mappings = Get-TtmMapping
            $dxaWebAppIds | ForEach-Object {
                    $dxaWebAppId = $_
                    $existingMapping = $mappings | Where { $_.PublicationId -eq $sitePublicationId -and $_.WebApplicationId -eq $dxaWebAppId}
                    if ($existingMapping)
                    {
                        Write-Host "Mapping already exists for Web Application '$dxaWebAppId': " $existingMapping.PrimaryMappedUrl
                    }
                    else
                    {
                        Add-TtmMapping -PublicationId $sitePublicationId -WebApplicationId $dxaWebAppId -ErrorAction Continue 
                    }
                }
        }
        else
        {
            Write-Warning "No DXA Web Applications (i.e. Web Applications with '$dxaSiteTypeKey' key) found in Topology Manager. Please run the ttm-prepare.ps1 script before the DXA CMS import."
        }
    }
    catch [System.Exception]
    {
        Write-Warning "Unable to create Topology Manager mappings. The script requires the Topology Manager cmdlets for that purpose."
        Write-Host "Please run the script on the CM Server or on a machine with Topology Manager cmdlets installed and TRIDION_TTM_SERVICEURL environment variable set."
    }
}

$coreServiceClient.Dispose()

# Attempt to run our custom resolver install script if we are using R2 mapping
if ($modelMapping -eq "R2")
{
    $distSource = Split-Path $MyInvocation.MyCommand.Path
    $distSource = $distSource.TrimEnd("\")

    $customResolverPath = Join-Path $distSource "\cms-customresolver.ps1"
    & $customResolverPath -extensionTargetFolder $DxaExtensionPath -cmSiteName $cmSiteName
}

