# VSSDK016 Avoid reflection on non-public Visual Studio SDK members

Non-public implementation details may change between SDK versions.

This analyzer reports `System.Type` member lookups when the `Type` came directly from `typeof` on an SDK-surface type and a constant `BindingFlags` argument includes `NonPublic`. Public-only lookups, including overloads without `BindingFlags`, do not produce this warning.

Use supported SDK APIs instead. VSSDK016 is a warning and has no automatic code fix.
