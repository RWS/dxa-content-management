dxa-content-management
===
SDL Digital Experience Accelerator Core Template Building Blocks


About
-----
The SDL Digital Experience Accelerator is a reference implementation of SDL Web 8 and SDL Tridion 2013 SP1 intended to help you create, design and publish an SDL Web/Tridion-based Website quickly.

You can find more details and a download of the entire release on https://community.sdl.com/developers/tridion_developer/m/mediagallery/852


Support
---------------
The SDL Digital Experience Accelerator is intended as a toolkit to help the SDL Tridion community and is not an officially supported SDL Tridion product.

If you encounter problems, reach out to the community: http://tridion.stackexchange.com/


Sources
-------

This repository contains the full source of all the DXA Core Template Building Blocks to give you insight in how they are built and what they do. 
You are free to use these sources under the terms and conditions of the license mentioned below, however we suggest you don't change them and implement your changes in a separate DXA module. 

Note that the Sdl.Web.Tridion.Templates.csproj references CM assemblies in _references subdirectories which are NOT included in this repository, 
because these assemblies cannot be distributed without a signed license agreement.

In order to build the project, the following CM assemblies will have to be obtained from an SDL Web/Tridion distribution and put in the appropriate _references subdirectories:

2013-sp1
 - Tridion.Common.dll
 - Tridion.ContentManager.Common.dll
 - Tridion.ContentManager.dll
 - Tridion.ContentManager.Publishing.dll
 - Tridion.ContentManager.TemplateTypes.dll
 - Tridion.ContentManager.Templating.dll
 - Tridion.ContentManager.TypeRegistration.dll
 - Tridion.Logging.dll

cm-8.1
 - Tridion.TopologyManager.Client.dll
 - Microsoft.OData.Client.dll
 - Microsoft.OData.Core.dll
 - Microsoft.OData.Edm.dll
 - Microsoft.Spatial.dll
 - Newtonsoft.Json.dll


Documentation
-------------

Documentation can be found online in the SDL doc portal, you can find details about this in the download on the SDL Community site.


Repositories
------------

The following repositories with source code are available:

 - https://github.com/sdl/dxa-content-management - Core Template Building Blocks
 - https://github.com/sdl/dxa-html-design - Whitelabel HTML Design
 - https://github.com/sdl/dxa-modules - Modules (.NET and Java)
 - https://github.com/sdl/dxa-web-application-dotnet - ASP.NET MVC web application (incl. framework)
 - https://github.com/sdl/dxa-web-application-java - Java Spring MVC web application (incl. framework)


Branches and Contributions
--------------------------

We are using the following branching strategy:

 - master - Represents the latest stable version. This may be an pre-release version (tagged as "DXA x.y Sprint z"). Updated each development Sprint (approx. bi-weekly).
 - develop - Represents the latest development version. Updated very frequently (typically nightly).
 - release/x.y - Represents the x.y Release. If hotfixes are applicable, they will be applied to the appropriate release branch, so that the release branch actually represent the initial release plus hotfixes.

All releases (including pre-releases) are Tagged. 
 
If you wish to submit a Pull Request, it should normally be submitted on the develop branch, so it can be incorporated in the upcoming release.
Fixes for really severe/urgent issues (which qualify as hotfixes) should be submitted as Pull Request on the appropriate release branch.
Please always submit an Issue for the problem and indicate whether you think it qualifies as a hotfix; Pull Requests on release branches will only be accepted after agreement on the severity of the issue.
Furthermore, Pull Requests on release branches are expected to be extensively tested.

Of course, it's also possible to report an Issue without associated Pull Requests.


License
-------
Copyright (c) 2014-2016 SDL Group.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
