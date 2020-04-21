# **Cloud.Core.FeatureFlag.LaunchDarkly** 
[![Build status](https://dev.azure.com/cloudcoreproject/CloudCore/_apis/build/status/Cloud.Core%20Packages/Cloud.Core.FeatureFlag.LaunchDarkly_Package)](https://dev.azure.com/cloudcoreproject/CloudCore/_build/latest?definitionId=12)![Code Coverage](https://cloud1core.blob.core.windows.net/codecoveragebadges/Cloud.Core.FeatureFlag.LaunchDarkly-LineCoverage.png) [![Cloud.Core.FeatureFlag.LaunchDarkly package in Cloud.Core feed in Azure Artifacts](https://feeds.dev.azure.com/cloudcoreproject/dfc5e3d0-a562-46fe-8070-7901ac8e64a0/_apis/public/Packaging/Feeds/8949198b-5c74-42af-9d30-e8c462acada6/Packages/9922dfd4-a522-4363-8f25-20e717f65596/Badge)](https://dev.azure.com/cloudcoreproject/CloudCore/_packaging?_a=package&feed=8949198b-5c74-42af-9d30-e8c462acada6&package=9922dfd4-a522-4363-8f25-20e717f65596&preferRelease=true)

An implementation of the IFeatureFlag wrapper for the Launch Darkly Client.

## Usage

You can explicitly create the Launch Darkly Service with an SDK key as follows:

```csharp
var ldService = new LaunchDarklyService("Launch Darkly Key");
```

Or allow the key to be picked up automatically from config:

```csharp
IConfiguration config = configBuilder.Build();

// looks for the "LaunchDarklySdkKey" key within config.
var ldService = new LaunchDarklyService(config); 
```

Or you can use the service collection extension as follows:

```csharp

// With a key directly.
services.AddLaunchDarklyFeatureFlags("Launch Darkly Key");

// With an existing client.
services.AddLaunchDarklyFeatureFlags(new LdClient());

// Or allow it to pickup from config.
services.AddLaunchDarklyFeatureFlags();
```
This method will then take IConfiguration as an automatically resolved dependency and look for a config value of **"LaunchDarklySdkKey"** from the configuration.  You must ensure it exists!  It can be gathered from SecureVault.

_**NOTE:  If used in a web API, a 404 response will be returned.  This is to indicate the method or endpoint is not available when the flag is disabled.**_

## Attribute Usage

This can be used in the AppHost or in a WebHost.  Using the WebHost as an example, you may want an entire controller to be controlled via a feature flag.  That would be done as follows:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[FeatureFlag("MigrationsV2")] // ADDED HERE!
[SwaggerResponse(404, "MigrationsV2 feature flag is disabled.", Type = null)]
public class MappingFilesController : ControllerBase
{
    private readonly MigrationsContext _migrationsContext;
    private readonly ILogger<MappingFilesController> _logger;

    public MappingFilesController(MigrationsContext migrationsContext, ILogger<MappingFilesController> logger)
    {
        _migrationsContext = migrationsContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MappingFile>), 200)]
    [SwaggerResponse(200, "Retrieve all items", typeof(IEnumerable<MappingFile>))]
    public async Task<ActionResult<IEnumerable<MappingFile>>> Get() 
    {
        return await _migrationsContext.MappingFiles.ToListAsync();
    }
}
```

You can also opt to add at method level within the same controller:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[SwaggerResponse(404, "MigrationsV2 feature flag is disabled.", Type = null)]
public class MappingFilesController : ControllerBase
{
    private readonly MigrationsContext _migrationsContext;
    private readonly ILogger<MappingFilesController> _logger;

    public MappingFilesController(MigrationsContext migrationsContext, ILogger<MappingFilesController> logger)
    {
        _migrationsContext = migrationsContext;
        _logger = logger;
    }

    [FeatureFlag("MigrationsV2")] // ADDED HERE!
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MappingFile>), 200)]
    [SwaggerResponse(200, "Retrieve all items", typeof(IEnumerable<MappingFile>))]
    public async Task<ActionResult<IEnumerable<MappingFile>>> Get() 
    {
        return await _migrationsContext.MappingFiles.ToListAsync();
    }

    [FeatureFlag("MigrationsV3")] // ADDED HERE!
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IEnumerable<MappingFile>), 200)]
    [SwaggerResponse(200, "Retrieve specific item", typeof(MappingFile))]
    public async Task<ActionResult<MappingFile>> Get(int id) 
    {
        var item = await _migrationsContext.MappingFiles.FindAsync(x => x.id == id);
        if (item.IsNullOrDefault())
        {
            return NotFound();
        }
        
        return item;
    }
}
```

## Test Coverage
A threshold will be added to this package to ensure the test coverage is above 80% for branches, functions and lines.  If it's not above the required threshold 
(threshold that will be implemented on ALL of the core repositories to gurantee a satisfactory level of testing), then the build will fail.

## Compatibility
This package has has been written in .net Standard and can be therefore be referenced from a .net Core or .net Framework application. The advantage of utilising from a .net Core application, 
is that it can be deployed and run on a number of host operating systems, such as Windows, Linux or OSX.  Unlike referencing from the a .net Framework application, which can only run on 
Windows (or Linux using Mono).
 
## Setup
This package is built using .net Standard 2.1 and requires the .net Core 3.1 SDK, it can be downloaded here: 
https://www.microsoft.com/net/download/dotnet-core/

IDE of Visual Studio or Visual Studio Code, can be downloaded here:
https://visualstudio.microsoft.com/downloads/

## How to access this package
All of the Cloud.Core.* packages are published to a internal NuGet feed.  To consume this on your local development machine, please add the following feed to your feed sources in Visual Studio:
https://pkgs.dev.azure.com/cloudcoreproject/CloudCore/_packaging/Cloud.Core/nuget/v3/index.json
 
For help setting up, follow this article: https://docs.microsoft.com/en-us/vsts/package/nuget/consume?view=vsts


<img src="https://cloud1core.blob.core.windows.net/icons/cloud_core_small.PNG" />
