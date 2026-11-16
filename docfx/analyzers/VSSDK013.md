# VSSDK013 Use APIs in the Visual Studio SDK surface

Visual Studio APIs outside the packages referenced by an extension can change independently of the SDK and may cause compatibility problems.

This analyzer reports references to `Microsoft.VisualStudio.*` APIs whose assembly is not included in the surface list provided by the referenced SDK packages. The SDK package supplies the list as an `AdditionalFile` tagged with `VsSdkSurface` metadata.

The list contains one assembly simple name per line. Blank lines and lines beginning with `#` are ignored. When the analyzer receives no non-empty tagged list, it skips surface checks and reports VSSDK014 only if the compiler-visible MSBuild property `VsSdkSurfaceExpected` is `true`. Supplied tagged lists are analyzed regardless of that property.

Use an API included in the referenced SDK package surface. VSSDK013 is a warning and has no automatic code fix.
