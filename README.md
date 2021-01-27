SDL Digital Experience Accelerator CM-side framework
===
Build status
------------
- Develop: [![Build Status](https://travis-ci.com/sdl/dxa-content-management.svg?branch=develop)](https://travis-ci.com/sdl/dxa-content-management)
- 2.2: [![Build Status](https://travis-ci.com/sdl/dxa-content-management.svg?branch=release%2F2.2)](https://travis-ci.com/sdl/dxa-content-management)
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

 
Support
---------------
At SDL we take your investment in Digital Experience very seriously, if you encounter any issues with the Digital Experience Accelerator, please use one of the following channels:

- Report issues directly in [this repository](https://github.com/sdl/dxa-content-management/issues)
- Ask questions 24/7 on the SDL Tridion Community at https://tridion.stackexchange.com
- Contact SDL Professional Services for DXA release management support packages to accelerate your support requirements


Documentation
-------------
Documentation can be found online in the SDL documentation portal: https://docs.sdl.com/sdldxa

Building
---------
In order to build this repository you must first make sure you restore all the required packages from nuget.org using the build target 'Restore':

```
msbuild ciBuild.proj /t:Restore
```

After restoring all packages you can build the repository:
```
msbuild ciBuild.proj
```
You can also specify a build version to tag your artifacts:
```
msbuild ciBuild.proj /p:Version=Major.Minor.Path.Build
```

Testing
-------
To run the unit tests and code coverage of the built artifacts you can run:
```
msbuild ciBuild.proj /t:Test
```

Generating Release Artifacts
----------------------------
To generate all the artifacts (after you have built the repository) you can use the following target:
```
msbuild ciBuild.proj /t:Artifacts
```

Publishing the Data Model to Public Repository
----------------------------------------------
The Data Model used by the Template Building Blocks and the DXA framework is contained within the Sdl.Web.DataModel project. The template building blocks use this project to generate the data model that ulitimatly gets serialized to Json and published into the broker DB. The data model is also used by the DXA framework to deserialize the Json. Since this is a shared project, any changes that are made should be published to the public nuget.org repository. You can do this with the following:
```
msbuild ciBuild.prog /t:PublishPackages /p:NuGetRepositoryUrl="nuget.org"
```
To publish to NuGet.org you must have the correct API key registered.



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
Copyright (c) 2014-2021 SDL Group.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
