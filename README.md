![Icon](https://raw.githubusercontent.com/kzu/corebuild/master/docs/corebuild-32.png) CoreBuild
================

Simplified MSBuild-based build scripts empowered by NuGet

`CoreBuild` leverages a feature called MSBuild Sdks, which were introduced very recently in 
MSBuild 15.0 (Visual Studio 15.6+ in particular) and allows simplified MSBuild project authoring. 
`CoreBuild` provides the necessary targets and properties to enable NuGet `PackageReference` 
support in your build scripts.

## Installing

Just create your SDK-style MSBuild script project as follows:

```xml
<Project Sdk="CoreBuild/1.0.0-alpha">
	<!-- Your properties, targets and PackageReference items here -->
</Project>
```

> NOTE: Consider making it [CoreBuild Standard](http://www.corebuild.io) 
> compliant so that your contributors can easily know what steps are necessary to 
> clone, configure, build, test and run your project.

Alternatively, you can download and boostrap the project in a single 
command by running the following on the folder you intend to create 
the new `build.proj` file, typically your repository root.

From a PowerShell command prompt:

```
curl https://bit.ly/corebuild -o build.proj; msbuild build.proj /nologo /v:m; msbuild build.proj /nologo /t:help
```

From a regular command prompt using curl.exe:

```
curl -k -L https://bit.ly/corebuild -o build.proj && msbuild build.proj /nologo /v:m && msbuild build.proj /nologo /t:help
```

This will download the [sample project](https://github.com/kzu/corebuild/blob/master/build.proj), 
run `/t:Configure` to restore packages, and run the `/t:Help` target so you can see how documentation 
can be authored and rendered nicely from your build project.

## What

`CoreBuild` provides a [basic starting point](https://github.com/kzu/corebuild/blob/master/build.proj) 
for writing build scripts using MSBuild taking advantage of the newest features of MSBuild 15+ 
and NuGet packages for consuming reusable MSBuild props, targets and tasks which is also 
[CoreBuild Standard](http://www.corebuild.io) compliant.

CoreBuild also provides automatic targets and properties help via the the 
[CoreBuild.Help](https://www.nuget.org/packages/CoreBuild.Help) package, which is 
automatically referenced by the SDK.

## Why

Writing MSBuild targets is getting considerably more convenient as MSBuild is evolving in 
v15.0 and beyond. Together with the built-in support for NuGet package restore from MSBuild, 
the combination is now much more powerfull and allows for more concise and readable build scripts.

Examples of the increased power allowed by this new combination:

* Levaraging a NuGet package natively for doing versioning:

  ```xml
		<ItemGroup>
			<PackageReference Include="GitInfo" Version="*" />
		</ItemGroup>

		<Target Name="Build" DependsOnTargets="GitVersion">
			...
		</Target>
  ```

  This brings the latest & greatest version of [GitInfo](https://www.nuget.org/packages/GitInfo) 
  for versioning the built artifacts, for example.
  Note the concise notation for item metadata (`Version` attribute above) as 
  well as the [floating dependency version](https://docs.nuget.org/ndocs/consume-packages/dependency-resolution#floating-versions).

* Levaraging a NuGet package natively for detecting `XBuild` builds:

  ```xml
		<ItemGroup>
			<PackageReference Include="MSBuilder.IsXBuild" Version="*" />
		</ItemGroup>

		<Target Name="Build">
			<Error Condition="'$(IsXBuild)' == 'true'" Text="This build script requires MSBuild." />
			...
		</Target>
  ```


There are many more such reusable build blocks at [MSBuilder](https://github.com/MobileEssentials/MSBuilder).

* Levaraging xunit NuGet package natively for running tests:

  ```xml
		<ItemGroup>
			<PackageReference Include="xunit.runner.msbuild" Version="2.2.0" />
		</ItemGroup>

		<ItemGroup>
			<TestAssembly Include="src\**\*Tests*.dll" />
		</ItemGroup>

		<Target Name="Test">
			<xunit Assemblies="@(TestAssembly)" />
		</Target>
  ```

## How

In order to enable package restore from MSBuild, the `CoreBuild` SDK opts-in to the NuGet 4.0 
features available in VS2017 for s"SDK Style" MSBuild projects, by specifying `netstandard1.0` as 
its [TargetFramework](https://github.com/kzu/corebuild/blob/master/src/Sdk/Sdk.props#L4).

NuGet will automatically generate the restore artifacts in the `.nuget` folder alongside your 
`build.proj`, which is typically ignored by default in source control (i.e. via `.gitignore`):

		\root
			- build.proj
			\.nuget
				- [nuget restore artifacts here, updated by /t:Restore]

The `Sdk.props` and `Sdk.targets` then import the generated targets from NuGet, allowing 
your main `build.proj` project to readily consume their artifacts. 
A typical [`build.proj`](https://github.com/kzu/corebuild/blob/master/src/build.proj) therefore 
looks quite clean:

```xml
<Project Sdk="CoreBuild/[VERSION]" DefaultTargets="Build">

	<ItemGroup>
		<!-- Some PackageReferences for reusable MSBuild "scriptlets"... -->
	</ItemGroup>

	<Target Name="Build">
		...
	</Target>

	<Target Name="Clean">
		...
	</Target>

	<Target Name="Rebuild" DependsOnTargets="Clean;Build" />

</Project>
```
