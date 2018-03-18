# SymbolSource

## Project introduction

SymbolSource processes and hosts PDB files and sources to provide on-demand symbol and source loading
for Visual Studio, WinDbg and compatible software, enabling debugging and source stepping of third-party
libraries published through NuGet.

The code in this repository is running the new SymbolSource service available at https://nuget.smbsrc.net, as descibed in  [Moving to the new SymbolSource engine](https://tripleemcoder.com/2015/10/04/moving-to-the-new-symbolsource-engine/). It hosts symbol and sources for corresponding packages found at https://nuget.org, as long as their authors chose to publish symbol packages as well.

To learn more about NuGet symbol packages have a look at [Creating symbol packages](https://docs.microsoft.com/en-us/nuget/create-packages/symbol-packages).

## Deprecated services and projects

The old service found at https://www.symbolsource.org is being phased out. It is still to be decided what to do with the documentation part of the website, and the source browsing experience.

This project also replaces [SymbolSource.Server.Basic](https://github.com/SymbolSource/SymbolSource.Community), which has been now deprecated. See below for instruction on how to use the code published here to setup your own server. The upgrade path is entirely manual for the time being.

## Contributing to the project

As this projects has only recently been open-sourced, we are in dire need of contributors. If you find anything worth fixing or extending, with or without the time or skill to create a pull request, do post an issue here on GitHub. Any help and input will be much appreciated.

## Running your own instance

The repository contains a standard Visual Studio solution which should build without any issues.

Runnable artifacts are produced by these projects:
* `SymbolSource.Server` - hosted under as an Azure Website or on-premise under IIS, receives packages and serves symbols and sources,
* `SymbolSource.Processor.Console` - hosted as a Azure Webjob or run as a console application, processes packages to index symbols and store all files separately for efficient serving.

Both applications are configured through standard `<appSettings>` entries.

Depending how you're going to run SymbolSource, you first need to select the appropriate security, storage and scheduler types, using the following keys, both in Server and Processor:
```xml
<add key="Container.SecurityType" value="SymbolSource.Contract.Security.$security, SymbolSource.Contract" />
<add key="Container.StorageType" value="SymbolSource.Contract.Storage.$storage, SymbolSource.Contract" />
<add key="Container.SchedulerType" value="SymbolSource.Contract.Scheduler.$scheduler, SymbolSource.Contract" />
```

Security services determine user permissions for various operations performed on SymbolSource, and request appropriate credentials. Storage services are responsible for handling package, symbol and source file storage. Scheduler services enable the Server to communicate with the Processor. 

Available types and their properties are described below. Properties are set with additional `<add key="$property" value="..." />` enties.

### $security = NuGetOrgSecurityService

Verifies package ownership with nuget.org.
* `InstanceSalt` - used for hashing API keys to create user ids
 
### $security = NullSecurityService

Provides public read access and full write access to specified API keys.
* `InstanceSalt` - used for hashing API keys to create user ids
* `NullSecurity.PushApiKeys` - 
* `NullSecurity.AllowNamedFeeds` - true if named feeds in the form of https://$hostname/$feed should be allowed for publishing and querying, otherwise false for a flat repository of packages under /

### $storage = AzureStorageService

Uses an Azure Storage account. Assumes full ownership of the account and will automatically create blob containers and tables.
* `AzureStorage.ConnectionString`

### $storage = LocalStorageService

Stores files in the a local filesystem.
* `LocalStorage.RootPath`

### $scheduler = WebJobsSchedulerService

Posts messages to an Azure Queue for a Processor running as an Azure Webjob. Shares the connection string with AzureStorageService.
* `PackageProcessor.BatchSize` - number of packages to retrieve and process in parallel
* `PackageProcessor.ServerUrl` - the URL, including protocol, under which Server is being exposed to symbol consumers, embedded in PDB files during indexing

### $scheduler = FileSchedulerService

Posts messages to disk as JSON files and monitors them using a FileSystemWatcher. Shares the filesystem location with LocalStorageService.

### $scheduler = ConsoleSchedulerService

Processes all waiting packages on pressing RETURN.

## Build status
[![Build status](https://ci.appveyor.com/api/projects/status/github/SymbolSource/SymbolSource?branch=master&svg=true)](https://ci.appveyor.com/project/TripleEmcoder/symbolsource/branch/master)

Note: Failure might be a false negative - there are some tests that do not exit gracefully that need fixing.
