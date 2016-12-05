CoreBuild.Updater:
=========================================

Allows target files to update themselves by 
fetching and comparing the ETag of a remote 
URL specified as the UpdateUrl.

Usage:

<PropertyGroup>
	<PropertyGroup>
    <!-- GitHub already provides proper ETag behavior for the raw endpoing for any file in a repository  -->
		<UpdateUrl>https://raw.githubusercontent.com/kzu/corebuild/master/build/corebuild/corebuild.props</UpdateUrl>
		<!-- Initial ETag can be empty and will be filled after the first update -->
		<ETag />
	</PropertyGroup>
</PropertyGroup>


Note: the targets file is overwritten with the remote 
file only if the local ETag is different than the remote 
ETag. 

Uses PowerShell on Windows and curl on Mac.