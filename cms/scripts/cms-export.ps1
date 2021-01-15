[CmdletBinding( SupportsShouldProcess=$false, PositionalBinding=$true)]
Param (
    # Specify what to export
    [string]$exportType = "all-publications", 

    # The URL of the CMS to export from
    [string]$cmsUrl = "http://dxadevweb8.ams.dev",

    # Exported content
	[string]$targetDir = 'C:\Temp\DXA',

    # The ID of the "100 Master" publication
    [string]$masterPubId = "tcm:0-2-1",

    # The ID of the "110 DXA Site Type" publication
    [string]$siteTypePubId = "tcm:0-1067-1",

    # The ID of the "200 Example Content" publication
    [string]$exampleContentPubId = "tcm:0-1068-1",

    # The ID of the "400 Example Site" publication
    [string]$exampleSitePubId = "tcm:0-8-1",

    # The Target Data Contract Version for the export (defaults to SDL Web 8.1)
    [int]$targetDataContractVersion = 201501 
)

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

$cmsAuth = "Windows"

#Include functions from ContentManagerUtils.ps1
$PSScriptDir = Split-Path $MyInvocation.MyCommand.Path
$importExportFolder = Join-Path $PSScriptDir "..\ImportExport"
. (Join-Path $importExportFolder "ContentManagerUtils.ps1")

# Initialization
if (!$cmsUrl.EndsWith("/")) { $cmsUrl = $cmsUrl + "/" }
$tempFolder = Get-TempFolder "DXA_export"
$exportPackageFolder = Join-Path $targetDir "cms\"
$modulesFolder = Join-Path $targetDir "modules\"
Initialize-ImportExport $importExportFolder $tempFolder

# Prepare export
$exportInstruction = New-Object Tridion.ContentManager.ImportExport.ExportInstruction
$exportInstruction.LogLevel = "Normal"
$exportInstruction.TargetDataContractVersion = $targetDataContractVersion
$exportInstruction.BluePrintMode = [Tridion.ContentManager.ImportExport.BluePrintMode]::ExportSharedItemsFromOwningPublication
$selection = @()
$items = New-Object System.Collections.Generic.List[System.String]

if ($exportType -eq "all-publications")
{
    $items.Add("tcm:0-6-65568") # "Developer" Group
    $items.Add("tcm:0-7-65568") # "Editor" Group
    $items.Add("tcm:0-8-65568") # "Site Manager" Group

    # From "000 Empty"
    $items.Add("/webdav/000 Empty") # Only the Publication and its dependencies

    # From "100 Master": Only export the Framework and Core Module here; other Modules are exported using $exportType = module-XYZ
    $items.Add("/webdav/100 Master") # Only the Publication and its dependencies
    $items.Add("/webdav/100 Master/Building Blocks/Content")
    $items.Add("/webdav/100 Master/Building Blocks/Modules")
    $items.Add("/webdav/100 Master/Building Blocks/Modules Content")
    $items.Add("/webdav/100 Master/Building Blocks/Settings")
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/Core",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/Core",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Home",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.TaxonomiesSelection $masterPubId

    # From "110 DXA Site Type"
    $items.Add("/webdav/110 DXA Site Type") # Only the Publication and its dependencies
    $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
    $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_Page Types",$true
    # See Content subtree below

    # From "200 Example Content"
    $items.Add("/webdav/200 Example Content") # Only the Publication and its dependencies
    # See Content subtree below

    # From "400 Example Site"
    $items.Add("/webdav/400 Example Site") # Only the Publication and its dependencies
    $items.Add("/webdav/400 Example Site/Home/000 Home.tpg") # local copy
    $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/_System/include",$false
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/010 Articles",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/020 Further Information",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/About",$true

    # NOTE: The Content subtree contains shared items from "110 DXA Site Type" and "200 Example Content", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content",$true

    # NOTE: Some Taxonomy-based Sitemap items are shared from "110 DXA Site Type", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
    $items.Add("/webdav/400 Example Site/Building Blocks/Framework/Site Manager/Schemas/Page Navigation Metadata.xsd")
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Sitemap [Navigation]",$true # The whole Taxonomy

    # Export all available dependencies, so do not set $exportInstruction.ExpandDependenciesOfTypes
}
else
{
    $dependencyTypes = New-Object System.Collections.Generic.List[Tridion.ContentManager.ImportExport.DependencyType]
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::Category)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::LinkedCategory)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::LinkedKeyword)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::LinkedSchema)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::DefaultKeyword)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::AllowedMultimediaType)

    # Publication dependencies (needed to make it possible to import child Publications even though they will inherit the dependent items)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::DefaultComponentTemplate)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::DefaultPageTemplate)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::DefaultTemplateBuildingBlock)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::DefaultMultimediaSchema)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::TaskProcess)
    $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::MetadataSchema)

    # Modules have indirect Component links to the HTML Design ZIPs (<Module Comp> -> <Core Comp> -> <HTML Design ZIPs>) which we don't want to include.
    if (!$exportType.StartsWith("module-"))
    {
        $dependencyTypes.Add([Tridion.ContentManager.ImportExport.DependencyType]::LinkedComponent)
    }

    $exportInstruction.ExpandDependenciesOfTypes = $dependencyTypes
}

switch ($exportType)
{
    "master-only"
    {
        #Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties
        $items.Add("/webdav/100 Master/Building Blocks/Content")
        $items.Add("/webdav/100 Master/Building Blocks/Modules")
        $items.Add("/webdav/100 Master/Building Blocks/Modules Content")
        $items.Add("/webdav/100 Master/Building Blocks/Settings")

        if ($targetDataContractVersion -eq 201501)
        {
            $items.Add("/webdav/100 Master/DXA Development")
            $items.Add("/webdav/100 Master/DXA Staging%2FLive")
        }

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/Core",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/Core",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Home",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.TaxonomiesSelection $masterPubId
    }

    "example-publications"
    {
        # From "100 Master". TODO: this is a work-around for making Publication Dependencies work; we can't use DependencyType.BusinessProcessType because we are using 2013 SP1 contract.
        if ($targetDataContractVersion -eq 201501)
        {
            $items.Add("/webdav/110 DXA Site Type/DXA Development")
            $items.Add("/webdav/400 Example Site/DXA Staging%2FLive")
        }

        # From "110 DXA Site Type"
        $items.Add("/webdav/110 DXA Site Type") # Only the Publication and its dependencies
        $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_Page Types",$true
        # See Content subtree below

        # From "200 Example Content"
        $items.Add("/webdav/200 Example Content") # Only the Publication and its dependencies
        # See Content subtree below

        # From "400 Example Site"
        $items.Add("/webdav/400 Example Site") # Only the Publication and its dependencies
        $items.Add("/webdav/400 Example Site/Home/000 Home.tpg") # local copy
        $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/_System/include",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/010 Articles",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/020 Further Information",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/About",$true

        # NOTE: The Content subtree contains shared items from "110 DXA Site Type" and "200 Example Content", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content",$true

        # NOTE: Some Taxonomy-based Sitemap items are shared from "110 DXA Site Type", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
        $items.Add("/webdav/400 Example Site/Building Blocks/Framework/Site Manager/Schemas/Page Navigation Metadata.xsd")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Sitemap [Navigation]",$true # The whole Taxonomy
    }

    "sitetype-only"
    {
        #Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties
        $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/_System/Publish HTML Design.tpg")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Building Blocks/Content/_Cloneable Content",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Building Blocks/Content/_Structure",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_Page Types",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/assets",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/include",$false
    }

    "content-only"
    {
        #Intentionally not selecting the Publication itself (it will be included as dependency anyways) because we don't want to overwrite the target Publication's properties
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content",$false
    }

    "website-only"
    {
        # TODO: Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties (title in particular)
        $items.Add("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml")
        $items.Add("/webdav/400 Example Site/Home/000 Home.tpg")
        $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure/Header",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure/Footer",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/_System/include",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/010 Articles",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/020 Further Information",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/About",$true
    }

    "module-GoogleAnalytics"
    {    
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/GoogleAnalytics",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/GoogleAnalytics",$true
    }

    "module-Search"
    {
        $items.Add("/webdav/400 Example Site/Home/Search Results.tpg")

        # NOTE: there are actually shared items, but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/Search",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Settings/Search",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules Content/Search",$true

        #TODO: importing fails without this for some reason (these are shared items again)
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Modules/Search",$true  
    }

    "module-MediaManager"
    {
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/MediaManager",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/MediaManager",$true
    }

    "module-ExperienceOptimization"
    {
        # NOTE: there are actually shared items, but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/ExperienceOptimization", $true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Settings/ExperienceOptimization", $true
    }

    "module-Impress"
    {
        # NOTE: there are actually shared items, but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/Impress", $true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Settings/Impress", $true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules Content/Impress",$true

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/Impress",$true

        #TODO: importing fails without this for some reason (these are shared items again)
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Modules/Impress",$true  
    }

    "module-51Degrees"
    {    
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/51Degrees",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/51Degrees",$true
    }

    "module-AudienceManager"
    {    
        $items.Add("/webdav/400 Example Site/Home/Login.tpg")

        # NOTE: these are actually shared items, but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/AudienceManager",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Settings/AudienceManager",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules Content/AudienceManager",$true

        #TODO: importing fails without this for some reason (these are shared items again)
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Modules/AudienceManager",$true  
    }

}

$selection += New-Object Tridion.ContentManager.ImportExport.ItemsSelection(,$items)

if ($exportType.StartsWith("module-"))
{
	$targetFile = $modulesFolder + $exportType.Substring(7) + "\$exportType.zip"
}
else
{
    $targetFile = "$($exportPackageFolder)$($exportType).zip"
    if (!(Test-Path $exportPackageFolder)) 
    {
        New-Item -Path $exportPackageFolder -ItemType Directory | Out-Null
    }
}

Export-CmPackage $targetFile $selection $exportInstruction

Write-Host "Done."