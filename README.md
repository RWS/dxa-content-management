SDL Digital Experience Accelerator CM-side framework
===
Build status
------------
- 1.8: [![Build Status](https://travis-ci.com/sdl/dxa-content-management.svg?branch=release%2F1.8)](https://travis-ci.com/sdl/dxa-content-management)

About
-----
The SDL Digital Experience Accelerator (DXA) is a reference implementation of SDL Tridion Sites 9 and SDL Web 8 intended to help you create, design and publish an SDL Tridion/Web-based website quickly.

DXA is available for both .NET and Java web applications. Its modular architecture consists of a framework and example web application, which includes all core SDL Tridion/Web functionality as well as separate Modules for additional, optional functionality.

This repository contains the source code of the DXA Core Template Building Blocks used on the Content Manager side. 
The full DXA distribution (including CM-side items and installation support) is downloadable from the [SDL AppStore](https://appstore.sdl.com/list/?search=dxa) 
or the GitHub Releases of the [dxa-web-application-dotnet](https://github.com/sdl/dxa-web-application-dotnet/releases) or [dxa-web-application-java](https://github.com/sdl/dxa-web-application-java/releases) repositories.

To facilitate upgrades, it is highly recommended to use an official, compiled version of the DXA Core TBBs (part of the DXA distribution) instead of a custom build.
If you really have to modify the DXA Core TBBs, we kindly request you to submit your changes as a Contribution; see below. 

Note that the `Sdl.Web.Tridion.Templates.csproj` project references CM assemblies in `_references` subdirectories which are **not** included in this repository, 
because these assemblies cannot be distributed without a signed license agreement.
In order to build the project, the following CM assemblies will have to be obtained from an SDL Web/Tridion distribution and put in the appropriate `_references` subdirectories:

cm-9.0:

 - Tridion.Common.dll
 - Tridion.ContentManager.Common.dll
 - Tridion.ContentManager.dll
 - Tridion.ContentManager.Publishing.dll
 - Tridion.ContentManager.TemplateTypes.dll
 - Tridion.ContentManager.Templating.dll
 - Tridion.ContentManager.TypeRegistration.dll
 - Tridion.ExternalContentLibrary.dll
 - Tridion.ExternalContentLibrary.V2.dll
 - Tridion.Logging.dll
 - Tridion.TopologyManager.Client.dll
 - Microsoft.OData.Client.dll
 - Microsoft.OData.Core.dll
 - Microsoft.OData.Edm.dll
 - Microsoft.Spatial.dll
 - Newtonsoft.Json.dll

(*) SDL Tridion 2013 SP1 is only supported up to DXA version 1.6.
 
Support
---------------
At SDL we take your investment in Digital Experience very seriously, if you encounter any issues with the Digital Experience Accelerator, please use one of the following channels:

- Report issues directly in [this repository](https://github.com/sdl/dxa-content-management/issues)
- Ask questions 24/7 on the SDL Tridion Community at https://tridion.stackexchange.com
- Contact SDL Professional Services for DXA release management support packages to accelerate your support requirements


Documentation
-------------
Documentation can be found online in the SDL documentation portal: https://docs.sdl.com/sdldxa


Repositories
------------
The following repositories with DXA source code are available:

 - https://github.com/sdl/dxa-content-management - CM-side framework (.NET Template Building Blocks)
 - https://github.com/sdl/dxa-html-design - Whitelabel HTML Design
 - https://github.com/sdl/dxa-model-service - DXA Model Service (Java)
 - https://github.com/sdl/dxa-modules - Modules (.NET and Java)
 - https://github.com/sdl/dxa-web-application-dotnet - ASP.NET MVC web application (incl. framework)
 - https://github.com/sdl/dxa-web-application-java - Java Spring MVC web application (incl. framework)


Branches and Contributions
--------------------------
We are using the following branching strategy:

 - `master` - Represents the latest stable version. This may be a pre-release version (tagged as `DXA x.y Sprint z`). Updated each development Sprint (approx. bi-weekly).
 - `develop` - Represents the latest development version. Updated very frequently (typically nightly).
 - `release/x.y` - Represents the x.y Release. If hotfixes are applicable, they will be applied to the appropriate release branch, so that the release branch actually represent the initial release plus hotfixes.

All releases (including pre-releases and hotfix releases) are tagged. 

If you wish to submit a Pull Request, it should normally be submitted on the `develop` branch, so it can be incorporated in the upcoming release.

Fixes for really severe/urgent issues (which qualify as hotfixes) should be submitted as Pull Request on the appropriate release branch.

Please always submit an Issue for the problem and indicate whether you think it qualifies as a hotfix; Pull Requests on release branches will only be accepted after agreement on the severity of the issue.
Furthermore, Pull Requests on release branches are expected to be extensively tested by the submitter.

Of course, it's also possible (and appreciated) to report an Issue without associated Pull Requests.


License
-------
Copyright (c) 2014-2020 SDL Group.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
