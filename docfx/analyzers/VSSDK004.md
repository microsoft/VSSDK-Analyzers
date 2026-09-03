# VSSDK004 Use BackgroundLoad flag in ProvideAutoLoad attribute for asynchronous auto load

In order to provide performance guarantees in critical scenarios such as startup and solution load,
Visual Studio will deprecate synchronous auto load requests in a future version.

This analyzer flags all use of `ProvideAutoLoad` attributes that doesn't provide a `BackgroundLoad` flag or
a `SkipWhenUIContextRulesActive` flag where latter tells Visual Studio versions supporting AsyncPackage to
ignore auto load request.

## Examples of patterns that are flagged by this analyzer

```csharp
[ProvideAutoLoad("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}")]
[PackageRegistration(UseManagedResourcesOnly = true)]
class MyCoolPackage : Package
{
    protected override void Initialize()
    {
        base.Initialize();
    }
}
```

## Solution

Provide BackgroundLoad flag in your ProvideAutoLoad and also derive from AsyncPackage as needed.

```csharp
[ProvideAutoLoad("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", PackageAutoLoadFlags.BackgroundLoad)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
class MyCoolPackage : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress);

        // When initialized asynchronously, we *may* be on a background thread at this point.
        // Do any initialization that requires the UI thread after switching to the UI thread.
        // Otherwise, remove the switch to the UI thread if you don't need it.
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }
}
```

For more information, please see [How to use AsyncPackage to load VS packages in the background](https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-use-asyncpackage-to-load-vspackages-in-the-background)
