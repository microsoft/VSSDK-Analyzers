# VSSDK010 Remove unnecessary `ProvideAutoLoad` attribute

Automatically loading a package has a performance cost. A package that only registers services, code expansions,
or other declarative contributions does not need to load unless it overrides `Package.Initialize`,
`AsyncPackage.InitializeAsync`, or `AsyncPackage.OnAfterPackageLoadedAsync`.

This analyzer reports each `ProvideAutoLoad` attribute on a package when neither the package nor an intermediate
base class overrides an applicable initialization method.

## Example of a pattern that is flagged by this analyzer

```csharp
using Microsoft.VisualStudio.Shell;

[ProvideAutoLoad("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", PackageAutoLoadFlags.BackgroundLoad)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyPackage : AsyncPackage
{
}
```

## Solution

Remove the `ProvideAutoLoad` attribute. Visual Studio can consume the package's declarative registrations without
loading the package.

```csharp
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyPackage : AsyncPackage
{
}
```

If the package needs to run initialization code, override `Initialize` or `InitializeAsync` as appropriate instead
of removing `ProvideAutoLoad`. An `AsyncPackage` may also override `OnAfterPackageLoadedAsync` for operations with
side effects that should run soon after package loading rather than as a strict part of initialization.
