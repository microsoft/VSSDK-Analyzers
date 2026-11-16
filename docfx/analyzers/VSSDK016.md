# VSSDK016 Avoid reflection-based member lookup on Visual Studio SDK types

Reflection-based member lookup can depend on member names and non-public implementation details that may change between SDK versions.

This analyzer reports `System.Type` member lookups by name and lookups using `BindingFlags.NonPublic` when the `Type` came directly from `typeof` on an SDK-surface type.

Use supported SDK APIs instead. VSSDK016 is a warning and has no automatic code fix.
