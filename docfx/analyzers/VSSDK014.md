# VSSDK014 Provide the Visual Studio SDK surface

The Visual Studio SDK compatibility analyzers need a surface list from the SDK package. The package supplies this as an `AdditionalFile` with `VsSdkSurface` metadata.

This warning appears once when no tagged file contains assembly names. References and reflection checks are skipped until a non-empty surface list is provided.
