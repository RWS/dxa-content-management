<#
.SYNOPSIS
   Add DXA Tridion Custom Resolver to CMS/Publishing Servers.
.DESCRIPTION
   Adds the DXA Custom Resolver to the CMS/Publishing server's GAC and modifies the Tridion.ContentManager.config (suitable for R2 mapping only).
.EXAMPLE
   & .\cms-customresolver.ps1
#>

[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding=$false, DefaultParametersetName='None')]
param (   
    #File system path of the root folder of DXA UI extension
    [Parameter(Mandatory=$true, HelpMessage="Folder where DXA UI Extension is installed")]
    [string]$extensionTargetFolder,

    #Name of Tridion CM site under IIS
    [Parameter(Mandatory=$false, HelpMessage="Name of Tridion CM site under IIS")]
    [string]$cmSiteName = "SDL Web",

    [Parameter(Mandatory=$false, HelpMessage="True if the UI extension should be deployed on a non secure CM (http)")]    
    [bool]$useHttpEndpoint = $true,

    [Parameter(Mandatory=$false, HelpMessage="True if the UI extension should be deployed on a secure CM (https)")]    
    [bool]$useHttpsEndpoint = $false
)

function GetOrCreate-Node([string]$path)
{
    $node = $config.SelectSingleNode($path)
    if(!$node)
    {
        $parts = $path.Split("/", [System.StringSplitOptions]::RemoveEmptyEntries)
        $path = ""        
        $parent = $config
        foreach($part in $parts)
        {
            $path += "/$part"
            $node = $config.SelectSingleNode($path)
            if($part -eq "dependentAssembly")
            {
                $node = $null
            }
            if(!$node)
            {
                $index = $part.IndexOf("[")
                if($index -Eq -1)
                {
                    $index = $part.Length
                }
                $part = $part.Substring(0, $index)
                $node = $config.CreateElement($part)
                $parent.AppendChild($node) | Out-Null
            }
            $parent = $node
        }        
    }
    return $node
}

function Set-Attribute([string]$path, [string]$attributeName, [string]$attributeValue)
{   
    $node = GetOrCreate-Node($path)
    $node.SetAttribute($attributeName, $attributeValue)    
}

function AddOrRemove-CustomResolverForType([string]$itemType)
{   
    $resolvers = $config.SelectSingleNode("/configuration/resolving/mappings/add[@itemType='$itemType']/resolvers")
    if($resolvers)
    {
        $node = $config.SelectSingleNode("/configuration/resolving/mappings/add[@itemType='$itemType']/resolvers/add[@type='$resolverName.$resolverTypeName']")
        if($node)
        {
            $resolvers.RemoveChild($node) | Out-Null
            $restartRequired = $true
        }
                    
        Write-Host " > Adding Custom Resolver configuration elements for mapping '$itemType'..."
        $node = $config.CreateElement("add")
        $node.SetAttribute("type", "$resolverName.$resolverTypeName")
        $node.SetAttribute("assembly", $asm)
        $resolvers.AppendChild($node) | Out-Null
        $restartRequired = $true
    }
}

function Add-ServiceEndpoint($parentElement, $binding)
{
    $name = "Services"
    if($binding -eq "Https")
    {
        $name = "ServicesSecure"
    }
    $endpoint = $parentElement.SelectSingleNode("endpoint[@name='$name']")
    if(!$endpoint)
    {
        $endpoint = $modelsWebConfig.CreateElement("endpoint")
        $endpoint.SetAttribute("name", "$name")
        $endpoint.SetAttribute("address", "")
        $endpoint.SetAttribute("behaviorConfiguration", "Tridion.Web.UI.ContentManager.WebServices.AspNetAjaxBehavior")
        $endpoint.SetAttribute("binding", "webHttpBinding")
        $endpoint.SetAttribute("bindingConfiguration", "Tridion.Web.UI.ContentManager.WebServices.Web$($binding)BindingConfig")
        $endpoint.SetAttribute("contract", "DXA.CM.Extensions.DXAResolver.Models.Interfaces.IServices")
        $parentElement.AppendChild($endpoint) | Out-Null
    }
}

function Remove-ServiceEndpoint($parentElement, $binding)
{
    $name = "Services"
    if($binding -eq "Https")
    {
        $name = "ServicesSecure"
    }
    $endpoint = $parentElement.SelectSingleNode("endpoint[@name='$name']")
    if($endpoint)
    {
        $parentElement.RemoveChild($endpoint) | Out-Null
    }
}

function TryStart-Service
{
    param ([string[]] $serviceNames)

    foreach ($serviceName in $serviceNames)
    {
        $service = Get-Service -Name $serviceName
        if ($service.StartType -ne 'Disabled')
        {
            Start-Service $serviceName
        }
        else
        {
            Write-Host "$serviceName service is Disabled"
        }
    }
}

function Test-IISPathExists($siteName)
{
    $IISPath = "IIS:\Sites\$siteName"
    return Test-Path $IISPath
}

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

$scriptHome = Split-Path $MyInvocation.MyCommand.Path
$scriptHome = $scriptHome.TrimEnd("\")
$distSource = "$scriptHome\extensions\DxaResolver"


$PSScriptDir = Split-Path $MyInvocation.MyCommand.Path
$resolverName = "Sdl.Web.DXAResolver"
$resolverTypeName = "Resolver"
$resolverAsm = "$PSScriptDir\$resolverName.dll"
$templateName = "Generate Data Presentation"

$restartRequired = $false

Import-Module "WebAdministration"

# if we're running on a CMS server install custom resolver in GAC
$cmsHomeDir = $env:TRIDION_CM_HOME
if ($cmsHomeDir)
{
    # Check what path our CM web app is located at:
    if (!(Test-IISPathExists $cmSiteName))
    { 
        Write-Host "Didn't find CM located under IIS at: '$cmSiteName', checking default locations..."
        $cmSiteName = "SDL Web"
        if (!(Test-IISPathExists $cmSiteName)) {
            $cmSiteName = "Tridion Sites Content Manager"
            if (!(Test-IISPathExists $cmSiteName)) {
                  throw "Failed to find CM site '$cmSiteName' under IIS"
            }
        }
    }
 
    [System.Reflection.Assembly]::Load("System.EnterpriseServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")| Out-Null
    $publish = New-Object System.EnterpriseServices.Internal.Publish
    $asm =[System.Reflection.Assembly]::LoadWithPartialName($resolverName) 

    Write-Host "Attempting to add '$resolverAsm' to GAC..."
    if (Test-Path $resolverAsm)
    {        
        $publish.GacInstall($resolverAsm)
        $asm =[System.Reflection.Assembly]::LoadWithPartialName($resolverName)
        if($asm -eq $null)
        {
            Write-Host " > Failed to add '$resolverAsm' to GAC."
        }
        $restartRequired = $true
    }
    else
    {
        Write-Host " > Failed to find custom resolver ('$resolverAsm')."
    }
    
    $cmsConfigFile =  Join-Path $cmsHomeDir "config\Tridion.ContentManager.config"
    
    if(Test-Path $cmsConfigFile)
    {
        [xml]$config = Get-Content $cmsConfigFile -ErrorAction Stop     
        Write-Host " > Updating $cmsConfigFile..."
        if (Test-Path $cmsConfigFile)
        {                 
            $configurationRoot = GetOrCreate-Node("/configuration")
            $sections = GetOrCreate-Node("/configuration/configSections")
            $node = $config.SelectSingleNode("/configuration/configSections/section[@name='$resolverName']")            
            if($node)
            {
                $sections.RemoveChild($node) | Out-Null
            }
            $node = $config.SelectSingleNode("/configuration/$resolverName")
            if ($node) 
            {
                $configurationRoot.RemoveChild($node) | Out-Null
            }


            AddOrRemove-CustomResolverForType("Tridion.ContentManager.CommunicationManagement.Page")
            AddOrRemove-CustomResolverForType("Tridion.ContentManager.ContentManagement.Component")
            AddOrRemove-CustomResolverForType("Tridion.ContentManager.CommunicationManagement.ComponentTemplate")
            AddOrRemove-CustomResolverForType("Tridion.ContentManager.CommunicationManagement.StructureGroup")
        }
        else
        {
            Write-Host " > Failed to find $cmsConfigFile"
        }
        $config.Save("$cmsConfigFile")
    }
    else
    {
        Write-Host " > Failed to locate $cmsConfigFile"
    }

    $virtualPathName = "DxaResolver"
    $editorsTargetPath = "$extensionTargetFolder\Editors"
    $modelsTargetPath = "$extensionTargetFolder\Models"

    $editorsDistPath = "$distSource\Editors"
    $modelsDistPath = "$distSource\Models"

    #Copy UI extension files
    Write-Host "Copying DXA UI Extension files to '$extensionTargetFolder' ..."
    if (!(Test-Path $extensionTargetFolder)) {
        New-Item -ItemType Directory -Path $extensionTargetFolder | Out-Null
    }       

    if (!(Test-Path $editorsTargetPath)) {
        New-Item -ItemType Directory -Path $editorsTargetPath | Out-Null
    }       

    Copy-Item $editorsDistPath\* $editorsTargetPath -Recurse -Force

    if (!(Test-Path $modelsTargetPath)) {
        New-Item -ItemType Directory -Path $modelsTargetPath | Out-Null
    }       

    Copy-Item $modelsDistPath\* $modelsTargetPath -Recurse -Force
    
    # Copy resource files
    $cmeHomeDir = $env:TRIDION_CME_SOURCE
    $cmeWebRoot = ""
    if ($cmeHomeDir) { 
        $cmeWebRoot = Join-Path $cmeHomeDir "WebRoot"
    }
    else {
        $cmeWebRoot = Join-Path $cmsHomeDir "web\WebUI\WebRoot"
    }
    $cmsResourcePath =  Join-Path $cmeWebRoot "App_GlobalResources\"
    Write-Host "Copying DXA UI Extension resource files to '$cmsResourcePath' ..."      
    Copy-Item $modelsDistPath\Resources\* $cmsResourcePath -Recurse -Force
    Copy-Item $editorsDistPath\Resources\* $cmsResourcePath -Recurse -Force
    
    # Copy artifacts to web\WebUI\WebRoot\bin
    $cmsBinPath =  Join-Path $cmeWebRoot "bin\"

    Write-Host "Copying DXA UI Extension binaries to '$cmsBinPath' ..."
    Copy-Item $distSource\bin\* $cmsBinPath -Recurse -Force

    # Create virtual paths under IIS
    $editorsVirtualFolder = "IIS:\Sites\$cmSiteName\WebUI\Editors"
    $modelsVirtualFolder = "IIS:\Sites\$cmSiteName\WebUI\Models"

    $virtualFolder = Get-Item $editorsVirtualFolder -ErrorAction SilentlyContinue
    if ($virtualFolder) 
    {
        New-Item $editorsVirtualFolder/$virtualPathName -type VirtualDirectory -physicalPath $editorsTargetPath -Force | Out-Null
    }
    else
    {
        Write-Host " > Failed to find virtual folder $editorsVirtualFolder"
    }

    $virtualFolder = Get-Item $modelsVirtualFolder -ErrorAction SilentlyContinue
    if ($virtualFolder) 
    {
        New-Item $modelsVirtualFolder/$virtualPathName -type VirtualDirectory -physicalPath $modelsTargetPath -Force | Out-Null
    }
    else
    {
        Write-Host " > Failed to find virtual folder $modelsVirtualFolder"
    }



    $cmsConfigFile =  Join-Path $cmeWebRoot "Configuration\System.config"        
    if(Test-Path $cmsConfigFile)
    {
        [xml]$config = Get-Content $cmsConfigFile -ErrorAction Stop     
        Write-Host " > Updating $cmsConfigFile..."

        $mgr=new-object System.Xml.XmlNamespaceManager($config.Psbase.NameTable)
        $mgr.AddNamespace("ns",$config.configuration.xmlns)
        
        # Create Editor
        $pNode = $config.SelectSingleNode("//ns:editors",$mgr)        
        if($pNode)
        {
            $node = $config.SelectSingleNode("//ns:editor[@name='DxaResolver']",$mgr)        
            if($node)
            {        
                $pNode.RemoveChild($node) | Out-Null
            }
            $node = $config.SelectSingleNode("//ns:editor[@name='DXAResolver']",$mgr)
            if($node)
            {        
                $pNode.RemoveChild($node) | Out-Null
            }
  
            $node = $config.CreateElement("editor", "http://www.sdltridion.com/2009/GUI/Configuration")
            $node.SetAttribute("name", "DXAResolver")
            $installPathNode = $config.CreateElement("installpath", "http://www.sdltridion.com/2009/GUI/Configuration")
            $installPathNode.InnerXml = $editorsTargetPath
            $configNode = $config.CreateElement("configuration", "http://www.sdltridion.com/2009/GUI/Configuration")
            $configNode.InnerXml = "Configuration\DXAResolver.Editor.config"
            $vdirNode = $config.CreateElement("vdir", "http://www.sdltridion.com/2009/GUI/Configuration")
            $vdirNode.InnerXml = $virtualPathName
            $node.AppendChild($installPathNode) | Out-Null
            $node.AppendChild($configNode) | Out-Null
            $node.AppendChild($vdirNode) | Out-Null
            $pNode.AppendChild($node) | Out-Null   
        }

        # Create Model
        $pNode = $config.SelectSingleNode("//ns:models",$mgr)        
        if($pNode)
        {
            $node = $config.SelectSingleNode("//ns:model[@name='DxaResolver']",$mgr)        
            if($node)
            {                
                $pNode.RemoveChild($node) | Out-Null
            }
            $node = $config.SelectSingleNode("//ns:model[@name='DXAResolver']",$mgr)
            if($node)
            {                
                $pNode.RemoveChild($node) | Out-Null
            }
  
            $node = $config.CreateElement("model", "http://www.sdltridion.com/2009/GUI/Configuration")
            $node.SetAttribute("name", "DXAResolver")
            $installPathNode = $config.CreateElement("installpath", "http://www.sdltridion.com/2009/GUI/Configuration")
            $installPathNode.InnerXml = $modelsTargetPath
            $configNode = $config.CreateElement("configuration", "http://www.sdltridion.com/2009/GUI/Configuration")
            $configNode.InnerXml = "Configuration\DXAResolver.Model.config"
            $vdirNode = $config.CreateElement("vdir", "http://www.sdltridion.com/2009/GUI/Configuration")
            $vdirNode.InnerXml = $virtualPathName
            $node.AppendChild($installPathNode) | Out-Null
            $node.AppendChild($configNode) | Out-Null
            $node.AppendChild($vdirNode) | Out-Null
            $pNode.AppendChild($node) | Out-Null     
        }
           
        $config.Save("$cmsConfigFile")
    }
    else
    {
        Write-Host " > Failed to locate $cmsConfigFile"
    }
   
    $modelsWebConfigFile = "$modelsTargetPath\\Web.Config"
    [xml] $modelsWebConfig = Get-Content $modelsWebConfigFile
    $serviceElement = $modelsWebConfig.SelectSingleNode("/configuration/system.serviceModel/services/service")

    if($useHttpEndpoint) 
    {
        Add-ServiceEndpoint $serviceElement "Http"
    }
    else
    {
        Remove-ServiceEndpoint $serviceElement "Http"
    }

    if($useHttpsEndpoint) 
    {
        Add-ServiceEndpoint $serviceElement "Https"
    }
    else
    {
        Remove-ServiceEndpoint $serviceElement "Https"
    }

    $modelsWebConfig.Save($modelsWebConfigFile)

    if($restartRequired)
    {
        Write-Host "Restarting Services..."
        Stop-Service "TcmPublisher", "TcmServiceHost"  -Force
        TryStart-Service "TcmPublisher", "TcmBatchProcessor", "TcmServiceHost", "TcmSearchHost", "TcmSearchIndexer", "TCMWorkflow"

        Write-Host "Resetting IIS ..."
        & iisreset
    }
    else
    {
        Write-Host "Didn't find an existing Custom Resolver installation so no need to restart your services..."
    }

    Write-Host " > Done"
}
else
{
    Write-Host "It seems that you are not running the script on a CMS/Publishing Server(s)."
    Write-Host "Please ensure that the Sdl.Web.Tridion.CustomResolver assembly ('$resolverAsm') is added to the GAC on all the CMS/Piblishing server(s)."
}
