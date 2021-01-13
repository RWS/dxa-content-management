# TODO: Make it possible to pass $exportType as Array of strings
[CmdletBinding( SupportsShouldProcess=$false, PositionalBinding=$true)]
Param (
    # Specify what to export
    [string]$exportType = "all-publications", 

    # The URL of the CMS to export from
	[string]$cmsUrl = "http://cm.dev.dxa.sdldev.net:7086",

    # CD Layout target dir
	[string]$targetDir = 'C:\Temp\DXA',

    # The Target Data Contract Version for the export (defaults to SDL Tridion 9.0)
    [int]$targetDataContractVersion = 201701,
    
	# User name that is used to connect to CM
    [string]$cmsUserName = "vagrant",
    
	# User password that is used to connect to CM
    [string]$cmsUserPassword = "vagrant"
)

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

$cmsAuth = "Windows"
$commonDir = Join-Path $targetDir "cd-layout-common\"

#Include functions from ContentManagerUtils.ps1
$importExportFolder = Join-Path $PSScriptRoot "..\ImportExport"
. (Join-Path $importExportFolder "ContentManagerUtils.ps1")

# Initialization
if (!$cmsUrl.EndsWith("/")) { $cmsUrl = $cmsUrl + "/" }
$tempFolder = Get-TempFolder "DXA_export"

$exportPackageFolder = Join-Path $commonDir "cms\"
$modulesFolder = Join-Path $commonDir "modules\"

Initialize-ImportExport $importExportFolder $tempFolder
$coreServiceClient = Get-CoreServiceClient "Service"

# Prepare export
$exportInstruction = New-Object Tridion.ContentManager.ImportExport.ExportInstruction
$exportInstruction.LogLevel = "Normal"
$exportInstruction.TargetDataContractVersion = $targetDataContractVersion
$exportInstruction.BluePrintMode = [Tridion.ContentManager.ImportExport.BluePrintMode]::ExportSharedItemsFromOwningPublication
$selection = @()
$items = New-Object System.Collections.Generic.List[System.String]

$defaultReadOptions = New-Object Tridion.ContentManager.CoreService.Client.ReadOptions

$masterPubId = ($coreServiceClient.Read('/webdav/100 Master', $defaultReadOptions)).Id


# NOTE: workaround for CRQ-5672: Unable to import target group with context expression
# Excluding context expressions app data, it will be directly created in import script
$importExportClient = Get-ImportExportServiceClient
$categories = [Tridion.ContentManager.ImportExport.ApplicationDataCategory[]]$importExportClient.GetApplicationDataCategories($false)
$excludeCategories = @("Context Expressions", "Custom Pages")

[string[]]$appDataCategories = $categories | Where-Object { $excludeCategories -notcontains $_.CategoryId } `
                                           | Select -ExpandProperty 'CategoryId'

$exportInstruction.ApplicationDataCategoryIds = $appDataCategories


function Remove-ContentMashupContentType
{
	$result = $false
    Write-Host "Remove 'Content Mashup' Content Type"
    $applicationId = "SiteEdit"
    $readOptions = New-Object "Tridion.ContentManager.CoreService.Client.ReadOptions"
    $itemId = $coreServiceClient.Read("/webdav/110 DXA Site Type", $readOptions).Id.ToString()

	$applicationData = [Tridion.ContentManager.CoreService.Client.ApplicationData]$coreServiceClient.ReadApplicationData($itemId, $applicationId)
	$content = [System.Text.Encoding]::UTF8.GetString($applicationData.Data)

	[xml]$appDataXml = New-Object System.Xml.XmlDocument
	[void]$appDataXml.LoadXml($content)

	$ns = New-Object System.Xml.XmlNamespaceManager($appDataXml.NameTable)
	$ns.AddNamespace("ns", $appDataXml.DocumentElement.NamespaceURI)
	$ns.AddNamespace("xlink", "http://www.w3.org/1999/xlink")

	$contentType = $appDataXml.SelectSingleNode('//ns:configuration/ns:Publication/ns:ContentTypes/ns:ContentType[@Title="Tridion Docs"]', $ns)
	if ($contentType)
	{
		$result = $true
		Write-Host 'Static Widget is found and will be removed'
		$contentType.ParentNode.RemoveChild($contentType)    
			
		$component = $coreServiceClient.TryRead("/webdav/110 DXA Site Type/Building Blocks/Modules Content/TridionDocsMashup/Content/_Cloneable Content/Tridion Docs Static Widget.xml", $readOptions)
		if ($component)
		{
			$componentId = 	$component.Id.ToString()		
			$appDataXml.SelectNodes("//ns:ContentType[@xlink:href='$componentId']", $ns) | ForEach-Object { $_.ParentNode.RemoveChild($_)}
		}
			
	}
    else
    {
        Write-Warning "Tridion Docs Content Type not found"
    }

    $bicyclePageTemplateId =  Get-TcmUri '/webdav/110%20DXA%20Site%20Type/Building%20Blocks/Modules/TridionDocsMashup/Editor/Templates/Bicycle%20Page%20With%20Static%20Region.tptcmp'
	$pageTemplateElement = $appDataXml.SelectSingleNode("/ns:configuration/ns:Publication/ns:PageTemplateSettings/ns:PageTemplate[@xlink:href='$bicyclePageTemplateId']", $ns)
	if ($pageTemplateElement)
    {
        $result = $true
        Write-Host "Bicycle Page Template found and will be removed"
        $pageTemplateElement.ParentNode.RemoveChild($pageTemplateElement)
    }
    else
    {
        Write-Warning "Bicycle Page Template not found"
    }

    if ($result)
    {
		$appDataAdapter = New-Object Tridion.ContentManager.CoreService.Client.ApplicationDataAdapter $applicationId, $appDataXml.configuration
		$appData = $appDataAdapter.ApplicationData
		$coreServiceClient.SaveApplicationData($itemId, $appData)
    }

	return $result
}

function Remove-Search-Dependencies
{
    Remove-MetadataFromItem "/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg"
    Remove-MetadataFromItem "/webdav/100 Master/Home/_System"
    Remove-TemplateFromCompound "/webdav/100 Master/Building Blocks/Modules/Search/Developer/Search Template Building Blocks/Enable Search Indexing.tbbcmp" `
                              "/webdav/100 Master/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions.tbbcmp"
    Remove-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Modules Content/Search/Search Box Configuration.xml" `
                                 "/webdav/400 Example Site/Building Blocks/Modules/Search/Site Manager/Templates/Search Box.tctcmp" `
                                 "/webdav/400 Example Site/Home/_System/include/Header.tpg"
}

function Add-Search-Dependencies
{
    Add-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Modules Content/Search/Search Box Configuration.xml" `
                              "/webdav/400 Example Site/Building Blocks/Modules/Search/Site Manager/Templates/Search Box.tctcmp" `
							  "/webdav/400 Example Site/Home/_System/include/Header.tpg" `
							   -regionName "Nav"
    Add-TemplateToCompound "/webdav/100 Master/Building Blocks/Modules/Search/Developer/Search Template Building Blocks/Enable Search Indexing.tbbcmp" `
                           "/webdav/100 Master/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions.tbbcmp"
    Add-MetadataToItem "/webdav/100 Master/Home/_System" `
					   "/webdav/100 Master/Building Blocks/Modules/Search/Editor/Schemas/Search Indexing Metadata.xsd" `
                       "<Metadata xmlns=""http://www.sdl.com/web/schemas/search""><NoIndex>Yes</NoIndex></Metadata>"
    Add-MetadataToItem "/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg" `
					   "/webdav/110 DXA Site Type/Building Blocks/Modules/Search/Editor/Schemas/Search Indexing Metadata.xsd" `
					   "<Metadata xmlns=""http://www.sdl.com/web/schemas/search""><NoIndex>Yes</NoIndex></Metadata>"
}

function Remove-AudienceManager-Dependencies
{
    Remove-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Modules Content/AudienceManager/Current User Widget.xml" `
								 "/webdav/400 Example Site/Building Blocks/Modules/AudienceManager/Site Manager/Templates/Current User Widget.tctcmp" `
							     "/webdav/400 Example Site/Home/_System/include/Header.tpg"
}

function Add-AudienceManager-Dependencies
{
    Add-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Modules Content/AudienceManager/Current User Widget.xml" `
							  "/webdav/400 Example Site/Building Blocks/Modules/AudienceManager/Site Manager/Templates/Current User Widget.tctcmp" `
							  "/webdav/400 Example Site/Home/_System/include/Header.tpg" `
							  3 `
							  "Nav"
}

function Remove-GoogleAnalytics-Dependencies
{
    Remove-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Settings/GoogleAnalytics/Site Manager/Google Analytics Configuration.xml" `
								 "/webdav/400 Example Site/Building Blocks/Modules/GoogleAnalytics/Site Manager/Templates/Google Analytics.tctcmp" `
								 "/webdav/400 Example Site/Home/_System/include/Footer.tpg"
}

function Add-GoogleAnalytics-Dependencies
{
    Add-ComponentPresentation "/webdav/400 Example Site/Building Blocks/Settings/GoogleAnalytics/Site Manager/Google Analytics Configuration.xml" `
							  "/webdav/400 Example Site/Building Blocks/Modules/GoogleAnalytics/Site Manager/Templates/Google Analytics.tctcmp" `
							  "/webdav/400 Example Site/Home/_System/include/Footer.tpg" `
							  -regionName "Links"
}

function Add-ContentMashupContentType
{
    Write-Host "Add 'Content Mashup' Content Type"
    $applicationId = "SiteEdit"
	$readOptions = New-Object "Tridion.ContentManager.CoreService.Client.ReadOptions"

	$itemId = $coreServiceClient.Read("/webdav/110 DXA Site Type", $readOptions).Id.ToString()

	$componentId = $coreServiceClient.Read("/webdav/110 DXA Site Type/Building Blocks/Modules Content/TridionDocsMashup/Content/_Cloneable Content/Tridion Docs Static Widget.xml", $readOptions).Id.ToString()
	$componentTemplateId = $coreServiceClient.Read("/webdav/100 Master/Building Blocks/Modules/TridionDocsMashup/Editor/Templates/Tridion Docs Static Widget.tctcmp", $readOptions).Id.ToString()
	$folderId = $coreServiceClient.Read("/webdav/110 DXA Site Type/Building Blocks/Modules Content/TridionDocsMashup/Content/Tridion Docs", $readOptions).Id.ToString()
	$pageTemplateId = $coreServiceClient.Read("/webdav/110 DXA Site Type/Building Blocks/Modules/TridionDocsMashup/Editor/Templates/Bicycle Page With Static Region.tptcmp", $readOptions).Id.ToString()

	$applicationData = $coreServiceClient.ReadApplicationData($itemId, $applicationId)
	$content = [System.Text.Encoding]::UTF8.GetString($applicationData.Data)

	# Some of the method calls are being casted to [void] to prevent from being printed to the output
	[xml]$appDataXml = New-Object System.Xml.XmlDocument
	[void]$appDataXml.LoadXml($content)

	$ns = New-Object System.Xml.XmlNamespaceManager($appDataXml.NameTable)
	$ns.AddNamespace("ns", $appDataXml.DocumentElement.NamespaceURI)
	$ns.AddNamespace("xlink", "http://www.w3.org/1999/xlink")

	$contentType = $appDataXml.SelectSingleNode('//ns:configuration/ns:Publication/ns:ContentTypes/ns:ContentType[@Title="Tridion Docs"]', $ns)

	if (-not $contentType)
	{
		$contentType = $appDataXml.CreateElement("ContentType", $appDataXml.configuration.NamespaceURI)
		[void]$contentType.SetAttribute("Title", "Tridion Docs")
		[void]$contentType.SetAttribute("InsertPosition", "bottom")
		[void]$contentType.SetAttribute("Description", "DXA Content Type for Tridion Docs Mashup.")

		$contentTitle = $appDataXml.CreateElement("ContentTitle", $appDataXml.configuration.NamespaceURI)
		[void]$contentTitle.SetAttribute("Type", "prompt")

		$component = $appDataXml.CreateElement("Component", $appDataXml.configuration.NamespaceURI)
		$xlink = $appDataXml.CreateAttribute("xlink", "href", $ns.LookupNamespace("xlink"))
		$xlink.Value = $componentId
		[void]$component.SetAttributeNode($xlink)

		$componentTemplate = $appDataXml.CreateElement("ComponentTemplate", $appDataXml.configuration.NamespaceURI)
		$xlink = $appDataXml.CreateAttribute("xlink", "href", $ns.LookupNamespace("xlink"))
		$xlink.Value = $componentTemplateId
		[void]$componentTemplate.SetAttributeNode($xlink)

		$folder = $appDataXml.CreateElement("Folder", $appDataXml.configuration.NamespaceURI)
		$xlink = $appDataXml.CreateAttribute("xlink", "href", $ns.LookupNamespace("xlink"))
		$xlink.Value = $folderId
		[void]$folder.SetAttributeNode($xlink)
		$folder.SetAttribute("CanChange", "no")

		[void]$contentType.AppendChild($contentTitle)
		[void]$contentType.AppendChild($component)
		[void]$contentType.AppendChild($componentTemplate)
		[void]$contentType.AppendChild($folder)

		[void]$appDataXml.configuration.Publication.ContentTypes.AppendChild($contentType)

		$pageTemplate = $appDataXml.SelectSingleNode('//ns:configuration/ns:Publication/ns:PageTemplateSettings/ns:PageTemplate[@xlink:href="' + $pageTemplateId + '"]', $ns)

		if (-not $pageTemplate)
		{
			$pageTemplate = $appDataXml.CreateElement("PageTemplate", $appDataXml.configuration.NamespaceURI)
			$xlink = $appDataXml.CreateAttribute("xlink", "href", $ns.LookupNamespace("xlink"))
			$xlink.Value = $pageTemplateId
			[void]$pageTemplate.SetAttributeNode($xlink)
			[void]$pageTemplate.SetAttribute("usePredefinedContentTypes", "true")
			[void]$appDataXml.configuration.Publication.PageTemplateSettings.AppendChild($pageTemplate)
		}
		
		foreach ($pageTemplate in $appDataXml.configuration.Publication.PageTemplateSettings.PageTemplate)
		{
			$contentTypeCheck = $appDataXml.CreateElement("se:ContentType", $appDataXml.configuration.NamespaceURI)
			$xlink = $appDataXml.CreateAttribute("xlink", "href", $ns.LookupNamespace("xlink"))
			$xlink.Value = $componentId
			[void]$contentTypeCheck.SetAttributeNode($xlink)
			[void]$pageTemplate.AppendChild($contentTypeCheck)
		}

		$appDataAdapter = New-Object Tridion.ContentManager.CoreService.Client.ApplicationDataAdapter $applicationId, $appDataXml.configuration
		$appData = $appDataAdapter.ApplicationData

		$coreServiceClient.SaveApplicationData($itemId, $appData)
	}
}

$restoreDependencies = $false
$restoreContentMashupContentType = $false
if (!$exportType.StartsWith("module-"))
{
    Write-Host "Exporting DXA Core package '$exportType'; removing Module dependencies..."

    Remove-AudienceManager-Dependencies
    Remove-Search-Dependencies
    Remove-GoogleAnalytics-Dependencies
    $restoreContentMashupContentType = Remove-ContentMashupContentType
    $restoreDependencies = $true
}

if ($exportType -eq "all-publications")
{

    $groupNames = @("Developer", "Editor", "Site Manager") 
    $groupsFilterData = New-Object Tridion.ContentManager.CoreService.Client.GroupsFilterData

    foreach ($groupName in $groupNames)
    {
        $group = $coreServiceClient.GetSystemWideList($groupsFilterData) | Where-Object { $_.Title -eq $groupName }
        $items.Add($group.Id)
    }
	
    # From "000 Empty"
    $items.Add("/webdav/000 Empty") # Only the Publication and its dependencies
    $items.Add("/webdav/000 Empty/Building Blocks/Modified in the last 7 days")

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
    # Export individual Taxonomies, because Modules (e.g. TridionDocsMashup) may have their own, which shouldn't go in the Core packages:
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Content Type",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/List Sort Type",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Navigation Type",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Sitemap [Navigation]",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Social Networks",$true

    # From "110 DXA Site Type"
    $items.Add("/webdav/110 DXA Site Type") # Only the Publication and its dependencies
    $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
    $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")    
	# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
	$items.Add("/webdav/110 DXA Site Type/Home/_System/Publish HTML Design.tpg")
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/assets",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/config",$true
	$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Content Tools.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Footer.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Header.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Left Navigation.tpg")
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/mappings",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/resources",$true    
	# NOTE: We should not select TridionDocsMashup module page types that are present in the "_Page Types" folder
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Accordion Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Article Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Dynamic List.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Gallery Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Location Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/News Article Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Redirect Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page with Carousel.tpg")
	$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Tabbed Content Page.tpg")
    # See Content subtree below

    # From "200 Example Content"
    $items.Add("/webdav/200 Example Content") # Only the Publication and its dependencies
    # See Content subtree below

    # From "400 Example Site"
    $items.Add("/webdav/400 Example Site") # Only the Publication and its dependencies
    $items.Add("/webdav/400 Example Site/Home/000 Home.tpg") # local copy
    $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")	
	# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
	$items.Add("/webdav/400 Example Site/Home/_System/include/Content Tools.tpg")
	$items.Add("/webdav/400 Example Site/Home/_System/include/Footer.tpg")
	$items.Add("/webdav/400 Example Site/Home/_System/include/Header.tpg")
	$items.Add("/webdav/400 Example Site/Home/_System/include/Left Navigation.tpg")	
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/010 Articles",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/020 Further Information",$true
    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/About",$true
    # NOTE: The Content subtree contains shared items from "110 DXA Site Type" and "200 Example Content", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Cloneable Content",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/About",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Articles",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Downloads",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Further Information",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Homepage",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Images",$true
	$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Video",$true
	
    # NOTE: Some Taxonomy-based Sitemap items are shared from "110 DXA Site Type", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
    $items.Add("/webdav/400 Example Site/Building Blocks/Framework/Site Manager/Schemas/Page Navigation Metadata.xsd")
	# NOTE: We should not select TridionDocsMashup module keyword which is present in the "Sitemap [Navigation]" category (whole Taxonomy)
	$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/000 Home.tkw")
	$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/010 Articles.tkw")
	$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/020 Further Information.tkw")
	$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/About.tkw")

    # Export all available dependencies, so do not set $exportInstruction.ExpandDependenciesOfTypes
}
else
{
    if ($exportType -ne "module-Test" -and $exportType -ne "framework-only")
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
}

function Get-ExportItems($path)
{
	$filter = New-Object Tridion.ContentManager.CoreService.Client.OrganizationalItemItemsFilterData
    $filter.ItemTypes = @([Tridion.ContentManager.CoreService.Client.ItemType]::Page, [Tridion.ContentManager.CoreService.Client.ItemType]::Component)
    $filter.BaseColumns = [Tridion.ContentManager.CoreService.Client.ListBaseColumns]::Extended
    $filter.IncludeRelativeWebDavUrlColumn = $true
	$list = [Tridion.ContentManager.CoreService.Client.RepositoryLocalObjectData[]]$coreServiceClient.GetList($path,$filter)
    $exportItems = @()
    foreach ($listItem in $list)
    {
        if ($listItem.BluePrintInfo.IsShared -ne $true)
        {            
            $exportItems += "$path/$($listItem.LocationInfo.WebDavUrl)"
        }            
    }    
    return $exportItems	
}

switch ($exportType)
{
    "master-only"
    {
        $items.Add("/webdav/000 Empty/Building Blocks/Modified in the last 7 days")

        #Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties
        $items.Add("/webdav/100 Master/Building Blocks/Content")
        $items.Add("/webdav/100 Master/Building Blocks/Modules")
        $items.Add("/webdav/100 Master/Building Blocks/Modules Content")
        $items.Add("/webdav/100 Master/Building Blocks/Settings")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/Core",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/Core",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Home",$true
        # Export individual Taxonomies, because Modules (e.g. TridionDocsMashup) may have their own, which shouldn't go in the Core packages:
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Content Type",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/List Sort Type",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Navigation Type",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Sitemap [Navigation]",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Social Networks",$true
    }
	
	"framework-only"
    {
		$groupNames = @("Developer", "Editor", "Site Manager") 
		$groupsFilterData = New-Object Tridion.ContentManager.CoreService.Client.GroupsFilterData

		foreach ($groupName in $groupNames)
		{
			$group = $coreServiceClient.GetSystemWideList($groupsFilterData) | Where-Object { $_.Title -eq $groupName }
			$items.Add($group.Id)
		}
		
		$items.Add("/webdav/100 Master/Building Blocks")
		$items.Add("/webdav/100 Master/Building Blocks/Framework")
		$items.Add("/webdav/100 Master/Building Blocks/Framework/Developer/Templates/Generate Navigation.tctcmp")
		$items.Add("/webdav/100 Master/Building Blocks/Framework/Developer/Templates/Publish Settings.tctcmp")
		$items.Add("/webdav/100 Master/Building Blocks/Framework/Developer/Templates/JSON.tptcmp")
		$items.Add("/webdav/100 Master/Building Blocks/Settings")		
	
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework/Developer/Schemas",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework/Developer/Templates/DXA%2ELegacy",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework/Developer/Templates/DXA%2ER2",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework/Developer/Templates/DXA%2EUpgrade",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Framework/Site Manager",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/Core",$true	
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Home",$true	
    }

    "example-publications"
    {
		$items.Add("/webdav/110 DXA Site Type/DXA Development")
		$items.Add("/webdav/400 Example Site/DXA Staging%2FLive")

        # From "110 DXA Site Type"
        $items.Add("/webdav/110 DXA Site Type") # Only the Publication and its dependencies
        $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")
		# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
		$items.Add("/webdav/110 DXA Site Type/Home/_System/Publish HTML Design.tpg")
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/assets",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/config",$true
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Content Tools.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Footer.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Header.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Left Navigation.tpg")
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/mappings",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/resources",$true    
		# NOTE: We should not select TridionDocsMashup module page types that are present in the "_Page Types" folder
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Accordion Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Article Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Dynamic List.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Gallery Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Location Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/News Article Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Redirect Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page with Carousel.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Tabbed Content Page.tpg")
        # See Content subtree below

        # From "200 Example Content"
        $items.Add("/webdav/200 Example Content") # Only the Publication and its dependencies
        # See Content subtree below

        # From "400 Example Site"
        $items.Add("/webdav/400 Example Site") # Only the Publication and its dependencies
        $items.Add("/webdav/400 Example Site/Home/000 Home.tpg") # local copy
        $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")
		# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
		$items.Add("/webdav/400 Example Site/Home/_System/include/Content Tools.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Footer.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Header.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Left Navigation.tpg")	
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/010 Articles",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/020 Further Information",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/About",$true

        # NOTE: The Content subtree contains shared items from "110 DXA Site Type" and "200 Example Content", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Cloneable Content",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/About",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Articles",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Downloads",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Further Information",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Homepage",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Images",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/Video",$true
		$items.Add("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml")

        # NOTE: Some Taxonomy-based Sitemap items are shared from "110 DXA Site Type", but BluePrintMode.ExportSharedItemsFromOwningPublication will export them in their Owning Publication.
        $items.Add("/webdav/400 Example Site/Building Blocks/Framework/Site Manager/Schemas/Page Navigation Metadata.xsd")
		# NOTE: We should not select TridionDocsMashup module keyword which is present in the "Sitemap [Navigation]" category (whole Taxonomy)
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/000 Home.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/010 Articles.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/020 Further Information.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/About.tkw")
    }

    "sitetype-only"
    {
        #Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties
        $items.Add("/webdav/110 DXA Site Type/Home/_Error Page Not Found.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/000 Home.tpg")
        $items.Add("/webdav/110 DXA Site Type/Home/_System/Publish HTML Design.tpg")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Building Blocks/Content/_Cloneable Content",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Building Blocks/Content/_Structure",$true
		# NOTE: We should not select TridionDocsMashup module page types that are present in the "_Page Types" folder
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Accordion Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Article Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Dynamic List.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Gallery Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Location Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/News Article Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Redirect Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Section Page with Carousel.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_Page Types/Tabbed Content Page.tpg")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Home/_System/assets",$true
        # NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Content Tools.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Footer.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Header.tpg")
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/Left Navigation.tpg")	
    }

    "content-only"
    {
        #Intentionally not selecting the Publication itself (it will be included as dependency anyways) because we don't want to overwrite the target Publication's properties
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/_Cloneable Content",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/_Structure",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/About",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Articles",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Downloads",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Further Information",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Homepage",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Images",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Content/Video",$true
    }

    "website-only"
    {
        # TODO: Intentionally not selecting the Publication itself because we don't want to overwrite the target Publication's properties (title in particular)
        $items.Add("/webdav/400 Example Site/Building Blocks/Content/Sitemap.xml")
        $items.Add("/webdav/400 Example Site/Home/000 Home.tpg")
        $items.Add("/webdav/400 Example Site/Home/Sitemap.tpg")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure/Header",$false
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Content/_Structure/Footer",$false
		# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
		$items.Add("/webdav/400 Example Site/Home/_System/include/Content Tools.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Footer.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Header.tpg")
		$items.Add("/webdav/400 Example Site/Home/_System/include/Left Navigation.tpg")	
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
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/MediaManager/Editor/Templates",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/MediaManager",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100%20Master/Building%20Blocks/Modules/MediaManager/Admin",$true
		$items.Add("/webdav/100%20Master/Building%20Blocks/Modules/MediaManager")
	    $items.Add("/webdav/100%20Master/Building%20Blocks/Modules/MediaManager/Editor")
	    $items.Add("/webdav/100%20Master/Building%20Blocks/Modules/MediaManager/Editor/Schemas")
	    $items.Add("/webdav/100%20Master/Building%20Blocks/Modules/MediaManager/Editor/Schemas/SDL%20Media%20Manager")
		$items.Add("/webdav/100%20Master/Building%20Blocks/Modules/MediaManager/Editor/Schemas/SDL%20Media%20Manager/ExternalContentLibraryStubSchema-mm.xsd")
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

    "module-Ugc"
    {
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/Ugc", $true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules Content/Ugc",$true
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
	
    "module-TridionDocsMashup"
    {    
		#Categories and Keywords
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Content Reference Type",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Product Family Name",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Product Release Name",$true
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/030 Products.tkw")
		
		#Schemas and Component Templates for Editorial Flow, Developer Flow and Example Product
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Modules/TridionDocsMashup",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/100 Master/Building Blocks/Settings/TridionDocsMashup",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Modules Content/TridionDocsMashup",$true
		

		#Include Page Template, Example Product Page Template and Product Example Page
		$items.Add("/webdav/110 DXA Site Type/Home/_System/include/TridionDocsMashup:Topics.tpg")
		$items.Add("/webdav/400 Example Site/Home/_Page Types/Bicycle Page.tpg")
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Home/030 Products",$true
		
		#TODO: importing fails without this for some reason (these are shared items again) AND exporting Default Page Template that was included in 110 Publication
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/110 DXA Site Type/Building Blocks/Modules/TridionDocsMashup",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/200 Example Content/Building Blocks/Modules/TridionDocsMashup",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules/TridionDocsMashup",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/400 Example Site/Building Blocks/Modules Content/TridionDocsMashup",$true
    }

    "module-Test"
	{		
        $items.Add("/webdav/100 Master/Building Blocks/cx.isAndroid.ttg")
        $items.Add("/webdav/100 Master/Building Blocks/cx.isApple.ttg")
        $items.Add("/webdav/100 Master/Building Blocks/cx.isChrome.ttg")
        $items.Add("/webdav/100 Master/Building Blocks/cx.isMobile.ttg")

        $items.Add("/webdav/100 Master/Building Blocks/Modules_Dev/Flickr")   


		# From "401 adcevora.com"
        <# 401 adcevora.com is not needed for automated tests. Skipping for now because it is not compatible with Native Regions (yet).
	    $items.Add("/webdav/401 adcevora.com")
		$items.Add("/webdav/401 adcevora.com/Home/_Error Page Not Found.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/_Navigation.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/_Vanity Url Test.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/Login.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/redirect_test.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/Search Results.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/Special Offer Test.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/test with es.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/test1243_1.tpg")
		$items.Add("/webdav/401 adcevora.com/Home/TSI-2368.tpg")
	    $items.Add("/webdav/401 adcevora.com/Home/000 Home.tpg")
	    $items.Add("/webdav/401 adcevora.com/Home/Sitemap.tpg")

		$items.Add("/webdav/401%20adcevora%2Ecom/DXA%20Evora%20BPT")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Framework/Developer/Templates/DXA%2ER2/Default%20Page%20Template%20Finish%20Actions.tbbcmp")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Framework/Developer/Templates/DXA%2ER2")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions.tbbcmp")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions (With Search Indexing).tbbcmp")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Page Content.tbbcmp")

		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules/Core/Editor/Templates/Content Page Without Navigation.tptcmp")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules/Test/TestPageMarkup.tptcmp")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules/Test/TestPageMarkup.tptcmp")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules/Search/Developer/Search%20Template%20Building%20Blocks/Enable%20Search%20Indexing.tbbcmp")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules/Core/Editor/Schemas/Download.xsd")

		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules/Core/Editor/Schemas/Image.xsd")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules/Core/Editor/Templates/Dynamic Article CT.tctcmp")

		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules Content/Current User Widget.xml")
		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules Content/Login Form.xml")

		$items.Add("/webdav/401 adcevora.com/Building Blocks/Modules/Test/TestEclEntity.xsd")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Content/Downloads")


		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules/Test")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules/Test/Test%20Page%20Metadata.xsd")

		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules_Dev/Flickr/D5F/C9F/ecl%3A0-flickr-5695933543_456ce40ba4_72157626542559591-img-file.ecl")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules_Dev/Flickr/Copy%20of%20Flickr%20Image.tctcmp")
		$items.Add("/webdav/401%20adcevora%2Ecom/Content%20Type/Parent%20Keyword.tkw")
		$items.Add("/webdav/401%20adcevora%2Ecom/Building%20Blocks/Modules_Dev/Flickr/Flickr%20Image.tctcmp")


	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/_System/include",$false
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/010 Rooms",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/020 Location",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/030 Photos",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401%20adcevora%2Ecom/Home/040%20More%2E%2E%2E",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/050 External",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/About",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/SmartTarget",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Home/Test",$true

	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Building Blocks/Content",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Building Blocks/Modules/Test",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Building Blocks/Modules/Core/Editor/Templates",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Building Blocks/Modules/Products",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 adcevora.com/Building Blocks/Modules/MediaManager",$true


	    $items.Add("/webdav/500 adcevora.com (Portuguese)")
		$items.Add("/webdav/500%20adcevora%2Ecom%20%28Portuguese%29/DXA%20Evora%20BPT")
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Home/010 Quartos",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Home/020 Localiza%C3%A7%C3%A3o",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Home/030 Galleria",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Home/040 Mais%2E%2E%2E",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Home/050 External",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Portuguese)/Building Blocks/Content",$true

		$items.Add("/webdav/500 adcevora.com (Spanish)")
		$items.Add("/webdav/500%20adcevora%2Ecom%20%28Spanish%29/DXA%20Evora%20BPT")
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Home/010 Habitaciones",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Home/020 Ubicaci%C3%B3n",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Home/030 Fotos",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Home/040 Mas%2E%2E%2E",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Home/050 External",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 adcevora.com (Spanish)/Building Blocks/Content",$true

		$items.Add("/webdav/600 adcevora.com (Mexican)")
		$items.Add("/webdav/600%20adcevora%2Ecom%20%28Mexican%29/DXA%20Evora%20BPT")
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Home/010 Habitaciones",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Home/020 Ubicaci%C3%B3n",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Home/030 Fotos",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Home/040 Mas%2E%2E%2E",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Home/050 External",$true
	    $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/600 adcevora.com (Mexican)/Building Blocks/Content",$true
        #>


		# From "401 Automated Test Parent"
        $items.Add("/webdav/401 Automated Test Parent")
        $items.Add("/webdav/401 Automated Test Parent/Building Blocks/Modules/Core/Editor/Schemas/Image.xsd")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Test Taxonomy [Navigation]",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Test Taxonomy [Navigation]",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Test Taxonomy 2 (not Navigation)",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/TSI-811 Boolean Category",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/TSI-811 Test Category",$true
		# NOTE: We should not select TridionDocsMashup module keyword which is present in the "Sitemap [Navigation]" category (whole Taxonomy)
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/000 Home.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/010 Articles.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/020 Further Information.tkw")
		$items.Add("/webdav/400 Example Site/Sitemap [Navigation]/About.tkw")
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Building Blocks/Content/Test",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Building Blocks/Modules/Test",$true
		# NOTE: We should not select TridionDocsMashup module include pages that are present in the "include" folder
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/_System/assets",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/_System/config",$true

		$items.Add("/webdav/401 Automated Test Parent/Building Blocks/Settings/Core/Site Manager/Language Selector Configuration.xml")
		$items.Add("/webdav/401 Automated Test Parent/Building Blocks/Settings/Core/Site Manager/Localization Configuration.xml")

		$items.Add("/webdav/401 Automated Test Parent/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Page Content.tbbcmp")
		$items.Add("/webdav/401 Automated Test Parent/Building Blocks/Modules/MediaManager/Editor/Schemas/SDL Media Manager/ExternalContentLibraryStubSchema-mm.xsd")

		# NOTE: Need to add folder as item explicitly to export all related data. Otherwise it matches the folder from parent publication and causes incorrect publishing
		$items.Add("/webdav/401 Automated Test Parent/Home")

		$items.Add("/webdav/401 Automated Test Parent/Sitemap [Navigation]")

		$items.Add("/webdav/401 Automated Test Parent/Home/_System")
		$items.Add("/webdav/401 Automated Test Parent/Home/_System/include/Content Tools.tpg")
		$items.Add("/webdav/401 Automated Test Parent/Home/_System/include/Footer.tpg")
		$items.Add("/webdav/401 Automated Test Parent/Home/_System/include/Header.tpg")
		$items.Add("/webdav/401 Automated Test Parent/Home/_System/include/Left Navigation.tpg")
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/_System/mappings",$true
		$selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/_System/resources",$true    
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/Acceptance",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/Smoke",$true
        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/401 Automated Test Parent/Home/Regression",$true

        $temp = Get-ExportItems "/webdav/401 Automated Test Parent/Home"
        
        $items.Add("/webdav/500 Automated Test Child")
		$items.Add("/webdav/500 Automated Test Child/Home")
		$items.Add("/webdav/500 Automated Test Child/Home/_System")

		$items.Add("/webdav/500 Automated Test Child/Building Blocks/Settings/Core/Site Manager/Localization Configuration.xml")
		$items.Add("/webdav/500 Automated Test Child/Building Blocks/Framework/Developer/Templates/Generate Data Presentation (Disabled).tctcmp")
		$items.Add("/webdav/500 Automated Test Child/Building Blocks/Framework/Developer/Templates/Publish Settings.tctcmp")


		$temp += Get-ExportItems "/webdav/500 Automated Test Child/Building Blocks/Content/Test"
        $temp += Get-ExportItems "/webdav/500 Automated Test Child/Building Blocks/Content/Test/Regression"
        $temp += Get-ExportItems "/webdav/500 Automated Test Child/Building Blocks/Content/Test/Smoke"
        $temp += Get-ExportItems "/webdav/500 Automated Test Child/Home"
        $temp += Get-ExportItems "/webdav/500 Automated Test Child/Home/Regression"
        $temp += Get-ExportItems "/webdav/500 Automated Test Child/Home/Smoke"
		
        $items.Add("/webdav/500 Automated Test Parent (Legacy)")
        $items.Add("/webdav/500 Automated Test Parent (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Component Template Finish Actions.tbbcs")
        $items.Add("/webdav/500 Automated Test Parent (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions.tbbcs")
        $items.Add("/webdav/500 Automated Test Parent (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Component Content.tbbcs")
        $items.Add("/webdav/500 Automated Test Parent (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Page Content.tbbcs")

        $items.Add("/webdav/500 Example Site (Legacy)")
        $items.Add("/webdav/500 Example Site (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Component Template Finish Actions.tbbcs")
        $items.Add("/webdav/500 Example Site (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Default Page Template Finish Actions.tbbcs")
        $items.Add("/webdav/500 Example Site (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Component Content.tbbcs")
        $items.Add("/webdav/500 Example Site (Legacy)/Building Blocks/Framework/Developer/Templates/DXA.R2/Render Page Content.tbbcs")

        $selection += New-Object Tridion.ContentManager.ImportExport.SubtreeSelection "/webdav/500 Example Site (Legacy)/Building Blocks/Modules/Core (Legacy)",$true
        # Ensure that localized Pages are included:
        $temp += Get-ExportItems "/webdav/500 Example Site (Legacy)/Home"
        $temp += Get-ExportItems "/webdav/500 Example Site (Legacy)/Home/010 Articles"
        $temp += Get-ExportItems "/webdav/500 Example Site (Legacy)/Home/010 Articles/010 News"
        $temp += Get-ExportItems "/webdav/500 Example Site (Legacy)/Home/020 Further Information"
        $temp += Get-ExportItems "/webdav/500 Example Site (Legacy)/Home/About"
        
        $items.Add("/webdav/600 Automated Test Child (Legacy)")

        $selection += New-Object Tridion.ContentManager.ImportExport.ItemsSelection(,[System.Collections.Generic.List[System.String]]$temp)
	}
}

$selection += New-Object Tridion.ContentManager.ImportExport.ItemsSelection(,$items)

if ($exportType.StartsWith("module-"))
{
    $targetFile = $modulesFolder + $exportType.Substring(7) + "\$exportType.zip"
    $moduleFolder = $targetFile | Split-Path
    if (!(Test-Path $moduleFolder)) 
    {
        New-Item -Path $moduleFolder -ItemType Directory | Out-Null
    }
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

if ($restoreContentMashupContentType)
{
	Write-Host "Restore 'ContentMashup' content type because it was removed before 'Export'"
	Add-ContentMashupContentType
}

if ($restoreDependencies)
{
    Write-Host "Restoring Module dependencies..."
    Add-AudienceManager-Dependencies
    Add-Search-Dependencies
    Add-GoogleAnalytics-Dependencies
}

$coreServiceClient.Dispose()
Write-Host "Done."