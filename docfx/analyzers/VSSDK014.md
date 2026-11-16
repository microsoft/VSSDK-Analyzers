# VSSDK014 Provide the Visual Studio SDK surface

The Visual Studio SDK compatibility analyzers need a surface list from the SDK package. The package supplies this as an `AdditionalFile` with `VsSdkSurface` metadata.

SDK packages opt in to this warning by setting the MSBuild property `VsSdkSurfaceExpected` to `true` and exposing it through a `CompilerVisibleProperty` item.

This warning appears once when the property is `true` and no tagged file contains assembly names. References and reflection checks are skipped until a non-empty surface list is provided. Analyzer-only projects without a surface do not receive this warning unless they opt in. Supplied tagged lists are analyzed regardless of the property.
