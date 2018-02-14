![Icon](https://raw.githubusercontent.com/kzu/corebuild/master/docs/corebuild-32.png) CoreBuild
================

Simplified MSBuild-based build scripts empowered by NuGet


## Installing

Create the folder where you will author the corebuild-based script. 
This is typically your repository root.

From a PowerShell command prompt:

```
curl https://bit.ly/corebuild -o build.proj; msbuild /nologo /v:m; msbuild /nologo /v:m /t:configure; msbuild /nologo /t:help
```

From a regular command prompt using curl.exe:

```
curl -k -L https://bit.ly/corebuild -o build.proj && msbuild /nologo /v:m && msbuild /nologo /v:m /t:configure && msbuild /nologo /t:help
```

The initial "build" is used to initialize the build script by downloading the required dependent 
targets and persisting the initial `ETag` used afterwards for checking for udpates.

Updating to latest `corebuild` imports:

```
msbuild build.proj /t:update
```
or
```
build /update
```

> NOTE: updating `corebuild` will never overwrite your custom `build.proj` or 
> `build.cmd`, only the dependent targets in the `build` subfolder created when initially installed.


## What

`corebuild` provides a [basic starting point](https://github.com/kzu/corebuild/blob/master/src/build.proj) 
for writing build scripts using MSBuild taking advantage of the newest features of MSBuild 15+ 
and NuGet packages for consuming reusable MSBuild props, targets and tasks which is also 
[CoreBuild Standard](http://www.corebuild.io) compliant.

It also provides a [default batch file](https://github.com/kzu/corebuild/blob/master/src/build.cmd) 
that can be used to easily ensure the basic requirements are met (MSBuild 15+ and Visual Studio 
2017+ developer command prompt).

Finally, it provides automatic targets and properties help via the the 
[CoreBuild.Help](https://www.nuget.org/packages/CoreBuild.Help) package.

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

`corebuild` provides a [.props](https://github.com/kzu/corebuild/blob/master/src/build/corebuild.props) and 
a [.targets](https://github.com/kzu/corebuild/blob/master/src/build/corebuild.targets) that configure the 
relevant NuGet imports to turn on package restore and consume the build targets and tasks provided by those 
packages.

In order to enable package restore from MSBuild, `corebuild` opts-in to the NuGet 4.0 features available 
in VS2017 for "SDK Style" MSBuild projects, by specifying `netstandard1.0` as its 
[TargetFramework](https://github.com/kzu/corebuild/blob/master/src/build/corebuild.props#L5).

NuGet will automatically generate the restore artifacts in the `corebuild\.nuget` folder alongside your 
`corebuild.proj`, which is typically ignored by default in source control (i.e. via `.gitignore`):

		\root
			- build.proj
			- build.cmd
			- msbuild.rsp
			\.nuget
				- [nuget restore artifacts here, updated by /t:Restore]
			\build
				- corebuild.props     [self-updating via /t:Update]
				- corebuild.targets   [self-updating via /t:Update]
				- update.targets      [self-updating via /t:Update]

The `corebuild.props` and `corebuild.targets` then import the generated targets from NuGet, allowing 
your main `build.proj` project to readily consume their artifacts. 
A typical [`corebuild.proj`](https://github.com/kzu/corebuild/blob/master/src/build.proj) therefore 
looks quite clean:

```xml
<Project DefaultTargets="Build">
	<Import Project="corebuild\corebuild.props" />

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

	<Import Project="corebuild\corebuild.targets" />
</Project>
```

`corebuild` also provides its own self-updating mechanism via the [CoreBuild.Updater](https://www.nuget.org/packages/CoreBuild.Updater) 
nuget package, which intelligently updates the main `.props` and `.targets` by checking the persisted local 
[ETag](https://github.com/kzu/corebuild/blob/master/src/build/corebuild.props#L13) property against the 
GitHub-provided one for the raw file in the main repository.
