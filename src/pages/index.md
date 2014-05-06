<!--
template = page
title = nAdoni
-->
nAdoni is a library that provides a way to check for updates to one or more binaries via an update manifest, and then to download an archive containing the updated binaries. 

The manifest file is an XML file which is signed with a RSA key to provide some form of security in the update process. 

# Setup

### Create a key file:

In order to create the manifest files a RSA key is needed. The complete RSA key needs to be provided to both the manifest generator and the public part of the RSA key needs to be provided when the manifest is parsed.

A RSA private key file contains the following information:

	``` xml
	<RSAKeyValue>
	    <Modulus>MODULUS_VALUE_HERE</Modulus>
	    <Exponent>EXPONENT_VALUE_HERE</Exponent>
	    <P>P_VALUE_HERE</P>
	    <Q>Q_VALUE_HERE</Q>
	    <DP>DP_VALUE_HERE</DP>
	    <DQ>DQ_VALUE_HERE</DQ>
	    <InverseQ>INVERSE_Q_VALUE_HERE</InverseQ>
	    <D>D_VALUE_HERE</D>
	</RSAKeyValue>
	```

A RSA public key file contains the following information:

	``` xml
	<RSAKeyValue>
	    <Modulus>MODULUS_VALUE_HERE</Modulus>
	    <Exponent>EXPONENT_VALUE_HERE</Exponent>
	</RSAKeyValue>
	```
	
These kinds of files can be written with the `nAdoni.KeyGenerator` executable by executing the following command line:
    
    nAdoni.KeyGenerator.exe --private=<FILE-PATH-PRIVATE-OUTPUT> --public=<FILE-PATH-PUBLIC-OUTPUT> --keysize=<KEYSIZE>

Where:
* __FILE-PATH-PRIVATE-OUTPUT__ - The full path to the XML file that contains the public and private parts of the key.
* __FILE-PATH-PUBLIC-OUTPUT__ - The full path to the XML file that contains only the public part of the key.
* __KEYSIZE__ - The size of the key, defaults to 2048 bits.
    
### Create a manifest file:

* Create an archive file that contains all the files that should be part of the update.
* Get the `nAdoni.ManifestBuilder` executable and execute the following command line:

        nAdoni.ManifestBuilder.exe --ProductName=<PRODUCT-NAME> --ProductVersion=<PRODUCT-VERSION> --FilePath=<FILE-PATH-TO-ARCHIVE-FILE> --DownloadUrl=<URL-TO-ARCHIVE-FILE> --KeyFile=<SIGNING-FILE-PATH> --KeyContainerName=<SIGNING-KEY-CONTAINER-NAME> --Output=<OUTPUT-PATH>

	Where
 * __PRODUCT-NAME__ - The name of the product that should be updated. This name is not directly used in finding new updates but may be used for display purposes in the application that is checking for updates.
 * __PRODUCT-VERSION__ - The version of the product that is contained in the archive file. An application looking for updates will compare their version number with this one in order to determine if it should update or not.
 * __FILE-PATH-TO-ARCHIVE-FILE__ - The full path to the archive file, at the moment the manifest file is being created. This path is used to load the archive file and calculate the hash.
 * __URL-TO-ARCHIVE-FILE__ - The URL from which the archive can be downloaded. 
 * __SIGNING-FILE-PATH__ - The full path to the RSA private key file with which the manifest file should be signed. This parameter does not need to be provided if a key container name is provided.
 * __SIGNING-KEY-CONTAINER-NAME__ - The name of the key container that contains the private key with which the manifest file should be signed. This parameter does not need to be provided if a key file path is provided.
 * __OUTPUT-PATH__ - The full path to the location where the output file should be placed.
	
### In the application

``` c#

var updater = new NAdoni.Updater();

// Check for updates
var info = updater.MostRecentUpdateOnRemoteServer(manifestUri, publicKeyXml, currentVersion);

if (info.UpdateIsAvailableAndValid)
{
	var task = updater.StartDownloadAsync(info);
	task.Wait();

	var fileInfo = task.Result;

	// Now you have a FileInfo object with the path to your archive file
}

```


# Installation instructions

The library section of nAdoni is available on [NuGet.org](http://www.nuget.org). And the manifest builder is available as ZIP archive from the [releases page](https://github.com/pvandervelde/nAdoni/releases).


# How to build

The solution files are created in Visual Studio 2012 (using .NET 4.5) and the entire project can be build by invoking MsBuild on the nadoni.integration.msbuild script. This will build the binaries, the NuGet package and the ZIP archive. The binaries will be placed in the `build\bin\AnyCpu\Release` directory and the NuGet package and the ZIP archive will be placed in the `build\deploy` directory.

Note that the build scripts assume that:

* The binaries should be signed, however the SNK key file is not included in the repository so a new key file has to be [created][snkfile_msdn]. The key file is referenced through an environment variable called `SOFTWARE_SIGNING_KEY_PATH` that has as value the full path of the key file. 
* GIT can be found on the PATH somewhere so that it can be called to get the hash of the last commit in the current repository. This hash is embedded in the nAdoni assemblies together with information about the build configuration and build time and date.

# Origin
This code of nAdoni is based on the code for an [simple auto-update library for WPF applications](http://blogs.msdn.com/b/dotnetinterop/archive/2008/03/28/simple-auto-update-for-wpf-apps.aspx) which is licensed under the [Ms-PL license](http://opensource.org/licenses/ms-pl).

[snkfile_msdn]: http://msdn.microsoft.com/en-us/library/6f05ezxy(v=vs.110).aspx