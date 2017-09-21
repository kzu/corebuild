![Icon](https://raw.github.com/kzu/IFluentInterface/master/images/icon-32.png) CoreBuild Standard
================

Defines the required targets that an MSBuild project or script must support in order 
to be considered CoreBuild Standard compatible.

# Why

Building and running repositories with managed code and MSBuild scripts is generally 
inconsistent because there isn't a common way that all .NET developers adopt, resulting 
in the need to hope the repository provides a README with instructions.

What if there could be a very visible badge that just told me that a repo is compliant 
with some standard and that would mean I could configure, build and run it in a uniform
way? That's the value that CoreBuild Standard provides.

# What

In order to be considered CoreBuild Standard compatible, an MSBuild project or script 
needs to provide the following targets:

* `Configure`: initial target to run right after cloning the repository, typically run 
  only once, unless you modify the project or synchronize changes and dependencies 
  changed. Would typically do a NuGet restore of projects/solutions too.

* `Build`: builds whatever needs to be built in order to use the project.

* `Test`: runs tests that ensure the build will work.

* `Run`: runs the project.


Note that the actual implementation of these targets is completely left to each project, 
since the meaning of each will vary wildly depending on the project type.

If your project is CoreBuild Standard compatible, just add the following badge on its 
main page: 

[![CoreBuild Standard](https://img.shields.io/badge/corebuild-standard-blue.svg)](http://www.corebuild.io)

Markdown:

```
[![CoreBuild Standard](https://img.shields.io/badge/corebuild-standard-blue.svg)](http://www.corebuild.io)
```

## Help

Documenting MSBuild targets and properties is important and also generally non-standard. 
To make documenting targets easier for consumers, we provide the 
[CoreBuild.Help](https://www.nuget.org/packages/CoreBuild.Help) NuGet package that whiich  
renders documentation for public configurable properties and additional targets available 
in an MSBuild project or script. The basic heuristics are simple:

* Any property or target that doesn't start with an underscore is considered public
* Any XML comment right before the target or property is considered its documentation.

Here is an examples of the output of running `msbuild /t:help` on MSBuild projects with 
the package installed:

Source:

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <!-- Configuration to use for Build. Defaults to 'Debug' if empty. -->
        <Configuration Condition="'$(Configuration)' == ''">Debug</Configuration>

        <_CI Condition="'$(TF_BUILD)' == 'true' or '$(APPVEYOR)' != ''">true</_CI>
    </PropertyGroup>

    <!-- Runs NuGet restore and provisions machine-wide dependencies if necessary -->
    <Target Name="Configure">...</Target>

    <!--
    ============================================
    Builds the project
    ============================================
    -->
    <Target Name="Build">...</Target>

    <!-- Runs unit tests -->
    <Target Name="Test">...</Target>

    <!-- Installs the main VSIX into VS Experimental and runs it -->
    <Target Name="Run">...</Target>

    <!-- 
    ****************************************************************
    Publishes the VSIX extension and NuGet package to the the VS 
    marketplace and nuget.org respectively.
    ****************************************************************
    --> 
    <Target Name="Publish">...</Target>

    <Target Name="_SignOutput" AfterTargets="Build" Condition="'$(_CI)' == 'true'">...</Target>
</Project>
```

Output:
```
Help:
   __        __
  /   _  _ _|__)   .| _|
  \__(_)| (-|__)|_|||(_|

  Standard: YES √ (Configure, Build, Test and Run targets supported)

  Properties:
        - Configuration: Configuration to use for Build. Defaults to 'Debug' if empty.

  Targets:
        - Build: Builds the project
        - Configure: Runs NuGet restore and provisions machine-wide dependencies if necessary
        - Publish: Publishes the VSIX extension and NuGet package to the the VS
                   marketplace and nuget.org respectively.
        - Run: Installs the main VSIX into VS Experimental and runs it
        - Test: Runs unit tests

  Help: properties to customize what `Help` reports
        - HelpExclude: A wildcard pattern to evaluate against properties or targets for exclusion on Help target runs
        - HelpInclude: A wildcard pattern to evaluate against properties or targets for inclusion on Help target runs
        - HelpProperties: Get help on public properties
        - HelpTargets: Get help on public targets
```

Note that the documentation can be wrapped in sequences of `=` or `*` which a very common 
practice in MSBuild targets.

Note also that you can use further tweak what Help reports.

## Build Scripts

Creating MSBuild build scripts that can easily consume NuGet packages isn't exactly straightforward, 
so CoreBuild also provides help in that front. You can easily bootstrap a build script with the 
instructions provided at https://github.com/kzu/corebuild.



# Credits

Icon [compile](https://thenounproject.com/term/compile/1002713/) by TooJooGoo from the Noun Project