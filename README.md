# CoreBuild

Simplified MSBuild-based build scripts empowered by NuGet


## Installing

Create the folder where you will author the corebuild-based script. 
This is typically your repository root.

Using curl:

		curl -k -L https://bit.ly/corebuild -o corebuild.proj && msbuild corebuild.proj /v:minimal
	
Using PowerShell:

		%WINDIR%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -Command "& { Invoke-WebRequest -Uri https://bit.ly/corebuild -OutFile corebuild.proj }" && msbuild corebuild.proj /v:minimal

The initial "build" is used to initialize the build script by downloading the required dependent 
targets and persisting the initial `ETag` used afterwards for checking for udpates.

Updating to latest `corebuild` imports:

		msbuild corebuild.proj /t:update


> NOTE: updating `corebuild` will never overwrite your custom `corebuild.proj`, only 
> the dependent targets in the `corebuild` subfolder created when initially installed.


## What

`corebuild` provides a basic starting point for writing build scripts 
using MSBuild taking advantage of the newest features of MSBuild 15+ 
and NuGet packages for consuming reusable MSBuild props, targets and 
tasks.


## Why

Writing MSBuild targets is getting considerably more convenient as MSBuild 
is evolving in v15 and beyond. Together with the built-in support for NuGet 
package restore from MSBuild, the combination is now much more powerfull 
and allows for more concise and readable build scripts.

Examples of the increased power allowed by this new combination:

* Levaraging a NuGet package natively for doing versioning:

		<ItemGroup>
			<PackageReference Include="GitInfo" Version="*" />
		</ItemGroup>

		<Target Name="Build" DependsOnTargets="GitVersion">
			...
		</Target>

  This brings the latest & greatest version of [GitInfo](https://www.nuget.org/packages/GitInfo) 
  for versioning the built artifacts, for example.
  Note the concise notation for item metadata (`Version` attribute above) as 
  well as the [floating dependency version](https://docs.nuget.org/ndocs/consume-packages/dependency-resolution#floating-versions).

* Levaraging a NuGet package natively for detecting `XBuild` builds:

		<ItemGroup>
			<PackageReference Include="MSBuilder.IsXBuild" Version="*" />
		</ItemGroup>

		<Target Name="Build">
			<Error Condition="'$(IsXBuild)' == 'true'" Text="This build script requires MSBuild." />
			...
		</Target>


There are many more such reusable build blocks at [MSBuilder](https://github.com/MobileEssentials/MSBuilder).


## How

`corebuild` provides a [.props](https://github.com/kzu/corebuild/blob/master/build/corebuild/corebuild.props) and 
a [.targets](https://github.com/kzu/corebuild/blob/master/build/corebuild/corebuild.targets) that configure the 
relevant NuGet imports to turn on package restore and consume the build targets and tasks provided by those 
packages.

In order to enable package restore from MSBuild, `corebuild` opts-in to the NuGet 4.0 features available 
to .NET Core projects, by specifying `netcore50` as its [TargetFramework](https://github.com/kzu/corebuild/blob/master/build/corebuild/corebuild.props#L5).

NuGet will automatically generate the restore artifacts in the `.nuget` folder inside `corebuild` alongside your 
`corebuild.proj`, which is typically ignored by default in source control (i.e. via `.gitignore`):

		\root
			- corebuild.proj
			- project.json
			\corebuild
				- corebuild.props     [self-updating via /t:Update]
				- corebuild.targets   [self-updating via /t:Update]
				- update.targets      [self-updating via /t:Update]
				\.nuget
					- [nuget restore artifacts here, updated by /t:Restore]

The `corebuild.props` and `corebuild.targets` then import the generated targets from NuGet, allowing 
your main `corebuild.proj` project to readily consume their artifacts.

`corebuild` also provides its own self-updating mechanism via the [CoreBuild.Updater](https://www.nuget.org/packages/CoreBuild.Updater) 
nuget package, which intelligently updates the main `.props` and `.targets` by checking the persisted local 
`ETag` property against the GitHub-provided one for the raw file in the main repository.

