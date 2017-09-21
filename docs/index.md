# CoreBuild Standard

Defines the required targets that an MSBuild project or script must support in order 
to be considered CoreBuild Standard compatible.

## Why

Building and running repositories with managed code and MSBuild scripts is generally 
inconsistent because there isn't a common way that all .NET developers adopt, resulting 
in the need to hope the repository provides a README with instructions.

What if there could be a very visible badge that just told me that a repo is compliant 
with some standard and that would mean I could configure, build and run it in a uniform
way? That's the value that CoreBuild Standard provides.

## What

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