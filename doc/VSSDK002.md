# VSSDK002 PackageRegistration matches Package

The `PackageRegistrationAttribute.AllowsBackgroundLoading` parameter on your VS package class
should indicate whether your class derives from `AsyncPackage`.

## Examples of patterns that are flagged by this analyzer

```csharp
[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : AsyncPackage
{
}
```

or

```csharp
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : Package
{
}
```

## Solution

Update the PackageRegistration attribute to match the base type of your package class.
Specifically, the `AllowsBackgroundLoading` parameter should be set to `true`
if and only if your package derives from `AsyncPackage`.

```csharp
[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package
{
}
```

or

```csharp
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : AsyncPackage
{
}
```

A code fix is offered for this diagnostic to automatically apply this change.
