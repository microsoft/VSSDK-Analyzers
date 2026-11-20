# VSSDK013 Avoid APIs outside the Visual Studio SDK surface

Visual Studio APIs outside the packages referenced by an extension can change independently of the SDK and may cause compatibility problems.

This analyzer reports direct references to `Microsoft.VisualStudio.*` APIs whose assembly is not part of the SDK package surface referenced by your project.

Use a supported API from your referenced SDK packages instead. If the API belongs to another supported SDK package, add a reference to that package. Do not reference implementation assemblies from the Visual Studio installation to bypass this warning.

The SDK packages provide the information used by this check. When that information is unavailable, the analyzer skips these checks without a warning. [VSSDK015](VSSDK015.md) covers unsupported reflection lookups separately.

VSSDK013 is a warning and has no automatic code fix.
