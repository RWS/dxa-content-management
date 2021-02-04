#Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope Process

Param (
    # Module to export permissions for
    [string]$module = "Core",

    # Set this to the DXA Development CMS URL
    [string]$cmsUrl = "http://localhost",
	
	# Comma separated list of Groups to export rights and permissions from
    [string]$groups = "Everyone,Developer,Editor,Site Manager"
)


# Terminate script on first occurred exception
trap {
    Write-Error $_.Exception.Message
    exit 1
}

function Invoke-InitDlls {
    if (-not $global:ImportExportIsInitialized) {
        Add-Type -assemblyName mscorlib
        Add-Type -assemblyName System.ServiceModel
        Add-Type -Path "$($dllsFolder)Tridion.Common.dll"
        Add-Type -Path "$($dllsFolder)Tridion.ContentManager.CoreService.Client.dll"

        $global:ImportExportIsInitialized = $true
    }
}

function Get-CoreServiceClient {
    param(
        [parameter(Mandatory=$false)]
        [AllowNull()]
        [ValidateSet("Service","Upload","Download")]
        [string]$type="Service"
    )        
    
	$binding = New-Object System.ServiceModel.WSHttpBinding
	$binding.MaxBufferPoolSize = [int]::MaxValue
	$binding.MaxReceivedMessageSize = [int]::MaxValue
	$binding.ReaderQuotas.MaxArrayLength = [int]::MaxValue
	$binding.ReaderQuotas.MaxBytesPerRead = [int]::MaxValue
	$binding.ReaderQuotas.MaxStringContentLength = [int]::MaxValue
	$binding.ReaderQuotas.MaxNameTableCharCount = [int]::MaxValue
	
	switch($type)
	{
		"Service" {
			$binding.Security.Mode = "Message"
			$endpoint = New-Object System.ServiceModel.EndpointAddress ($cmsUrl + "/webservices/CoreService201501.svc/wsHttp")
			New-Object Tridion.ContentManager.CoreService.Client.SessionAwareCoreServiceClient $binding,$endpoint
        }
	}
}

function Export-Rights($publicationTitle) 
{
    $webdavUrl = "/webdav/$publicationTitle"
    Write-Host "Exporting Rights of '$webdavUrl' ..."
    $pub = $core.Read($webdavUrl, $readOptions)

    $xmlPub = $xmlDoc.CreateElement("publication")
    $xmlPub.SetAttribute("path", $pub.LocationInfo.WebDavUrl)

    foreach ($entry in $pub.AccessControlList.AccessControlEntries) 
    {
        # Only export rights of listed groups
        $title = $entry.Trustee.Title
        $rights = ($entry.AllowedRights -replace ", ", ",")
        if ($groupList -contains $title) 
        {
            $xmlRights = $xmlDoc.CreateElement("rights")
            $xmlRights.SetAttribute("group", $title)
            $xmlRights.InnerText =$rights
            $dummy = $xmlPub.appendChild($xmlRights)
            Write-Host "`t$title : $rights"
        }
    }

    $dummy = $xmlRoot.appendChild($xmlPub)
}

function Export-PermissionsForOrgItem($idOrWebdavUrl) 
{
    $orgItem = $core.TryRead($idOrWebdavUrl, $readOptions)
    if (!$orgItem)
    {
        Write-Warning "Organizational Item '$idOrWebdavUrl' does not exist; skipping."
        return $orgItem
    }

    $webdavUrl = $orgItem.LocationInfo.WebDavUrl
    Write-Host "Exporting Permissions of '$webdavUrl' ..."

    # Only export Permissions that are not inherited
    if ($orgItem.IsPermissionsInheritanceRoot) 
    {
        $xmlOrgItem = $xmlDoc.CreateElement("organizationalItem")
        $xmlOrgItem.SetAttribute("path", $webdavUrl)

        foreach ($entry in $orgItem.AccessControlList.AccessControlEntries) 
        {
            # Only export permissions of listed groups
            $title = $entry.Trustee.Title
            $permissions = ($entry.AllowedPermissions -replace ", ", ",")
            if ($groupList -contains $title) 
            {
                $xmlPermissions = $xmlDoc.CreateElement("permissions")
                $xmlPermissions.SetAttribute("group", $title)
                $xmlPermissions.InnerText = $permissions
                $dummy = $xmlOrgItem.appendChild($xmlPermissions)
                Write-Host "`t$title : $permissions"
            }
        }

        $dummy = $xmlRoot.appendChild($xmlOrgItem)
    } 
    else 
    {
        Write-Host "`tOrganizational Item '$webdavUrl' has inherited permissions; skipping."
    }

    return $orgItem
}

function Export-PermissionsForSubtree($idOrWebdavUrl) 
{
    $orgItem = Export-PermissionsForOrgItem $idOrWebdavUrl
    if (!$orgItem)
    {
        return;
    }

    # Recurse:
    # Filter on Folders and Structure Groups
    $filter = New-Object Tridion.ContentManager.CoreService.Client.OrganizationalItemItemsFilterData
    $filter.ItemTypes = (2, 4)
    $children = $core.GetList($idOrWebdavUrl, $filter)

    foreach ($item in $children) 
    {
        Export-PermissionsForSubtree $item.Id
    }
}

function Export-Permissions($publicationTitle, $organizationalItemPath, $recursive = $true) 
{
    $webdavUrl = "/webdav/$publicationTitle/$organizationalItemPath"

    if ($recursive)
    {
        Export-PermissionsForSubtree $webdavUrl
    }
    else
    {
        $dummy = Export-PermissionsForOrgItem $webdavUrl
    }

    Write-Host "=========="
}

# Initialization
$tempFolder = Join-Path $env:ALLUSERSPROFILE "DXA_export_permissions\"
if (!(Test-Path $tempFolder)) {
    New-Item -ItemType Directory -Path $tempFolder | Out-Null
}
$dllsFolder = Join-Path $PSScriptRoot "..\ImportExport\sites9\"
$cmsFolder = Join-Path $targetDir "cms\"

Invoke-InitDlls
$groupList = $groups.Split(",")
if (!(Test-Path $cmsFolder)) {
    New-Item -Path $cmsFolder -ItemType Directory | Out-Null
}
$xmlFile = Join-Path $cmsFolder "permissions.xml"
if ($module -ne "Core")
{
	$moduleFolder = Join-Path $targetDir "modules\$module"
	if (!(Test-Path $moduleFolder)) {
		New-Item -Path $moduleFolder -ItemType Directory | Out-Null
	}
	$xmlFile = Join-Path $moduleFolder "permissions.xml"
}

Write-Host "CMS: '$cmsUrl'"
Write-Host "Groups: '$groups'"
Write-Host "Module: '$module'"
Write-Host "Output path: '$xmlFile'"

$xmlDoc = New-Object System.XML.XMLDocument
$xmlRoot = $xmlDoc.CreateElement("export")

# Create core service client and default read options
$core = Get-CoreServiceClient "Service"
$readOptions = New-Object Tridion.ContentManager.CoreService.Client.ReadOptions

if ($module -eq "Core")
{

	# Export Rights from Publications
	Export-Rights "000 Empty"
	Export-Rights "100 Master"
	Export-Rights "110 DXA Site Type"
	Export-Rights "200 Example Content"
	Export-Rights "400 Example Site"

	# Export Permissions from Organizational Items
	Export-Permissions "000 Empty" "Building Blocks" 
	Export-Permissions "000 Empty" "Home"

	Export-Permissions "100 Master" "Building Blocks/Framework"
	Export-Permissions "100 Master" "Building Blocks/Modules" $false
	Export-Permissions "100 Master" "Building Blocks/Settings" $false
	Export-Permissions "100 Master" "Building Blocks/Content"

	Export-Permissions "110 DXA Site Type" "Building Blocks/Content"
	Export-Permissions "110 DXA Site Type" "Home/_Page Types"

	Export-Permissions "200 Example Content" "Building Blocks/Content"

	Export-Permissions "400 Example Site" "Home/010 Articles"
	Export-Permissions "400 Example Site" "Home/020 Further Information"
	Export-Permissions "400 Example Site" "Home/About"

	# Export Permissions of these Categories too
	Export-Permissions "100 Master" "Content Classification" $false
	Export-Permissions "100 Master" "Content Type" $false
	Export-Permissions "100 Master" "List Sort Type" $false
	Export-Permissions "100 Master" "Navigation Type" $false
	Export-Permissions "100 Master" "Social Networks" $false
}

# Export Permissions for given Module
Export-Permissions "100 Master" ("Building Blocks/Modules/" + $module)
Export-Permissions "100 Master" ("Building Blocks/Settings/" + $module)
Export-Permissions "400 Example Site" ("Building Blocks/Modules Content/" + $module)

$core.dispose()

Write-Host "Saving '$xmlFile' ..."
$dummy = $xmlDoc.appendChild($xmlRoot)
$xmlDoc.Save($xmlFile)

Write-Host "Done."
