# VSSDK012 Use the latest `VisualStudioServices` version

`Microsoft.VisualStudio.VisualStudioServices` exposes versioned service catalogs. Newer catalogs include the services from earlier versions and may provide additional functionality or performance improvements.

This analyzer reports a reference to an older catalog when the referenced Visual Studio SDK exposes a higher-numbered version.

## Example of a pattern that is flagged by this analyzer

```csharp
var service = VisualStudioServices.VS2019_7.DiagnosticManagerService;
```

## Solution

Use the latest version available from the referenced Visual Studio SDK.

```csharp
var service = VisualStudioServices.VS2022_14.DiagnosticManagerService;
```

Because changing a service version may change behavior, this diagnostic has `Info` severity and does not offer an automatic code fix.
