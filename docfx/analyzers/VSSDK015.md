# VSSDK015 Avoid unsupported reflection on Visual Studio APIs

Reflection can load Visual Studio APIs outside the SDK packages referenced by your extension or access non-public SDK members. These implementation details may change between Visual Studio versions.

This analyzer reports:

- Constant strings passed to `Type.GetType`, `Assembly.GetType`, and `Assembly.Load*` that name Visual Studio types or assemblies outside your referenced SDK package surface. Generic type arguments are checked too, even when the containing type is supported or is not a Visual Studio type. Unqualified type names are checked only when they can be resolved against your project's references.
- `System.Type` member lookups directly on `typeof` an SDK-surface type when a constant `BindingFlags` argument includes `NonPublic`. This includes method, constructor, property, field, event, nested-type, and member searches, plus `FindMembers` and `InvokeMember`.

Public-only lookups, including overloads without `BindingFlags`, do not produce the non-public-member warning. Looking up members on an array type does not look up members on its element type.

Use public APIs included in your referenced SDK packages instead. When the SDK packages do not provide surface information, the analyzer skips these checks without a warning. [VSSDK013](VSSDK013.md) covers direct API references separately.

VSSDK015 is a warning and has no automatic code fix.
