# VSSDK015 Use reflection with APIs in the Visual Studio SDK surface

Reflection strings can load Visual Studio assemblies and types outside the SDK packages referenced by an extension. Those APIs can change independently of the SDK and may cause compatibility problems.

This analyzer checks constant strings passed to `Type.GetType`, `Assembly.GetType`, and `Assembly.Load*`. For type names, it checks the assembly named in an assembly-qualified name or resolves an unqualified type name against the compilation references. For assembly loads, it checks the assembly name against the package surface list.

Use a type or assembly included in the referenced SDK package surface. VSSDK015 is a warning and has no automatic code fix.
